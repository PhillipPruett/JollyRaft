using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class PeerDiscoveryTests
    {
        [Test]
        public async Task each_node_should_be_aware_of_each_peer()
        {
            var nodes = TestNode.CreateCluster();
            var expectedPeerIds = nodes.Select(n => n.Id);

            nodes.ForEach(n => expectedPeerIds.Where(id => id != n.Id).Should().Contain(n.GetPeerIds()));
        }

        [Test]
        public async Task nodes_do_not_add_themselves_as_peers()
        {
            var nodes = TestNode.CreateCluster();
            nodes.ForEach(n => n.GetPeerIds().Should().NotContain(p => p == n.Id));
        }
    }
}