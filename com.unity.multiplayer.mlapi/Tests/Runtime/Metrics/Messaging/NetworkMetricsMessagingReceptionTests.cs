using System;
using System.Collections;
using System.IO;
using System.Linq;
using MLAPI.Metrics;
using MLAPI.Serialization;
using NUnit.Framework;
using Unity.Multiplayer.MetricTypes;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.RuntimeTests.Metrics.Messaging
{
    public class NetworkMetricsMessagingReceptionTests
    {
        NetworkManager m_Server;
        NetworkManager m_Client;
        NetworkMetrics m_ClientMetrics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!MultiInstanceHelpers.Create(1, out m_Server, out var clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            if (!MultiInstanceHelpers.Start(true, m_Server, clients))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(m_Server));

            m_Client = clients.First();
            m_ClientMetrics = m_Client.NetworkMetrics as NetworkMetrics;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MultiInstanceHelpers.Destroy();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackNamedMessageReceivedMetric()
        {
            var messageName = Guid.NewGuid().ToString();
            LogAssert.Expect(LogType.Log, $"Received from {m_Server.LocalClientId}");
            m_Client.CustomMessagingManager.RegisterNamedMessageHandler(messageName, (sender, payload) =>
            {
                Debug.Log($"Received from {sender}");
            });

            var waitForMetricValues = new WaitForMetricValues<NamedMessageEvent>(m_ClientMetrics.Dispatcher, MetricNames.NamedMessageReceived);

            m_Server.CustomMessagingManager.SendNamedMessage(messageName, m_Client.LocalClientId, Stream.Null);

            yield return waitForMetricValues.WaitForAFewFrames();

            var namedMessageReceivedValues = waitForMetricValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, namedMessageReceivedValues.Count);

            var namedMessageReceived = namedMessageReceivedValues.First();
            Assert.AreEqual(messageName, namedMessageReceived.Name);
            Assert.AreEqual(m_Server.LocalClientId, namedMessageReceived.Connection.Id);
        }

        [UnityTest]
        public IEnumerator TrackUnnamedMessageReceivedMetric()
        {
            var waitForMetricValues = new WaitForMetricValues<UnnamedMessageEvent>(m_ClientMetrics.Dispatcher, MetricNames.UnnamedMessageReceived);

            m_Server.CustomMessagingManager.SendUnnamedMessage(m_Client.LocalClientId, new NetworkBuffer());

            yield return waitForMetricValues.WaitForAFewFrames();

            var unnamedMessageReceivedValues = waitForMetricValues.EnsureMetricValuesHaveBeenFound();
            Assert.AreEqual(1, unnamedMessageReceivedValues.Count);

            var unnamedMessageReceived = unnamedMessageReceivedValues.First();
            Assert.AreEqual(m_Server.LocalClientId, unnamedMessageReceived.Connection.Id);
        }
    }
}
