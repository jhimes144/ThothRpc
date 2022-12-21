using Spectre.Console;
using ThothRpc.Optimizer;

namespace ThothChatRoomExample
{
    internal class Program
    {
        static ChatServerService _server;
        static ChatClientService _client;

        static void Main(string[] args)
        {
            ThothOptimizer.Instance.Optimize();

            AnsiConsole.Write(new Rule("[yellow bold underline]Welcome to chat.[/]").RuleStyle("grey").Centered());
            var isServer = AnsiConsole.Confirm("Do you want to be the server (y) or client (n)");
                
            if (isServer)
            {
                _server = new ChatServerService();
                _client = new ChatClientService(_server.GetHub(), null);
            }
            else
            {
                PromptConnect();
            }

            var username = AnsiConsole.Ask<string>("Please enter your user name that will identify you in the chat.");
            _client.SubmitSetUsername(username);
            _client.DisplayUserNames().Wait();

            ChatLoop();
        }

        static void PromptConnect()
        {
            var address = AnsiConsole.Ask<string>("Please enter address of server.");
            _client = new ChatClientService(null, address);
        }

        static void ChatLoop()
        {
            AnsiConsole.WriteLine();
            var chatMessage = AnsiConsole.Ask<string>(">");

            _client.SubmitChat(chatMessage);
            ChatLoop();
        }
    }
}