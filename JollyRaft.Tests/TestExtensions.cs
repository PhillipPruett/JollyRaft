using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public static IDisposable StartRandomlyCrashing(this IEnumerable<Node> nodes, Subject<IEnumerable<Peer>> peerObservable, Random random)
        {
            return Observable.Interval(TestNode.ElectionTimeout)
                             .Subscribe(o =>
                                        {
                                            var nodeIndexToRemove = random.Next(0, nodes.Count());
                                            var nodesWithMissingRandomNode  = nodes.Where((n, i) =>
                                                                                          {
                                                                                              if (i == nodeIndexToRemove)
                                                                                              {
                                                                                                  Console.WriteLine("Removing Node " + n.Id);
                                                                                              }
                                                                                              return i != nodeIndexToRemove;
                                                                                          });
                                            peerObservable.OnNext(nodesWithMissingRandomNode.Select(n => n.AsPeer()));
                                        },
                                        e => Debug.WriteLine(e.Message));
        }
    }
}