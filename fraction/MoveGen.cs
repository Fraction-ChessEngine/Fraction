using System;
using System.Runtime.CompilerServices;

namespace fraction;
public static class MoveGen {
    /*
    Architektur:
    -funktion die einmal über das board loopt und für alle sqrs die mgl moves berechnet
    suboptimale performance, muss aber nur 1x pro board executed werden

    64 iterationen zur generation des possibleMovesBBs[] array (für jedes sqr getPseudoLegalMoves callen)
    x64 iterationen um über das currMoveBB zu loopen und ein board mit dem entsprechenden move zu generieren
    (kann optimiert werden da man wegen getSmallestBit und getBiggestBit nicht von 0-63 gehen muss)
    ==> muss sehr intensiv gebenchmarked werden
    */

    private static void GenerateMovesForDoublePiece(
        Chessboard b,
        BitBoard pieceBB,
        ref Vision[] possibleMoves,
        ref int currIndex, Piece type,
        bool includeCoverage = false
    ) {
        int amount = pieceBB.PopCount;
        switch (amount) {
            case 1:
                int i1 = pieceBB.LowestOne;
                Vision v = GetVisionForPieceAt(b, i1, type, includeCoverage);
                if (v.MoveBB == 0ul)
                    break;
                possibleMoves[currIndex] = v;
                currIndex++;
                break;
            case 2:
                Span<int> j = stackalloc int[2];
                _ = pieceBB.FindOnes(j);
                Vision v1 = GetVisionForPieceAt(b, j[0], type, includeCoverage);
                Vision v2 = GetVisionForPieceAt(b, j[1], type, includeCoverage);

                if (v1.MoveBB != 0ul) {
                    possibleMoves[currIndex] = v1;
                    currIndex++;
                }

                if (v2.MoveBB != 0ul) {
                    possibleMoves[currIndex] = v2;
                    currIndex++;
                }

                break;
            default:
                break;
        }
    }

    public static Span<Vision> GenerateMoves(Chessboard b, bool forWhite, bool includeCoverage = false) {
        Vision[] possibleMoves = new Vision[16]; //weil maximal 16 pieces die je ein "Moves" bekommen
        b.GeneratePinnedPieceBB(forWhite);

        int currIndex = 0;

        if (forWhite) {
            GenerateMovesForDoublePiece(b, b.WRookBB, ref possibleMoves, ref currIndex, Piece.wRook, includeCoverage);
            GenerateMovesForDoublePiece(b, b.WKnightBB, ref possibleMoves, ref currIndex, Piece.wKnight, includeCoverage);
            GenerateMovesForDoublePiece(b, b.WBishopBB, ref possibleMoves, ref currIndex, Piece.wBishop, includeCoverage);

            GenerateMovesForPawns(b, b.WPawnBB, ref possibleMoves, ref currIndex, Piece.wPawn, includeCoverage);

            GenerateMovesForKing(b, b.WKingBB, ref possibleMoves, ref currIndex, Piece.wKing, includeCoverage);
            GenerateMovesForQueens(b, b.WQueenBB, ref possibleMoves, ref currIndex, Piece.wQueen, includeCoverage);

        } else {
            GenerateMovesForDoublePiece(b, b.BRookBB, ref possibleMoves, ref currIndex, Piece.bRook, includeCoverage);
            GenerateMovesForDoublePiece(b, b.BKnightBB, ref possibleMoves, ref currIndex, Piece.bKnight, includeCoverage);
            GenerateMovesForDoublePiece(b, b.BBishopBB, ref possibleMoves, ref currIndex, Piece.bBishop, includeCoverage);

            GenerateMovesForPawns(b, b.BPawnBB, ref possibleMoves, ref currIndex, Piece.bPawn, includeCoverage);

            GenerateMovesForKing(b, b.BKingBB, ref possibleMoves, ref currIndex, Piece.bKing, includeCoverage);
            GenerateMovesForQueens(b, b.BQueenBB, ref possibleMoves, ref currIndex, Piece.bQueen, includeCoverage);
        }

        return possibleMoves.AsSpan(0..currIndex);
    }

