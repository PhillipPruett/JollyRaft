using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JollyRaft
{
    public partial class Node
    {
        public class Leader : IRequestHandler
        {
            private readonly Node node;

            public Leader(Node node)
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
                if (request.Term <= node.Term)
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
                Debug.WriteLine("{0}: Adding Log {1}", node.NodeInfo(), log);
                return await node.SendAppendEntries(log, false);
            }

            public async Task SendHeartBeat()
            {
                Debug.WriteLine(string.Format("{0}: Sending HearthBeat", node.NodeInfo()));
                await node.SendAppendEntries(null, true);
            }

            public async Task StartElection()
            {
                
            }
        }
    }
}