namespace JollyRaft
{
    public class VoteRequest
    {
        public VoteRequest(string id, int term, int lastLogTerm, int lastLogIndex)
        {
            LastLogTerm = lastLogTerm;
            LastLogIndex = lastLogIndex;
            Id = id;
            Term = term;

            //TODO: some basic validation so this object cant be created incorrectly

        }

        public int Term { get; private set; }
        public string Id { get; private set; }
        public int LastLogIndex { get; private set; }
        public int LastLogTerm { get; private set; }
    }
}