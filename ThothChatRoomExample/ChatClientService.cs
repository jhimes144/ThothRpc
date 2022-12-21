using Spectre.Console;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc;
using ThothRpc.Attributes;
using ThothRpc.Base;
using ThothRpc.LiteNetLib;
using ThothRpc.MessagePack;

namespace ThothChatRoomExample
{
    internal class ChatClientService
    {
        readonly ClientHub _hub;

        public ChatClientService(ServerHub localServer, string address)
        {
            var builder = ClientHubBuilder.BuildClient()
                .UseMessagePack();

            if (localServer == null)
            {
                _hub = builder
                    .UseTransport<LiteNetRpcClient>()
                    .Build();

                _hub.ConnectAsync(address, 9000, "ThothChatRoom").AsTask().Wait();
            }
            else
            {
                _hub = builder
                    .UseLocalServer(localServer)
                    .Build();
            }

            _hub.Register(this);
        }

        public async Task DisplayUserNames()
        {
            var usernames = await _hub.InvokeServerAsync<ChatServerService, IEnumerable<string>>
                (s => s.GetUsernames());

            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Users in this chatroom:");

            foreach (var name in usernames)
            {
                AnsiConsole.WriteLine(name);
            }
        }

        public void SubmitSetUsername(string username)
        {
            _hub.InvokeForgetServer<ChatServerService>(DeliveryMode.ReliableUnordered,
                s => s.ReportReady(username));
        }

        public void SubmitChat(string message)
        {
            _hub.InvokeForgetServer<ChatServerService>(DeliveryMode.ReliableUnordered,
                s => s.SendChat(message));
        }

        [ThothMethod]
        public void WriteAlert(string message)
        {
            AnsiConsole.WriteLine(message);
        }

        [ThothMethod]
        public void WriteChatMessage(string username, string message)
        {
            AnsiConsole.WriteLine($"{username}: {message}");
        }
    }
}
