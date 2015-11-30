namespace JollyRaft
{
    public class VoteResult
    {
        public VoteResult(int currentTerm, bool voteGranted)
        {
            CurrentTerm = currentTerm;
            VoteGranted = voteGranted;
        }

        public int CurrentTerm { get; private set; }
        public bool VoteGranted { get; private set; }
    }
}