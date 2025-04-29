using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using fraction;

namespace fraction;
static class Eval {
    //31 mio iterations /second, dh kein bottleneck
    public static float BasicStaticEval(Chessboard b) {
        float white = 0;
        white += b.WKingBB.PopCount * 10000f;
        white += b.WRookBB.PopCount * 5f;
        white += b.WBishopBB.PopCount * 3f;
        white += b.WKnightBB.PopCount * 2.8f;
        white += b.WQueenBB.PopCount * 9f;
        white += b.WPawnBB.PopCount;
        white += RelativeValue(b.WPawnBB, Piece.wPawn);
        white += RelativeValue(b.WRookBB, Piece.wRook);
        white += RelativeValue(b.WBishopBB, Piece.wBishop);
        white += RelativeValue(b.WKingBB, Piece.wKing);
        white += RelativeValue(b.WKnightBB, Piece.wKnight);
        white += RelativeValue(b.WQueenBB, Piece.wQueen);

        white += PawnQuality(true, b.WPawnBB, b.BPawnBB, 0.3f, 0.1f, -0.2f, -0.3f);//random ass values, need to be optimised

        float black = 0;
        black += b.BKingBB.PopCount * 10000f;
        black += b.BRookBB.PopCount * 5f;
        black += b.BBishopBB.PopCount * 3f;
        black += b.BKnightBB.PopCount * 2.8f;
        black += b.BQueenBB.PopCount * 9f;
        black += b.BPawnBB.PopCount;
        black += RelativeValue(b.BPawnBB, Piece.bPawn);
        black += RelativeValue(b.BRookBB, Piece.bRook);
        black += RelativeValue(b.BBishopBB, Piece.bBishop);
        black += RelativeValue(b.BKingBB, Piece.bKing);
        black += RelativeValue(b.BKnightBB, Piece.bKnight);
        black += RelativeValue(b.BQueenBB, Piece.bQueen);

        black += PawnQuality(false, b.BPawnBB, b.WPawnBB, 0.3f, 0.1f, -0.2f, -0.3f);//random ass values, need to be optimised

        return white - black;
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
    static float RelativeValue(BitBoard bb, Piece type) {
        float value = (bb & pieceMasks1[(int)type]).PopCount * pieceFightValue[(int)type] * 0.1f;

        // branchless durch lut
        value += (bb & pieceMasks2[(int)type]).PopCount * pieceFightValue[(int)type] * 0.1f;

        return value;
    }



    //kann theoretisch mit BB_Lookup.getpawnattacksqrs verbessert werden
    public static float PawnQuality(bool forWhite, BitBoard pawnBB, BitBoard enemyPawnBB,
    float passedPWeight, float pChainWeight, float doubledPWeight, float isolatedPWeight) {
        /* 
        gut: connected pawns, passed pawns
        schlecht: isoliert, doubled
        */
        float qualityNum = 0;
        int pawnCount = pawnBB.PopCount;
        Span<int> pawnArr = stackalloc int[pawnCount];
        _ = pawnBB.FindOnes(pawnArr);

        int passedPawnCount = 0;
        int pawnChain = 0;
        int doubledPawns = 0;
        int isolatedPawns = 0;

        for (int i = 0; i < pawnCount; i++) {
            int currP = pawnArr[i];
            int x = currP % 8;
            int y = currP >> 3;

            BitBoard passedPawnMask;
            if (forWhite) {
                if (x == 0) {
                    passedPawnMask = BitBoard.VerticalLines(0b11) << (currP + 8);
                    if (((1ul << (currP + 9)) & pawnBB) != 0) pawnChain++;
                    if ((BitBoard.VerticalLine(1) & pawnBB) == 0) isolatedPawns++;

                } else if (x == 7) {
                    passedPawnMask = BitBoard.VerticalLines(0b11) << (currP - 1 + 8);
                    if (((1ul << (currP + 7)) & pawnBB) != 0) pawnChain++;
                    if ((BitBoard.VerticalLine(6) & pawnBB) == 0) isolatedPawns++;

                } else {
                    passedPawnMask = BitBoard.VerticalLines(0b111) << (currP - 1 + 8);
                    if (((1ul << (currP + 7)) & pawnBB) != 0) pawnChain++;
                    if (((1ul << (currP + 9)) & pawnBB) != 0) pawnChain++;
                    if ((BitBoard.VerticalLines(0b101) << (x - 1) & pawnBB) == 0) isolatedPawns++;
                }
            } else {
                if (x == 0) {
                    passedPawnMask = BitBoard.VerticalLines(0b11) & ((1ul << (y * 8)) - 1);
                    if (((1ul << (currP - 7)) & pawnBB) != 0) pawnChain++;
                    if ((BitBoard.VerticalLine(1) & pawnBB) == 0) isolatedPawns++;

                } else if (x == 7) {
                    passedPawnMask = BitBoard.VerticalLines(0b11) << 6 & ((1ul << (y * 8)) - 1);
                    if (((1ul << (currP - 9)) & pawnBB) != 0) pawnChain++;
                    if ((BitBoard.VerticalLine(6) & pawnBB) == 0) isolatedPawns++;

                } else {
                    passedPawnMask = BitBoard.VerticalLines(0b111) << (x - 1) & ((1ul << (y * 8)) - 1);
                    if (((1ul << (currP - 9)) & pawnBB) != 0) pawnChain++;
                    if (((1ul << (currP - 7)) & pawnBB) != 0) pawnChain++;
                    if ((BitBoard.VerticalLines(0b101) << (x - 1) & pawnBB) == 0) isolatedPawns++;
                }
            }

            if ((passedPawnMask & enemyPawnBB) == 0) passedPawnCount++;
            doubledPawns += (BitBoard.VerticalLine(x) & (~(1ul << currP)) & pawnBB).PopCount;
        }

        qualityNum += passedPawnCount * passedPWeight + pawnChain * pChainWeight + doubledPawns * doubledPWeight + isolatedPawns * isolatedPWeight;

        return qualityNum;
    }



    /* 
    faktoren für eval:

    Dinge die in der moveGen generiert werden und gecached werden können:
        -anzahl der kontrollierten sqrs (je zentraler desto relevanter) 
        -pins


    Dinge die hier isoliert berechnen werden müssen
        -connected pawns 
        -past pawns 
        -king safety (king ist möglichst weit weg von center, nicht auf lines von slidern)
        -isolated pawns
    
     */
}
