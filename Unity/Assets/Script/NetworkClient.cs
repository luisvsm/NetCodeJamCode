using UnityEngine;
using System;

using NetcodeIO.NET;
using ReliableNetcode;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    // The message queue is pooled
    // Memory required is max size * queue length in bytes
    public const int MaxMessageSize = 200;
    public const int MessageQueueLength = 10;
    private int bufferPos = 0;
    private QueuedBuffer[] bufferQueue = new QueuedBuffer[MessageQueueLength];
    private Queue<QueuedBuffer> readQueue = new Queue<QueuedBuffer>();

    // Internal variables
    private ReliableEndpoint reliableEndpoint;
    private Client client;
    private bool hasInit;

    public delegate void OnNetworkMessageDelegate(NetworkMessage.MessageType type, byte[] buffer, int size);
    public event OnNetworkMessageDelegate OnMessageReceived;

    // Singleton structure, requires a NetworkClient in the scene to work
    private static NetworkClient _instance;
    public static NetworkClient instance
    {
        get
        {
            // Find the NetworkClient instance and call init
            if (_instance == null)
            {
                _instance = GameObject.Find("NetworkClient").GetComponent<NetworkClient>();
                if (_instance == null)
                {
                    Debug.LogError("Can't find NetworkClient GameObject. Please check that it exists in the scene.");
                }
                _instance.Init();
            }

            return _instance;
        }
    }

    // Init function, called exactly once
    public void Init()
    {
        // Set up NetcodeIO.NET client object and callbacks
        client = new Client();
        client.OnStateChanged += ClientStateChanged;
        client.OnMessageReceived += MessageReceivedHandler;

        for (int i = 0; i < bufferQueue.Length; i++)
        {
            bufferQueue[i] = new QueuedBuffer();
        }

        hasInit = true;
    }

    public void JoinAndStartLocalDevServer()
    {
        LocalServer.instance.StartServer();

        client.Connect(
            // byte[2048] connect token as returned by a TokenFactory
            LocalServer.instance.GetLocalHostToken()
        );
    }
    public void JoinOnlineServer(string connectToken)
    {
        byte[] connectTokenBytes = Convert.FromBase64String(connectToken);

        Debug.Log("connectTokenBytes: " + connectTokenBytes.Length);
        client.Connect(connectTokenBytes);
    }
    public void Disconnect()
    {
        LocalServer.instance.StopServer();
        client.Disconnect();
    }

    void ClientStateChanged(ClientState state)
    {
        Debug.Log("clientStateChanged: " + state.ToString());
        if (state == ClientState.Connected)
        {
            //Set up ReliableNetcode endpoint object and callbacks
            reliableEndpoint = new ReliableEndpoint();
            reliableEndpoint.ReceiveCallback += ReliableReceiveCallback;
            reliableEndpoint.TransmitCallback += ReliableTransmitCallback;

            SendFindGame();
        }
        else
        {
            reliableEndpoint = null;
        }
    }

    void MessageReceivedHandler(byte[] payload, int payloadSize)
    {
        // when you receive a datagram, pass the byte array and the number of bytes to ReceivePacket
        // this will extract messages from the datagram and call your custom ReceiveCallback with any received messages.
        reliableEndpoint.ReceivePacket(payload, payloadSize);
    }

    void ReliableReceiveCallback(byte[] buffer, int size)
    {
        // this will be called when the endpoint extracts messages from received packets
        // buffer is byte[] and size is number of bytes in the buffer.
        // do not keep a reference to buffer as it will be pooled after this function returns
        try
        {
            if (size > MaxMessageSize)
            {
                Debug.LogError("Packet was too large, dropping.");
                return;
            }

            bufferPos = (bufferPos + 1) % bufferQueue.Length;

            buffer.CopyTo(bufferQueue[bufferPos].buffer, 0);
            bufferQueue[bufferPos].size = size;

            readQueue.Enqueue(bufferQueue[bufferPos]);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            throw;
        }
    }

    void ReliableTransmitCallback(byte[] buffer, int size)
    {
        if (client.State != ClientState.Connected)
        {
            Debug.Log("Not sending packet, client state invalid: " + client.State.ToString());
            return;
        }
        // this will be called when a datagram is ready to be sent across the network.
        // buffer is byte[] and size is number of bytes in the buffer
        // do not keep a reference to the buffer as it will be pooled after this function returns
        try
        {
            client.Send(buffer, size);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            throw;
        }
    }

    void Update()
    {
        if (reliableEndpoint != null)
            // Update internal buffers
            reliableEndpoint.Update();

        if (readQueue.Count > 0)
        {
            QueuedBuffer queuedBuffer = readQueue.Dequeue();
            if (OnMessageReceived != null)
            {
                OnMessageReceived(
                    NetworkMessage.ParseMessage(queuedBuffer.buffer, queuedBuffer.size),
                    queuedBuffer.buffer,
                    queuedBuffer.size
                );
            }
            else
            {
                Debug.LogError("[NetworkClient.cs] OnMessageReceived is null. Ignoring received message");
            }
        }
    }

    public void SendFindGame()
    {

        reliableEndpoint.SendMessage(
            BitConverter.GetBytes((UInt16)NetworkMessage.MessageType.FindGame),
            2,
            QosType.Reliable
        );
    }

    public void SendMove(GameController.MoveType move)
    {
        if (client.State == ClientState.Connected)
        {
            byte[] data = null;
            switch (move)
            {
                case GameController.MoveType.Left:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputLeft
                    );
                    break;
                case GameController.MoveType.Right:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputRight
                    );
                    break;
                case GameController.MoveType.Down:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputDown
                    );
                    break;
                case GameController.MoveType.Rotate:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputRotate
                    );
                    break;
                default:
                    break;
            }

            if (data != null)
            {
                Debug.Log("[Client] Sending " + ((UInt16)move));
                reliableEndpoint.SendMessage(data, data.Length, QosType.Reliable);
            }
        }
    }
}