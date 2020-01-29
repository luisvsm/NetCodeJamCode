
using System;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

public class HostServer
{
    //public const string HostIP = "0.0.0.0";
    public const string HostIP = "45.76.125.216";
    public static ServerMain gameServer;

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting game server :)");

        gameServer = new ServerMain();
        gameServer.Start(HostIP);
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Listen(IPAddress.Loopback, 8080);
                })
                .UseStartup<GetToken>();
            });
}