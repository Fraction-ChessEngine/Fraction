using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace fraction
{
    public class Chessboard
    {
        //0 ist ganz rechts, 63 ist ganz links, 0=a1, 63=h8
        public ulong whitePiecesBB = 0b0000000000000000000000000000000000000000000000001111111111111111;
        public ulong blackPiecesBB = 0b1111111111111111000000000000000000000000000000000000000000000000;
        public ulong bRookBB = 0b1000000100000000000000000000000000000000000000000000000000000000;
        public ulong wRookBB = 0b0000000000000000000000000000000000000000000000000000000010000001;
        public ulong bBishopBB = 0b0010010000000000000000000000000000000000000000000000000000000000;
        public ulong wBishopBB = 0b0000000000000000000000000000000000000000000000000000000000100100;
        public ulong bKnightBB = 0b0100001000000000000000000000000000000000000000000000000000000000;
        public ulong wKnightBB = 0b0000000000000000000000000000000000000000000000000000000001000010;
        public ulong wQueenBB = 0b0000000000000000000000000000000000000000000000000000000000001000;
        public ulong bQueenBB = 0b0000100000000000000000000000000000000000000000000000000000000000;
        public ulong wKingBB = 0b0000000000000000000000000000000000000000000000000000000000010000;
        public ulong bKingBB = 0b0000100000000000000000000000000000000000000000000000000000000000;
        public ulong wPawnBB = 0b0000000000000000000000000000000000000000000000001111111100000000;
        public ulong bPawnBB = 0b0000000011111111000000000000000000000000000000000000000000000000;
        public ulong wControlledSqrBB = 0b11111111ul << 16,
            bControlledSqrBB = 0b11111111ul << 40;

        public string history = "";
        public bool afterCapturePly = false;
        public int quiescenceSearchPlies = 0;

        /// <summary>
        /// Hiermit kann durch FENtoPos funktionen ein board gebaut werden
        /// </summary>
        /// <param name="pieces_"></param>
        public Chessboard(Dictionary<int, Piece> pieces_)
        {
            //bitboards müssen generiert werden
            bPawnBB = Utility.GetBBofPosition(pieces_, Piece.bPawn);
            wPawnBB = Utility.GetBBofPosition(pieces_, Piece.wPawn);
            bBishopBB = Utility.GetBBofPosition(pieces_, Piece.bBishop);
            wBishopBB = Utility.GetBBofPosition(pieces_, Piece.wBishop);
            bQueenBB = Utility.GetBBofPosition(pieces_, Piece.bQueen);
            wQueenBB = Utility.GetBBofPosition(pieces_, Piece.wQueen);
            bKingBB = Utility.GetBBofPosition(pieces_, Piece.bKing);
            wKingBB = Utility.GetBBofPosition(pieces_, Piece.wKing);
            bKnightBB = Utility.GetBBofPosition(pieces_, Piece.bKnight);
            wKnightBB = Utility.GetBBofPosition(pieces_, Piece.wKnight);
            bRookBB = Utility.GetBBofPosition(pieces_, Piece.bRook);
            wRookBB = Utility.GetBBofPosition(pieces_, Piece.wRook);

            whitePiecesBB = wPawnBB | wBishopBB | wKingBB | wKnightBB | wRookBB | wQueenBB;
            blackPiecesBB = bPawnBB | bBishopBB | bKingBB | bKnightBB | bRookBB | bQueenBB;
        }

        public Chessboard() { }

        public Chessboard(
            ulong wKingBB,
            ulong bKingBB,
            ulong wKnightBB,
            ulong bKnightBB,
            ulong wQueenBB,
            ulong bQueenBB,
            ulong wRookBB,
            ulong bRookBB,
            ulong wBishopBB,
            ulong bBishopBB,
            ulong wPawnBB,
            ulong bPawnBB,
            string history,
            bool afterCapturePly,
            int qsp
        )
        {
            this.wKingBB = wKingBB;
            this.bKingBB = bKingBB;
            this.wKnightBB = wKnightBB;
            this.bKnightBB = bKnightBB;
            this.wQueenBB = wQueenBB;
            this.bQueenBB = bQueenBB;
            this.wRookBB = wRookBB;
            this.bRookBB = bRookBB;
            this.wBishopBB = wBishopBB;
            this.bBishopBB = bBishopBB;
            this.wPawnBB = wPawnBB;
            this.bPawnBB = bPawnBB;
            this.afterCapturePly = afterCapturePly;
            quiescenceSearchPlies = qsp;

            this.whitePiecesBB = wKingBB | wKnightBB | wQueenBB | wRookBB | wBishopBB | wPawnBB;
            this.blackPiecesBB = bKingBB | bKnightBB | bQueenBB | bRookBB | bBishopBB | bPawnBB;

            this.history = history;
        }

        //berechnet neue BBs für die kontrollierten sqrs der beiden seiten
        public void UpdateAttackedSqrBB(Vision[] visions, bool forWhite)
        {
            ulong attackSqrBB = 0;

            for (int i = 0; i < visions.Length; i++)
            {
                Vision v = visions[i];
                if (v == null)
                    break;
                ulong bb = v.MoveBB;

                //pawns müssen gesondert berechnet werden wegen des unterschieds zwischen bewegung und schlagzug
                if (v.pieceType == Piece.wPawn || v.pieceType == Piece.bPawn)
                {
                    bb &= ~MoveSets.VerticalLineBB(v.PosIndex % 8);

                    int y = v.PosIndex >> 3;
                    int x = v.PosIndex & 7;

                    bb |= BB_Lookup.GetPawnAttackSqrs(x, y, forWhite);
                }

                attackSqrBB |= bb;
            }

            if (forWhite)
            {
                wControlledSqrBB = attackSqrBB;
            }
            else
            {
                bControlledSqrBB = attackSqrBB;
            }
        }

        public static Chessboard FromFEN(string fen)
        {
            return new Chessboard(Utility.FENtoPosition(fen));
        }

        /// <summary>
        /// Returnt immer ein Piece, dh davor muss überprüft werden ob hier überhaupt ein Piece existiert
        /// | Sehr ineffiziente Funktion
        /// </summary>
        /// <param name="posIndex"></param>
        /// <returns></returns>
        public Piece GetPieceAt(int posIndex)
        {
            //kann optimiert werden mit blackPiecesBB und whitePiecesBB,
            //aber diese funktion ist nicht dafür gedacht in performance-critical
            //teilen des bots ausgeführt zu werden
            if (MoveSets.IsBitSet(wPawnBB, posIndex))
                return Piece.wPawn;
            if (MoveSets.IsBitSet(bPawnBB, posIndex))
                return Piece.bPawn;
            if (MoveSets.IsBitSet(wKingBB, posIndex))
                return Piece.wKing;
            if (MoveSets.IsBitSet(bKingBB, posIndex))
                return Piece.bKing;
            if (MoveSets.IsBitSet(wKnightBB, posIndex))
                return Piece.wKnight;
            if (MoveSets.IsBitSet(bKnightBB, posIndex))
                return Piece.bKnight;
            if (MoveSets.IsBitSet(wQueenBB, posIndex))
                return Piece.wQueen;
            if (MoveSets.IsBitSet(bQueenBB, posIndex))
                return Piece.bQueen;
            if (MoveSets.IsBitSet(wRookBB, posIndex))
                return Piece.wRook;
            if (MoveSets.IsBitSet(bRookBB, posIndex))
                return Piece.bRook;
            if (MoveSets.IsBitSet(wBishopBB, posIndex))
                return Piece.wBishop;
            if (MoveSets.IsBitSet(bBishopBB, posIndex))
                return Piece.bBishop;

            return 0;
        }

        public bool HasPieceAt(int posIndex)
        {
            return MoveSets.IsBitSet(whitePiecesBB | blackPiecesBB, posIndex);
        }

        /// <summary>
        /// Kann benutzt werden um die Farbe eines Pieces auf einem Sqr zu checken, Davor muss überprüft werden ob hier überhaupt ein Piece existiert !!!
        /// </summary>
        /// <returns></returns>
        public bool HasWhitePieceAt(int index)
        {
            return MoveSets.IsBitSet(whitePiecesBB, index);
        }


        public ulong pinnedBB;

        //forWhite = white is pinned
        public void GeneratePinnedPieceBB(bool forWhite)
        {
            /*
            rook bb auf king legen
            bishopbb auf king legen

            pieces der anderen farbe drauflegen und auf intersections prüfen
            pieces der eigenen farbe drauflegen und auf intersections prüfen

            intersection -> zwischensektion isolieren

            wenn in zwischensektion nur 1 piece: hier ein pinned bit aktivieren
             */
            if (forWhite)
            {
                int kingIndex = Utility.FindSingleSetBit(wKingBB);

                int y = kingIndex >> 3;
                int x = kingIndex & 7;

                ulong rookSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex);

                //enemy pieces die sightlines auf den king haben
                ulong intersectionsStraight = rookSightlines & (bRookBB | bQueenBB);

                ulong intersectionHoriWest =  intersectionsStraight &  MoveSets.InterpolateHorizontal(kingIndex+ (7 - x),kingIndex );
                intersectionHoriWest = intersectionHoriWest==0?0: 1ul<<MoveSets.GetSmallestBit(intersectionHoriWest);

                ulong intersectionHoriEast =  intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex, kingIndex - x);
                intersectionHoriEast = intersectionHoriEast==0?0: 1ul<<MoveSets.GetBiggestBit(intersectionHoriEast);

                ulong intersectionVertiBottom=intersectionsStraight & MoveSets.InterpolateVertical(kingIndex, kingIndex - y*8);
                intersectionVertiBottom = intersectionVertiBottom==0?0: 1ul<<MoveSets.GetBiggestBit(intersectionVertiBottom);

                ulong intersectionVertiTop = MoveSets.InterpolateVertical(kingIndex+(8-y)*8, kingIndex);
                intersectionVertiTop = intersectionVertiTop==0?0: 1ul<<MoveSets.GetSmallestBit(intersectionVertiBottom);

                ulong friendsInSightlines = /* whitePiecesBB & */ (intersectionHoriEast|intersectionHoriWest|intersectionVertiBottom|intersectionVertiTop);
                /* DIe scheisse funktioniert nicht, alle intersections individuell an geigneter position testen */
                pinnedBB = friendsInSightlines;// intersectionHoriWest;//intersectionHoriEast | intersectionHoriWest;
            }
        }

        /// <summary>
        /// Generiert stumpf ein Board wo das Piece von StartIndex zu EndIndex bewegt wurde
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public Chessboard GenerateBoardWithMove(int startIndex, int endIndex, Piece type)
        {
            bool isCapture = MoveSets.IsBitSet(blackPiecesBB | whitePiecesBB, endIndex);

            //der king kann gecaptured werden weil das capturen des king essentiell für checkmate detection ist
            ulong wKingBB_ = Utility.SetBBtoNullAt(wKingBB, endIndex);
            ulong bKingBB_ = Utility.SetBBtoNullAt(bKingBB, endIndex);

            ulong wKnightBB_ = Utility.SetBBtoNullAt(wKnightBB, endIndex);
            ulong bKnightBB_ = Utility.SetBBtoNullAt(bKnightBB, endIndex);
            ulong wQueenBB_ = Utility.SetBBtoNullAt(wQueenBB, endIndex);
            ulong bQueenBB_ = Utility.SetBBtoNullAt(bQueenBB, endIndex);
            ulong wRookBB_ = Utility.SetBBtoNullAt(wRookBB, endIndex);
            ulong bRookBB_ = Utility.SetBBtoNullAt(bRookBB, endIndex);
            ulong wBishopBB_ = Utility.SetBBtoNullAt(wBishopBB, endIndex);
            ulong bBishopBB_ = Utility.SetBBtoNullAt(bBishopBB, endIndex);
            ulong wPawnBB_ = Utility.SetBBtoNullAt(wPawnBB, endIndex);
            ulong bPawnBB_ = Utility.SetBBtoNullAt(bPawnBB, endIndex);

            //alle bitboards müssen geupdated werden
            switch (type)
            {
                case Piece.wPawn:
                    wPawnBB_ = Utility.UpdateBB(wPawnBB, startIndex, endIndex);
                    //auto queen
                    if (endIndex > 55)
                    {
                        wPawnBB_ = Utility.SetBBtoNullAt(wPawnBB_, endIndex);
                        wQueenBB_ += 1ul << endIndex;
                    }
                    break;

                case Piece.bPawn:
                    bPawnBB_ = Utility.UpdateBB(bPawnBB, startIndex, endIndex);

                    if (endIndex < 8)
                    {
                        bPawnBB_ = Utility.SetBBtoNullAt(bPawnBB_, endIndex);
                        bQueenBB_ += 1ul << endIndex;
                    }
                    break;

                //funktioniert weil es nur einen king geben darf
                case Piece.wKing:
                    wKingBB_ = 1ul << endIndex;
                    break;

                case Piece.bKing:
                    bKingBB_ = 1ul << endIndex;
                    break;

                case Piece.wKnight:
                    wKnightBB_ = Utility.UpdateBB(wKnightBB, startIndex, endIndex);
                    break;

                case Piece.bKnight:
                    bKnightBB_ = Utility.UpdateBB(bKnightBB, startIndex, endIndex);
                    break;

                case Piece.wQueen:
                    wQueenBB_ = Utility.UpdateBB(wQueenBB, startIndex, endIndex);
                    break;

                case Piece.bQueen:
                    bQueenBB_ = Utility.UpdateBB(bQueenBB, startIndex, endIndex);
                    break;

                case Piece.wRook:
                    wRookBB_ = Utility.UpdateBB(wRookBB, startIndex, endIndex);
                    break;

                case Piece.bRook:
                    bRookBB_ = Utility.UpdateBB(bRookBB, startIndex, endIndex);
                    break;

                case Piece.wBishop:
                    wBishopBB_ = Utility.UpdateBB(wBishopBB, startIndex, endIndex);
                    break;

                case Piece.bBishop:
                    bBishopBB_ = Utility.UpdateBB(bBishopBB, startIndex, endIndex);
                    break;
            }

            return new Chessboard(
                wKingBB_,
                bKingBB_,
                wKnightBB_,
                bKnightBB_,
                wQueenBB_,
                bQueenBB_,
                wRookBB_,
                bRookBB_,
                wBishopBB_,
                bBishopBB_,
                wPawnBB_,
                bPawnBB_,
                history /* + "; " + type.getSymbol() +" "+ Utility.posToAN(startIndex) + " -> " + Utility.posToAN(endIndex)  */
                ,
                isCapture,
                quiescenceSearchPlies
            );
        }
    }
}
