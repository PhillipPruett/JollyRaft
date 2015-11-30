namespace JollyRaft
{
    public class AppendEntriesResult
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentTerm">current term. for the leader to update itself</param>
        /// <param name="success">true if follower contained entry matching PreviousLogIndex and PreviousLogTerm</param>
        public AppendEntriesResult(int currentTerm, bool success)
        {
            CurrentTerm = currentTerm;
            Success = success;
        }

        public int CurrentTerm { get; private set; }
        public bool Success { get; private set; }
    }
}