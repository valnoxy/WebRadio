using DiscordRPC;
using DiscordRPC.Logging;
using System;

namespace WebRadio.Common
{
    public class DiscordRpc
    {
        public static DiscordRpcClient client;
        public static string _currentSenderName;

        public static void Initialize()
        {
            client = new DiscordRpcClient("1130664919805739130");
            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };
            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };
            client.Initialize();
        }

        public static void Dispose()
        {
            Console.WriteLine("Disposing Discord RPC client ...");
            client.Dispose();
        }
    }
}
