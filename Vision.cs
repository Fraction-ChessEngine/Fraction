using System;

namespace fraction;

/// <summary>
/// kann für jedes Piece generiert werden, enthält alle pseudolegalen Moves die dieses Piece machen kann als BB
/// </summary>
public class Vision
{
    public int PosIndex, setBits;
    public ulong MoveBB;
    public Piece pieceType;

    public Vision(int i, ulong m, Piece piece)
    {
        PosIndex = i;
        MoveBB = m;
        pieceType = piece;
        setBits = Eval.NumberOfSetBits(MoveBB);
    }

    public void PrintBB()
    {
        Utility.PrintBitBoard(MoveBB, PosIndex);
    }

    public static void PrintMovesArr(Span<Vision> moves)
    {
        foreach (Vision m in moves)
        {
            m.PrintBB();
        }
    }
}
