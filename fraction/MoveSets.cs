using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace fraction;
public static class MoveSets
{
    public static ulong getPseudoLegalMoves_bb(
        Chessboard board,
        int posIndex,
        out Piece pieceType
    )
    {
        /*
        Algorithm:
        Find out which Piecetype we are currently handling
        Dependent on piecetype and position on the board, a bitboard with the sightlines of the given piece at the given position is chosen
        -> Some bit manipulations result in a bitboard which contains all squares that the given piece can (pseudolegally) move to
        */

        bool isWhite = IsBitSet(board.whitePiecesBB, posIndex);
        ulong sameColorPieces = isWhite ? board.whitePiecesBB : board.blackPiecesBB;

        if (IsBitSet(board.wPawnBB, posIndex))
        {
            pieceType = Piece.wPawn;
            int y = posIndex >> 3;
            int x = posIndex & 7;

            //ulong attackSqrs = 0b101ul << (posIndex + 7);//covered die 2 sqrs die diagonal vor dem pawn liegen
            ulong attackSqrs = BB_Lookup.GetPawnAttackSqrs(x, y, true);
            ulong allPiecesBB = board.whitePiecesBB | board.blackPiecesBB;

            ulong enemyPiecesBB = allPiecesBB & ~sameColorPieces;

            ulong targetSqrs = (attackSqrs & enemyPiecesBB);
            ulong moveSqrs = (~allPiecesBB & (1ul << posIndex + 8));

            int sqrTwoAbove = posIndex + 16;
            moveSqrs |=
                (moveSqrs != 0 && !IsBitSet(allPiecesBB, sqrTwoAbove))
                    ? (y == 1 ? 1ul << sqrTwoAbove : 0)
                    : 0;
            return targetSqrs | moveSqrs;
        }
        else if (IsBitSet(board.bPawnBB, posIndex))
        {
            pieceType = Piece.bPawn;

            int y = posIndex >> 3;
            int x = posIndex & 7;

            //ulong attackSqrs = 0b101ul << (posIndex - 9);//covered die 2 sqrs die diagonal vor dem pawn liegen
            ulong attackSqrs = BB_Lookup.GetPawnAttackSqrs(x, y, false);
            ulong allPiecesBB = board.whitePiecesBB | board.blackPiecesBB;

            ulong enemyPiecesBB = allPiecesBB & ~sameColorPieces;

            ulong targetSqrs = (attackSqrs & enemyPiecesBB);
            ulong moveSqrs = (~allPiecesBB & (1ul << posIndex - 8));

            int sqrTwoAbove = posIndex - 16;
            moveSqrs |=
                (moveSqrs != 0 && !IsBitSet(allPiecesBB, sqrTwoAbove))
                    ? (y == 6 ? 1ul << (sqrTwoAbove) : 0)
                    : 0;

            return targetSqrs | moveSqrs;
        }
        else if (IsBitSet(board.bRookBB | board.wRookBB, posIndex))
        {
            pieceType = isWhite ? Piece.wRook : Piece.bRook;

            ulong patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.bRook, posIndex);
            ulong allPiecesBB = board.whitePiecesBB | board.blackPiecesBB;

            ulong targetBB = patternBB & allPiecesBB;

            ulong pseudoTargetSqrs = GetPseudoTargetSqrsRook(targetBB, posIndex);
            ulong targetSqrs = pseudoTargetSqrs & ~sameColorPieces;

            return targetSqrs;
        }
        else if (IsBitSet(board.wKnightBB | board.bKnightBB, posIndex))
        {
            pieceType = isWhite ? Piece.wKnight : Piece.bKnight;

            ulong patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.bKnight, posIndex);

            ulong targetSqrs = patternBB & ~sameColorPieces;