    private static void GenerateMovesForKing(Chessboard b, BitBoard kingBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece king, bool includeCoverage = false) {

        int kingIndex = kingBB.LowestOne;

        Vision vKing = GetVisionForPieceAt(b, kingIndex, king, includeCoverage);

        if (vKing.MoveBB != 0) {
            possibleMoves[currIndex] = vKing;
            currIndex++;
        }
    }

    private static void GenerateMovesForPawns(
            Chessboard b, BitBoard pawnBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece pawn, bool includeCoverage = false) {

        Span<int> pawns = stackalloc int[pawnBB.PopCount];
        _ = pawnBB.FindOnes(pawns);

        /*  Utility.PrintBitBoard(b.WPawnBB); Console.WriteLine();
         Utility.PrintBitBoard(b.BPawnBB); */

        for (int i = 0; i < pawns.Length; i++) {
            Vision v = GetVisionForPieceAt(b, pawns[i], pawn, includeCoverage);
            if (v.MoveBB == 0 /* && !includeCoverage --not necessary--*/) {
                continue;
            }

            //   Console.WriteLine("Index =  " + currIndex + ", pos = " + pawns[i]);
            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }

    //autoQueen is on, so max. 8 queens
    private static void GenerateMovesForQueens(Chessboard b, BitBoard queenBB,
            ref Vision[] possibleMoves, ref int currIndex, Piece queen, bool includeCoverage = false) {

        Span<int> queens = stackalloc int[queenBB.PopCount];
        _ = queenBB.FindOnes(queens);

        for (int i = 0; i < queens.Length; i++) {
            Vision v = GetVisionForPieceAt(b, queens[i], queen, includeCoverage);

            if (v.MoveBB == 0) continue;

            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }


    /// <summary>
    /// wird gecalled wenn eindeutig ein check vorhanden ist, forWhite = white is in check
    /// </summary>
    /// <param name="b"></param>
    /// <param name="forWhite"></param>
    /// <returns></returns>
    public static Span<Vision> GenerateMovesForCheck(Chessboard b, bool forWhite) {
        /* 
        Algo: rausfinden ob doubleCheck
        Wenn doubleCheck:
            king bewegen
        
        ansonsten: 
            wenn knight:
                king bewegen oder knight nehmen
            ansonsten:
                king bewegen, piece nehmen, blocken

        */
        BitBoard combined = 0;
        foreach (BitBoard bb in b.CheckPieceBBs) combined |= bb;
        int amount = combined.PopCount;

        int posIndex = (forWhite ? b.WKingBB : b.BKingBB).LowestOne;
        BitBoard sameColorPieces = forWhite ? b.WhitePiecesBB : b.BlackPiecesBB;
        BitBoard enemyControlSqrs = forWhite ? b.BControlledSqrBB : b.WControlledSqrBB;
        Piece king = forWhite ? Piece.wKing : Piece.bKing;

        //weil ein kingmove immer ein valider ausweg ist, castleSqrs=0 weil eh nicht gecastled werden darf
        Vision kingVision = new(
            posIndex,
            MoveSets.GetKingPseudoLegalMoves(posIndex, sameColorPieces, enemyControlSqrs, 0),
            king
            );

        bool kingMobile = kingVision.MoveBB != 0;//wenn 0: kingVision darf nicht returnt werden


        b.GeneratePinnedPieceBB(forWhite);

        //double check, king muss bewegt werden
        if (amount > 1) {
            if (kingMobile) return new Vision[] { kingVision };
            b.isCheckMate = true;
            return null;//entspricht checkmate

        } else {//nur ein piece checkt den king
            return GenerateMovesForSingleCheck(b, forWhite, combined, posIndex);
        }
    }

    //muss mit checkmates getestet werden
    /// <summary>
    /// Wird in GenerateMovesForCheck gecalled wenn es nur ein Piece check gibt
    /// </summary>
    /// <returns></returns>
    private static Span<Vision> GenerateMovesForSingleCheck(Chessboard b, bool forWhite, BitBoard combined, int kingIndex) {
        Span<Vision> pseudoLegal = GenerateMoves(b, forWhite);

        Vision[] legal = new Vision[16];
        int currIndex = 0;

        if ((b.CheckPieceBBs[0] | b.CheckPieceBBs[1]) != 0) {//es ist ein knight oder pawn, dh kein piece kann blocken

            //überprüfen ob knight genommen werden kann
            for (int i = 0; i < pseudoLegal.Length; i++) {
                Vision v = pseudoLegal[i];

                //der king dürfte auch andere moves ausser captures machen
                if (v.PosIndex != kingIndex) {
                    v.MoveBB &= combined;//nur moves die das checkPiece capturen werden zugelassen
                }

                if (v.MoveBB != 0) {
                    legal[currIndex] = v;
                    currIndex++;
                }

            }
            //no legal moves found in check, therefore checkmate
            if (currIndex == 0) {
                b.isCheckMate = true;
                return null;
            }

            return legal.AsSpan(0..currIndex);

        } else {//es ist ein slider
            BitBoard checkLine = GetCheckLine(kingIndex, combined.LowestOne) & ~(1ul << kingIndex);


            for (int i = 0; i < pseudoLegal.Length; i++) {
                Vision v = pseudoLegal[i];

                //der king darf nicht blocken
                if (v.PosIndex != kingIndex) {
                    v.MoveBB &= checkLine;
                }

                if (v.MoveBB != 0) {
                    legal[currIndex] = v;
                    currIndex++;
                }
            }
            //no legal moves found in check, therefore checkmate
            if (currIndex == 0) {
                b.isCheckMate = true;
                return null;
            }
            return legal.AsSpan(0..currIndex);
        }
    }


    /// <summary>
    /// kingIndex, checkPieceIndex, enthält noch beide PieceSqrs 
    ///(king muss nicht entfernt werden weil ergebnis sowieso mit pseudoLegalmoves ver-und-et wird)
    /// </summary>
    /// <param name="k"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static BitBoard GetCheckLine(int k, int c) {
        int yKing = k >> 3;
        int xKing = k & 7;

        int yCheck = c >> 3;
        int xCheck = c & 7;

        int xDiff = xCheck - xKing;
        int yDiff = yCheck - yKing;

        int big, small;
        if (k > c) {
            big = k;
            small = c;
        } else {
            big = c;
            small = k;
        }

        if (yKing == yCheck) return MoveSets.InterpolateHorizontal(big, small);
        if (xKing == xCheck) return MoveSets.InterpolateVertical(big, small);
        if (xDiff == yDiff) return MoveSets.InterpolateDiagonal(big, small);
        if (xDiff == -yDiff) return MoveSets.InterpolateAntiDiagonal(big, small);

        throw new Exception("GetCheckLine macht Probleme, korrekte Interpolation nicht eindeutig");
    }

    public static Chessboard[] GenerateBoards(Chessboard b, bool whitesTurn, bool perft = false) {

        //provisorische lösung
        Span<Vision> attackVisions = GenerateMoves(b, !whitesTurn, true);
        b.UpdateAttackedSqrBB(attackVisions, !whitesTurn);

        bool isCheck = b.IsInCheck(whitesTurn);

        Span<Vision> visions = isCheck ? GenerateMovesForCheck(b, whitesTurn) : GenerateMoves(b, whitesTurn);

        //gesamtlänge des endarrays wird bestimmt
        int endLength = 0;
        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            endLength += v.MoveBB.PopCount;
        }

        Chessboard[] boards = new Chessboard[endLength];
        int index = 0;


#pragma warning disable CA2014

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            Span<int> moves = stackalloc int[v.MoveBB.PopCount];
            _ = v.MoveBB.FindOnes(moves);

            for (int j = 0; j < v.MoveBB.PopCount; j++) {
                Chessboard cb = b.GenerateBoardWithMove(v.PosIndex, moves[j], v.PieceType);
                boards[index] = cb;
                index++;

                //frisst hoffentlich nicht zu viel performance
                if (perft) {
                    Testing.perftmoves[index - 1] = Utility.PosToAN(v.PosIndex) +
                        Utility.PosToAN(moves[j]);
                }
            }
        }
#pragma warning restore CA2014

        return boards;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vision GetVisionForPieceAt(Chessboard b, int i, Piece type, bool includeCoverage = false) {
        BitBoard bb = MoveSets.GetPseudoLegalMoves(b, i, type, includeCoverage);

        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (b.pinnedBB[i] && !includeCoverage) {
            BitBoard pinLine = b.GetPinLineBB(1ul << i);
            bb &= pinLine;
        }

        return new Vision(i, bb, type);
    }
}
