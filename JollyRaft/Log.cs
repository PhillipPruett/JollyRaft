using System.Collections.Generic;
using System.Linq;

namespace JollyRaft
{
    public class Log
    {
        private int index;

        public Log()
        {
            Entries = new List<LogEntry>();

            Add(1, null);
        }

        public List<LogEntry> Entries { get; private set; }
        public int LastIndex { get; private set; }
        public int LastTerm { get; private set; }
        public int PreviousLogIndex { get; private set; }
        public int PreviousLogTerm { get; private set; }

        public void Add(int term, string log)
        {
            index++;
            Entries.Add(new LogEntry(index, term, log));
            PreviousLogIndex = LastIndex;
            PreviousLogTerm = LastTerm;
            LastIndex = index;
            LastTerm = term;
        }

        public void RemoveEntryAndThoseAfterIt(int entryIndex)
        {
            Entries.RemoveAll(e => e.Index >= entryIndex);
        }
    }
}