using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class Given_a_node_that_has_just_started
    {
        [Test]
        public async Task after_an_election_timeout_a_leader_is_elected_or_candidates_are_running()
        {
            var nodes = TestNode.CreateCluster();
            nodes.ForEach(n => n.Start());

            await Task.Delay(TestNode.MaxElectionTimeout);
            nodes.Should().Contain(n => n.State == State.Leader || n.State == State.Candidate,
                                   "Election timeouts can be inflated by a max of 10%. after that time a leader " +
                                   "should be elected.");
        }

        [Test]
        public async Task after_several_election_timeouts_only_a_single_leader_is_in_office()
        {
            var nodes = TestNode.CreateCluster();
            nodes.ForEach(n => n.Start());
            await nodes.WaitForLeader();
            nodes.Should().ContainSingle(n => n.State == State.Leader);
            await Task.Delay(TestNode.MaxElectionTimeout);
            nodes.Should().ContainSingle(n => n.State == State.Leader);
            await Task.Delay(TestNode.MaxElectionTimeout);
            nodes.Should().ContainSingle(n => n.State == State.Leader);
        }

        [Test]
        public async Task Nodes_start_as_followers()
        {
            var nodes = TestNode.CreateCluster();
            nodes.Count(n => n.State == State.Follower).Should().Be(3);
        }

        [Test]
        public async Task when_peers_arent_discovered_until_after_node_start_then_a_leader_will_still_be_elected()
        {
            var peerObservable = new Subject<IEnumerable<Peer>>();

            var nodes = new List<Node>();

            for (int i = 1; i <= 3; i++)
            {
                nodes.Add(new TestNode(new NodeSettings("node" + i, TestNode.ElectionTimeout, TestNode.HeartBeatTimeout, peerObservable)));
            }
            nodes.ForEach(n => n.Start());

            await Task.Delay(TestNode.MaxElectionTimeout);
            peerObservable.OnNext(nodes.Select(n => n.AsPeer()));
            await Task.Delay(TestNode.MaxElectionTimeout);
            nodes.Should().Contain(n => n.State == State.Leader || n.State == State.Candidate,
                                   "Election timeouts can be inflated by a max of 10%. after that time a leader " +
                                   "should be elected.");
        }
    }
}