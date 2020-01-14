using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public PlayerBoard playerBoard1;
    public PlayerBoard playerBoard2;
    public List<List<GameObject>> GameBoard = new List<List<GameObject>>();
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
            GameBoard.Add(new List<GameObject>());
            for (int j = 0; j < boardHeight * 2; j++)
            {
                GameBoard[i].Add(Instantiate(BaseBlock));
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
    }

    // Update is called once per frame
    void Update()
    {
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
        Debug.Log("updateDisplay");
        for (int i = 0; i < playerBoard.board.Count; i++)
        {
            for (int j = 0; j < playerBoard.board[i].Count; j++)
            {
                if (playerBoard.board[i][j] == 0)
                {
                    Debug.Log("i: " + i + " j: " + j);
                    GameBoard[i][j + heightOffset].SetActive(false);
                }
                else
                {
                    GameBoard[i][j + heightOffset].SetActive(true);
                }
            }
        }
    }
}
