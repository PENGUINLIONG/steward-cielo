using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Liongbot.Dispatch;
using LiongbotPeripheral.GroupMe;
using Liongbot.Command.SyntaxProviders;
using StewardCielo.Controllers;
using StewardCielo.BackEnds;

namespace StewardCielo {
    public class Program {
        private static Dispatcher dispatcher;
        public static void Main(string[] args) {
            dotenv.net.DotEnv.Config();

            // Initiate Dispatcher.
            var botId = Environment.GetEnvironmentVariable("BOT_ID");
            Console.WriteLine("Working as bot of ID: " + botId);
            var composer = new GroupMeComposer();
            dispatcher = new Dispatcher(composer);
            var sp = new SimpleSyntaxProvider();
            var front = new GroupMeFrontEnd(botId);
            ReceiverController.MessageReceived += (sender, e) => front.AspCall(e.MessageContent);
            dispatcher.AddFrontEnd(front);
            dispatcher.AddBackEnd(new LaundryBackEnd(sp));
            dispatcher.AddBackEnd(new VisitorBackEnd(sp));
            dispatcher.Launch();
            State.SchedulePersistence();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseUrls("https://0.0.0.0:11223/")
                .Build();
                host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
