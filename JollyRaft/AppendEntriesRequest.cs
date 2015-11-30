namespace JollyRaft
{
    public class AppendEntriesRequest
    {
        public AppendEntriesRequest(string id, int term, int previousLogIndex, int previousLogTerm, LogEntry[] entries, int commitIndex)
        {
            Term = term;
            Id = id;
            PreviousLogIndex = previousLogIndex;
            PreviousLogTerm = previousLogTerm;
            Entries = entries;
            CommitIndex = commitIndex;
        }

        public int Term { get; set; }
        public string Id { get; set; }
        public int PreviousLogIndex { get; set; }
        public int PreviousLogTerm { get; set; }
        public LogEntry[] Entries { get; set; }
        public int CommitIndex { get; set; }
    }
}