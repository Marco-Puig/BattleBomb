using System.Net;
using System.Net.Sockets;
using BattleBomb.Gameplay.Net;
using BattleBomb.Platform.Net;
using NUnit.Framework;
using UnityEngine;

namespace BattleBomb.Tests.EditMode.Net
{
    public sealed class NetSessionHostTests
    {
        [Test]
        public void Hosting_on_a_busy_port_stays_offline_and_says_why()
        {
            var squatter = new TcpListener(IPAddress.Loopback, 0);
            squatter.Start();
            var go = new GameObject("NetSessionHostTests");
            try
            {
                NetSession net = go.AddComponent<NetSession>();
                var transport = new LocalSocketTransport(((IPEndPoint)squatter.LocalEndpoint).Port);

                Assert.DoesNotThrow(() => net.Host(transport), "A busy port escaped as an exception.");
                Assert.That(net.Role, Is.EqualTo(NetRole.Offline), "The session believes it is hosting with nothing listening.");
                Assert.That(net.IsConnected, Is.False);
                Assert.That(net.Status, Does.Contain("busy"));
            }
            finally
            {
                Object.DestroyImmediate(go);
                squatter.Stop();
            }
        }
    }
}
