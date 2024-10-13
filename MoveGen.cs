using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace fraction
{
    /// <summary>
    /// kann für jedes Piece generiert werden, enthält alle pseudolegalen Moves die dieses Piece machen kann als BB
    /// </summary>
    public class Vision
    {
        public int PosIndex,
            setBits;
        public ulong MoveBB;
        public Piece pieceType;

        public Vision(int i, ulong m, Piece piece)
        {
            PosIndex = i;
            MoveBB = m;
            pieceType = piece;
            setBits = Eval.NumberOfSetBits(MoveBB);
        }

        public void PrintBB()
        {
            Utility.PrintBitBoard(MoveBB, PosIndex);
        }

        public static void PrintMovesArr(Vision[] moves)
        {
            foreach (Vision m in moves)
            {
                if (m == null)
                    return;
                m.PrintBB();
            }
        }
    }

    static class MoveGen
    {
        /*
        Architektur:
        -funktion die einmal über das board loopt und für alle sqrs die mgl moves berechnet
        suboptimale performance, muss aber nur 1x pro board executed werden

        64 iterationen zur generation des possibleMovesBBs[] array (für jedes sqr getPseudoLegalMoves callen)
        x64 iterationen um über das currMoveBB zu loopen und ein board mit dem entsprechenden move zu generieren
        (kann optimiert werden da man wegen getSmallestBit und getBiggestBit nicht von 0-63 gehen muss)



        ==> muss sehr intensiv gebenchmarked werden
        */
        static void GenerateMovesForDoublePiece(
            Chessboard b,
            ulong pieceBB,
            bool forWhite,
            ref Vision[] possibleMoves,
            ref int currIndex
        )
        {
            int amount = Eval.NumberOfSetBits(pieceBB);
            switch (amount)
            {
                case 1:
                    int i1 = Utility.FindSingleSetBit(pieceBB);
                    Vision v = GetVisionForPieceAt(b, i1);
                    if (v.MoveBB == 0ul)
                        break;
                    possibleMoves[currIndex] = v;
                    currIndex++;
                    break;
                case 2:
                    int j1,
                        j2;
                    Utility.FindTwoSetBits(pieceBB, out j1, out j2);
                    Vision v1 = GetVisionForPieceAt(b, j1);
                    Vision v2 = GetVisionForPieceAt(b, j2);

                    if (v1.MoveBB != 0ul)
                    {
                        possibleMoves[currIndex] = v1;
                        currIndex++;
                    }

                    if (v2.MoveBB != 0ul)
                    {
                        possibleMoves[currIndex] = v2;
                        currIndex++;
                    }

                    break;
                default:
                    break;
            }
        }

        public static Vision[] GenerateMoves(Chessboard b, bool forWhite)
        {
            Vision[] possibleMoves = new Vision[16]; //weil maximal 16 pieces die je ein "Moves" bekommen
            int currIndex = 0;

            //damit alle hier verwendeten funktionen access auf ein korrektes pin bb haben
            b.GeneratePinnedPieceBB(forWhite);

            if (forWhite)
            {
                GenerateMovesForDoublePiece(
                    b,
                    b.wRookBB,
                    forWhite,
                    ref possibleMoves,
                    ref currIndex
                );
                GenerateMovesForDoublePiece(
                    b,
                    b.wKnightBB,
                    forWhite,
                    ref possibleMoves,
                    ref currIndex
                );
                GenerateMovesForDoublePiece(
                    b,
                    b.wBishopBB,
                    forWhite,
                    ref possibleMoves,
                    ref currIndex
                );

                //pawns
                int pawns = Eval.NumberOfSetBits(b.wPawnBB);
                int[] pawnArr = Utility.FindSetBitsMax(b.wPawnBB, pawns);

                for (int i = 0; i < pawns; i++)
                {
                    Vision v = GetVisionForPieceAt(b, pawnArr[i]);
                    if (v.MoveBB == 0)
                        continue;
                    possibleMoves[currIndex] = v;
                    currIndex++;
                }

                //king, es kann nur einen geben
                int kingIndex = Utility.FindSingleSetBit(b.wKingBB);

                Vision vKing = GetVisionForPieceAt(b, kingIndex);
                if (vKing.MoveBB != 0)
                {
                    possibleMoves[currIndex] = vKing;
                    currIndex++;
                }

                //queens, es kann maximal 8 geben
                int queens = Eval.NumberOfSetBits(b.wQueenBB);
                int[] queenArr = Utility.FindSetBitsMax(b.wQueenBB, queens);

                for (int i = 0; i < queens; i++)
                {
                    Vision v = GetVisionForPieceAt(b, queenArr[i]);
                    if (v.MoveBB == 0)
                        continue;
                    possibleMoves[currIndex] = v;
                    currIndex++;
                }
            }
            else
            {
                GenerateMovesForDoublePiece(
                    b,
                    b.bRookBB,
                    forWhite,
                    ref possibleMoves,
                    ref currIndex
                );
                GenerateMovesForDoublePiece(
                    b,
                    b.bKnightBB,
                    forWhite,
                    ref possibleMoves,
                    ref currIndex
                );
                GenerateMovesForDoublePiece(
                    b,
                    b.bBishopBB,
                    forWhite,
                    ref possibleMoves,
                    ref currIndex
                );

                //pawns
                int pawns = Eval.NumberOfSetBits(b.bPawnBB);
                int[] pawnArr = Utility.FindSetBitsMax(b.bPawnBB, pawns);

                for (int i = 0; i < pawns; i++)
                {
                    Vision v = GetVisionForPieceAt(b, pawnArr[i]);
                    if (v.MoveBB == 0)
                        continue;
                    possibleMoves[currIndex] = v;
                    currIndex++;
                }

                //king, es kann nur einen geben
                int kingIndex = Utility.FindSingleSetBit(b.bKingBB);

                Vision vKing = GetVisionForPieceAt(b, kingIndex);
                if (vKing.MoveBB != 0)
                {
                    possibleMoves[currIndex] = vKing;
                    currIndex++;
                }

                //queens, es kann maximal 8 geben
                int queens = Eval.NumberOfSetBits(b.bQueenBB);
                int[] queenArr = Utility.FindSetBitsMax(b.bQueenBB, queens);

                for (int i = 0; i < queens; i++)
                {
                    Vision v = GetVisionForPieceAt(b, queenArr[i]);
                    if (v.MoveBB == 0)
                        continue;
                    possibleMoves[currIndex] = v;
                    currIndex++;
                }
            }

            return possibleMoves;
        }

        public static Chessboard[] GenerateBoards(Chessboard b, bool whitesTurn)
        {
            Vision[] visions = GenerateMoves(b, whitesTurn);

            //damit im nächsten zug der gegner king keine illegalen moves macht
            b.UpdateAttackedSqrBB(visions, whitesTurn);

            //gesamtlänge des endarrays wird bestimmt
            int endLength = 0;
            int visionCount = 0;
            for (int i = 0; i < visions.Length; i++)
            {
                Vision v = visions[i];

                if (v == null)
                {
                    visionCount = i;
                    break;
                }
                endLength += v.setBits;
            }

            Chessboard[] boards = new Chessboard[endLength];
            int index = 0;

            for (int i = 0; i < visionCount; i++)
            {
                Vision v = visions[i];

                int[] moveArr = Utility.FindSetBitsMax(v.MoveBB, v.setBits);
                for (int j = 0; j < v.setBits; j++)
                {
                    boards[index] = b.GenerateBoardWithMove(v.PosIndex, moveArr[j], v.pieceType);
                    index++;
                }
            }

            return boards;
        }

        public static Chessboard[] GenerateBoards_DEBUG(
            Chessboard b,
            bool whitesTurn,
            out string[] moves
        )
        {
            Console.WriteLine();
            Vision[] visions = GenerateMoves(b, whitesTurn);

            //damit im nächsten zug der gegner king keine illegalen moves macht
            b.UpdateAttackedSqrBB(visions, whitesTurn);

            //gesamtlänge des endarrays wird bestimmt
            int endLength = 0;
            int visionCount = 0;
            for (int i = 0; i < visions.Length; i++)
            {
                Vision v = visions[i];

                if (v == null)
                {
                    visionCount = i;
                    break;
                }
                endLength += v.setBits;
            }

            Chessboard[] boards = new Chessboard[endLength];
            moves = new string[endLength];

            int index = 0;

            for (int i = 0; i < visionCount; i++)
            {
                Vision v = visions[i];

                int[] moveArr = Utility.FindSetBitsMax(v.MoveBB, v.setBits);
                for (int j = 0; j < v.setBits; j++)
                {
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

        public static Vision GetVisionForPieceAt(Chessboard b, int i)
        {
            Piece pieceType;
            ulong bb = MoveSets.getPseudoLegalMoves_bb(b, i, out pieceType);
            //  bool isCheck =isWhite ? (bb & b.bKingBB) != 0ul : (bb & b.wKingBB) != 0ul;
            
            //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
            if(MoveSets.IsBitSet(b.pinnedBB,i)){
                bb &= b.pinnedBB;
            }

            return new Vision(i, bb, pieceType);
        }
    }
}
