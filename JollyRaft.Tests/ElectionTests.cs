using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class ElectionTests
    {
        [Test]
        public async Task after_an_election_all_nodes_should_have_the_new_leader_as_their_leader()
        {
            var nodes = TestNode.CreateCluster();
            var leader = nodes.First();
            await leader.StartElection();

            nodes.ForEach(n => n.CurrentLeader.Should().Be(leader.Id));
        }

        [Test]
        public async Task after_an_election_with_one_candidate_and_two_followers_the_candidate_becomes_a_leader()
        {
            var nodes = TestNode.CreateCluster();
            var futureLeader = nodes.First(n => n.Id == "node1");
            await futureLeader.StartElection();

            futureLeader.State.Should().Be(State.Leader);
        }

        [Test]
        public async Task all_nodes_start_as_followers()
        {
            TestNode.CreateCluster().ForEach(n => n.State.Should().Be(State.Follower));
        }

        [Test]
        public void elections_are_ran_shortly_after_node_startup()
        {
            var testScheduler = new TestScheduler();
            testScheduler.AdvanceTo(DateTimeOffset.UtcNow.Ticks);
            var nodes = TestNode.CreateCluster(scheduler: testScheduler);
            nodes.Start();

            testScheduler.AdvanceBy(TestNode.ElectionTimeout.Ticks*2);
            nodes.Should().Contain(n => n.State == State.Leader || n.State == State.Candidate);
        }

        [Test]
        public async Task when_a_candidate_recieves_an_AppendEntries_from_a_leader_that_candidate_becomes_a_follower()
        {
            var peerObservable = new Subject<IEnumerable<Peer>>();
            var nodes = TestNode.CreateCluster(peerObservable: peerObservable);
            var leader = nodes.First();
            await leader.StartElection();

            var usurper = TestNode.CreateTestNode("usurper", peerObservable);
            nodes.Add(usurper);
            peerObservable.OnNext(nodes.Select(n => n.AsPeer()));
            await usurper.StartElection();

            leader.State.Should().Be(State.Leader);
            usurper.State.Should().Be(State.Candidate);

            await leader.SendHeartBeat();

            usurper.State.Should().Be(State.Follower);
        }

        [Test]
        public async Task when_a_cluster_already_has_a_leader_if_an_usurper_node_attempts_an_election_it_fails()
        {
            var peerObservable = new Subject<IEnumerable<Peer>>();
            var nodes = TestNode.CreateCluster(peerObservable: peerObservable);
            var leader = nodes.First();
            await leader.StartElection();

            var usurper = TestNode.CreateTestNode("usurper", peerObservable);
            nodes.Add(usurper);
            peerObservable.OnNext(nodes.Select(n => n.AsPeer()));
            await usurper.StartElection();

            leader.State.Should().Be(State.Leader);
            usurper.State.Should().Be(State.Candidate, "the node should remain a candidate and continue trying elections until it is contacted by a leader");
        }

        [Test]
        public async Task when_a_candidate_recieves_a_vote_response_from_a_node_with_a_higher_term_number_then_that_candidate_steps_down()
        {
            var peerObservable = new Subject<IEnumerable<Peer>>();
            var nodes = TestNode.CreateCluster(peerObservable: peerObservable);
            var leader = nodes.First();
            await leader.StartElection();
            leader.StepDown(2, "none");
            await leader.StartElection();
            nodes.ForEach(n => n.Term.Should().Be(3));

            var candidateThatsBehind = TestNode.CreateTestNode("candidateThatsBehind", peerObservable);
            nodes.Add(candidateThatsBehind);
            peerObservable.OnNext(nodes.Select(n => n.AsPeer()));
            await candidateThatsBehind.StartElection();

            leader.State.Should().Be(State.Leader);
            candidateThatsBehind.Term.Should().Be(3);
            candidateThatsBehind.State.Should().Be(State.Follower, "the node should have immediatly stepped down because it attempted to start an election for term 2 but the cluster was on term 3");
        }

        [Test]
        public async Task when_a_node_is_not_aware_of_any_peers_it_will_not_start_an_election_stagnating_at_the_first_term()
        {
            var node = TestNode.CreateCluster(1).Single();
            await node.StartElection();

            node.CurrentLeader.Should().BeNullOrWhiteSpace();
            node.Term.Should().Be(1);
        }

        [Test]
        public async Task when_a_node_starts_an_election_it_initally_becomes_a_candidate()
        {
            var nodes = TestNode.CreateCluster().OfType<TestNode>();
            var futureCandidate = nodes.First(n => n.Id == "node1");
            nodes.Skip(1).ForEach(n => n.GrantVotes = false);
            await futureCandidate.StartElection();

            futureCandidate.State.Should().Be(State.Candidate);
        }

        [Test]
        public async Task when_all_nodes_try_to_start_an_election_at_the_same_time_only_one_is_elected()
        {
            var nodes = TestNode.CreateCluster();

            nodes.ForEach(n => n.StartElection());

            nodes.Should().ContainSingle(n => n.State == State.Leader);
        }

        [Test]
        public async Task elections_work_for_large_clusters()
        {
            var nodes = TestNode.CreateCluster(clusterSize: 100);

            nodes.ForEach(n => n.StartElection());

            nodes.Should().ContainSingle(n => n.State == State.Leader);
        }
    }
}