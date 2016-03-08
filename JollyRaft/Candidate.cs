using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JollyRaft
{
    public partial class Node
    {
        public class Candidate : IRequestHandler
        {
            private readonly Node node;

            public Candidate(Node node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }
                this.node = node;
            }

            public async Task<AppendEntriesResult> AppendEntries(AppendEntriesRequest request)
            {
                Debug.WriteLine("{0}: GOT APPEND ENTRIES CALL {1}: leader commit {2} : entries: {3}", node.NodeInfo(), request.Id, request.CommitIndex, String.Concat(request.Entries.Select(e => e.Log + " ")));

                if (request.Term < node.Term)
                {
                    return new AppendEntriesResult(node.Term, false);
                }

                node.CurrentLeader = request.Id;
                node.requestHandler = new Follower(node);
                node.Term = request.Term;
                node.lastHeartBeat = node.settings.Scheduler.Now;
                return await node.requestHandler.AppendEntries(request);
            }

            public async Task<VoteResult> Vote(VoteRequest request)
            {
                if (request.Term < node.Term)
                {
                    return new VoteResult(node.Term, false);
                }
                //this needs some sort of lock around it. although it would be very rare in a deployed system, the nodes in unit tests have hit
                //this block at the exact same time, resulting in both nodes getting the vote.
                //using a lock for now. should be replaced at some point with interlocked for perf
                lock (node.grantVoteLocker)
                {
                    if (request.Term > node.Term)
                    {
                        node.StepDown(node.Term, request.Id);
                    }

                    return new VoteResult(node.Term, false);
                }
            }

            public async Task<LogResult> AddLog(string log)
            {
                return new LogRejected
                       {
                           LeaderId = node.CurrentLeader
                       };
            }

            public async Task SendHeartBeat()
            {
            }

            public async Task StartElection()
            {
                var now = node.settings.Scheduler.Now;
                if (node.lastHeartBeat <= now - node.electionTimeout)
                {
                    lock (node.grantVoteLocker)
                    {
                        node.requestHandler = new Candidate(node);
                        Debug.WriteLine("{0}: Starting a new Election. current term {1}. election Timout {2}ms", node.NodeInfo(), node.Term, node.electionTimeout.TotalMilliseconds);
                        node.Term++;

                        node.CurrentLeader = node.Id;
                    }
                    var successCount = 0;

                    var blah = node.Peers.Select(async p =>
                                                       {
                                                           var vote = await p.RequestVote(new VoteRequest(node.Id, node.Term, node.LocalLog.LastTerm, node.LocalLog.LastIndex));
                                                           if (vote.VoteGranted)
                                                           {
                                                               Debug.WriteLine("{0}: Vote granted from {1}", node.NodeInfo(), p.Id);
                                                               Interlocked.Increment(ref successCount);
                                                           }
                                                           else
                                                           {
                                                               if (vote.CurrentTerm > node.Term)
                                                               {
                                                                   Debug.WriteLine("{0}: stepping down as vote returned had higher term {1}", node.NodeInfo(), vote.CurrentTerm);
                                                                   node.StepDown(vote.CurrentTerm, p.Id);
                                                               }
                                                           }
                                                       });

                    foreach (var task in blah)
                    {
                        await task;
                    }

                    if (node.ConcencusIsReached(successCount) && node.State == State.Candidate)
                    {
                        node.requestHandler = new Leader(node);
                        Debug.WriteLine("{0}: Elected as Leader. votes {1}. needed {2}", node.NodeInfo(), successCount, node.PeerAgreementsNeededForConcensus());
                        await SendHeartBeat();
                    }
                    else
                    {
                        Debug.WriteLine("{0}: Not elected. votes {1}. needed {2}", node.NodeInfo(), successCount, node.PeerAgreementsNeededForConcensus());
                        node.randomizeElectionDelays = true;
                    }
                }
                else
                {
                    Debug.WriteLine("{0}: election was attempted before election timeout. lastHeartBeat {1} < now {2} - electionTimeout {3}", node.NodeInfo(), node.lastHeartBeat, now, node.electionTimeout.TotalSeconds);
                    Debug.WriteLine("{0}: need {1}ms more.", node.NodeInfo(), ((now - node.electionTimeout) - node.lastHeartBeat).TotalMilliseconds, now, node.electionTimeout.TotalSeconds);
                }
            }
        }
    }
}