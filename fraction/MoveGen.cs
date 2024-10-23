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
            GenerateMovesForDoublePiece(b, b.WRookBB, ref possibleMoves, ref currIndex);
            GenerateMovesForDoublePiece(b, b.WKnightBB, ref possibleMoves, ref currIndex);
            GenerateMovesForDoublePiece(b, b.WBishopBB, ref possibleMoves, ref currIndex);

            GenerateMovesForPawns(b, b.WPawnBB, ref possibleMoves, ref currIndex, includeCoverage);

            GenerateMovesForKing(b, b.WKingBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForQueens(b, b.WQueenBB, ref possibleMoves, ref currIndex);

        } else {
            GenerateMovesForDoublePiece(b, b.BRookBB, ref possibleMoves, ref currIndex);
            GenerateMovesForDoublePiece(b, b.BKnightBB, ref possibleMoves, ref currIndex);
            GenerateMovesForDoublePiece(b, b.BBishopBB, ref possibleMoves, ref currIndex);

            GenerateMovesForPawns(b, b.BPawnBB, ref possibleMoves, ref currIndex, includeCoverage);

            GenerateMovesForKing(b, b.BKingBB, ref possibleMoves, ref currIndex, includeCoverage);
            GenerateMovesForQueens(b, b.BQueenBB, ref possibleMoves, ref currIndex);
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
    static Span<Vision> GenerateMovesForCheck(Chessboard b, bool forWhite) {
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

        //selbe struktur wie in generateMoves, nur dass nur moves genommen werden die check verhindern können
        Vision[] possibleMoves = new Vision[16]; //weil maximal 16 pieces die je ein "Moves" bekommen
        b.GeneratePinnedPieceBB(forWhite);

        //weil ein kingmove immer ein valider ausweg ist
        Vision kingVision = new(
            posIndex,
            MoveSets.GetKingPseudoLegalMoves(posIndex, sameColorPieces, enemyControlSqrs),
            king
            );

        bool kingMobile = kingVision.MoveBB != 0;//wenn 0: darf nicht returnt werden

        //double check, king muss bewegt werden
        if (amount > 1) {
            return new Vision[] { kingVision };

        } else {//nur ein piece checkt den king

            if (b.CheckPieceBBs[1] != 0) {//es ist ein knight
                                          //überprüfen ob knight genommen werden kann



            } else {//es ist ein slider

            }

            return null;
        }
    }

    public static Chessboard[] GenerateBoards(Chessboard b, bool whitesTurn) {

        //provisorische lösung
        Span<Vision> attackVisions = GenerateMoves(b, !whitesTurn, true);
        b.UpdateAttackedSqrBB(attackVisions, !whitesTurn);

        //wenn check ist muss white in einer anderen funktion moves generieren
        //sowas wie GenerateMovesForCheck(...)
        bool isCheck = b.IsInCheck(whitesTurn);

        Span<Vision> visions;
        if (isCheck && false) {
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
        ulong bb = MoveSets.GetPseudoLegalMoves(b, i, out pieceType, includeCoverage);
        //  bool isCheck =isWhite ? (bb & b.bKingBB) != 0ul : (bb & b.wKingBB) != 0ul;


        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (MoveSets.IsBitSet(b.pinnedBB, i)) {
            bb &= b.pinnedBB;
        }

        return new Vision(i, bb, pieceType);
    }
}
