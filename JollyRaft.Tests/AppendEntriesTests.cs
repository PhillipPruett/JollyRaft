using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class Given_a_cluster_with_split_brain_with_each_half_having_its_own_leader_on_different_term_numbers
    {
        private Subject<IEnumerable<Peer>> leftBrainPeerObservable;
        private Subject<IEnumerable<Peer>> rightBrainPeerObservable;
        private List<Node> leftBrain;
        private List<Node> rightBrain;
        private Node leftBrainLeader;
        private VirtualScheduler scheduler;

        [SetUp]
        public void SetUp()
        {
            scheduler = new VirtualScheduler();
            leftBrainPeerObservable = new Subject<IEnumerable<Peer>>();
            rightBrainPeerObservable = new Subject<IEnumerable<Peer>>();

            leftBrain = TestNode.CreateCluster(clusterSize: 5, peerObservable: leftBrainPeerObservable, scheduler: scheduler);
            rightBrain = TestNode.CreateCluster(clusterSize: 5, peerObservable: rightBrainPeerObservable, scheduler: scheduler);

            leftBrainLeader = leftBrain.First();
            leftBrainLeader.StartElection().Wait();
            rightBrain.First().StartElection().Wait();

            rightBrain.First().StepDown(2, "none");
            scheduler.AdvanceBy(TestNode.ElectionTimeout);
            rightBrain.First().StartElection().Wait();
            rightBrain.ForEach(n => n.Term.Should().Be(3));
        }

        [Test]
        public async Task when_reunited_and_the_cluster_leader_behind_in_term_number_sends_an_append_entries_then_it_fails_and_steps_down()
        {
            leftBrain.AddRange(rightBrain);
            leftBrainPeerObservable.OnNext(leftBrain.Select(n => n.AsPeer()));

            await leftBrainLeader.SendAppendEntries("log", false);

            leftBrainLeader.State.Should().Be(State.Follower);
            leftBrainLeader.Term.Should().Be(3);
        }
    }
}