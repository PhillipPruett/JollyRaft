using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class NodeTests
    {
        [Test]
        public async Task node_constructor_throws_argument_null_exception_when_null_settings_are_passed_in()
        {
            Action createNode = () => new Node(null);

            createNode.ShouldThrow<ArgumentNullException>();
        }
    }
}