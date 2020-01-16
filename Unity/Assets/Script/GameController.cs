using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public SwipeInput swipeInput = new SwipeInput();
    public PlayerBoard playerBoard1;
    public PlayerBoard playerBoard2;
    public List<List<Block>> GameBoard = new List<List<Block>>();
    public GameObject BaseBlock;
    public RectTransform BaseBlockParant;
    public int blockSize = 64;
    public int boardWidth = 10, boardHeight = 14;
    public int widthOffset = -320, heightOffset = -448, middleGapHeight = 6;
    public float TicksPerSecond = 1;
    private float lastTick = 0;
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
    }

    // Update is called once per frame
    void Update()
    {
        swipeInput.Update();
        if (swipeInput.swipedLeft || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            playerBoard1.MoveLeft();
            updateDisplay(playerBoard1, 0);
        }
        if (swipeInput.swipedRight || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            playerBoard1.MoveRight();
            updateDisplay(playerBoard1, 0);
        }

        if (swipeInput.swipedDown || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            lastTick = Time.time;
            playerBoard1.GameTick();
            updateDisplay(playerBoard1, 0);
        }

        if (swipeInput.swipedUp || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            playerBoard1.Rotate();
            updateDisplay(playerBoard1, 0);
        }

        if (lastTick + (1 / TicksPerSecond) < Time.time)
        {
            lastTick = Time.time;

            playerBoard1.GameTick();
            playerBoard2.GameTick();

            updateDisplay(playerBoard1, 0);
            updateDisplay(playerBoard2, boardHeight);
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
