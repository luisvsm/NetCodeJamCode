using System;

public class HostServer
{
    private static ServerMain gameServer;

    public static void Main(string[] args)
    {
        Console.WriteLine("Starting game server :)");
        gameServer = new ServerMain();
        gameServer.Start("local");
    }
}