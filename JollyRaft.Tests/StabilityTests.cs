using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Xml.Schema;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class StabilityTests
    {
        private List<LogEntry> lastKnownLeaderEntries;

        [Test]
        [Ignore("test needs to be thought out more")]
        public async Task when_a_cluster_has_nodes_that_crash_very_often_then_logs_are_still_persisted()
        {
            Subject<IEnumerable<Peer>> peerObservable = new Subject<IEnumerable<Peer>>();
            var cluster = TestNode.CreateCluster(clusterSize: 20, peerObservable: peerObservable);
            cluster.Start();
            await cluster.WaitForLeader();

            var crasher = cluster.StartRandomlyCrashing(peerObservable, new Random());

            for (int i = 0; i < 30; i++)
            {
                Console.WriteLine(
                    i + " " + string.Join(", ", cluster.Where(n => n.State == State.Leader).Select(n => n.Id)));

                await Task.Delay(TestNode.ElectionTimeout);
            }
            await cluster.WaitForLeader();
            cluster.Should().Contain(n => n.State == State.Leader);
        }

        [Test]
        public async Task logs_can_be_deterministicly_written_to_a_leader()
        {
            var scheduler = Scheduler.Default;
            var cluster = TestNode.CreateCluster(clusterSize: 2, scheduler: scheduler);
            cluster.Start();
            await cluster.WaitForLeader();
            var expectedServerLog = new Log();

            Enumerable.Range(1,100).ForEach(async i =>
                                            {
                                                await cluster.Leader().AddLog(i.ToString());
                                                expectedServerLog.Add(cluster.Leader().Term, i.ToString());
                                            });

            cluster.Leader().LocalLog.ShouldBeEquivalentTo(expectedServerLog);
            cluster.Leader().ServerLog.ShouldBeEquivalentTo(expectedServerLog);
        }

        [Test]
        public async Task logs_can_be_quickly_written_in_parallel_to_a_leader()
        {
            var scheduler = Scheduler.Default;
            var cluster = TestNode.CreateCluster(clusterSize: 4, scheduler: scheduler);
            cluster.Start();
            await cluster.WaitForLeader();
            var expectedServerLog = new Log();

            var result = Enumerable.Range(1, 100).ParrallelForEach( i =>
            {
                cluster.Leader().AddLog(i.ToString()).Wait();
                expectedServerLog.Add(2, i.ToString());
            });

            cluster.Leader().LocalLog.Entries.Count.Should().Be(101);
            cluster.Leader().ServerLog.Entries.Count.Should().Be(101);

            cluster.Followers().ForEach(f =>
                                        {
                                            Enumerable.Range(1, 100).ForEach(i =>
                                                                             {
                                                                                 var s = i.ToString();
                                                                                 f.LocalLog.Entries.Select(e => e.Log).Should().Contain(s);
                                                                             });
                                        });
        }   
        
        [Test]
        public async Task logs_can_be_quickly_written_in_parallel_to_a_leader_with_test_scheduler()
        {
            var scheduler = new VirtualScheduler();
            var cluster = TestNode.CreateCluster(clusterSize: 4, scheduler: scheduler);
            cluster.Start();
            await cluster.WaitForLeader(scheduler);
            var expectedServerLog = new Log();

            var result = Enumerable.Range(1, 100).ParrallelForEach( i =>
            {
                cluster.Leader().AddLog(i.ToString()).Wait();
                expectedServerLog.Add(cluster.Leader().Term, i.ToString());
            });

            scheduler.AdvanceBy(TimeSpan.FromSeconds(10));
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10));

            cluster.Leader().LocalLog.Entries.Count.Should().Be(101);
            cluster.Leader().ServerLog.Entries.Count.Should().Be(101);

            cluster.Followers().ForEach(f =>
                                        {
                                            Enumerable.Range(1, 100).ForEach(i =>
                                                                             {
                                                                                 var s = i.ToString();
                                                                                 f.LocalLog.Entries.Select(e => e.Log).Should().Contain(s);
                                                                             });
                                        });
        }

        [Test]
        public async Task clusters_remain_in_a_valid_state_throughout_logging()
        {
            var scheduler = new VirtualScheduler();
            var cluster = TestNode.CreateCluster(clusterSize: 2, scheduler: scheduler);
            cluster.Start();
            await cluster.WaitForLeader(scheduler);
            var expectedServerLog = new Log();

            var validationCounter = 0;

            Observable.Interval(TimeSpan.FromSeconds(1), scheduler)
                      .Subscribe(async o =>
                                       {
                                           Validate(cluster);
                                           validationCounter ++;
                                       },
                                 e => Debug.WriteLine(e.Message));

            Enumerable.Range(1, 100).ForEach(async i =>
            {
                await cluster.Leader().AddLog(i.ToString());
                expectedServerLog.Add(cluster.Leader().Term, i.ToString());
                scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
            });

          
            Console.WriteLine("Validation ran {0} times on cluster", validationCounter);
        }

        private void Validate(List<Node> cluster)
        {
            //From Raft Paper:
            //Election Safety: at most one leader can be elected in a
            //given term. §5.2

            cluster.Should().ContainSingle(n => n.State == State.Leader);

            //Leader Append-Only: a leader never overwrites or deletes
            //entries in its log; it only appends new entries. §5.3

            //Leader Completeness: if a log entry is committed in a
            //given term, then that entry will be present in the logs
            //of the leaders for all higher-numbered terms. §5.4

            var leader = cluster.Leader();
            if (lastKnownLeaderEntries == null)
            {
                lastKnownLeaderEntries = leader.ServerLog.Entries;
            }
            leader.ServerLog.Entries.Take(lastKnownLeaderEntries.Count).ShouldAllBeEquivalentTo(lastKnownLeaderEntries);
            lastKnownLeaderEntries = leader.ServerLog.Entries;

            //Log Matching: if two logs contain an entry with the same
            //index and term, then the logs are identical in all entries
            //up through the given index. §5.3

            cluster.ForEach(n =>
                            {
                                var lastEntry = n.ServerLog.Entries.Last();
                                cluster.ForEach(nn =>
                                                {
                                                    if (nn.ServerLog.Entries.Contains(lastEntry))
                                                    {
                                                        n.ServerLog.Entries.Take(lastEntry.Index).ShouldBeEquivalentTo(nn.ServerLog.Entries.Take(lastEntry.Index));
                                                    }
                                                });
                            });

      
            //State Machine Safety: if a server has applied a log entry
            //at a given index to its state machine, no other server
            //will ever apply a different log entry for the same index.
            //§5.4.3

            cluster.ForEach(n =>
            {
                cluster.ForEach(nn =>
                                {
                                    n.ServerLog.Entries.ForEach(e =>
                                                                {
                                                                    var entryToCompare = nn.ServerLog.Entries.SingleOrDefault(ee => ee.Index == e.Index);
                                                                    if (entryToCompare != null)
                                                                    {
                                                                        e.Log.Should().Be(entryToCompare.Log);
                                                                    }
                                                                });
                                });
            });
        }
    }
}