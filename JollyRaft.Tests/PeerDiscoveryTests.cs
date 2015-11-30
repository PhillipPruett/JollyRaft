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
    public class PeerDiscoveryTests
    {
        [SetUp]
        public void SetUp()
        {
            peerObservable = new Subject<IEnumerable<Peer>>();

            var node1 = new Node(new NodeSettings("node1", ElectionTimeout, HeartBeatTimeout, peerObservable));
            var node2 = new Node(new NodeSettings("node2", ElectionTimeout, HeartBeatTimeout, peerObservable));
            var node3 = new Node(new NodeSettings("node3", ElectionTimeout, HeartBeatTimeout, peerObservable));

            nodes = new List<Node> {node1, node2, node3};

            peerObservable.OnNext(nodes.Select(n => n.AsPeer()));
        }

        private List<Node> nodes = new List<Node>();
        private readonly TimeSpan ElectionTimeout = TimeSpan.FromSeconds(1);
        private readonly TimeSpan HeartBeatTimeout = TimeSpan.FromMilliseconds(100);
        private Subject<IEnumerable<Peer>> peerObservable;

        [Test]
        public async Task each_node_should_be_aware_of_each_peer()
        {
            var expectedPeerIds = nodes.Select(n => n.Id);

            nodes.ForEach(n => expectedPeerIds.Where(id => id != n.Id).Should().Contain(n.GetPeerIds()));
        }

        [Test]
        public async Task nodes_do_not_add_themselves_as_peers()
        {
            nodes.ForEach(n => n.GetPeerIds().Should().NotContain(p => p == n.Id));
        }
    }
}