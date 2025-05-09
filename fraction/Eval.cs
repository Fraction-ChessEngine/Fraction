using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using fraction;

namespace fraction;
static class Eval
{
    //31 mio iterations /second, dh kein bottleneck
    public static Score BasicStaticEval(Chessboard b)
    {
        int white = 0;
        white += b.WKingBB.PopCount * 1_000_000;
        white += b.WRookBB.PopCount * 500;
        white += b.WBishopBB.PopCount * 300;
        white += b.WKnightBB.PopCount * 280;
        white += b.WQueenBB.PopCount * 900;
        white += b.WPawnBB.PopCount;
        white += RelativeValue(b.WPawnBB, Piece.wPawn);
        white += RelativeValue(b.WRookBB, Piece.wRook);
        white += RelativeValue(b.WBishopBB, Piece.wBishop);
        white += RelativeValue(b.WKingBB, Piece.wKing);
        white += RelativeValue(b.WKnightBB, Piece.wKnight);
        white += RelativeValue(b.WQueenBB, Piece.wQueen);

        int black = 0;
        black += b.BKingBB.PopCount * 1_000_000;
        black += b.BRookBB.PopCount * 500;
        black += b.BBishopBB.PopCount * 300;
        black += b.BKnightBB.PopCount * 280;
        black += b.BQueenBB.PopCount * 900;
        black +=b.BPawnBB.PopCount;
        black += RelativeValue(b.BPawnBB, Piece.bPawn);
        black += RelativeValue(b.BRookBB, Piece.bRook);
        black += RelativeValue(b.BBishopBB, Piece.bBishop);
        black += RelativeValue(b.BKingBB, Piece.bKing);
        black += RelativeValue(b.BKnightBB, Piece.bKnight);
        black += RelativeValue(b.BQueenBB, Piece.bQueen);

        return new Score(ScoreType.Centipawns, white - black);
    }

    /*private static Dictionary<Piece, BitBoard> pieceMasks1 = new Dictionary<Piece, BitBoard>{
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
    private static BitBoard[] pieceMasks1 = new BitBoard[]
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
    private static BitBoard[] pieceMasks2 = new BitBoard[]
    {
            0b111111111111111100000000000000000000000000000000,
            0, 0, 0, 0, 0, // other pieces
            0,// padding, think twice before removing!
            0,// padding, think twice before removing!
            0b1111111111111111,
            0, 0, 0, 0, 0, // other pieces
    };

    private static int[] pieceFightValue = new int[]
    {
            100,
            300,
            270,
            500,
            200,
            900,
            0,// padding, think twice before removing!
            0,// padding, think twice before removing!
            100,
            300,
            270,
            500,
            200,
            900
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int RelativeValue(BitBoard bb, Piece type)
    {
        int value = (bb & pieceMasks1[(int)type]).PopCount * pieceFightValue[(int)type] / 10;

        // branchless durch lut
        value += (bb & pieceMasks2[(int)type]).PopCount * pieceFightValue[(int)type] / 10;

        return value;
    }
}
