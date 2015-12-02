using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JollyRaft;
using Newtonsoft.Json;

namespace Samples
{
    public static class Extensions
    {
        public static StringContent AsStringContent(this object obj)
        {
            return new StringContent(
                JsonConvert.SerializeObject(obj),
                Encoding.UTF8,
                "application/json");
        }

        public static T DeserializeAs<T>(this string val)
        {
            return JsonConvert.DeserializeObject<T>(val);
        }

        public static async Task<HttpClient> WaitForLeader(this IEnumerable<HttpClient> httpClients)
        {
            while (httpClients.All(c => c.GetAsync("state").Result.Content.ReadAsStringAsync().Result
                                         .DeserializeAs<State>() != State.Leader))
            {
                await Task.Delay(TestNode.MaxElectionTimeout);
            }

            return httpClients.Single(c => c.GetAsync("state").Result.Content.ReadAsStringAsync().Result
                                            .DeserializeAs<State>() == State.Leader);
        }
    }
}