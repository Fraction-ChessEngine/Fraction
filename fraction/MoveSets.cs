using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace fraction;
public static class MoveSets {
    /// <summary>
    /// includeCoverage: ob auch gedeckte pieces ins BB sollen, 
    /// obwohl diese nicht als legaler move zur verfügung stehen
    /// </summary>
    /// <param name="board"></param>
    /// <param name="posIndex"></param>
    /// <param name="pieceType"></param>
    /// <param name="includeCoverage"></param>
    /// <returns></returns>
    public static BitBoard GetPseudoLegalMoves(
        Chessboard board,
        int posIndex,
        out Piece pieceType,
        bool includeCoverage = false
    ) {
        /*
        Algorithm:
        Find out which Piecetype we are currently handling
        Dependent on piecetype and position on the board, a bitboard with the sightlines of the given piece at the given position is chosen
        -> Some bit manipulations result in a bitboard which contains all squares that the given piece can (pseudolegally) move to
        
        if includeCoverageFlag is set, king needs to be ignored as the functions purpose
        is to generate controlled squrs, and the king does not block sliders
        from controlling the sqrs behind him
        */

        bool isWhite = board.WhitePiecesBB[posIndex];
        BitBoard sameColorPieces = includeCoverage ? 0 : isWhite ? board.WhitePiecesBB : board.BlackPiecesBB;
        BitBoard enemyControlSqrs = isWhite ? board.BControlledSqrBB : board.WControlledSqrBB;

        if (board.WPawnBB[posIndex]) {
            pieceType = Piece.wPawn;
            int y = posIndex >> 3;
            int x = posIndex & 7;

            //BitBoard attackSqrs = 0b101ul << (posIndex + 7);//covered die 2 sqrs die diagonal vor dem pawn liegen
            BitBoard attackSqrs = BB_Lookup.GetPawnAttackSqrs(x, y, true);
            BitBoard allPiecesBB = board.WhitePiecesBB | board.BlackPiecesBB;

            BitBoard enemyPiecesBB = allPiecesBB & ~sameColorPieces;

            BitBoard targetSqrs = attackSqrs & enemyPiecesBB;
            BitBoard moveSqrs = ~allPiecesBB & (1ul << posIndex + 8);

            int sqrTwoAbove = posIndex + 16;
            moveSqrs |= (moveSqrs != 0 && !allPiecesBB[sqrTwoAbove]) ? (y == 1 ? 1ul << sqrTwoAbove : 0) : 0;

            return targetSqrs | moveSqrs;
        } else if (board.BPawnBB[posIndex]) {
            pieceType = Piece.bPawn;

            int y = posIndex >> 3;
            int x = posIndex & 7;

            //BitBoard attackSqrs = 0b101ul << (posIndex - 9);//covered die 2 sqrs die diagonal vor dem pawn liegen
            BitBoard attackSqrs = BB_Lookup.GetPawnAttackSqrs(x, y, false);
            BitBoard allPiecesBB = board.WhitePiecesBB | board.BlackPiecesBB;

            BitBoard enemyPiecesBB = allPiecesBB & ~sameColorPieces;

            BitBoard targetSqrs = attackSqrs & enemyPiecesBB;
            BitBoard moveSqrs = ~allPiecesBB & (1ul << posIndex - 8);

            int sqrTwoAbove = posIndex - 16;
            moveSqrs |= (moveSqrs != 0 && !allPiecesBB[sqrTwoAbove]) ? (y == 6 ? 1ul << (sqrTwoAbove) : 0) : 0;

            return targetSqrs | moveSqrs;
        } else if ((board.BRookBB | board.WRookBB)[posIndex]) {
            pieceType = isWhite ? Piece.wRook : Piece.bRook;
            return GetSliderPseudoLegalMoves(board, posIndex, sameColorPieces, Piece.wRook, includeCoverage, isWhite);


        }//es ist ein bishop, beinahe selber code wie rook wegen ähnlichem attackpattern
          else if ((board.WBishopBB | board.BBishopBB)[posIndex]) {
            pieceType = isWhite ? Piece.wBishop : Piece.bBishop;
            return GetSliderPseudoLegalMoves(board, posIndex, sameColorPieces, Piece.wBishop, includeCoverage, isWhite);

        } else if ((board.WKnightBB | board.BKnightBB)[posIndex]) {
            pieceType = isWhite ? Piece.wKnight : Piece.bKnight;
            return GetKnightPseudoLegalMoves(posIndex, sameColorPieces);

        } //es ist ein king, beinah selber code wie beim knight wegen der konstanten anzahl an mgl feldern
          else if ((board.WKingBB | board.BKingBB)[posIndex]) {
            pieceType = isWhite ? Piece.wKing : Piece.bKing;

            return GetKingPseudoLegalMoves(posIndex, sameColorPieces, enemyControlSqrs);

        }  //es ist eine queen
          else if ((board.WQueenBB | board.BQueenBB)[posIndex]) {
            pieceType = isWhite ? Piece.wQueen : Piece.bQueen;

            return GetSliderPseudoLegalMoves(board, posIndex, sameColorPieces, Piece.wRook, includeCoverage, isWhite)
            | GetSliderPseudoLegalMoves(board, posIndex, sameColorPieces, Piece.wBishop, includeCoverage, isWhite);
        } //das moveset der pawns ist abhängig von der farbe

        pieceType = Piece.wKing; //default value
        Console.WriteLine("-- Something went wrong in getPseudoLegalMovesBB --");
        Program.DisplayBoard(board);
        Console.WriteLine("posIndex: " + posIndex);
        Console.WriteLine("includeCoverage: " + includeCoverage);

        throw new Exception("Ich halte mal an damit du dir die Fehlermeldung angucken kannst, Potz Blitz!");
    }

