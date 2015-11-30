using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace JollyRaft
{
    public class NodeSettings
    {
        public NodeSettings(string nodeId, TimeSpan electionTimeout, TimeSpan heartBeatTimeout, IObservable<IEnumerable<Peer>> peerObserver, IScheduler scheduler = null)
        {
            if (String.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentNullException("nodeId");
            }
            if (heartBeatTimeout > electionTimeout)
            {
                throw new ArgumentException("The HeartBeatTimeout is required to be smaller than the ElectionTimeout");
            }
            if (peerObserver == null)
            {
                throw new ArgumentNullException("peerObserver");
            }

            NodeId = nodeId;
            ElectionTimeout = electionTimeout;
            HeartBeatTimeout = heartBeatTimeout;
            Scheduler = scheduler ?? DefaultScheduler.Instance;
            PeerObserver = peerObserver;
        }

        public string NodeId { get; private set; }
        public TimeSpan ElectionTimeout { get; private set; }
        public TimeSpan HeartBeatTimeout { get; private set; }
        public IScheduler Scheduler { get; private set; }
        public IObservable<IEnumerable<Peer>> PeerObserver { get; set; }
    }
}