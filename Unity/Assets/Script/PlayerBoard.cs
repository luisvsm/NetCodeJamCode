using System.Collections.Generic;

public class PlayerBoard
{
    private int[,] board;
    private int[,] boardCache;
    public List<GamePiece> pieces = new List<GamePiece>();
    public List<GamePiece> piecesToAddToBoard = new List<GamePiece>();
    private int boardWidth, boardHeight;
    int[][] blockCache = null;

    public void Reset()
    {
        board = new int[boardWidth, boardHeight];
        boardCache = new int[boardWidth, boardHeight];
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                board[i, j] = 0;

            }
        }
    }

    public void PlacePiece()
    {
        System.Random random = new System.Random();

        pieces.Add(new GamePiece((GamePiece.Type)random.Next(1, 8), boardWidth, boardHeight));


        int[][] blocks = pieces[pieces.Count - 1].GetBlocks();

        for (int i = 0; i < blocks.Length; i++)
        {
            if (GetBlock(blocks[i][0], blocks[i][1]) > 0)
            {
                Reset();
                return;
            }
        }
    }

    public PlayerBoard(int width, int height)
    {
        boardWidth = width;
        boardHeight = height;
        Reset();
    }

    public void SetBlock(GamePiece.Type type, int x, int y)
    {
        if (OutOfBounds(x, y))
        {
            return;
        }
        else
        {
            board[x, y] = (int)type;
        }
    }
    public int GetBlock(int x, int y)
    {
        if (OutOfBounds(x, y))
        {
            return -1;
        }
        else
        {
            return board[x, y];
        }
    }
    public int GetBlockCache(int x, int y)
    {
        if (OutOfBounds(x, y))
        {
            return -1;
        }
        else
        {
            return boardCache[x, y];
        }
    }
    public void SetBlockCache(GamePiece.Type type, int x, int y)
    {
        if (OutOfBounds(x, y))
        {
            return;
        }
        else
        {
            boardCache[x, y] = (int)type;
        }
    }

    public int[,] GetBoard()
    {
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = 0; j < boardHeight; j++)
            {
                boardCache[i, j] = board[i, j];
            }
        }

        foreach (GamePiece piece in pieces)
        {
            blockCache = piece.GetBlocks();
            for (int i = 0; i < blockCache.Length; i++)
            {
                if (OutOfBounds(blockCache[i][0], blockCache[i][1]))
                    continue;

                SetBlockCache(piece.pieceType, blockCache[i][0], blockCache[i][1]);
            }
        }

        return boardCache;
    }

    private bool OutOfBounds(int x, int y)
    {
        return x < 0 ||
                x >= boardWidth ||
                y < 0 ||
                y >= boardHeight;
    }

    // A game tick is each time blocks drop down 1 row
    public void GameTick()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.MoveDown();
        }

        foreach (GamePiece piece in pieces)
        {
            blockCache = piece.GetBlocks();
            for (int i = 0; i < blockCache.Length; i++)
            {
                if (
                    blockCache[i][1] == boardHeight ||
                    GetBlock(blockCache[i][0], blockCache[i][1]) > 0
                )
                {
                    piece.MoveUp();
                    piecesToAddToBoard.Add(piece);
                    i = blockCache.Length;
                }
            }
        }

        foreach (GamePiece piece in piecesToAddToBoard)
        {
            AddToBoard(piece);
            pieces.Remove(piece);
        }

        bool clearLine;

        for (int j = 0; j < boardHeight; j++)
        {
            clearLine = true;

            for (int i = 0; i < boardWidth; i++)
            {

                if (board[i, j] < 1)
                {
                    clearLine = false;
                    break;
                }

            }

            if (clearLine)
            {
                ClearLine(j);
            }
        }

        if (piecesToAddToBoard.Count > 0)
        {
            piecesToAddToBoard.Clear();
            PlacePiece();
        }
    }
    private void ClearLine(int line)
    {
        for (int i = 0; i < boardWidth; i++)
        {
            for (int j = line; j > 0; j--)
            {
                board[i, j] = board[i, j - 1];
            }
        }
    }
    private void AddToBoard(GamePiece piece)
    {
        blockCache = piece.GetBlocks();
        for (int i = 0; i < blockCache.Length; i++)
        {
            SetBlock(piece.pieceType, blockCache[i][0], blockCache[i][1]);
        }
    }

    public void MoveLeft()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.MoveLeft(this);
        }
    }
    public void MoveRight()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.MoveRight(this);
        }
    }
    public void Rotate()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.Rotate(this);
        }
    }
}
