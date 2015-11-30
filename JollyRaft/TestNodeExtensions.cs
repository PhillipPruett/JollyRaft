using System.Collections.Generic;

namespace JollyRaft
{
    public static class TestNodeExtensions
    {
        public static void Start(this IEnumerable<Node> nodes)
        {
            nodes.ForEach(n => n.Start());
        }
    }
}