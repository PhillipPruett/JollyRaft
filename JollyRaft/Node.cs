using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace JollyRaft
{
    public partial class Node : IDisposable
    {
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly object grantVoteLocker = new object();
        private readonly NodeSettings settings;
        private int CommitIndex; // starts as 0
        private TimeSpan electionTimeout;
        private DateTimeOffset lastHeartBeat = DateTimeOffset.MinValue;
        private IEnumerable<Peer> Peers = new Peer[0];
        private bool started;
        private bool randomizeElectionDelays = false;
        private IRequestHandler requestHandler;

        public Node(NodeSettings nodeSettings)
        {
            if (nodeSettings == null)
            {
                throw new ArgumentNullException("nodeSettings");
            }

            settings = nodeSettings;
            requestHandler = new Follower(this);
            electionTimeout = nodeSettings.ElectionTimeout;
            Id = settings.NodeId;
            LocalLog = new Log();
            ServerLog = new Log();
            Term = 1;
   

            disposables.Add(nodeSettings.PeerObserver
                                        .Subscribe(Observer.Create<IEnumerable<Peer>>(peers =>
                                                                                      {
                                                                                          Peers = peers.Where(p => p.Id != Id);
                                                                                          //Peers.ForEach(p => Debug.WriteLine("{0} new peer discovered: {1}", NodeInfo(), p.Id));
                                                                                      },
                                                                                      ex => Debug.WriteLine("OnError: {0}", ex.Message),
                                                                                      () => Debug.WriteLine("OnCompleted"))));

        }

        public Log LocalLog { get; private set; }
        public Log ServerLog { get; private set; }
        public int Term { get; private set; }
        public string Id { get; private set; }

        public State State
        {
            get
            {
                return (requestHandler is Follower)
                           ? State.Follower
                           : (requestHandler is Candidate)
                                 ? State.Candidate
                                 : State.Leader;
            }
        }

        public string CurrentLeader { get; private set; }

        public void Dispose()
        {
            disposables.Dispose();
        }

        public void Start()
        {
            if (started)
            {
                return;
            }

            var timeSpan = new TimeSpan((long)(settings.ElectionTimeout.Ticks * .5)).Randomize(90);
            //Election Holder
            electionTimeout = settings.ElectionTimeout.Randomize(10);
            disposables.Add(Observable.Interval(electionTimeout, settings.Scheduler)
                                      .Subscribe(async o =>
                                                       {
                                                           if (randomizeElectionDelays)
                                                           {
                                                               await Task.Delay(timeSpan);
                                                           }
                                                           await StartElection();
                                                       },
                                                 e => Debug.WriteLine(e.Message)));

            //Heartbeat pumper
            disposables.Add(Observable.Interval(settings.HeartBeatTimeout, settings.Scheduler)
                                      .Subscribe(async o => { await SendHeartBeat(); },
                                                 e => Debug.WriteLine(e.Message)));

            started = true;
        }

        public virtual async Task StartElection()
        {
            if (!Peers.Any())
            {
                return;
            }

            await requestHandler.StartElection();
        }

        private string NodeInfo()
        {
            return string.Format("{0}.{1}", Id, requestHandler.GetType().Name);
        }



        private bool ConcencusIsReached(int amountOfAgreements)
        {
            Debug.WriteLine("{0}: checking concencus: amountOfAgreements({1}) >= PeerAgreementsNeededForConcensus({2})", NodeInfo(), amountOfAgreements, PeerAgreementsNeededForConcensus());
            return amountOfAgreements >= PeerAgreementsNeededForConcensus();
        }

        private int PeerAgreementsNeededForConcensus()
        {
            return (Peers.Count() + 1)/2;
        }

        public virtual async Task<VoteResult> Vote(VoteRequest request)
        {
            return await requestHandler.Vote(request);
        }

        public async Task<LogResult> AddLog(string log)
        {
            return await requestHandler.AddLog(log);
        }

        public async Task SendHeartBeat()
        {
            await requestHandler.SendHeartBeat();
        }

        private async Task CommitToServerLog(string log)
        {
            if (log == null)
            {
                return;
            }
            Debug.WriteLine("{0}: Commiting to server log {1}", NodeInfo(), log);
            ServerLog.Add(Term, log);
            CommitIndex = ServerLog.LastIndex;
            await SendHeartBeat();
        }

        private async Task<AppendEntriesResult> AppendAllTheEntries(bool heartBeat, Peer peer)
        {
            Debug.WriteLine("{0}: Appending all the entires! {1}", NodeInfo(), peer.Id);
            var result = await peer.AppendEntries(new AppendEntriesRequest(Id,
                                                                           Term,
                                                                           LocalLog.PreviousLogIndex,
                                                                           LocalLog.PreviousLogTerm,
                                                                           heartBeat
                                                                               ? LocalLog.Entries.Where(e => e.Index > peer.CommitIndex).ToArray()
                                                                               : LocalLog.Entries.ToArray(),
                                                                           CommitIndex));
            Debug.WriteLine("{0}: Adding AppendEntry result from {1} = {2}", NodeInfo(), peer.Id, result.Success);
            if (result.Success)
            {
                peer.CommitIndex = CommitIndex;
            }
            return result;
        }

        public virtual async Task<LogResult> SendAppendEntries(string log, bool heartBeat)
        {
            if (!heartBeat)
            {
                LocalLog.Add(Term, log);
            }

            var successCount = 0;

            Peers.ForEach(async peer =>
                                {
                                    Debug.WriteLine("{0}: sending append entries to {1}", NodeInfo(), peer.Id);
                                    var result = (await AppendAllTheEntries(heartBeat, peer)
                                                            .ToObservable()
                                                            .Timeout(TimeSpan.FromSeconds(5))
                                                            .Catch<AppendEntriesResult, TimeoutException>(e => Observable.Return(new AppendEntriesResult(0, false))));

                                    if (result.CurrentTerm > Term)
                                    {
                                        StepDown(result.CurrentTerm, peer.Id);
                                    }

                                    if (result.Success)
                                    {
                                        Interlocked.Increment(ref successCount);
                                    }
                                    Debug.WriteLine("{0}: Got a result {1}", NodeInfo(), peer.Id);
                                });

            if (successCount >= PeerAgreementsNeededForConcensus())
            {
                await CommitToServerLog(log);
            }

            return new LogCommited
                   {
                       ServerLog = ServerLog
                   };
        }

        public virtual async Task<AppendEntriesResult> AppendEntries(AppendEntriesRequest request)
        {
            return await requestHandler.AppendEntries(request);
        }

        public void StepDown(int newTerm, string idOfNodeWithHigherTerm)
        {
            Debug.WriteLine("{0}: Stepping down due to Node {1} having a higher term. new term: {2} old term: {3} ",
                            Id,
                            idOfNodeWithHigherTerm,
                            newTerm,
                            Term);
            requestHandler = new Follower(this);
            Term = newTerm;
            CurrentLeader = null;
            lastHeartBeat = settings.Scheduler.Now;
        }

        public IEnumerable<string> GetPeerIds()
        {
            return Peers.Select(p => p.Id);
        }

        public Peer AsPeer()
        {
            return new Peer(Id, Vote, AppendEntries);
        }
    }
}