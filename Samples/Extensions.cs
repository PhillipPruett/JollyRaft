using System.Net.Http;
using System.Text;
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
    }
}