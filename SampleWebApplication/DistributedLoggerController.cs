using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using JollyRaft;

namespace SampleWebApplication
{
    public class DistributedLoggerController : ApiController
    {
        private readonly Node node;

        public DistributedLoggerController(Node node)
        {
            this.node = node;
        }

        [HttpGet, Route("name")]
        public string Name()
        {
            return string.Format("Hi, i'm node {0}", node.Id);
        }

        [HttpPost, Route("log")]
        public async Task<Log> Log([FromBody] LogRequest request)
        {
            var logResult = await node.AddLog(request.Log);

            if (logResult is LogRejected)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                                                {
                                                    Content = new StringContent(string.Format("Please direct all log requests to Node {0}",
                                                                                              (logResult as LogRejected).LeaderId))
                                                });
            }

            if (logResult is LogRejected)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return (logResult as LogCommited).ServerLog;
        }

        [HttpPost, Route("requestVote")]
        public async Task<VoteResult> RequestVote([FromBody] VoteRequest request)
        {
            return await node.Vote(request);
        }

        [HttpPost, Route("appendentries")]
        public async Task<AppendEntriesResult> AppendEntries([FromBody] AppendEntriesRequest request)
        {
            return await node.AppendEntries(request);
        }

        [HttpGet, Route("state")]
        public async Task<State> State()
        {
            return node.State;
        }
    }

    public class LogRequest
    {
        public string Log { get; set; }
    }
}