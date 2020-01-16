
public class GamePiece
{
    public enum Type
    {
        I = 1,
        O = 2,
        T = 3,
        S = 4,
        Z = 5,
        J = 6,
        L = 7
    }
    public Type pieceType;

    int[][][] OffsetData;
    int rotation = 0;
    int positionX, positionY;
    int width = 0, height = 0;
    int[][] blockCache;
    bool invalidateCache = true;

    public GamePiece(int boardWidth, int boardHeight) : this(Type.I, boardWidth, boardHeight)
    {
    }

    public GamePiece(Type type, int boardWidth, int boardHeight)
    {
        height = boardHeight;
        width = boardWidth;
        pieceType = type;
        positionY = 0;
        positionX = boardWidth / 2;
        OffsetData = GetPieceOffsets(type);
        blockCache = new int[OffsetData[rotation].Length][];

        for (int i = 0; i < blockCache.Length; i++)
        {
            blockCache[i] = new int[2];
        }
    }

    private void clearBlockCache()
    {
        invalidateCache = true;
    }

    public int[][] GetBlocks()
    {
        if (invalidateCache)
        {
            invalidateCache = false;

            for (int i = 0; i < OffsetData[rotation].Length; i++)
            {
                blockCache[i][0] = OffsetData[rotation][i][0] + positionX;
                blockCache[i][1] = OffsetData[rotation][i][1] + positionY;
            }
        }

        return blockCache;
    }

    public void Rotate(int[,] board)
    {
        int rollbackRotation = rotation;
        rotation = (rotation + 1) % OffsetData.Length;
        clearBlockCache();
        GetBlocks();

        for (int i = 0; i < blockCache.Length; i++)
        {
            if (
                blockCache[i][0] < 0 ||
                blockCache[i][0] >= width ||
                blockCache[i][1] >= height ||
                (
                    blockCache[i][1] >= 0 &&
                    board[blockCache[i][0], blockCache[i][1]] != 0
                )
            )
            {
                clearBlockCache();
                rotation = rollbackRotation;
                break;
            }
        }
    }

    public void MoveDown()
    {
        positionY = positionY + 1;
        clearBlockCache();
    }

    public void MoveUp()
    {
        positionY = positionY - 1;
        clearBlockCache();
    }

    public void MoveLeft(int[,] board)
    {
        positionX = positionX + 1;
        clearBlockCache();
        GetBlocks();

        for (int i = 0; i < blockCache.Length; i++)
        {
            if (blockCache[i][0] >= width || board[blockCache[i][0], blockCache[i][1]] != 0)
            {
                clearBlockCache();
                positionX = positionX - 1;
                break;
            }
        }
    }
    public void MoveRight(int[,] board)
    {
        positionX = positionX - 1;
        clearBlockCache();
        GetBlocks();

        for (int i = 0; i < blockCache.Length; i++)
        {
            if (blockCache[i][0] < 0 || board[blockCache[i][0], blockCache[i][1]] != 0)
            {
                clearBlockCache();
                positionX = positionX + 1;
                break;
            }
        }

    }

    public int[][][] GetPieceOffsets(Type type)
    {
        if (type == Type.I)
        {
            return new int[][][] {
                new int[][] {
                    new int[] { 1, 0},
                    new int[] { 0, 0},
                    new int[] { -1, 0},
                    new int[] { -2, 0}
                },
                new int[][] {
                    new int[] { 0, 1 },
                    new int[] { 0, 0 },
                    new int[] { 0, -1 },
                    new int[] { 0, -2 }
                }
            };
        }
        else if (type == Type.O)
        {
            return new int[][][] {
                new int[][] {
                   new int[] { 0, 0 },
                   new int[] { 0, 1 },
                   new int[] { 1, 1 },
                   new int[] { 1, 0 }
                }
            };
        }/*
        else if (type == Type.T)
        {
            return new int[][,] {
                new int[,] {
                    { -1, 0 },
                    { 0, 0 },
                    { 1, 0 },
                    { 0, 1 }
                },
                new int[,] {
                    { 0, -1 },
                    { 0, 0 },
                    { 1, 0 },
                    { 0, 1 }
                },
                new int[,] {
                    { -1, 0 },
                    { 0, 0 },
                    { 1, 0 },
                    { 0, -1 }
                },
                new int[,] {
                    { 0, -1 },
                    { 0, 0 },
                    { -1, 0 },
                    { 0, 1 }
                }
            };
        }
        else if (type == Type.S)
        {
            return new int[][,] {
                new int[,] {
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 }
                }
            };
        }
        else if (type == Type.Z)
        {
            return new int[][,] {
                new int[,] {
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 }
                }
            };
        }
        else if (type == Type.J)
        {
            return new int[][,] {
                new int[,] {
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 }
                }
            };
        }
        else if (type == Type.L)
        {
            return new int[][,] {
                new int[,] {
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 },
                    { 0, 0 }
                }
            };
        }*/
        else
        {
            return new int[][][] { };
        }
    }
}
