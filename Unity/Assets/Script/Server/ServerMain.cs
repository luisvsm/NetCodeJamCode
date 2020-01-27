using NetcodeIO.NET;
using System.Globalization;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Timers;
using System;

#if UNITY_EDITOR
using UnityEngine;
#endif

public class ServerMain
{
    Server server;
    TokenFactory tokenFactory;
    int maxClients = 2;
    int port = 5240;
    int threadSleepMsBetweenUpdates = 10;
    ulong protocolID = 1L;
    ulong tokenSequenceNumber = 1L;
    ulong clientID = 1L;
    Dictionary<ulong, RemoteGameClient> clientList = new Dictionary<ulong, RemoteGameClient>();
    private static System.Timers.Timer updateTick;

    public void Log(string log)
    {
#if UNITY_EDITOR
        Debug.Log("[Server] " + log);
#endif
    }
    public void LogError(object log)
    {
#if UNITY_EDITOR
        Debug.LogError(log);
#endif
    }

    public void Stop()
    {
        server.Stop();
        updateTick.Stop();
    }

    public void Start(string publicAddress, byte[] privKey = null)
    {
        this.privKey = privKey;

        if (publicAddress == "" || publicAddress == "local")
        {
            publicAddress = getLocalIPAddress();
        }

        Log("Address: " + publicAddress);

        server = new Server(
            maxClients,     // int maximum number of clients which can connect to this server at one time
            publicAddress, port,    // string public address and int port clients will connect to
            protocolID,     // ulong protocol ID shared between clients and server
            getPrivateKey()     // byte[32] private crypto key shared between backend servers
        );

        tokenFactory = new TokenFactory(
            protocolID,      // must be the same protocol ID as passed to both client and server constructors
            getPrivateKey()       // byte[32], must be the same as the private key passed to the Server constructor
        );

        // Called when a client has connected
        server.OnClientConnected += clientConnectedHandler;		// void( RemoteClient client )

        // Called when a client disconnects
        server.OnClientDisconnected += clientDisconnectedHandler;	// void( RemoteClient client )

        // Called when a payload has been received from a client
        // Note that you should not keep a reference to the payload, as it will be returned to a pool after this call completes.
        server.OnClientMessageReceived += messageReceivedHandler;   // void( RemoteClient client, byte[] payload, int payloadSize )

        //server.LogLevel = NetcodeLogLevel.Debug;

        server.Start();         // start the server running


        SetUpdateTimer();
    }

    private void SetUpdateTimer()
    {
        // Create a timer with a two second interval.
        updateTick = new System.Timers.Timer(threadSleepMsBetweenUpdates);
        // Hook up the Elapsed event for the timer. 
        updateTick.Elapsed += OnUpdate;
        updateTick.AutoReset = true;
        updateTick.Enabled = true;
    }

    private void OnUpdate(object source, ElapsedEventArgs e)
    {
        foreach (KeyValuePair<ulong, RemoteGameClient> entry in clientList)
        {
            entry.Value.Update();
        }
    }

    public byte[] GetLocalHostToken()
    {
        tokenSequenceNumber++;
        clientID++;

        return tokenFactory.GenerateConnectToken(
            new IPEndPoint[] { createIPEndPoint(getLocalIPAddress() + ":" + port) },		// IPEndPoint[] list of addresses the client can connect to. Must have at least one and no more than 32.
            300,		// in how many seconds will the token expire
            10,		// how long it takes until a connection attempt times out and the client tries the next server.
            tokenSequenceNumber,		// ulong token sequence number used to uniquely identify a connect token.
            clientID,		// ulong ID used to uniquely identify this client
            new byte[0]		// byte[], up to 256 bytes of arbitrary user data (available to the server as RemoteClient.UserData)
        );
    }

    private void clientConnectedHandler(RemoteClient client)
    {
        Log("clientConnectedHandler");
        clientList.Add(client.ClientID, new RemoteGameClient(client, server));
    }

    private void clientDisconnectedHandler(RemoteClient client)
    {
        Log("clientDisconnectedHandler");
        clientList.Remove(client.ClientID);
    }

    private void messageReceivedHandler(RemoteClient client, byte[] payload, int payloadSize)
    {
        if (client.Confirmed)
        {
            clientList[client.ClientID].ReceivePacket(payload, payloadSize);
        }
        else
        {
            Log("Message from unconfirmed client. Ignore it");
        }
    }

    // https://stackoverflow.com/a/27376368
    private string getLocalIPAddress()
    {
        string localIP;
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        return localIP;
    }

    // https://stackoverflow.com/a/2727880
    // Handles IPv4 and IPv6 notation.
    private IPEndPoint createIPEndPoint(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length < 2)
        {
            LogError("Invalid endpoint format");
            return null;
        }

        IPAddress ip;
        if (ep.Length > 2)
        {
            if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
            {
                LogError("Invalid ip-adress");
                return null;
            }
        }
        else
        {
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                LogError("Invalid ip-adress");
                return null;
            }
        }
        int port;
        if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
        {
            LogError("Invalid port");
            return null;
        }
        return new IPEndPoint(ip, port);
    }

    byte[] privKey;
    private byte[] getPrivateKey()
    {
        if (privKey == null)
        {
            privKey = new byte[32];
            //RNGCryptoServiceProvider is an implementation of a random number generator.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(privKey); // The array is now filled with cryptographically strong random bytes.
        }

        return privKey;
    }
}