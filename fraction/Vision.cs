using System;

namespace fraction;

/// <summary>
/// kann für jedes Piece generiert werden, enthält alle pseudolegalen Moves die dieses Piece machen kann als BB
/// </summary>
public struct Vision {
    public int PosIndex { get; init; }
    public BitBoard MoveBB { get; set; }
    public Piece PieceType { get; init; }

    public Vision(int i, BitBoard m, Piece piece) {
        PosIndex = i;
        MoveBB = m;
        PieceType = piece;
    }

    public void PrintBB() {
        Utility.PrintBitBoard(MoveBB, PosIndex);
    }

    public static void PrintMovesArr(Span<Vision> moves) {
        foreach (Vision m in moves) {
            m.PrintBB();
        }
    }
}
