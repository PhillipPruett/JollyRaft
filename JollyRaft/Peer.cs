using System.Diagnostics;
using System.Threading.Tasks;

namespace JollyRaft
{
    public class Peer
    {
        public delegate Task<AppendEntriesResult> AppendEntriesDelegate(AppendEntriesRequest request);

        public delegate Task<VoteResult> RequestVoteDelegate(VoteRequest request);

        private readonly AppendEntriesDelegate appendEntries;
        public readonly string Id;
        private readonly RequestVoteDelegate requestVote;

        public Peer(string id, RequestVoteDelegate requestVote, AppendEntriesDelegate appendEntries)
        {
            this.requestVote = requestVote;
            Id = id;
            this.appendEntries = appendEntries;
            CommitIndex = 1;
        }

        public int CommitIndex { get; set; }

        public async Task<AppendEntriesResult> AppendEntries(AppendEntriesRequest request)
        {
            return await appendEntries(request);
        }

        public async Task<VoteResult> RequestVote(VoteRequest request)
        {
            //Debug.WriteLine(string.Format("{0} requesting vote from {1}", request.Id, Id));
            var voteResult = await requestVote(request);

            //Debug.WriteLine(string.Format("{0} got vote result from {1}", request.Id, Id));
            return voteResult;
        }
    }
}