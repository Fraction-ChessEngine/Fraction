using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace fraction;
static class MoveGen {
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
        ulong pieceBB,
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
            GenerateMovesForDoublePiece(b, b.WRookBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForDoublePiece(b, b.WKnightBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForDoublePiece(b, b.WBishopBB, ref possibleMoves, ref currIndex, includeCoverage);

            GenerateMovesForPawns(b, b.WPawnBB, ref possibleMoves, ref currIndex, includeCoverage);

            GenerateMovesForKing(b, b.WKingBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForQueens(b, b.WQueenBB, ref possibleMoves, ref currIndex, includeCoverage);

        } else {
            GenerateMovesForDoublePiece(b, b.BRookBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForDoublePiece(b, b.BKnightBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForDoublePiece(b, b.BBishopBB, ref possibleMoves, ref currIndex, includeCoverage);

            GenerateMovesForPawns(b, b.BPawnBB, ref possibleMoves, ref currIndex, includeCoverage);

            GenerateMovesForKing(b, b.BKingBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForQueens(b, b.BQueenBB, ref possibleMoves, ref currIndex, includeCoverage);
        }

        return possibleMoves[0..currIndex];
    }

    private static void GenerateMovesForKing(Chessboard b, ulong kingBB, ref Vision[] possibleMoves,
            ref int currIndex, bool includeCoverage = false) {

        int kingIndex = Utility.FindSingleSetBit(kingBB);

        Vision vKing = GetVisionForPieceAt(b, kingIndex, includeCoverage);

        if (vKing.MoveBB != 0) {
            possibleMoves[currIndex] = vKing;
            currIndex++;
        }
    }

    private static void GenerateMovesForPawns(
            Chessboard b, ulong pawnBB, ref Vision[] possibleMoves,
            ref int currIndex, bool includeCoverage = false) {


        int pawns = Eval.NumberOfSetBits(pawnBB);

        int[] pawnArr = Utility.FindSetBitsMax(pawnBB, pawns);

        for (int i = 0; i < pawns; i++) {
            Vision v = GetVisionForPieceAt(b, pawnArr[i], includeCoverage);
            if (v.MoveBB == 0)
                continue;
            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }

    //das einzige piece zu dem man promoten kann, dh es kann 8 geben
    private static void GenerateMovesForQueens(Chessboard b, ulong queenBB,
            ref Vision[] possibleMoves, ref int currIndex, bool includeCoverage = false) {

        int queens = Eval.NumberOfSetBits(queenBB);
        int[] queenArr = Utility.FindSetBitsMax(queenBB, queens);

        for (int i = 0; i < queens; i++) {
            Vision v = GetVisionForPieceAt(b, queenArr[i], includeCoverage);
            if (v.MoveBB == 0)
                continue;
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
        ulong combined = 0;
        foreach (ulong bb in b.CheckPieceBBs) combined |= bb;
        int amount = MoveSets.CountSetBits(combined);

        int posIndex = Utility.FindSingleSetBit(forWhite ? b.WKingBB : b.BKingBB);
        ulong sameColorPieces = forWhite ? b.WhitePiecesBB : b.BlackPiecesBB;
        ulong enemyControlSqrs = forWhite ? b.BControlledSqrBB : b.WControlledSqrBB;
        Piece king = forWhite ? Piece.wKing : Piece.bKing;

        //weil ein kingmove immer ein valider ausweg ist
        Vision kingVision = new(
            posIndex,
            MoveSets.GetKingPseudoLegalMoves(posIndex, sameColorPieces, enemyControlSqrs),
            king
            );

        bool kingMobile = kingVision.MoveBB != 0;//wenn 0: kingVision darf nicht returnt werden


        b.GeneratePinnedPieceBB(forWhite);

        //double check, king muss bewegt werden
        if (amount > 1) {
            if (kingMobile) return new Vision[] { kingVision };
            return null;//entspricht checkmate

        } else {//nur ein piece checkt den king

            return GenerateMovesForSingleCheck(b, forWhite, combined, posIndex);
        }
    }

    /// <summary>
    /// Wird in GenerateMovesForCheck gecalled wenn es nur ein Piece check gibt
    /// </summary>
    /// <returns></returns>
    private static Span<Vision> GenerateMovesForSingleCheck(Chessboard b, bool forWhite, ulong combined, int kingIndex) {
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
            return legal[0..(currIndex)];

        } else {//es ist ein slider
            ulong checkLine = GetCheckLine(kingIndex, Utility.FindSingleSetBit(combined)) & ~(1ul << kingIndex);

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
            return legal[0..(currIndex)];
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
    public static ulong GetCheckLine(int k, int c) {
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

        //wenn check ist muss white in einer anderen funktion moves generieren
        //sowas wie GenerateMovesForCheck(...)
        bool isCheck = b.IsInCheck(whitesTurn);

        Span<Vision> visions;
        if (isCheck) {
            visions = GenerateMovesForCheck(b, whitesTurn);
        } else {
            visions = GenerateMoves(b, whitesTurn);
        }

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

            int[] moveArr = Utility.FindSetBitsMax(v.MoveBB, v.setBits);
            for (int j = 0; j < v.setBits; j++) {
                Chessboard cb = b.GenerateBoardWithMove(v.PosIndex, moveArr[j], v.pieceType);
                boards[index] = cb;
                index++;

                //frisst hoffentlich nicht zu viel performance
                if (perft) {
                    Testing.perftmoves[index - 1] = Utility.PosToAN((v.PosIndex)) +
                        Utility.PosToAN((moveArr[j]));
                }
            }
        }

        return boards;
    }

    public static Vision GetVisionForPieceAt(Chessboard b, int i, bool includeCoverage = false) {
        Piece pieceType;
        ulong bb = MoveSets.GetPseudoLegalMoves(b, i, out pieceType, includeCoverage);


        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (MoveSets.IsBitSet(b.pinnedBB, i)) {
            bb &= b.pinnedBB;
        }


        return new Vision(i, bb, pieceType);
    }
}
