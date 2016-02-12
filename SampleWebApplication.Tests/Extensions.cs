using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JollyRaft.Tests;
using Newtonsoft.Json;

namespace JollyRaft.Sample.Tests
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

        public static async Task WaitForLeader(this HttpClient[] httpClients)
        {
            var leaderHasNotBeenElected = true;
            while (true)
            {
               IEnumerable<Task<bool>> isLeader =  httpClients.Select(async c => (await (await c.GetAsync("state"))
                                         .Content.ReadAsStringAsync()).DeserializeAs<State>() == State.Leader);

                foreach (var check in isLeader)
                {
                    if (await check)
                    {
                        return;
                    }
                }

                await Task.Delay(TestNode.MaxElectionTimeout);
            }
        }
    }
}