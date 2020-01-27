using System.Collections;
using System.Text;
using System.Collections.Generic;
using NetcodeIO.NET;
using ReliableNetcode;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;

public class QueuedBuffer
{
    public byte[] buffer = new byte[200];
    public int size = 0;
}

public class GameController : MonoBehaviour
{
    private byte[] serverToken;
    private ServerMain localServer;
    private Thread localServerThread;
    private ReliableEndpoint reliableEndpoint;
    private Client client;
    private SwipeInput swipeInput = new SwipeInput();
    private PlayerBoard playerBoard1;
    private PlayerBoard playerBoard2;
    private List<List<Block>> GameBoard = new List<List<Block>>();
    public GameObject BaseBlock;
    public RectTransform BaseBlockParant;
    public int blockSize = 64;
    public int boardWidth = 10, boardHeight = 14;
    public int widthOffset = -320, heightOffset = -448, middleGapHeight = 6;
    public float TicksPerSecond = 1;
    private float lastTick = 0;
    private GameState currentState = GameState.NotPlaying;
    private enum GameState
    {
        InProgress,
        WaitingForSeed,
        NotPlaying
    }
    public enum MoveType
    {
        Left = 1,
        Right = 2,
        Rotate = 3,
        Down = 4
    }

    void OnApplicationQuit()
    {
        if (localServerThread != null)
        {
            localServerThread.Abort();
            localServerThread = null;
        }
        if (localServer != null)
        {
            localServer.Stop();
            localServer = null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        playerBoard1 = new PlayerBoard(boardWidth, boardHeight, 0);
        playerBoard2 = new PlayerBoard(boardWidth, boardHeight, 0);
        lastTick = Time.time;
        for (int i = 0; i < boardWidth; i++)
        {
            GameBoard.Add(new List<Block>());
            for (int j = 0; j < boardHeight * 2; j++)
            {
                GameObject block = Instantiate(BaseBlock);
                GameBoard[i].Add(block.GetComponent<Block>());
                GameBoard[i][j].transform.SetParent(BaseBlockParant, false);
                RectTransform rectTransform = GameBoard[i][j].GetComponent<RectTransform>();

                if (j >= boardHeight)
                    // player 1
                    rectTransform.anchoredPosition = new Vector2((64 * (boardWidth - i)) + widthOffset, (64 * ((boardHeight * 2) - j)) + heightOffset);
                else
                    // player 2
                    rectTransform.anchoredPosition = new Vector2((64 * (boardWidth - i)) + widthOffset, (64 * ((boardHeight * 2) - j)) + heightOffset + middleGapHeight);
            }
        }

        playerBoard1.OnLoseEvent += OnPlayerLost;

        client = new Client();
        // Called when the client's state has changed
        // Use this to detect when a client has connected to a server, or has been disconnected from a server, or connection times out, etc.
        client.OnStateChanged += clientStateChanged;            // void( ClientState state )

        // Called when a payload has been received from the server
        // Note that you should not keep a reference to the payload, as it will be returned to a pool after this call completes.
        client.OnMessageReceived += messageReceivedHandler;     // void( byte[] payload, int payloadSize )

        MakeLocalHostWork();
    }

    void OnPlayerLost()
    {
        currentState = GameState.NotPlaying;
    }

    void startLocalServer()
    {
        try
        {
            localServer.Start("local");
        }
        catch (ThreadAbortException)
        {
            Debug.Log("Aborted local server");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            //handle
        }
    }

    void SendMove(MoveType move)
    {
        if (client.State == ClientState.Connected)
        {
            byte[] data = null;
            switch (move)
            {
                case MoveType.Left:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputLeft
                    );
                    break;
                case MoveType.Right:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputRight
                    );
                    break;
                case MoveType.Down:
                    data = BitConverter.GetBytes(
                        (UInt16)NetworkMessage.MessageType.InputDown
                    );
                    break;
                case MoveType.Rotate:
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
    void MakeLocalHostWork()
    {
        if (localServer == null)
        {
            localServer = new ServerMain();
            localServerThread = new Thread(new ThreadStart(startLocalServer));

            reliableEndpoint = new ReliableEndpoint();
            reliableEndpoint.ReceiveCallback += ReliableReceiveCallback;
            reliableEndpoint.TransmitCallback += ReliableTransmitCallback;

            localServerThread.Start();
            Debug.Log("Started Server.");
        }


        StartCoroutine(WaitAndJoin());

    }

    IEnumerator WaitAndJoin()
    {
        yield return new WaitForSeconds(1);

        serverToken = localServer.GetLocalHostToken();
        client.Connect(serverToken);        // byte[2048] public connect token as returned by a TokenFactory
    }

    // Update is called once per frame
    void Update()
    {
        // update internal buffers
        if (reliableEndpoint != null)
            reliableEndpoint.Update();

        if (currentState == GameState.InProgress)
        {
            swipeInput.Update();
            if (swipeInput.swipedLeft || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                playerBoard1.MoveLeft();
                updateDisplay(playerBoard1, 0);
                SendMove(MoveType.Left);
            }
            if (swipeInput.swipedRight || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                playerBoard1.MoveRight();
                updateDisplay(playerBoard1, 0);
                SendMove(MoveType.Right);
            }

            if (swipeInput.swipedDown || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                lastTick = Time.time;
                playerBoard1.GameTick();
                updateDisplay(playerBoard1, 0);
                SendMove(MoveType.Down);
            }

            if (swipeInput.swipedUp || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                playerBoard1.Rotate();
                updateDisplay(playerBoard1, 0);
                SendMove(MoveType.Rotate);
            }

            if (lastTick + (1 / TicksPerSecond) < Time.time)
            {
                lastTick = Time.time;

                playerBoard1.GameTick();

                updateDisplay(playerBoard1, 0);

                SendMove(MoveType.Down);
            }
        }

        if (readQueue.Count > 0)
        {
            QueuedBuffer queuedBuffer = readQueue.Dequeue();

            HandleNetworkMessage(
                NetworkMessage.ParseMessage(queuedBuffer.buffer, queuedBuffer.size),
                queuedBuffer.buffer,
                queuedBuffer.size
            );
        }
    }

    void HandleNetworkMessage(NetworkMessage.MessageType type, byte[] buffer, int size)
    {
        Debug.Log("[client] HandleNetworkMessage " + type);
        switch (type)
        {
            case NetworkMessage.MessageType.PlayerJoined:
                break;
            case NetworkMessage.MessageType.PlayerLeft:
                break;
            case NetworkMessage.MessageType.PlayerSeeds:
                currentState = GameState.InProgress;
                int[] seeds = NetworkMessage.ParseSeedData(buffer, size);
                Debug.Log("Seeds: " + seeds[0] + "," + seeds[1]);
                playerBoard1.Reset(seeds[0]);
                playerBoard2.Reset(seeds[0]);

                playerBoard1.PlacePiece();
                playerBoard2.PlacePiece();

                updateDisplay(playerBoard1, 0);
                updateDisplay(playerBoard2, boardHeight);
                break;
            case NetworkMessage.MessageType.InputLeft:
                playerBoard2.MoveLeft();
                updateDisplay(playerBoard2, boardHeight);
                break;
            case NetworkMessage.MessageType.InputRight:
                playerBoard2.MoveRight();
                updateDisplay(playerBoard2, boardHeight);
                break;
            case NetworkMessage.MessageType.InputRotate:
                playerBoard2.Rotate();
                updateDisplay(playerBoard2, boardHeight);
                break;
            case NetworkMessage.MessageType.InputDown:
                playerBoard2.GameTick();
                updateDisplay(playerBoard2, boardHeight);
                break;
            default:
                break;
        }
    }

    void clientStateChanged(ClientState state)
    {
        Debug.Log("clientStateChanged: " + state.ToString());
        if (state == ClientState.Connected)
        {

            reliableEndpoint.SendMessage(
                BitConverter.GetBytes((UInt16)NetworkMessage.MessageType.FindGame),
                2,
                QosType.Reliable
            );
        }
    }

    void messageReceivedHandler(byte[] payload, int payloadSize)
    {
        //Debug.Log("[Client] messageReceivedHandler");
        // when you receive a datagram, pass the byte array and the number of bytes to ReceivePacket
        // this will extract messages from the datagram and call your custom ReceiveCallback with any received messages.
        try
        {
            reliableEndpoint.ReceivePacket(payload, payloadSize);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            throw;
        }
    }
    QueuedBuffer[] bufferQueue = new QueuedBuffer[10];
    int bufferPos = 0;

    Queue<QueuedBuffer> readQueue = new Queue<QueuedBuffer>();


    void ReliableReceiveCallback(byte[] buffer, int size)
    {
        //Debug.Log("[Client] ReliableReceiveCallback");
        // this will be called when the endpoint extracts messages from received packets
        // buffer is byte[] and size is number of bytes in the buffer.
        // do not keep a reference to buffer as it will be pooled after this function returns
        try
        {
            if (size > 200)
            {
                Debug.LogError("Packet was too large, dropping.");
                return;
            }

            bufferPos = (bufferPos + 1) % bufferQueue.Length;
            if (bufferQueue[bufferPos] == null)
            {
                bufferQueue[bufferPos] = new QueuedBuffer();
            }

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

    void updateDisplay(PlayerBoard playerBoard, int heightOffset)
    {
        int[,] board = playerBoard.GetBoard();

        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                if (i < 0 || i > boardWidth || j < 0 || j > boardHeight)
                    continue;

                GameBoard[i][j + heightOffset].SetBlockType(board[i, j]);
            }
        }
    }
}
