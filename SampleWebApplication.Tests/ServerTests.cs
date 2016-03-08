using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using JollyRaft.Tests;
using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using NUnit.Framework;
using SampleWebApplication;

namespace JollyRaft.Sample.Tests
{
    [TestFixture]
    public class ServerTests
    {
        private HttpClient[] CreateServers()
        {
            var peerObservable = new Subject<IEnumerable<Peer>>();

            var clientsById = new List<KeyValuePair<string, HttpClient>>
                              {
                                  CreateServer(peerObservable, "node1"),
                                  CreateServer(peerObservable, "node2"),
                                  CreateServer(peerObservable, "node3")
                              };

            var peers = clientsById.Select(c =>
                                           new Peer(c.Key,
                                                    async request =>
                                                          {
                                                              var result = (await (await c.Value.PostAsync("requestvote/",
                                                                                                           request.AsStringContent()))
                                                                                      .Content
                                                                                      .ReadAsStringAsync());
                                                              return result.DeserializeAs<VoteResult>();
                                                          },
                                                    async request =>
                                                          {
                                                              var result = (await (await c.Value.PostAsync("appendentries/",
                                                                                                           request.AsStringContent()))
                                                                                      .Content
                                                                                      .ReadAsStringAsync());

                                                              return result.DeserializeAs<AppendEntriesResult>();
                                                          })
                );

            peerObservable.OnNext(peers);
            var httpClients = clientsById.Select(c => c.Value).ToArray();
            httpClients.WaitForLeader().Wait();
            return httpClients;
        }

        private KeyValuePair<string, HttpClient> CreateServer(IObservable<IEnumerable<Peer>> peerObservable, string nodeId)
        {
            var server = TestServer.Create(app =>
                                           {
                                               var startup = new StartUp();
                                               startup.ConfigureApplication(app, nodeId, peerObservable);
                                           });
            Trace.Listeners.RemoveAt(Trace.Listeners.Count - 1); //hack because Owin registers an extra trace listener and makes the log output double
            return new KeyValuePair<string, HttpClient>(nodeId, server.HttpClient);
        }

        [Test]
        public async Task a_leader_is_elected_amongst_the_servers()
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            CreateServers().Should().ContainSingle(c => c.GetAsync("state").Result.Content.ReadAsStringAsync().Result.DeserializeAs<State>() == State.Leader);
        }

        [Test]
        public void each_server_has_a_distinct_node_id()
        {
            var httpClients = CreateServers();
            var id1 = JsonConvert.DeserializeObject<string>(httpClients.First().GetAsync("name").Result.Content.ReadAsStringAsync().Result);
            var id2 = JsonConvert.DeserializeObject<string>(httpClients.Skip(1).First().GetAsync("name").Result.Content.ReadAsStringAsync().Result);

            id1.Should().NotBe(id2);
        }

        [Test]
        public void ServerIsReachable()
        {
            CreateServers().First().GetAsync("name").Result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Test]
        public void when_log_is_requestedof_a_follower_bad_request_is_returned()
        {
            CreateServers().First(c => c.GetAsync("state").Result.Content.ReadAsStringAsync().Result.DeserializeAs<State>() == State.Follower)
                           .PostAsync("log",
                                      new
                                      {
                                          Log = "first commit"
                                      }.AsStringContent())
                           .Result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Test]
        public void when_log_is_requestedof_a_leader_OK_is_returned()
        {
            CreateServers().First(c => c.GetAsync("state").Result.Content.ReadAsStringAsync().Result.DeserializeAs<State>() == State.Leader)
                           .PostAsync("log",
                                      new
                                      {
                                          Log = "first commit"
                                      }.AsStringContent())
                           .Result.StatusCode.Should().Be(HttpStatusCode.OK);

        }

    }
}