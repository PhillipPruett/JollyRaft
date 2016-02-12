using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class Given_a_cluster_with_a_leader_and_one_node_that_has_gone_dark
    {
        [SetUp]
        public void SetUp()
        {
            testScheduler = new VirtualScheduler();
            nodes = TestNode.CreateCluster(scheduler: testScheduler);
            nodes.Start();
            leader = nodes.WaitForLeader(testScheduler).Result;

            darkNode = nodes.First(n => n != leader) as TestNode;
            darkNode.SleepOnAppendEntries = true;
        }

        private List<Node> nodes;
        private Node leader;
        private TestNode darkNode;
        private VirtualScheduler testScheduler;

        [Test]
        public async void when_a_command_is_sent_the_consensus_can_still_be_reached()
        {
            await leader.AddLog("first command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout);
            leader.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command"));
        }

        [Test]
        public async void when_the_dark_node_comes_back_online_it_is_updated_by_the_leader()
        {
            await leader.AddLog("first command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout);
            leader.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command"));

            darkNode.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(1, 1, null));
            darkNode.SleepOnAppendEntries = false;

            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout);
            darkNode.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command"));
        }
    }
}