            return targetSqrs;
        } //es ist ein king, beinah selber code wie beim knight wegen der konstanten anzahl an mgl feldern
        else if (IsBitSet(board.wKingBB | board.bKingBB, posIndex))
        {
            pieceType = isWhite ? Piece.wKing : Piece.bKing;

            ulong patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.bKing, posIndex);

            ulong targetSqrs = patternBB & ~sameColorPieces;
            targetSqrs &= ~(isWhite ? board.bControlledSqrBB : board.wControlledSqrBB);

            return targetSqrs;
        } //es ist ein bishop, beinahe selber code wie rook wegen ähnlichem attackpattern
        else if (IsBitSet(board.wBishopBB | board.bBishopBB, posIndex))
        {
            pieceType = isWhite ? Piece.wBishop : Piece.bBishop;
            ulong patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.bBishop, posIndex);
            ulong allPiecesBB = board.whitePiecesBB | board.blackPiecesBB;

            ulong targetBB = patternBB & allPiecesBB;

            ulong pseudoTargetSqrs = GetPseudoTargetSqrsBishop(targetBB, posIndex);
            ulong targetSqrs = pseudoTargetSqrs & ~sameColorPieces;

            return targetSqrs;
        } //es ist eine queen
        else if (IsBitSet(board.wQueenBB | board.bQueenBB, posIndex))
        {
            pieceType = isWhite ? Piece.wQueen : Piece.bQueen;
            ulong patternBB1 = BB_Lookup.GetBBforPieceAtSqr(Piece.bBishop, posIndex);
            ulong patternBB2 = BB_Lookup.GetBBforPieceAtSqr(Piece.bRook, posIndex);

            ulong allPiecesBB = board.whitePiecesBB | board.blackPiecesBB;

            ulong targetBB1 = patternBB1 & allPiecesBB;
            ulong targetBB2 = patternBB2 & allPiecesBB;
            ulong pseudoTargetSqrs =
                GetPseudoTargetSqrsBishop(targetBB1, posIndex)
                | GetPseudoTargetSqrsRook(targetBB2, posIndex);

            ulong targetSqrs = pseudoTargetSqrs & ~sameColorPieces;

            return targetSqrs;
        } //das moveset der pawns ist abhängig von der farbe

        pieceType = Piece.wKing; //default value
        Console.WriteLine("Something went wrong in getPseudoLegalMovesBB, index = " + posIndex);
        Program.DisplayBoard(board);
        return 0;
    }

    /// <summary>
    /// Nimmt BB mit sqrs die im sichtfeld eines pieces liegen, entfernt sqrs die das
    /// piece effektiv nicht sehen kann weil andere pieces im Weg stehen; enthält noch pieces derselben farbe
    /// |Gilt für pieces mit dem Bishop attackpattern;
    /// Macht >15mio iterations / second
    /// </summary>
    /// <param name="sqrs"></param>
    /// <param name="posIndex"></param>
    /// <returns></returns>
    public static ulong GetPseudoTargetSqrsBishop(ulong pieceBB, int posIndex)
    {
        pieceBB &= ~(1ul << posIndex);
        ; //das bit an der position von von posIndex wird 0 gesetzt um komplikationen zu vermeiden

        ulong nullifier = posIndex == 0 ? 0 : 1ul;
        int reverseIndex = 64 - posIndex;

        ulong diagBB = GetDiagonal(posIndex) & pieceBB;
        ulong antiDiagBB = GetAntiDiagonal(posIndex) & pieceBB;

        ulong diagSW = ((diagBB << reverseIndex) >> reverseIndex) * nullifier;
        ulong diagNE = ((diagBB >> posIndex) << posIndex);
        int indexSW =
            diagSW == 0 ? projectdiagSWLookupTable[posIndex] : GetBiggestBit(diagSW) % 64;
        int indexNE =
            diagNE == 0 ? projectdiagNELookupTable[posIndex] : GetSmallestBit(diagNE) % 64;

        ulong diag = InterpolateDiagonal(indexNE, indexSW);

        ulong antiDiagNW = ((antiDiagBB >> posIndex) << posIndex);
        ulong antiDiagSe = ((antiDiagBB << reverseIndex) >> reverseIndex) * nullifier;
        int indexSE =
            antiDiagSe == 0
                ? projectAntiDiagSELookupTable[posIndex]
                : GetBiggestBit(antiDiagSe) % 64;
        int indexNW =
            antiDiagNW == 0
                ? projectAntiDiagNWLookupTable[posIndex]
                : GetSmallestBit(antiDiagNW) % 64;

        ulong antiDiag = InterpolateAntiDiagonal(indexNW, indexSE);

        return antiDiag | diag;
    }

    private static int[] projectAntiDiagSELookupTable =
    {
            0, 1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5, 6, 7, 15, 2, 3, 4, 5, 6, 7,
            15, 23, 3, 4, 5, 6, 7, 15, 23, 31, 4, 5, 6, 7, 15, 23, 31, 39, 5,
            6, 7, 15, 23, 31, 39, 47, 6, 7, 15, 23, 31, 39, 47, 55, 7, 15, 23,
            31, 39, 47, 55, 63
    };

    private static int[] projectAntiDiagNWLookupTable =
    {
            0, 8, 16, 24, 32, 40, 48, 56, 8, 16, 24, 32, 40, 48, 56, 57, 16,
            24, 32, 40, 48, 56, 57, 58, 24, 32, 40, 48, 56, 57, 58, 59, 32, 40,
            48, 56, 57, 58, 59, 60, 40, 48, 56, 57, 58, 59, 60, 61, 48, 56, 57,
            58, 59, 60, 61, 62, 56, 57, 58, 59, 60, 61, 62, 63,
        };

    private static int[] projectdiagSWLookupTable =
    {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 0, 1, 2, 3, 4, 5, 6, 16, 8, 0, 1, 2, 3,
            4, 5, 24, 16, 8, 0, 1, 2, 3, 4, 32, 24, 16, 8, 0, 1, 2, 3, 40, 32,
            24, 16, 8, 0, 1, 2, 48, 40, 32, 24, 16, 8, 0, 1, 56, 48, 40, 32,
            24, 16, 8, 0,
        };

    private static int[] projectdiagNELookupTable =
    {
            63, 55, 47, 39, 31, 23, 15, 7, 62, 63, 55, 47, 39, 31, 23, 15, 61,
            62, 63, 55, 47, 39, 31, 23, 60, 61, 62, 63, 55, 47, 39, 31, 59, 60,
            61, 62, 63, 55, 47, 39, 58, 59, 60, 61, 62, 63, 55, 47, 57, 58, 59,
            60, 61, 62, 63, 55, 56, 57, 58, 59, 60, 61, 62, 63,
    };

    //NW>SE
    public static ulong InterpolateAntiDiagonal(int indexNW, int indexSE)
    {
        ulong filler = InterpolateHorizontal(indexNW, indexSE);
        ulong diag = GetAntiDiagonal(indexNW);

        return filler & diag;
    }

    //NE>SW
    public static ulong InterpolateDiagonal(int indexNE, int indexSW)
    {
        ulong filler = InterpolateHorizontal(indexNE, indexSW);
        ulong diag = GetDiagonal(indexNE);

        return filler & diag;
    }

    private static readonly ulong[] diagonals =
    {
            (1ul << 7),
            (1ul << 6) | (1ul << 15),
            (1ul << 5) | (1ul << 14) | (1ul << 23),
            (1ul << 4) | (1ul << 13) | (1ul << 22) | (1ul << 31),
            (1ul << 3) | (1ul << 12) | (1ul << 21) | (1ul << 30) | (1ul << 39),
            (1ul << 2) | (1ul << 11) | (1ul << 20) | (1ul << 29) | (1ul << 38) | (1ul << 47),
            (1ul << 1)
                | (1ul << 10)
                | (1ul << 19)
                | (1ul << 28)
                | (1ul << 37)
                | (1ul << 46)
                | (1ul << 55),
            (1ul << 0)
                | (1ul << 9)
                | (1ul << 18)
                | (1ul << 27)
                | (1ul << 36)
                | (1ul << 45)
                | (1ul << 54)
                | (1ul << 63),
            (1ul << 8)
                | (1ul << 17)
                | (1ul << 26)
                | (1ul << 35)
                | (1ul << 44)
                | (1ul << 53)
                | (1ul << 62),
            (1ul << 16) | (1ul << 25) | (1ul << 34) | (1ul << 43) | (1ul << 52) | (1ul << 61),
            (1ul << 24) | (1ul << 33) | (1ul << 42) | (1ul << 51) | (1ul << 60),
            (1ul << 32) | (1ul << 41) | (1ul << 50) | (1ul << 59),
            (1ul << 40) | (1ul << 49) | (1ul << 58),
            (1ul << 48) | (1ul << 57),
            (1ul << 56),
            (1ul << 7) //um nicht %15 machen zu müssen
        };

    //returnt die diagonale in der sich ein sqr befindet
    public static ulong GetDiagonal(int posIndex)
    {
        int y = posIndex >> 3;
        int x = posIndex & 7;

        int diagonalIndex = y - x + 7;

        return diagonals[diagonalIndex];
    }

    public static ulong GetDiagonal(int x, int y)
    {
        int diagonalIndex = y - x + 7;

        return diagonals[diagonalIndex];
    }

    private static readonly ulong[] antiDiagonals =
    {
            (1ul << 0),
            (1ul << 1) | (1ul << 8),
            (1ul << 2) | (1ul << 9) | (1ul << 16),
            (1ul << 3) | (1ul << 10) | (1ul << 17) | (1ul << 24),
            (1ul << 4) | (1ul << 11) | (1ul << 18) | (1ul << 25) | (1ul << 32),
            (1ul << 5) | (1ul << 12) | (1ul << 19) | (1ul << 26) | (1ul << 33) | (1ul << 40),
            (1ul << 6)
                | (1ul << 13)
                | (1ul << 20)
                | (1ul << 27)
                | (1ul << 34)
                | (1ul << 41)
                | (1ul << 48),
            (1ul << 7)
                | (1ul << 14)
                | (1ul << 21)
                | (1ul << 28)
                | (1ul << 35)
                | (1ul << 42)
                | (1ul << 49)
                | (1ul << 56),
            (1ul << 15)
                | (1ul << 22)
                | (1ul << 29)
                | (1ul << 36)
                | (1ul << 43)
                | (1ul << 50)
                | (1ul << 57),
            (1ul << 23) | (1ul << 30) | (1ul << 37) | (1ul << 44) | (1ul << 51) | (1ul << 58),
            (1ul << 31) | (1ul << 38) | (1ul << 45) | (1ul << 52) | (1ul << 59),
            (1ul << 39) | (1ul << 46) | (1ul << 53) | (1ul << 60),
            (1ul << 47) | (1ul << 54) | (1ul << 61),
            (1ul << 55) | (1ul << 62),
            (1ul << 63),
        };

    //returnt die antidiagonale in der sich ein sqr befindet
    public static ulong GetAntiDiagonal(int posIndex)
    {
        int y = posIndex >> 3;
        int x = posIndex & 7;

        int antiDiagonalIndex = x + y;

        return antiDiagonals[antiDiagonalIndex];
    }

    public static ulong GetAntiDiagonal(int x, int y)
    {
        int antiDiagonalIndex = x + y;
        return antiDiagonals[antiDiagonalIndex];
    }

    /// <summary>
    /// Nimmt BB mit sqrs die im sichtfeld eines pieces liegen, entfernt sqrs die das
    /// piece effektiv nicht sehen kann weil andere pieces im Weg stehen; enthält noch pieces derselben farbe
    /// |Gilt für pieces mit dem Rook attackpattern;
    /// Macht >21mio iterations/second
    /// </summary>
    /// <param name="sqrs"></param>
    /// <param name="posIndex"></param>
    /// <returns></returns>
    public static ulong GetPseudoTargetSqrsRook(ulong pieceBB, int posIndex)
    {
        int y = posIndex >> 3;
        int x = posIndex & 7;

        pieceBB &= ~(1ul << posIndex);
        ; //das bit an der position von von posIndex wird 0 gesetzt um komplikationen zu vermeiden

        ulong nullifier = posIndex == 0 ? 0 : 1ul;
        int reverseIndex = 64 - posIndex;

        //die x-Koordinate von posIndex ist die position in der
        //hori line, y ist die position in der verti line
        ulong hori = HorizontalLineBB(y) & pieceBB;
        ulong horiEast = (hori >> posIndex) << posIndex;
        ulong horiWest = ((hori << reverseIndex) >> reverseIndex) * nullifier;

        //vertikale lines
        ulong verti = VerticalLineBB(x) & pieceBB;
        ulong vertiTop = (verti >> posIndex) << posIndex;
        ulong vertiBottom = ((verti << reverseIndex) >> reverseIndex) * nullifier;

        //die bits werden isoliert
        int indexWest = horiWest == 0 ? 8 * y : GetBiggestBit(horiWest); //wird null wenn horiWest=0
        int indexEast = horiEast == 0 ? 8 * y + 7 : GetSmallestBit(horiEast) % 64;
        int indexTop = vertiTop == 0 ? 56 + x : GetSmallestBit(vertiTop) % 64;
        int indexBottom = GetBiggestBit(vertiBottom);

        //indexBottom wird ignoriert weil es zwar 0 wird, die xKoordinate aber von i1, dh indexTop festgelegt wird
        //indexBottom produziert auch wenn es 0 wird richtige ergebnisse weil es nicht mehr verwendet wird

        ulong horizontalLine = InterpolateHorizontal(indexEast, indexWest);
        ulong verticalLine = InterpolateVertical(indexTop, indexBottom);

        return horizontalLine | verticalLine;
    }

    /// <summary>
    /// Setzt alle Bits eines Bitboards auf true, die sich zwischen i1 und i2 befinden (inklusive) |
    /// i1 > i2
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    /// <returns></returns>
    public static ulong InterpolateHorizontal(int i1, int i2)
    {
        /*
        ulong n1 = 1 << i1-i2;
        n1 <<= 1;
        n1 -= 1;
        n1 <<= i2;
        */
        return (((1ul << (i1 - i2)) << 1) - 1) << i2;
    }

    //  return ((1ul << (i1 - i2 + 1)) - 1) << i2;//gibt fehler bei 63 , 0 weil dann der mittlere term=64, dh gar kein shift findet statt

    public static ulong InterpolateVertical(int i1, int i2)
    {
        int x = i1 & 7; //basically modulo 8

        ulong filler = InterpolateHorizontal(i1, i2);
        ulong verticalMask = VerticalLineBB(x);

        return filler & verticalMask;
    }

    /// <summary>
    /// Returnt den Index (!) des kleinsten "1" bits
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static int GetSmallestBit(ulong n)
    {
        return (int)System.Runtime.Intrinsics.X86.Bmi1.X64.TrailingZeroCount(n);
    }

    public static int CountSetBits(ulong n)
    {
        return System.Numerics.BitOperations.PopCount(n);
    }

    /// <summary>
    /// Returnt den Index (!) des größten "1" bits
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static int GetBiggestBit(ulong n)
    {
        return Utility.BitScanReverse(n);
        //return BitOperations.LeadingZeroCount(n);
    }

    /// <summary>
    /// Returnt eine horizontale Linie mit einer gegebenen y-Koordinate als Bitboard
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public static ulong HorizontalLineBB(int y)
    {
        //y Element von [0 , 7]
        return 0b11111111ul << (y * 8);
    }

    /// <summary>
    /// Returnt eine vertikale Linie mit einer gegebenen x-Koordinate als Bitboard
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static ulong VerticalLineBB(int x)
    {
        return 0b0000000100000001000000010000000100000001000000010000000100000001ul << x;
    }

    //eigentlich sehr obvious
    //https://stackoverflow.com/questions/43724490/how-to-reset-single-bit-in-ulong
    //https://stackoverflow.com/questions/2431732/checking-if-a-bit-is-set-or-not
    public static bool IsBitSet(ulong b, int pos)
    {
        return (b & (1ul << pos)) != 0;
    }
}
