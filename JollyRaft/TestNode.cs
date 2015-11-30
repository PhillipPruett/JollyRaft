using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace JollyRaft
{
    public class TestNode : Node
    {
        public static TimeSpan ElectionTimeout = TimeSpan.FromSeconds(1);
        public static TimeSpan HeartBeatTimeout = TimeSpan.FromMilliseconds(100);
        public static TimeSpan MaxElectionTimeout = new TimeSpan((long) (ElectionTimeout.Ticks*1.1));
        public bool GrantVotes = true;
        public bool SleepOnAppendEntries = false;

        public TestNode(NodeSettings nodeSettings) : base(nodeSettings)
        {
        }

        public override async Task<AppendEntriesResult> AppendEntries(AppendEntriesRequest request)
        {
            if (SleepOnAppendEntries)
            {
                await Task.Delay(Timeout.Infinite);
            }
            return await base.AppendEntries(request);
        }

        public override Task<VoteResult> Vote(VoteRequest request)
        {
            if (!GrantVotes)
            {
                return Task.FromResult(new VoteResult(Term, false));
            }
            return base.Vote(request);
        }

        public static List<Node> CreateCluster(int clusterSize = 3, Subject<IEnumerable<Peer>> peerObservable = null, IScheduler scheduler = null)
        {
            peerObservable = peerObservable ?? new Subject<IEnumerable<Peer>>();
            scheduler = scheduler ?? Scheduler.Default;

            var nodes = new List<Node>();

            for (int i = 1; i <= clusterSize; i++)
            {
                nodes.Add(new TestNode(new NodeSettings("node" + i, ElectionTimeout, HeartBeatTimeout, peerObservable, scheduler)));
            }
            peerObservable.OnNext(nodes.Select(n => n.AsPeer()));

            return nodes;
        }

        public static TestNode CreateTestNode(string nodeId, Subject<IEnumerable<Peer>> peerObservable = null, IScheduler scheduler = null)
        {
            peerObservable = peerObservable ?? new Subject<IEnumerable<Peer>>();
            scheduler = scheduler ?? Scheduler.Default;

            return new TestNode(new NodeSettings(nodeId, ElectionTimeout, HeartBeatTimeout, peerObservable, scheduler));
        }
    }
}