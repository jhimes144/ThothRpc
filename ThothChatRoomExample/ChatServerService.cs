using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc;
using ThothRpc.Attributes;
using ThothRpc.Events;
using ThothRpc.LiteNetLib;
using ThothRpc.MessagePack;

namespace ThothChatRoomExample
{
    internal class ChatServerService
    {
        readonly ServerHub _hub;
        readonly ConcurrentDictionary<int, string> _userNamesByClientId 
            = new();

        public ChatServerService() 
        {
            _hub = ServerHubBuilder.BuildServer()
                .UseTransport<LiteNetRpcServer>()
                .UseMessagePack()
                .Build();

            _hub.Register(this);
            _hub.Listen(9000, "ThothChatRoom");

            _hub.PeerDisconnected += onPeerDisconnected;
        }

        public ServerHub GetHub()
        {
            return _hub;
        }

        private void onPeerDisconnected(object sender, PeerInfoEventArgs e)
        {
            if (_userNamesByClientId.TryGetValue(e.Peer.PeerId, out var userName))
            {
                var message = $"{userName} has left the chat.";

                _hub.InvokeForgetAllClients<ChatClientService>(DeliveryMode.ReliableUnordered,
                    c => c.WriteAlert(message));
            }
        }

        [ThothMethod]
        public List<string> GetUsernames()
        {
            return _userNamesByClientId.Values.ToList();
        }

        [ThothMethod]
        public void ReportReady(string userName)
        {
            var client = _hub.GetCurrentPeer();

            _userNamesByClientId[client.PeerId] = userName;
            var message = $"{userName} has joined the chat.";

            _hub.InvokeForgetAllClients<ChatClientService>(DeliveryMode.ReliableUnordered,
                c => c.WriteAlert(message));
        }

        [ThothMethod]
        public void SendChat(string message)
        {
            var client = _hub.GetCurrentPeer();
            var username = _userNamesByClientId.GetOrAdd(client.PeerId, "Unknown User");

            _hub.InvokeForgetAllClients<ChatClientService>(DeliveryMode.ReliableUnordered,
                c => c.WriteChatMessage(username, message));
        }
    }
}
