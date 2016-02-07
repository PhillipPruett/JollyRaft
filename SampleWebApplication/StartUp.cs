using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Reactive.Subjects;
using System.Web.Http;
using JollyRaft;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Owin;
using Pocket;

namespace SampleWebApplication
{
    public class StartUp
    {
        private readonly IObservable<IEnumerable<Peer>> peersObservable = new Subject<IEnumerable<Peer>>();
        private Node thisServersNode;

        public void Configuration(IAppBuilder app)
        {
            //ConfigureApplication(app, Guid.NewGuid().ToString(), peersObservable);
            //ConfigureAndStartRaft(Guid.NewGuid().ToString(), peersObservable);
        }

        public void ConfigureApplication(IAppBuilder app, string nodeId, IObservable<IEnumerable<Peer>> peerObserver)
        {
            thisServersNode = new Node(new NodeSettings(nodeId,
                                                      TimeSpan.FromSeconds(5),
                                                      TimeSpan.FromSeconds(1),
                                                      peerObserver));

            var config = new HttpConfiguration();
            var pocket = new PocketContainer();
            config.ResolveDependenciesUsing(pocket);
            pocket.RegisterSingle(c => thisServersNode);
            config.MapHttpAttributeRoutes();
            JsonSerialization(config);
            app.UseWebApi(config);

            thisServersNode.Start();
        }
        public void ConfigureAndStartRaft(string nodeId, IObservable<IEnumerable<Peer>> peerObserver)
        {

            thisServersNode.Start();
        }

        private static void JsonSerialization(HttpConfiguration config)
        {
            var defaultSettings = new JsonSerializerSettings
                                  {
                                      Formatting = Formatting.Indented,
                                      ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                      Converters = new List<JsonConverter>
                                                   {
                                                       new StringEnumConverter {CamelCaseText = true},
                                                   }
                                  };

            JsonConvert.DefaultSettings = () => { return defaultSettings; };

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings = defaultSettings;
        }

        
    }
}