using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Web.Http;
using JollyRaft;
using Owin;
using Pocket;

namespace SampleWebApplication
{
    public class StartUp
    {
        private readonly IObservable<IEnumerable<Peer>> peerObservable = new Subject<IEnumerable<Peer>>();
        private Node thisServersNode;

        public void Configuration(IAppBuilder app)
        {
            ConfigureApplication(app);
            ConfigureAndStartRaft(Guid.NewGuid().ToString(), peerObservable);
        }

        public void ConfigureApplication(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            var pocket = new PocketContainer();
            config.ResolveDependenciesUsing(pocket);
            pocket.Register(c => thisServersNode);
            config.MapHttpAttributeRoutes();
            app.UseWebApi(config);
        }

        public string ConfigureAndStartRaft(string nodeId, IObservable<IEnumerable<Peer>> peerObservable)
        {
            thisServersNode = new Node(new NodeSettings(nodeId,
                                                        TimeSpan.FromSeconds(5),
                                                        TimeSpan.FromSeconds(1),
                                                        peerObservable));
            thisServersNode.Start();
            return thisServersNode.Id;
        }
    }
}