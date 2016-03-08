using System.Threading.Tasks;

namespace JollyRaft
{
    public interface IRequestHandler
    {
        Task<AppendEntriesResult> AppendEntries(AppendEntriesRequest request);
        Task<VoteResult> Vote(VoteRequest request);
        Task<LogResult> AddLog(string log);
        Task SendHeartBeat();
        Task StartElection();
    }
}