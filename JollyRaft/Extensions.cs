using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JollyRaft
{
    public static class Extensions
    {
        public static TimeSpan Randomize(this TimeSpan timeSpan, int maxPercentageChange)
        {
            if (maxPercentageChange < 0)
            {
                throw new ArgumentException("maxPercentageChange should not be less than 0", "maxPercentageChange");
            }
            if (maxPercentageChange == 0)
            {
                return timeSpan;
            }

            double maxChangeInTicks = timeSpan.Ticks*(1.0/maxPercentageChange);

            var random = new Random();
            var possitiveOrNegative = (random.Next(1, 3)%2 == 0 ? 1 : -1);

            return new TimeSpan((long) (random.NextDouble()*maxChangeInTicks)*possitiveOrNegative + timeSpan.Ticks);
        }

        public static void ParrallelForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            Parallel.ForEach(collection, action);
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }
    }
}