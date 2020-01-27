using System.Collections;
using System.Text;
using System.Collections.Generic;
using NetcodeIO.NET;
using ReliableNetcode;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;

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
        playerBoard1 = new PlayerBoard(boardWidth, boardHeight);
        playerBoard2 = new PlayerBoard(boardWidth, boardHeight);
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
        playerBoard1.PlacePiece();


        client = new Client();
        // Called when the client's state has changed
        // Use this to detect when a client has connected to a server, or has been disconnected from a server, or connection times out, etc.
        client.OnStateChanged += clientStateChanged;            // void( ClientState state )

        // Called when a payload has been received from the server
        // Note that you should not keep a reference to the payload, as it will be returned to a pool after this call completes.
        client.OnMessageReceived += messageReceivedHandler;		// void( byte[] payload, int payloadSize )
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
            byte[] data = BitConverter.GetBytes((UInt16)move);
            Debug.Log("[Client] Sending " + ((UInt16)move));
            reliableEndpoint.SendMessage(data, data.Length, QosType.Reliable);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // update internal buffers
        if (reliableEndpoint != null)
            reliableEndpoint.Update();

        if (Input.GetKeyDown(KeyCode.L))
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
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            serverToken = localServer.GetLocalHostToken();
            client.Connect(serverToken);		// byte[2048] public connect token as returned by a TokenFactory
        }

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
            playerBoard2.GameTick();

            updateDisplay(playerBoard1, 0);
            updateDisplay(playerBoard2, boardHeight);

            SendMove(MoveType.Down);
        }
    }

    void clientStateChanged(ClientState state)
    {
        Debug.Log("clientStateChanged: " + state.ToString());
    }

    void messageReceivedHandler(byte[] payload, int payloadSize)
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
        Debug.Log("Got buffer! " + Encoding.UTF8.GetString(buffer, 0, size));
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
        client.Send(buffer, size);
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
