using ReliableNetcode;
using NetcodeIO.NET;
using System;

#if UNITY_EDITOR
using UnityEngine;
#endif

public class RemoteGameClient
{
    private RemoteClient remoteClient;
    private ReliableEndpoint reliableEndpoint;


    public void Log(string log)
    {
#if UNITY_EDITOR
        Debug.Log("[Server.RemoteGameClient] " + log);
#endif
    }
    public void LogError(object log)
    {
#if UNITY_EDITOR
        Debug.LogError(log);
#endif
    }

    public RemoteGameClient(RemoteClient remoteClient)
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
        Log("Messge! " + BitConverter.ToUInt16(buffer, 0));
    }

    public void ReliableTransmitCallback(byte[] buffer, int size)
    {
        // this will be called when a datagram is ready to be sent across the network.
        // buffer is byte[] and size is number of bytes in the buffer
        // do not keep a reference to the buffer as it will be pooled after this function returns
        remoteClient.SendPayload(buffer, size);
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