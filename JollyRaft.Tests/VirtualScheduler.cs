using System;
using Microsoft.Reactive.Testing;

namespace JollyRaft.Tests
{
    public class VirtualScheduler : TestScheduler
    {
        public VirtualScheduler()
        {
            AdvanceTo(DateTimeOffset.UtcNow.Ticks);
        }

        public void AdvanceBy(TimeSpan timeSpan)
        {
            AdvanceBy(timeSpan.Ticks);
        }

        public void AdvanceTo(DateTimeOffset dateTimeOffset)
        {
            AdvanceTo(dateTimeOffset.Ticks);
        }
    }
}