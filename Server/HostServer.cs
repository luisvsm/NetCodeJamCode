
using System;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

public class HostServer
{
    public const string HostIP = "45.76.125.216";
    public static ServerMain gameServer;

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting game server :)");

        gameServer = new ServerMain();
        gameServer.Start(HostIP);
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<GetToken>()
            .Build();
}