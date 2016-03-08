using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace JollyRaft.Tests
{
    public static class TestExtensions
    {
        public static async Task<Node> WaitForLeader(this IEnumerable<Node> nodes, TestScheduler testScheduler = null)
        {
            while (nodes.All(n => n.State != State.Leader))
            {
                if (testScheduler != null)
                {
                    testScheduler.AdvanceBy(TestNode.MaxElectionTimeout.Ticks);
                }
                else
                {
                    await Task.Delay(TestNode.MaxElectionTimeout);
                }
            }

            return nodes.Single(n => n.State == State.Leader);
        }

        public static  void LogStatus(this IEnumerable<Node> nodes)
        {
            nodes.ForEach(n => Console.WriteLine(string.Format("{0} - {1} - term {2}", n.Id, n.State, n.Term)));
        }

        public static Node Leader(this IEnumerable<Node> nodes)
        {
            return nodes.SingleOrDefault(n => n.State == State.Leader);
        }

        public static IEnumerable<Node> Followers(this IEnumerable<Node> nodes)
        {
            return nodes.Where(n => n.State == State.Follower);
        }

        public static IDisposable StartRandomlyCrashing(this Node[] nodes, Subject<IEnumerable<Peer>> peerObservable, Random random, IScheduler scheduler = null)
        {
            return Observable.Interval(new TimeSpan(TestNode.ElectionTimeout.Ticks*7), scheduler ?? Scheduler.Default)
                             .Subscribe(o =>
                                        {
                                            nodes.Start();
                                            var nodeIndexToRemove = random.Next(0, nodes.Count());
                                            Console.WriteLine(string.Format("Removing Node at index {0}", nodeIndexToRemove));
                                            var id = nodes[nodeIndexToRemove].Id;
                                            nodes[nodeIndexToRemove] = new TestNode(new NodeSettings(id, TestNode.ElectionTimeout, TestNode.HeartBeatTimeout, peerObservable, scheduler));
                                            peerObservable.OnNext(nodes.Where(n => n.Id != id).Select(n => n.AsPeer()));
                                        },
                                        e => Debug.WriteLine(e.Message));
        }
    }
}