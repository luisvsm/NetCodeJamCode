using System.Collections;
using System.Collections.Generic;

public class PlayerBoard
{
    public List<List<int>> board = new List<List<int>>();

    public PlayerBoard(int width, int height)
    {
        for (int i = 0; i < width; i++)
        {
            board.Add(new List<int>());
            for (int j = 0; j < height; j++)
            {
                board[i].Add(0);
            }
        }
    }

    // A game tick is each time blocks drop down 1 row
    public void GameTick()
    {

    }
}
