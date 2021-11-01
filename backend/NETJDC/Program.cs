using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NETJDC
{
    public class Program
    {
        public static void Main(string[] args)
        {

            Console.WriteLine(@"                           ");
            Console.WriteLine(@"       ___  ________  ________");
            Console.WriteLine(@"      |\  \|\   ___ \|\   ____\");
            Console.WriteLine(@"      \ \  \ \  \ |\ \ \  \___ |");
            Console.WriteLine(@"    __ \ \  \ \  \ \\ \ \  \      ");
            Console.WriteLine(@"   |\  \\_\  \ \  \_\\ \ \  \____  ");
            Console.WriteLine(@"   \ \________\ \_______\ \_______\ ");
            Console.WriteLine(@"    \| ________|\| ______|\| ______|");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(@"                           By Nolan  ");
            Console.WriteLine("");
            Console.WriteLine("");

            
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
