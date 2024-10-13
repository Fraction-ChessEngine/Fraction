using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using fraction;

namespace fraction;
static class Eval
{
    //31 mio iterations /second, dh kein bottleneck
    public static float BasicStaticEval(Chessboard b)
    {
        float white = 0;
        white += NumberOfSetBits(b.WKingBB) * 10000f;
        white += NumberOfSetBits(b.WRookBB) * 5f;
        white += NumberOfSetBits(b.WBishopBB) * 3f;
        white += NumberOfSetBits(b.WKnightBB) * 2.8f;
        white += NumberOfSetBits(b.WQueenBB) * 9f;
        white += NumberOfSetBits(b.WPawnBB);
        white += RelativeValue(b.WPawnBB, Piece.wPawn);
        white += RelativeValue(b.WRookBB, Piece.wRook);
        white += RelativeValue(b.WBishopBB, Piece.wBishop);
        white += RelativeValue(b.WKingBB, Piece.wKing);
        white += RelativeValue(b.WKnightBB, Piece.wKnight);
        white += RelativeValue(b.WQueenBB, Piece.wQueen);

        float black = 0;
        black += NumberOfSetBits(b.BKingBB) * 10000f;
        black += NumberOfSetBits(b.BRookBB) * 5f;
        black += NumberOfSetBits(b.BBishopBB) * 3f;
        black += NumberOfSetBits(b.BKnightBB) * 2.8f;
        black += NumberOfSetBits(b.BQueenBB) * 9f;
        black += NumberOfSetBits(b.BPawnBB);
        black += RelativeValue(b.BPawnBB, Piece.bPawn);
        black += RelativeValue(b.BRookBB, Piece.bRook);
        black += RelativeValue(b.BBishopBB, Piece.bBishop);
        black += RelativeValue(b.BKingBB, Piece.bKing);
        black += RelativeValue(b.BKnightBB, Piece.bKnight);
        black += RelativeValue(b.BQueenBB, Piece.bQueen);

        return white - black;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumberOfSetBits(ulong i)
    {
        return (int)System.Runtime.Intrinsics.X86.Popcnt.X64.PopCount(i);
    }

    /*private static Dictionary<Piece, ulong> pieceMasks1 = new Dictionary<Piece, ulong>{
                    {Piece.wPawn,0b1111111111111111111111111111111100000000000000000000000000000000},
                    {Piece.wBishop,0b0000000000011000001111000011110000111100001111000001100000000000},
                    {Piece.wKnight,0b0000000000011000001111000011110000111100001111000001100000000000},
                    {Piece.wRook,  0b0000000011111111000000000000000000000000000000000000000000000000},
                    {Piece.wKing,  0b0000000000000000000000000000000000000000000000000000000011100111},
                    {Piece.wQueen,  0b1111111100000000000000000000000000000000000000000011110000000000},
                    {Piece.bPawn,0b11111111111111111111111111111111},
                    {Piece.bBishop,0b0000000000011000001111000011110000111100001111000001100000000000},
                    {Piece.bKnight,0b0000000000011000001111000011110000111100001111000001100000000000},
                    {Piece.bRook,  0b0000000000000000000000000000000000000000000000001111111100000000},
                    {Piece.bKing,  0b1110011100000000000000000000000000000000000000000000000000000000},
                    {Piece.bQueen,  0b0000000000111100000000000000000000000000000000000000000011111111},
            };*/
    private static ulong[] pieceMasks1 = new ulong[]
    {
            0b1111111111111111111111111111111100000000000000000000000000000000,
            0b0000000000011000001111000011110000111100001111000001100000000000,
            0b0000000000011000001111000011110000111100001111000001100000000000,
            0b0000000011111111000000000000000000000000000000000000000000000000,
            0b0000000000000000000000000000000000000000000000000000000011100111,
            0b1111111100000000000000000000000000000000000000000011110000000000,
            0,// padding, think twice before removing!
            0,// padding, think twice before removing!
            0b11111111111111111111111111111111,
            0b0000000000011000001111000011110000111100001111000001100000000000,
            0b0000000000011000001111000011110000111100001111000001100000000000,
            0b0000000000000000000000000000000000000000000000001111111100000000,
            0b1110011100000000000000000000000000000000000000000000000000000000,
            0b0000000000111100000000000000000000000000000000000000000011111111,
    };

    // trifft nur auf pawns zu, rest wird ignoriert
    private static ulong[] pieceMasks2 = new ulong[]
    {
            0b111111111111111100000000000000000000000000000000,
            0, 0, 0, 0, 0, // other pieces
            0,// padding, think twice before removing!
            0,// padding, think twice before removing!
            0b1111111111111111,
            0, 0, 0, 0, 0, // other pieces
    };

    private static float[] pieceFightValue = new float[]
    {
            1f,
            3f,
            2.7f,
            5f,
            2f,
            9f,
            0f,// padding, think twice before removing!
            0f,// padding, think twice before removing!
            1f,
            3f,
            2.7f,
            5f,
            2f,
            9f
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float RelativeValue(ulong bb, Piece type)
    {
        float value = NumberOfSetBits(bb & pieceMasks1[(int)type]) * pieceFightValue[(int)type] * 0.1f;

        // branchless durch lut
        value += NumberOfSetBits(bb & pieceMasks2[(int)type]) * pieceFightValue[(int)type] * 0.1f;

        return value;
    }
}
