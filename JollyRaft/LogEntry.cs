using System.Security.Principal;

namespace JollyRaft
{
    public class LogEntry
    {
        public LogEntry(int index, int term, string log)
        {
            Index = index;
            Term = term;
            Log = log;
        }

        public int Index { get; private set; }
        public int Term { get; private set; }
        public string Log { get; private set; }

    }
}