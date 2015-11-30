using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class Given_a_leader_that_has_been_elected
    {
        [SetUp]
        public void SetUp()
        {
            testScheduler = new TestScheduler();
            testScheduler.AdvanceTo(DateTimeOffset.UtcNow.Ticks);
            nodes = TestNode.CreateCluster(scheduler: testScheduler);
            nodes.Start();
            leader = nodes.WaitForLeader(testScheduler).Result;
        }

        private List<Node> nodes;
        private Node leader;
        private TestScheduler testScheduler;

        [Test]
        public async void after_the_leader_commits_to_the_server_log_the_followers_commit_to_their_server_log()
        {
            await leader.AddLog("first command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout.Ticks);
            nodes.Where(n => n != leader).ForEach(n => n.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command")));
        }

        [Test]
        public async void when_a_follower_recieves_a_command_it_rejects_it_and_directs_towards_the_leader()
        {
            var result = await nodes.First(n => n.State != State.Leader).AddLog("first command");
            result.Should().BeOfType<LogRejected>();
        }

        [Test]
        public async void when_concencus_is_reached_the_leader_commits_the_command_to_the_server_long()
        {
            await leader.AddLog("first command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout.Ticks);
            leader.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command"));
        }

        [Test]
        public async void when_the_leader_recieves_a_command_it_immediately_appends_that_command_to_its_local_log_entries()
        {
            await leader.AddLog("first command");
            leader.LocalLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command"));
        }

        [Test]
        public async void when_the_leader_recieves_a_command_it_immediately_sends_that_command_to_its_followers()
        {
            await leader.AddLog("first command");
            nodes.Where(n => n != leader).ForEach(n => n.LocalLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command")));
        }

        [Test]
        public async void when_the_leader_recieves_multiple_command_it_replicates_them_to_its_followers()
        {
            await leader.AddLog("first command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout.Ticks);
            nodes.ForEach(n => n.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(2, 2, "first command")));

            await leader.AddLog("second command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout.Ticks);
            nodes.ForEach(n => n.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(3, 2, "second command")));

            await leader.AddLog("third command");
            testScheduler.AdvanceBy(TestNode.HeartBeatTimeout.Ticks);
            nodes.ForEach(n => n.ServerLog.Entries.Last().ShouldBeEquivalentTo(new LogEntry(4, 2, "third command")));
        }
    }
}