using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class GameController : MonoBehaviour
{
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

        NetworkClient.instance.OnMessageReceived += HandleNetworkMessage;
        NetworkClient.instance.JoinAndStartLocalDevServer();
    }

    void OnPlayerLost()
    {
        currentState = GameState.NotPlaying;
    }

    // Update is called once per frame
    void Update()
    {

        if (currentState == GameState.InProgress)
        {
            swipeInput.Update();
            if (swipeInput.swipedLeft || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                playerBoard1.MoveLeft();
                updateDisplay(playerBoard1, 0);
                NetworkClient.instance.SendMove(MoveType.Left);
            }
            if (swipeInput.swipedRight || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                playerBoard1.MoveRight();
                updateDisplay(playerBoard1, 0);
                NetworkClient.instance.SendMove(MoveType.Right);
            }

            if (swipeInput.swipedDown || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                lastTick = Time.time;
                playerBoard1.GameTick();
                updateDisplay(playerBoard1, 0);
                NetworkClient.instance.SendMove(MoveType.Down);
            }

            if (swipeInput.swipedUp || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                playerBoard1.Rotate();
                updateDisplay(playerBoard1, 0);
                NetworkClient.instance.SendMove(MoveType.Rotate);
            }

            if (lastTick + (1 / TicksPerSecond) < Time.time)
            {
                lastTick = Time.time;

                playerBoard1.GameTick();

                updateDisplay(playerBoard1, 0);
                NetworkClient.instance.SendMove(MoveType.Down);
            }
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
