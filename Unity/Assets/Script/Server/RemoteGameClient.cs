using ReliableNetcode;
using NetcodeIO.NET;
using System;


public class RemoteGameClient
{
    private Random random = new Random();
    public RemoteClient remoteClient;
    public RemoteGameClient remoteOpponent;
    private ReliableEndpoint reliableEndpoint;
    private Server server;


    public void Log(string log)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.Log("[Server.RemoteGameClient] " + log);
#else
        Console.WriteLine("[Server.RemoteGameClient] " + log.ToString());
#endif
    }
    public void LogError(object log)
    {
#if UNITY_EDITOR
        UnityEngine.Debug.LogError(log);
#else
        Console.WriteLine("[Error] " + log.ToString());
#endif
    }

    public RemoteGameClient(RemoteClient remoteClient, Server server)
    {
        reliableEndpoint = new ReliableEndpoint();
        reliableEndpoint.ReceiveCallback += ReliableReceiveCallback;
        reliableEndpoint.TransmitCallback += ReliableTransmitCallback;
        this.remoteClient = remoteClient;
    }

    public void ReliableReceiveCallback(byte[] buffer, int size)
    {
        // this will be called when the endpoint extracts messages from received packets
        // buffer is byte[] and size is number of bytes in the buffer.
        // do not keep a reference to buffer as it will be pooled after this function returns
        NetworkMessage.MessageType mType = NetworkMessage.ParseMessage(buffer, size);
        Log("Messge! " + mType);
        if (mType == NetworkMessage.MessageType.FindGame)
        {
            if (ServerMain.clientWaitingToMatch == null)
            {
                ServerMain.clientWaitingToMatch = this;
            }
            else
            {
                ServerMain.clientWaitingToMatch.remoteOpponent = this;
                remoteOpponent = ServerMain.clientWaitingToMatch;

                byte[] playerOneSeed = new byte[4];
                byte[] playerTwoSeed = new byte[4];
                random.NextBytes(playerOneSeed);
                random.NextBytes(playerTwoSeed);

                byte[] playerOneData = new byte[10];
                byte[] playerTwoData = new byte[10];

                BitConverter.GetBytes(
                    (UInt16)NetworkMessage.MessageType.PlayerSeeds
                ).CopyTo(playerOneData, 0);

                playerOneData.CopyTo(playerTwoData, 0);

                playerOneSeed.CopyTo(playerOneData, 2);
                playerOneSeed.CopyTo(playerTwoData, 6);

                playerTwoSeed.CopyTo(playerOneData, 6);
                playerTwoSeed.CopyTo(playerTwoData, 2);

                reliableEndpoint.SendMessage(playerOneData, playerOneData.Length, QosType.Reliable);
                ServerMain.clientWaitingToMatch.Send(playerTwoData, playerTwoData.Length, QosType.Reliable);
                ServerMain.clientWaitingToMatch = null;
            }
        }
        else
        {
            Log("Forwarding! " + mType);
            if (remoteOpponent == null)
            {
                server.Disconnect(remoteClient);
            }
            else
            {
                remoteOpponent.Send(BitConverter.GetBytes((UInt16)mType), 2, QosType.Reliable);
            }
        }
    }

    public void ReliableTransmitCallback(byte[] buffer, int size)
    {
        // this will be called when a datagram is ready to be sent across the network.
        // buffer is byte[] and size is number of bytes in the buffer
        // do not keep a reference to the buffer as it will be pooled after this function returns

        try
        {
            remoteClient.SendPayload(buffer, size);
        }
        catch (System.Exception ex)
        {
            Log(ex.ToString());
            throw;
        }
    }

    public void ReceivePacket(byte[] payload, int payloadSize)
    {
        reliableEndpoint.ReceivePacket(payload, payloadSize);
    }

    public void Update()
    {
        reliableEndpoint.Update();
    }
    public void Send(byte[] messageBytes, int messageSize, QosType qosType)
    {
        reliableEndpoint.SendMessage(messageBytes, messageSize, qosType);
    }
}