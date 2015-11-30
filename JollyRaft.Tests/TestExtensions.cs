using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Reactive.Testing;

namespace JollyRaft.Tests
{
    public static class TestExtensions
    {
        public static async Task<Node> WaitForLeader(this IEnumerable<Node> nodes, TestScheduler testScheduler = null)
        {
            while (nodes.All(n => n.State != State.Leader))
            {
                if (testScheduler != null)
                {
                    testScheduler.AdvanceBy(TestNode.MaxElectionTimeout.Ticks);
                }
                else
                {
                    await Task.Delay(TestNode.MaxElectionTimeout);
                }
            }

            return nodes.Single(n => n.State == State.Leader);
        }
    }
}