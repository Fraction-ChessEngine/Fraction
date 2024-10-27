using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

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
        bool forWhite,
        ref Vision[] possibleMoves,
        ref int currIndex,
        bool includeCoverage = false
    ) {
        int amount = Eval.NumberOfSetBits(pieceBB);
        switch (amount) {
            case 1:
                int i1 = Utility.FindSingleSetBit(pieceBB);
                Vision v = GetVisionForPieceAt(b, i1, includeCoverage);
                if (v.MoveBB == 0ul)
                    break;
                possibleMoves[currIndex] = v;
                currIndex++;
                break;
            case 2:
                int j1,
                    j2;
                Utility.FindTwoSetBits(pieceBB, out j1, out j2);
                Vision v1 = GetVisionForPieceAt(b, j1, includeCoverage);
                Vision v2 = GetVisionForPieceAt(b, j2, includeCoverage);

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
            GenerateMovesForDoublePiece(
                b,
                b.WRookBB,
                forWhite,
                ref possibleMoves,
                ref currIndex
            );
            GenerateMovesForDoublePiece(
                b,
                b.WKnightBB,
                forWhite,
                ref possibleMoves,
                ref currIndex
            );
            GenerateMovesForDoublePiece(
                b,
                b.WBishopBB,
                forWhite,
                ref possibleMoves,
                ref currIndex
            );

            //pawns
            int pawns = Eval.NumberOfSetBits(b.WPawnBB);
            int[] pawnArr = Utility.FindSetBitsMax(b.WPawnBB, pawns);

            for (int i = 0; i < pawns; i++) {
                Vision v = GetVisionForPieceAt(b, pawnArr[i], includeCoverage);
                if (v.MoveBB == 0)
                    continue;
                possibleMoves[currIndex] = v;
                currIndex++;
            }

            //king, es kann nur einen geben
            int kingIndex = Utility.FindSingleSetBit(b.WKingBB);

            Vision vKing = GetVisionForPieceAt(b, kingIndex, includeCoverage);

            if (vKing.MoveBB != 0) {
                possibleMoves[currIndex] = vKing;
                currIndex++;
            }

            //queens, es kann maximal 8 geben
            int queens = Eval.NumberOfSetBits(b.WQueenBB);
            int[] queenArr = Utility.FindSetBitsMax(b.WQueenBB, queens);

            for (int i = 0; i < queens; i++) {
                Vision v = GetVisionForPieceAt(b, queenArr[i], includeCoverage);
                if (v.MoveBB == 0)
                    continue;
                possibleMoves[currIndex] = v;
                currIndex++;
            }
        } else {
            GenerateMovesForDoublePiece(
                b,
                b.BRookBB,
                forWhite,
                ref possibleMoves,
                ref currIndex
            );
            GenerateMovesForDoublePiece(
                b,
                b.BKnightBB,
                forWhite,
                ref possibleMoves,
                ref currIndex
            );
            GenerateMovesForDoublePiece(
                b,
                b.BBishopBB,
                forWhite,
                ref possibleMoves,
                ref currIndex
            );

            //pawns
            int pawns = Eval.NumberOfSetBits(b.BPawnBB);
            int[] pawnArr = Utility.FindSetBitsMax(b.BPawnBB, pawns);

            for (int i = 0; i < pawns; i++) {
                Vision v = GetVisionForPieceAt(b, pawnArr[i], includeCoverage);
                if (v.MoveBB == 0)
                    continue;
                possibleMoves[currIndex] = v;
                currIndex++;
            }

            //king, es kann nur einen geben
            int kingIndex = Utility.FindSingleSetBit(b.BKingBB);

            Vision vKing = GetVisionForPieceAt(b, kingIndex, includeCoverage);

            if (vKing.MoveBB != 0) {
                possibleMoves[currIndex] = vKing;
                currIndex++;
            }

            //queens, es kann maximal 8 geben
            int queens = Eval.NumberOfSetBits(b.BQueenBB);
            int[] queenArr = Utility.FindSetBitsMax(b.BQueenBB, queens);

            for (int i = 0; i < queens; i++) {
                Vision v = GetVisionForPieceAt(b, queenArr[i], includeCoverage);
                if (v.MoveBB == 0)
                    continue;
                possibleMoves[currIndex] = v;
                currIndex++;
            }
        }

        return possibleMoves[0..currIndex];
    }

    public static Chessboard[] GenerateBoards(Chessboard b, bool whitesTurn) {

        //provisorische lösung
        Span<Vision> attackVisions = GenerateMoves(b, !whitesTurn, true);
        b.UpdateAttackedSqrBB(attackVisions, !whitesTurn);

        Span<Vision> visions = GenerateMoves(b, whitesTurn);


        //gesamtlänge des endarrays wird bestimmt
        int endLength = 0;
        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            endLength += v.setBits;
        }

        Chessboard[] boards = new Chessboard[endLength];
        int index = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];


            if (v.pieceType == Piece.wKing || v.pieceType == Piece.bKing) {
                BitBoard enemyCtrlSqrs = whitesTurn ? b.BControlledSqrBB : b.WControlledSqrBB;

                v.MoveBB &= ~enemyCtrlSqrs;
            }

            int[] moveArr = Utility.FindSetBitsMax(v.MoveBB, v.setBits);
            for (int j = 0; j < v.setBits; j++) {
                Chessboard cb = b.GenerateBoardWithMove(v.PosIndex, moveArr[j], v.pieceType);
                boards[index] = cb;
                index++;
            }
        }

        return boards;
    }



    public static Chessboard[] GenerateBoards_DEBUG(
        Chessboard b,
        bool whitesTurn,
        out string[] moves
    ) {
        //provisorische lösung
        Span<Vision> attackVisions = GenerateMoves(b, !whitesTurn);
        b.UpdateAttackedSqrBB(attackVisions, !whitesTurn);

        Console.WriteLine();
        Span<Vision> visions = GenerateMoves(b, whitesTurn);



        //gesamtlänge des endarrays wird bestimmt
        int endLength = 0;
        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            endLength += v.setBits;
        }

        Chessboard[] boards = new Chessboard[endLength];
        moves = new string[endLength];

        int index = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];

            int[] moveArr = Utility.FindSetBitsMax(v.MoveBB, v.setBits);
            for (int j = 0; j < v.setBits; j++) {
                boards[index] = b.GenerateBoardWithMove(v.PosIndex, moveArr[j], v.pieceType);
                moves[index] =
                    v.pieceType.GetSymbol()
                    + " "
                    + Utility.PosToAN(v.PosIndex)
                    + Utility.PosToAN(moveArr[j]);
                index++;
            }
        }

        return boards;
    }

    public static Vision GetVisionForPieceAt(Chessboard b, int i, bool includeCoverage = false) {
        Piece pieceType;
        BitBoard bb = MoveSets.getPseudoLegalMoves_bb(b, i, out pieceType, includeCoverage);
        //  bool isCheck =isWhite ? (bb & b.bKingBB) != 0ul : (bb & b.wKingBB) != 0ul;


        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (MoveSets.IsBitSet(b.pinnedBB, i)) {
            bb &= b.pinnedBB;
        }



        return new Vision(i, bb, pieceType);
    }
}
