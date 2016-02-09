using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class StabilityTests
    {
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

    }
}