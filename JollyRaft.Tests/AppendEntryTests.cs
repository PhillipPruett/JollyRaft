using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class AppendEntryTests
    {
        [Test]
        public async Task when_an_append_entries_request_is_sent_to_a_node_with_a_higher_term_number_then_that_node_then_that_node_removes_its_leader()
        {
            var nodes = TestNode.CreateCluster();
            var leader = nodes.First();
            await leader.StartElection();

            var node = nodes.First(n => n != leader);

            await node.AppendEntries(new AppendEntriesRequest("id", 5, 1, 4, new[] {new LogEntry(1, 1, null), new LogEntry(2, 5, "stuff")}, 1));

            node.CurrentLeader.Should().Be("id");
        }

        [Test]
        [Ignore]
        public async Task when_an_append_entries_request_is_sent_to_a_node_with_a_higher_term_number_then_that_node_then_that_node_rejects_the_request()
        {
            var nodes = TestNode.CreateCluster();
            var leader = nodes.First();
            await leader.StartElection();

            var node = nodes.First(n => n != leader);

            var result = await node.AppendEntries(new AppendEntriesRequest("id", 5, 1, 4, new[] {new LogEntry(1, 1, null), new LogEntry(2, 5, "stuff")}, 1));

            result.Success.Should().BeFalse(); //TODO(phpruett): im not totally sure wether or not this should accept or reject yet...
        }

        [Test]
        public async Task when_an_append_entries_request_is_sent_to_a_node_with_a_higher_term_number_then_that_node_then_that_node_updates_to_the_request_term_number()
        {
            var nodes = TestNode.CreateCluster();
            var leader = nodes.First();
            await leader.StartElection();

            var node = nodes.First(n => n != leader);

            var result = await node.AppendEntries(new AppendEntriesRequest("id", 5, 1, 4, new[] {new LogEntry(1, 1, null), new LogEntry(2, 5, "stuff")}, 1));

            result.CurrentTerm.Should().Be(5);
            node.Term.Should().Be(5);
        }

        [Test]
        public async Task when_an_append_entries_request_is_sent_to_a_node_with_a_lower_term_number_then_that_node_then_that_node_rejects_the_request()
        {
            var nodes = TestNode.CreateCluster();
            var leader = nodes.First();
            await leader.StartElection();

            var node = nodes.First(n => n != leader);

            var result = await node.AppendEntries(new AppendEntriesRequest("id", 1, 1, 4, new[] {new LogEntry(1, 1, null), new LogEntry(2, 5, "stuff")}, 0));

            result.Success.Should().BeFalse();
            result.CurrentTerm.Should().Be(2);
        }
    }
}