    public static BitBoard GetKingPseudoLegalMoves(int posIndex, BitBoard sameColorPieces, BitBoard enemyControlSqrs) {
        BitBoard patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.bKing, posIndex);

        BitBoard targetSqrs = patternBB & ~sameColorPieces;

        targetSqrs &= ~enemyControlSqrs; //hat bei perft 5 keinen effekt auf die zahlen, erst bei perft 6 gibt es unterschied

        return targetSqrs;
    }


    public static BitBoard GetKnightPseudoLegalMoves(int posIndex, BitBoard sameColorPieces) {
        BitBoard patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.wKnight, posIndex);

        BitBoard targetSqrs = patternBB & ~sameColorPieces;

        return targetSqrs;
    }

    /// <summary>
    /// Für Rook und Bishop
    /// </summary>
    /// <param name="board"></param>
    /// <param name="posIndex"></param>
    /// <param name="sameColorPieces"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static BitBoard GetSliderPseudoLegalMoves(Chessboard board, int posIndex,
                BitBoard sameColorPieces, Piece type, bool includeCoverage = false, bool isWhite = true) {

        BitBoard patternBB = BB_Lookup.GetBBforPieceAtSqr(type, posIndex);
        BitBoard allPiecesBB = board.WhitePiecesBB | board.BlackPiecesBB;

        //der enemyKing blockiert die sightlines eines sliders auf die felder
        //hinter dem king nicht, weil er dann per definition im nächsten zug verhindern muss in dieser sightline zu stehen
        if (includeCoverage) {
            BitBoard enemyKingBB = isWhite ? board.BKingBB : board.WKingBB;
            allPiecesBB &= ~enemyKingBB;
        }

        BitBoard targetBB = patternBB & allPiecesBB;

        BitBoard pseudoTargetSqrs = type == Piece.wBishop || type == Piece.bBishop ?
        GetPseudoTargetSqrsBishop(targetBB, posIndex) : GetPseudoTargetSqrsRook(targetBB, posIndex);

        BitBoard targetSqrs = pseudoTargetSqrs & ~sameColorPieces;

        return targetSqrs;
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
    public static BitBoard GetPseudoTargetSqrsBishop(BitBoard pieceBB, int posIndex) {
        pieceBB &= ~(1ul << posIndex);
        ; //das bit an der position von von posIndex wird 0 gesetzt um komplikationen zu vermeiden

        BitBoard nullifier = posIndex == 0 ? 0 : 1ul;
        int reverseIndex = 64 - posIndex;

        BitBoard diagBB = GetDiagonal(posIndex) & pieceBB;
        BitBoard antiDiagBB = GetAntiDiagonal(posIndex) & pieceBB;

        BitBoard diagSW = ((diagBB << reverseIndex) >> reverseIndex) * nullifier;
        BitBoard diagNE = ((diagBB >> posIndex) << posIndex);
        int indexSW =
            diagSW == 0 ? projectdiagSWLookupTable[posIndex] : GetBiggestBit(diagSW) % 64;
        int indexNE =
            diagNE == 0 ? projectdiagNELookupTable[posIndex] : GetSmallestBit(diagNE) % 64;

        BitBoard diag = InterpolateDiagonal(indexNE, indexSW);

        BitBoard antiDiagNW = ((antiDiagBB >> posIndex) << posIndex);
        BitBoard antiDiagSe = ((antiDiagBB << reverseIndex) >> reverseIndex) * nullifier;
        int indexSE =
            antiDiagSe == 0
                ? projectAntiDiagSELookupTable[posIndex]
                : GetBiggestBit(antiDiagSe) % 64;
        int indexNW =
            antiDiagNW == 0
                ? projectAntiDiagNWLookupTable[posIndex]
                : GetSmallestBit(antiDiagNW) % 64;

        BitBoard antiDiag = InterpolateAntiDiagonal(indexNW, indexSE);

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
    public static BitBoard InterpolateAntiDiagonal(int indexNW, int indexSE) {
        BitBoard filler = InterpolateHorizontal(indexNW, indexSE);
        BitBoard diag = GetAntiDiagonal(indexNW);

        return filler & diag;
    }

    //NE>SW
    public static BitBoard InterpolateDiagonal(int indexNE, int indexSW) {
        BitBoard filler = InterpolateHorizontal(indexNE, indexSW);
        BitBoard diag = GetDiagonal(indexNE);

        return filler & diag;
    }

    private static readonly BitBoard[] diagonals =
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
    public static BitBoard GetDiagonal(int posIndex) {
        int y = posIndex >> 3;
        int x = posIndex & 7;

        int diagonalIndex = y - x + 7;

        return diagonals[diagonalIndex];
    }

    public static BitBoard GetDiagonal(int x, int y) {
        int diagonalIndex = y - x + 7;

        return diagonals[diagonalIndex];
    }

    private static readonly BitBoard[] antiDiagonals =
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
    public static BitBoard GetAntiDiagonal(int posIndex) {
        int y = posIndex >> 3;
        int x = posIndex & 7;

        int antiDiagonalIndex = x + y;

        return antiDiagonals[antiDiagonalIndex];
    }

    public static BitBoard GetAntiDiagonal(int x, int y) {
        int antiDiagonalIndex = x + y;
        return antiDiagonals[antiDiagonalIndex];
    }

    /// <summary>
    /// Nimmt BB mit sqrs die im sichtfeld eines pieces liegen, entfernt sqrs die das
    /// piece effektiv nicht sehen kann weil andere pieces im Weg stehen; enthält noch pieces derselben farbe
    /// |Gilt für pieces mit dem Rook attackpattern
    /// </summary>
    /// <param name="sqrs"></param>
    /// <param name="posIndex"></param>
    /// <returns></returns>
    public static BitBoard GetPseudoTargetSqrsRook(BitBoard pieceBB, int posIndex) {
        int y = posIndex >> 3;
        int x = posIndex & 7;

        pieceBB &= ~(1ul << posIndex);
        ; //das bit an der position von von posIndex wird 0 gesetzt um komplikationen zu vermeiden

        BitBoard nullifier = posIndex == 0 ? 0 : 1ul;
        int reverseIndex = 64 - posIndex;

        //die x-Koordinate von posIndex ist die position in der
        //hori line, y ist die position in der verti line
        BitBoard hori = HorizontalLineBB(y) & pieceBB;
        BitBoard horiEast = (hori >> posIndex) << posIndex;
        BitBoard horiWest = ((hori << reverseIndex) >> reverseIndex) * nullifier;

        //vertikale lines
        BitBoard verti = VerticalLineBB(x) & pieceBB;
        BitBoard vertiTop = (verti >> posIndex) << posIndex;
        BitBoard vertiBottom = ((verti << reverseIndex) >> reverseIndex) * nullifier;

        //die bits werden isoliert
        int indexWest = horiWest == 0 ? 8 * y : GetBiggestBit(horiWest); //wird null wenn horiWest=0
        int indexEast = horiEast == 0 ? 8 * y + 7 : GetSmallestBit(horiEast) % 64;
        int indexTop = vertiTop == 0 ? 56 + x : GetSmallestBit(vertiTop) % 64;
        int indexBottom = GetBiggestBit(vertiBottom);

        //indexBottom wird ignoriert weil es zwar 0 wird, die xKoordinate aber von i1, dh indexTop festgelegt wird
        //indexBottom produziert auch wenn es 0 wird richtige ergebnisse weil es nicht mehr verwendet wird

        BitBoard horizontalLine = InterpolateHorizontal(indexEast, indexWest);
        BitBoard verticalLine = InterpolateVertical(indexTop, indexBottom);

        return horizontalLine | verticalLine;
    }

    /// <summary>
    /// Setzt alle Bits eines Bitboards auf true, die sich zwischen i1 und i2 befinden (inklusive) |
    /// i1 > i2
    /// </summary>
    /// <param name="index1"></param>
    /// <param name="index2"></param>
    /// <returns></returns>
    public static BitBoard InterpolateHorizontal(int i1, int i2) {
        /*
        BitBoard n1 = 1 << i1-i2;
        n1 <<= 1;
        n1 -= 1;
        n1 <<= i2;
        */
        return (((1ul << (i1 - i2)) << 1) - 1) << i2;
    }

    //  return ((1ul << (i1 - i2 + 1)) - 1) << i2;//gibt fehler bei 63 , 0 weil dann der mittlere term=64, dh gar kein shift findet statt

    public static BitBoard InterpolateVertical(int i1, int i2) {
        int x = i1 & 7; //basically modulo 8

        BitBoard filler = InterpolateHorizontal(i1, i2);
        BitBoard verticalMask = VerticalLineBB(x);

        return filler & verticalMask;
    }

    /// <summary>
    /// Returnt den Index (!) des kleinsten "1" bits
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static int GetSmallestBit(BitBoard n) {
        return (int)System.Runtime.Intrinsics.X86.Bmi1.X64.TrailingZeroCount(n);
    }

    public static int CountSetBits(BitBoard n) {
        return System.Numerics.BitOperations.PopCount(n);
    }

    /// <summary>
    /// Returnt den Index (!) des größten "1" bits
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public static int GetBiggestBit(BitBoard n) {
        return n.BitScanReverese;
        //return BitOperations.LeadingZeroCount(n);
    }

    /// <summary>
    /// Returnt eine horizontale Linie mit einer gegebenen y-Koordinate als Bitboard
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public static BitBoard HorizontalLineBB(int y) {
        //y Element von [0 , 7]
        return 0b11111111ul << (y * 8);
    }

    /// <summary>
    /// Returnt eine vertikale Linie mit einer gegebenen x-Koordinate als Bitboard
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static BitBoard VerticalLineBB(int x) {
        return 0b0000000100000001000000010000000100000001000000010000000100000001ul << x;
    }
}
