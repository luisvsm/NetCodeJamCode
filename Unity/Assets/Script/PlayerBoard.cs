using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerBoard
{
    private int[,] board;
    private int[,] boardCache;
    public List<GamePiece> pieces = new List<GamePiece>();
    public List<GamePiece> piecesToAddToBoard = new List<GamePiece>();
    private int boardWidth, boardHeight;
    int[][] blockCache = null;

    public void PlacePiece()
    {
        System.Random random = new System.Random();

        pieces.Add(new GamePiece((GamePiece.Type)random.Next(1, 3), boardWidth, boardHeight));
    }

    public PlayerBoard(int width, int height)
    {
        boardWidth = width;
        boardHeight = height;

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

        if (piecesToAddToBoard.Count > 0)
        {
            piecesToAddToBoard.Clear();
            PlacePiece();
        }
    }

    private void AddToBoard(GamePiece piece)
    {
        blockCache = piece.GetBlocks();
        for (int i = 0; i < blockCache.Length; i++)
        {
            board[blockCache[i][0], blockCache[i][1]] = (int)piece.pieceType;
        }
    }

    public void MoveLeft()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.MoveLeft(board);
        }
    }
    public void MoveRight()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.MoveRight(board);
        }
    }
    public void Rotate()
    {
        foreach (GamePiece piece in pieces)
        {
            piece.Rotate(board);
        }
    }
}
