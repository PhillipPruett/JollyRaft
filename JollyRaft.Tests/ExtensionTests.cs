using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;

namespace JollyRaft.Tests
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test, TestMethod, Owner("phpruett")]
        public void randomize_results_are_within_max_percentage_change()
        {
            var timeSpan = TimeSpan.FromSeconds(10);

            var min = timeSpan.Ticks - TimeSpan.FromSeconds(1).Ticks;
            var max = timeSpan.Ticks + TimeSpan.FromSeconds(1).Ticks;

            for (var i = 0; i < 50; i++)
            {
                var result = timeSpan.Randomize(10);
                result.Ticks.Should().BeInRange(min, max);
            }
        }

        [Test, TestMethod, Owner("phpruett")]
        public void results_are_fairly_randomly_distributed()
        {
            var timeSpan = TimeSpan.FromSeconds(10);

            var results = Enumerable.Range(1, 50).Select(i => timeSpan.Randomize(10).TotalMilliseconds).ToArray();

            var distinct = results.Distinct();
            distinct.Count().Should().BeGreaterThan(25);
        }

        [Test, TestMethod, Owner("phpruett")]
        public void timespans_can_be_randomized_with_0_max_percentage_change()
        {
            var timeSpan = TimeSpan.FromSeconds(1);

            var randomizedTimeSpan = timeSpan.Randomize(0);

            randomizedTimeSpan.Ticks.Should().Be(timeSpan.Ticks);
        }
    }
}