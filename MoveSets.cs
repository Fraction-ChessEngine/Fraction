using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace fraction
{
    static class MoveSets
    {
        public static ulong getPseudoLegalMoves_bb(
            Chessboard board,
            int posIndex,
            out Piece pieceType
        )
        {
            //bei allen bitboards überprüfen ob die am posIndex True sind um den typ des pieces zu finden

            //zuerst das vorberechnete bitboard (posIndex, piece) aufrufen um alle mgl theorethischen targetsrs zu berechnen
            //mit bit operationen die sqrs finden wo pieces (egal welcher farbe) in seinem attackPattern stehen
            //mit isolate() funktion in den geraden und diagonalen die sqrs entfernen die es nicht sehen kann
            //wir entfernen die bits wo pieces der selben farbe stehen
            //===> wir haben ein bitboard mit sqrs die das piece effektiv angreifen kann

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

                ulong pseudoTargetSqrs = getPseudoTargetSqrsRook(targetBB, posIndex);
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

                return targetSqrs;
            } //es ist ein bishop, beinahe selber code wie rook wegen ähnlichem attackpattern
            else if (IsBitSet(board.wBishopBB | board.bBishopBB, posIndex))
            {
                pieceType = isWhite ? Piece.wBishop : Piece.bBishop;
                ulong patternBB = BB_Lookup.GetBBforPieceAtSqr(Piece.bBishop, posIndex);
                ulong allPiecesBB = board.whitePiecesBB | board.blackPiecesBB;

                ulong targetBB = patternBB & allPiecesBB;

                ulong pseudoTargetSqrs = getPseudoTargetSqrsBishop(targetBB, posIndex);
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
                    getPseudoTargetSqrsBishop(targetBB1, posIndex)
                    | getPseudoTargetSqrsRook(targetBB2, posIndex);

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
        public static ulong getPseudoTargetSqrsBishop(ulong pieceBB, int posIndex)
        {
            pieceBB &= ~(1ul << posIndex);
            ; //das bit an der position von von posIndex wird 0 gesetzt um komplikationen zu vermeiden

            ulong nullifier = posIndex == 0 ? 0 : 1ul;
            int reverseIndex = 64 - posIndex;

            ulong diagBB = getDiagonal(posIndex) & pieceBB;
            ulong antiDiagBB = getAntiDiagonal(posIndex) & pieceBB;

            ulong diagSW = ((diagBB << reverseIndex) >> reverseIndex) * nullifier;
            ulong diagNE = ((diagBB >> posIndex) << posIndex);
            int indexSW =
                diagSW == 0 ? projectdiagSWLookupTable[posIndex] : getBiggestBit(diagSW) % 64;
            int indexNE =
                diagNE == 0 ? projectdiagNELookupTable[posIndex] : getSmallestBit(diagNE) % 64;

            ulong diag = interpolateDiagonal(indexNE, indexSW);

            ulong antiDiagNW = ((antiDiagBB >> posIndex) << posIndex);
            ulong antiDiagSe = ((antiDiagBB << reverseIndex) >> reverseIndex) * nullifier;
            int indexSE =
                antiDiagSe == 0
                    ? projectAntiDiagSELookupTable[posIndex]
                    : getBiggestBit(antiDiagSe) % 64;
            int indexNW =
                antiDiagNW == 0
                    ? projectAntiDiagNWLookupTable[posIndex]
                    : getSmallestBit(antiDiagNW) % 64;

            ulong antiDiag = interpolateAntiDiagonal(indexNW, indexSE);

            return antiDiag | diag;
        }

        private static int[] projectAntiDiagSELookupTable =
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            15,
            2,
            3,
            4,
            5,
            6,
            7,
            15,
            23,
            3,
            4,
            5,
            6,
            7,
            15,
            23,
            31,
            4,
            5,
            6,
            7,
            15,
            23,
            31,
            39,
            5,
            6,
            7,
            15,
            23,
            31,
            39,
            47,
            6,
            7,
            15,
            23,
            31,
            39,
            47,
            55,
            7,
            15,
            23,
            31,
            39,
            47,
            55,
            63
        };

        private static int[] projectAntiDiagNWLookupTable =
        {
            0,
            8,
            16,
            24,
            32,
            40,
            48,
            56,
            8,
            16,
            24,
            32,
            40,
            48,
            56,
            57,
            16,
            24,
            32,
            40,
            48,
            56,
            57,
            58,
            24,
            32,
            40,
            48,
            56,
            57,
            58,
            59,
            32,
            40,
            48,
            56,
            57,
            58,
            59,
            60,
            40,
            48,
            56,
            57,
            58,
            59,
            60,
            61,
            48,
            56,
            57,
            58,
            59,
            60,
            61,
            62,
            56,
            57,
            58,
            59,
            60,
            61,
            62,
            63,
        };

        private static int[] projectdiagSWLookupTable =
        {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            16,
            8,
            0,
            1,
            2,
            3,
            4,
            5,
            24,
            16,
            8,
            0,
            1,
            2,
            3,
            4,
            32,
            24,
            16,
            8,
            0,
            1,
            2,
            3,
            40,
            32,
            24,
            16,
            8,
            0,
            1,
            2,
            48,
            40,
            32,
            24,
            16,
            8,
            0,
            1,
            56,
            48,
            40,
            32,
            24,
            16,
            8,
            0,
        };

        private static int[] projectdiagNELookupTable =
        {
            63,
            55,
            47,
            39,
            31,
            23,
            15,
            7,
            62,
            63,
            55,
            47,
            39,
            31,
            23,
            15,
            61,
            62,
            63,
            55,
            47,
            39,
            31,
            23,
            60,
            61,
            62,
            63,
            55,
            47,
            39,
            31,
            59,
            60,
            61,
            62,
            63,
            55,
            47,
            39,
            58,
            59,
            60,
            61,
            62,
            63,
            55,
            47,
            57,
            58,
            59,
            60,
            61,
            62,
            63,
            55,
            56,
            57,
            58,
            59,
            60,
            61,
            62,
            63,
        };

        //NW>SE
        static ulong interpolateAntiDiagonal(int indexNW, int indexSE)
        {
            ulong filler = interpolateHorizontal(indexNW, indexSE);
            ulong diag = getAntiDiagonal(indexNW);

            return filler & diag;
        }

        //NE>SW
        static ulong interpolateDiagonal(int indexNE, int indexSW)
        {
            ulong filler = interpolateHorizontal(indexNE, indexSW);
            ulong diag = getDiagonal(indexNE);

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
        public static ulong getDiagonal(int posIndex)
        {
            int y = posIndex >> 3;
            int x = posIndex & 7;

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
        public static ulong getAntiDiagonal(int posIndex)
        {
            int y = posIndex >> 3;
            int x = posIndex & 7;

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
        public static ulong getPseudoTargetSqrsRook(ulong pieceBB, int posIndex)
        {
            int y = posIndex >> 3;
            int x = posIndex & 7;

            pieceBB &= ~(1ul << posIndex);
            ; //das bit an der position von von posIndex wird 0 gesetzt um komplikationen zu vermeiden

            ulong nullifier = posIndex == 0 ? 0 : 1ul;
            int reverseIndex = 64 - posIndex;

            //die x-Koordinate von posIndex ist die position in der
            //hori line, y ist die position in der verti line
            ulong hori = horizontalLineBB(y) & pieceBB;
            ulong horiEast = (hori >> posIndex) << posIndex;
            ulong horiWest = ((hori << reverseIndex) >> reverseIndex) * nullifier;

            //vertikale lines
            ulong verti = verticalLineBB(x) & pieceBB;
            ulong vertiTop = (verti >> posIndex) << posIndex;
            ulong vertiBottom = ((verti << reverseIndex) >> reverseIndex) * nullifier;

            //die bits werden isoliert
            int indexWest = horiWest == 0 ? 8 * y : getBiggestBit(horiWest); //wird null wenn horiWest=0
            int indexEast = horiEast == 0 ? 8 * y + 7 : getSmallestBit(horiEast) % 64;
            int indexTop = vertiTop == 0 ? 56 + x : getSmallestBit(vertiTop) % 64;
            int indexBottom = getBiggestBit(vertiBottom);

            //indexBottom wird ignoriert weil es zwar 0 wird, die xKoordinate aber von i1, dh indexTop festgelegt wird
            //indexBottom produziert auch wenn es 0 wird richtige ergebnisse weil es nicht mehr verwendet wird

            ulong horizontalLine = interpolateHorizontal(indexEast, indexWest);
            ulong verticalLine = interpolateVertical(indexTop, indexBottom);

            return horizontalLine | verticalLine;
        }

        /// <summary>
        /// Setzt alle Bits eines Bitboards auf true, die sich zwischen i1 und i2 befinden (inklusive) |
        /// i1 > i2
        /// </summary>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns></returns>
        public static ulong interpolateHorizontal(int i1, int i2)
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

        public static ulong interpolateVertical(int i1, int i2)
        {
            int x = i1 & 7; //basically modulo 8

            ulong filler = interpolateHorizontal(i1, i2);
            ulong verticalMask = verticalLineBB(x);

            return filler & verticalMask;
        }

        /// <summary>
        /// Returnt den Index (!) des kleinsten "1" bits
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int getSmallestBit(ulong n)
        {
            return (int)System.Runtime.Intrinsics.X86.Bmi1.X64.TrailingZeroCount(n);
        }

        /// <summary>
        /// Returnt den Index (!) des größten "1" bits
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int getBiggestBit(ulong n)
        {
            return Utility.BitScanReverse(n);
            //return BitOperations.LeadingZeroCount(n);
        }

        /// <summary>
        /// Returnt eine horizontale Linie mit einer gegebenen y-Koordinate als Bitboard
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        private static ulong horizontalLineBB(int y)
        {
            //y Element von [0 , 7]
            return 0b11111111ul << (y * 8);
        }

        /// <summary>
        /// Returnt eine vertikale Linie mit einer gegebenen x-Koordinate als Bitboard
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static ulong verticalLineBB(int x)
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
}
