using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace fraction;
public class Chessboard {
    //0 ist ganz rechts, 63 ist ganz links, 0=a1, 63=h8
    public BitBoard BRookBB { get; set; } = 0b1000000100000000000000000000000000000000000000000000000000000000;
    public BitBoard WRookBB { get; set; } = 0b0000000000000000000000000000000000000000000000000000000010000001;
    public BitBoard BBishopBB { get; set; } = 0b0010010000000000000000000000000000000000000000000000000000000000;
    public BitBoard WBishopBB { get; set; } = 0b0000000000000000000000000000000000000000000000000000000000100100;
    public BitBoard BKnightBB { get; set; } = 0b0100001000000000000000000000000000000000000000000000000000000000;
    public BitBoard WKnightBB { get; set; } = 0b0000000000000000000000000000000000000000000000000000000001000010;
    public BitBoard WQueenBB { get; set; } = 0b0000000000000000000000000000000000000000000000000000000000001000;
    public BitBoard BQueenBB { get; set; } = 0b0000100000000000000000000000000000000000000000000000000000000000;
    public BitBoard WKingBB { get; set; } = 0b0000000000000000000000000000000000000000000000000000000000010000;
    public BitBoard BKingBB { get; set; } = 0b0001000000000000000000000000000000000000000000000000000000000000;
    public BitBoard WPawnBB { get; set; } = 0b0000000000000000000000000000000000000000000000001111111100000000;
    public BitBoard BPawnBB { get; set; } = 0b0000000011111111000000000000000000000000000000000000000000000000;
    public BitBoard WhitePiecesBB { get; set; } = 0b0000000000000000000000000000000000000000000000001111111111111111;
    public BitBoard BlackPiecesBB { get; set; } = 0b1111111111111111000000000000000000000000000000000000000000000000;

    public BitBoard WControlledSqrBB { get; set; } = 0;// 0b11111111ul << 16;
    public BitBoard BControlledSqrBB { get; set; } = 0;//0b11111111ul << 40;

    public bool AfterCapturePly { get; set; } = false;

    //dient dem tracken einzelner boards im perft tree beim debuggen
    public int boardIndex = 0, parentIndex = 0;
    public static int BoardCount = 0;
    public BitBoard pinnedBB = 0;

    /// <summary>
    /// Hiermit kann durch FENtoPos funktionen ein board gebaut werden
    /// </summary>
    /// <param name="pieces_"></param>
    public Chessboard(Dictionary<int, Piece> pieces_) {
        //bitboards müssen generiert werden
        BPawnBB = Utility.GetBBofPosition(pieces_, Piece.bPawn);
        WPawnBB = Utility.GetBBofPosition(pieces_, Piece.wPawn);
        BBishopBB = Utility.GetBBofPosition(pieces_, Piece.bBishop);
        WBishopBB = Utility.GetBBofPosition(pieces_, Piece.wBishop);
        BQueenBB = Utility.GetBBofPosition(pieces_, Piece.bQueen);
        WQueenBB = Utility.GetBBofPosition(pieces_, Piece.wQueen);
        BKingBB = Utility.GetBBofPosition(pieces_, Piece.bKing);
        WKingBB = Utility.GetBBofPosition(pieces_, Piece.wKing);
        BKnightBB = Utility.GetBBofPosition(pieces_, Piece.bKnight);
        WKnightBB = Utility.GetBBofPosition(pieces_, Piece.wKnight);
        BRookBB = Utility.GetBBofPosition(pieces_, Piece.bRook);
        WRookBB = Utility.GetBBofPosition(pieces_, Piece.wRook);

        WhitePiecesBB = WPawnBB | WBishopBB | WKingBB | WKnightBB | WRookBB | WQueenBB;
        BlackPiecesBB = BPawnBB | BBishopBB | BKingBB | BKnightBB | BRookBB | BQueenBB;
    }

    public Chessboard() { }

    public Chessboard(
        BitBoard wKingBB, BitBoard bKingBB,
        BitBoard wKnightBB, BitBoard bKnightBB,
        BitBoard wQueenBB, BitBoard bQueenBB,
        BitBoard wRookBB, BitBoard bRookBB,
        BitBoard wBishopBB, BitBoard bBishopBB,
        BitBoard wPawnBB, BitBoard bPawnBB,
        bool afterCapturePly,
        BitBoard wCtrlBB,
        BitBoard bCtrlBB,
        int BoardIndex,
        int parentIndex
    ) {
        this.WKingBB = wKingBB;
        this.BKingBB = bKingBB;
        this.WKnightBB = wKnightBB;
        this.BKnightBB = bKnightBB;
        this.WQueenBB = wQueenBB;
        this.BQueenBB = bQueenBB;
        this.WRookBB = wRookBB;
        this.BRookBB = bRookBB;
        this.WBishopBB = wBishopBB;
        this.BBishopBB = bBishopBB;
        this.WPawnBB = wPawnBB;
        this.BPawnBB = bPawnBB;
        this.AfterCapturePly = afterCapturePly;

        this.WhitePiecesBB = wKingBB | wKnightBB | wQueenBB | wRookBB | wBishopBB | wPawnBB;
        this.BlackPiecesBB = bKingBB | bKnightBB | bQueenBB | bRookBB | bBishopBB | bPawnBB;

        WControlledSqrBB = wCtrlBB;
        BControlledSqrBB = bCtrlBB;

        this.boardIndex = BoardIndex;
        this.parentIndex = parentIndex;
    }

    //berechnet neue BBs für die kontrollierten sqrs der beiden seiten
    public void UpdateAttackedSqrBB(Span<Vision> visions, bool forWhite) {
        BitBoard attackSqrBB = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            BitBoard bb = v.MoveBB;

            //pawns müssen gesondert berechnet werden wegen des unterschieds zwischen bewegung und schlagzug
            if (v.pieceType == Piece.wPawn || v.pieceType == Piece.bPawn) {
                bb &= ~MoveSets.VerticalLineBB(v.PosIndex % 8);

                int y = v.PosIndex >> 3;
                int x = v.PosIndex & 7;

                bb |= BB_Lookup.GetPawnAttackSqrs(x, y, forWhite);
            }

            attackSqrBB |= bb;
        }

        if (forWhite) {
            WControlledSqrBB = attackSqrBB;
        } else {
            BControlledSqrBB = attackSqrBB;
        }
    }

    public static Chessboard FromFEN(string fen) {
        return new Chessboard(Utility.FENtoPosition(fen));
    }

    /// <summary>
    /// Returnt immer ein Piece, dh davor muss überprüft werden ob hier überhaupt ein Piece existiert
    /// | Sehr ineffiziente Funktion
    /// </summary>
    /// <param name="posIndex"></param>
    /// <returns></returns>
    public Piece GetPieceAt(int posIndex) {
        //kann optimiert werden mit blackPiecesBB und whitePiecesBB,
        //aber diese funktion ist nicht dafür gedacht in performance-critical
        //teilen des bots ausgeführt zu werden
        if (MoveSets.IsBitSet(WPawnBB, posIndex))
            return Piece.wPawn;
        if (MoveSets.IsBitSet(BPawnBB, posIndex))
            return Piece.bPawn;
        if (MoveSets.IsBitSet(WKingBB, posIndex))
            return Piece.wKing;
        if (MoveSets.IsBitSet(BKingBB, posIndex))
            return Piece.bKing;
        if (MoveSets.IsBitSet(WKnightBB, posIndex))
            return Piece.wKnight;
        if (MoveSets.IsBitSet(BKnightBB, posIndex))
            return Piece.bKnight;
        if (MoveSets.IsBitSet(WQueenBB, posIndex))
            return Piece.wQueen;
        if (MoveSets.IsBitSet(BQueenBB, posIndex))
            return Piece.bQueen;
        if (MoveSets.IsBitSet(WRookBB, posIndex))
            return Piece.wRook;
        if (MoveSets.IsBitSet(BRookBB, posIndex))
            return Piece.bRook;
        if (MoveSets.IsBitSet(WBishopBB, posIndex))
            return Piece.wBishop;
        if (MoveSets.IsBitSet(BBishopBB, posIndex))
            return Piece.bBishop;

        return 0;
    }

    public bool HasPieceAt(int posIndex) {
        return MoveSets.IsBitSet(WhitePiecesBB | BlackPiecesBB, posIndex);
    }

    /// <summary>
    /// Kann benutzt werden um die Farbe eines Pieces auf einem Sqr zu checken, Davor muss überprüft werden ob hier überhaupt ein Piece existiert !!!
    /// </summary>
    /// <returns></returns>
    public bool HasWhitePieceAt(int index) {
        return MoveSets.IsBitSet(WhitePiecesBB, index);
    }

    //kein unterschied zwischen weißen und schwarzen pins, weil sowieso nach jedem zug das BB aktualisiert werden muss

    /// <summary>
    /// forWhite = white wird gepinned
    /// </summary>
    /// <param name="forWhite"></param>
    public void GeneratePinnedPieceBB(bool forWhite) {
        int kingIndex;

        BitBoard rookSightlines;
        BitBoard bishopSightlines;

        //enemy pieces die sightlines auf den king haben, aka intersections of sightlines with pieces
        BitBoard intersectionsStraight;
        BitBoard intersectionDiags;

        if (forWhite) {
            kingIndex = Utility.FindSingleSetBit(WKingBB);

            rookSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex);
            bishopSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wBishop, kingIndex);


            intersectionsStraight = rookSightlines & (BRookBB | BQueenBB);
            intersectionDiags = bishopSightlines & (BBishopBB | BQueenBB);
        } else {
            kingIndex = Utility.FindSingleSetBit(BKingBB);

            rookSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex);
            bishopSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wBishop, kingIndex);

            intersectionsStraight = rookSightlines & (WRookBB | WQueenBB);
            intersectionDiags = bishopSightlines & (WBishopBB | WQueenBB);
        }

        BitBoard friendsInSightlines = 0;
        int y = kingIndex >> 3;
        int x = kingIndex & 7;

        BitBoard sameColorPieces = forWhite ? WhitePiecesBB : BlackPiecesBB;
        BitBoard enemyBlockers; //muss in funktion angepasst werden da auch bRook bBishop blocken kann


        if (intersectionsStraight != 0) {
            enemyBlockers = forWhite ? BKnightBB | BBishopBB | BPawnBB : WKnightBB | WBishopBB | WPawnBB;

            BitBoard intersectionHoriWest = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex, kingIndex - x);
            intersectionHoriWest = intersectionHoriWest == 0 ? 0 : MoveSets.InterpolateHorizontal(kingIndex, MoveSets.GetBiggestBit(intersectionHoriWest));
            intersectionHoriWest = ValidatePin(intersectionHoriWest, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionHoriEast = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex + (7 - x), kingIndex);
            intersectionHoriEast = intersectionHoriEast == 0 ? 0 : MoveSets.InterpolateHorizontal(MoveSets.GetSmallestBit(intersectionHoriEast), kingIndex);
            intersectionHoriEast = ValidatePin(intersectionHoriEast, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionVertiBottom = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex, kingIndex - y * 8);
            intersectionVertiBottom = intersectionVertiBottom == 0 ? 0 : MoveSets.InterpolateVertical(kingIndex, MoveSets.GetBiggestBit(intersectionVertiBottom));
            intersectionVertiBottom = ValidatePin(intersectionVertiBottom, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionVertiTop = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex + (8 - y) * 8, kingIndex);
            intersectionVertiTop = intersectionVertiTop == 0 ? 0 : MoveSets.InterpolateVertical(MoveSets.GetSmallestBit(intersectionVertiTop), kingIndex);
            intersectionVertiTop = ValidatePin(intersectionVertiTop, sameColorPieces, enemyBlockers, kingIndex);

            //wenn mehr oder weniger als ein piece der eigenen farbe auf der pinLine steht ist es kein pin

            friendsInSightlines |= sameColorPieces & (intersectionHoriEast | intersectionHoriWest | intersectionVertiBottom | intersectionVertiTop);
        }

        if (intersectionDiags != 0) {
            enemyBlockers = forWhite ? BKnightBB | BRookBB | BPawnBB : WKnightBB | WRookBB | WPawnBB;

            BitBoard antiDiag = MoveSets.GetAntiDiagonal(x, y);
            int nw = MoveSets.GetBiggestBit(antiDiag);
            BitBoard intersectionDiagNW = intersectionDiags & MoveSets.InterpolateAntiDiagonal(nw, kingIndex);
            intersectionDiagNW = intersectionDiagNW == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(MoveSets.GetSmallestBit(intersectionDiagNW), kingIndex);
            intersectionDiagNW = ValidatePin(intersectionDiagNW, sameColorPieces, enemyBlockers, kingIndex);

            int se = MoveSets.GetSmallestBit(antiDiag);
            BitBoard intersectionDiagSE = intersectionDiags & MoveSets.InterpolateAntiDiagonal(kingIndex, se);
            intersectionDiagSE = intersectionDiagSE == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(kingIndex, MoveSets.GetBiggestBit(intersectionDiagSE));
            intersectionDiagSE = ValidatePin(intersectionDiagSE, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard diag = MoveSets.GetDiagonal(x, y);
            int ne = MoveSets.GetBiggestBit(diag);
            BitBoard intersectionDiagNE = intersectionDiags & MoveSets.InterpolateDiagonal(ne, kingIndex);
            intersectionDiagNE = intersectionDiagNE == 0 ? 0 : MoveSets.InterpolateDiagonal(MoveSets.GetSmallestBit(intersectionDiagNE), kingIndex);
            intersectionDiagNE = ValidatePin(intersectionDiagNE, sameColorPieces, enemyBlockers, kingIndex);

            int sw = MoveSets.GetSmallestBit(diag);
            BitBoard intersectionDiagSW = intersectionDiags & MoveSets.InterpolateDiagonal(kingIndex, sw);
            intersectionDiagSW = intersectionDiagSW == 0 ? 0 : MoveSets.InterpolateDiagonal(kingIndex, MoveSets.GetBiggestBit(intersectionDiagSW));
            intersectionDiagSW = ValidatePin(intersectionDiagSW, sameColorPieces, enemyBlockers, kingIndex);

            friendsInSightlines |= intersectionDiagNE | intersectionDiagNW | intersectionDiagSE | intersectionDiagSW;
        }

        pinnedBB = friendsInSightlines & ~WKingBB & ~BKingBB;//damit niemand auf die idee kommt, dass der king gepinnt ist
    }


    private BitBoard ValidatePin(BitBoard sightLine, BitBoard sameColorPieces, BitBoard enemyBlockers, int kingIndex) {
        sameColorPieces = Utility.SetBBtoNullAt(sameColorPieces, kingIndex);

        if ((sightLine & enemyBlockers) != 0) return 0;//enemy steht auf der pinLine

        return MoveSets.CountSetBits(sightLine & sameColorPieces) == 1 ? sightLine : 0;
    }

    /// <summary>
    /// Reihenfolge (nach Wert sortiert, aufsteigend): Pawn, Knight, Bishop, Rook, Queen
    /// </summary>
    public BitBoard[] CheckPieceBBs = new BitBoard[5];

    //forWhite = white is in check
    public bool IsInCheck(bool forWhite) {

        BitBoard knightBB, queenBB, rookBB, bishopBB, pawnBB, sameColorPieces;
        BitBoard pawnDoub;//einzigen beiden bits wo pawns checken können
        int kingIndex;

        if (forWhite) {
            sameColorPieces = WhitePiecesBB;
            knightBB = BKnightBB;
            queenBB = BQueenBB;
            rookBB = BRookBB;
            bishopBB = BBishopBB;
            pawnBB = BPawnBB;

            kingIndex = Utility.FindSingleSetBit(WKingBB);

            int x = kingIndex & 7;
            int y = kingIndex >> 3;

            pawnDoub = MoveSets.HorizontalLineBB(y + 1) & (0b101ul << (kingIndex + 7)) & BPawnBB;
        } else {
            sameColorPieces = BlackPiecesBB;
            knightBB = WKnightBB;
            queenBB = WQueenBB;
            rookBB = WRookBB;
            bishopBB = WBishopBB;
            pawnBB = WPawnBB;

            kingIndex = Utility.FindSingleSetBit(BKingBB);

            int x = kingIndex & 7;
            int y = kingIndex >> 3;

            pawnDoub = MoveSets.HorizontalLineBB(y - 1) & (0b101ul << (kingIndex - 9)) & WPawnBB;
        }


        //king perspective 
        BitBoard kingPersKnight = BB_Lookup.GetBBforPieceAtSqr(Piece.wKnight, kingIndex);
        /* MoveSets.GetPseudoTargetSqrsRook(BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex), kingIndex) */
        BitBoard kingPersRook = MoveSets.GetSliderPseudoLegalMoves(this, kingIndex, sameColorPieces, Piece.wRook);
        BitBoard kingPersBishop = MoveSets.GetSliderPseudoLegalMoves(this, kingIndex, sameColorPieces, Piece.wBishop);
        BitBoard kingPersQueen = kingPersBishop | kingPersRook;


        //hier sind bits gesetzt wo pieces stehen die check geben
        //wenn leer gibt kein piece check
        CheckPieceBBs[1] = knightBB & kingPersKnight;
        CheckPieceBBs[4] = queenBB & kingPersQueen;
        CheckPieceBBs[3] = rookBB & kingPersRook;
        CheckPieceBBs[2] = bishopBB & kingPersBishop;

        CheckPieceBBs[0] = pawnBB & pawnDoub;

        return (CheckPieceBBs[0] | CheckPieceBBs[1] | CheckPieceBBs[2] | CheckPieceBBs[3] | CheckPieceBBs[4]) != 0;
    }


    /// <summary>
    /// Generiert stumpf ein Board wo das Piece von StartIndex zu EndIndex bewegt wurde
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    public Chessboard GenerateBoardWithMove(int startIndex, int endIndex, Piece type) {
        bool isCapture = MoveSets.IsBitSet(BlackPiecesBB | WhitePiecesBB, endIndex);

        //der king kann gecaptured werden weil das capturen des king essentiell für checkmate detection ist
        BitBoard wKingBB_ = Utility.SetBBtoNullAt(WKingBB, endIndex);
        BitBoard bKingBB_ = Utility.SetBBtoNullAt(BKingBB, endIndex);

        BitBoard wKnightBB_ = Utility.SetBBtoNullAt(WKnightBB, endIndex);
        BitBoard bKnightBB_ = Utility.SetBBtoNullAt(BKnightBB, endIndex);
        BitBoard wQueenBB_ = Utility.SetBBtoNullAt(WQueenBB, endIndex);
        BitBoard bQueenBB_ = Utility.SetBBtoNullAt(BQueenBB, endIndex);
        BitBoard wRookBB_ = Utility.SetBBtoNullAt(WRookBB, endIndex);
        BitBoard bRookBB_ = Utility.SetBBtoNullAt(BRookBB, endIndex);
        BitBoard wBishopBB_ = Utility.SetBBtoNullAt(WBishopBB, endIndex);
        BitBoard bBishopBB_ = Utility.SetBBtoNullAt(BBishopBB, endIndex);
        BitBoard wPawnBB_ = Utility.SetBBtoNullAt(WPawnBB, endIndex);
        BitBoard bPawnBB_ = Utility.SetBBtoNullAt(BPawnBB, endIndex);


        //alle bitboards müssen geupdated werden
        switch (type) {
            case Piece.wPawn:
                wPawnBB_ = Utility.UpdateBB(WPawnBB, startIndex, endIndex);
                //auto queen
                if (endIndex > 55) {
                    wPawnBB_ = Utility.SetBBtoNullAt(wPawnBB_, endIndex);
                    wQueenBB_ += 1ul << endIndex;
                }
                break;

            case Piece.bPawn:
                bPawnBB_ = Utility.UpdateBB(BPawnBB, startIndex, endIndex);

                if (endIndex < 8) {
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
                wKnightBB_ = Utility.UpdateBB(WKnightBB, startIndex, endIndex);
                break;

            case Piece.bKnight:
                bKnightBB_ = Utility.UpdateBB(BKnightBB, startIndex, endIndex);
                break;

            case Piece.wQueen:
                wQueenBB_ = Utility.UpdateBB(WQueenBB, startIndex, endIndex);
                break;

            case Piece.bQueen:
                bQueenBB_ = Utility.UpdateBB(BQueenBB, startIndex, endIndex);
                break;

            case Piece.wRook:
                wRookBB_ = Utility.UpdateBB(WRookBB, startIndex, endIndex);
                break;

            case Piece.bRook:
                bRookBB_ = Utility.UpdateBB(BRookBB, startIndex, endIndex);
                break;

            case Piece.wBishop:
                wBishopBB_ = Utility.UpdateBB(WBishopBB, startIndex, endIndex);
                break;

            case Piece.bBishop:
                bBishopBB_ = Utility.UpdateBB(BBishopBB, startIndex, endIndex);
                break;
        }

        BoardCount++;

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
            isCapture,
            WControlledSqrBB,
            BControlledSqrBB,
            BoardCount,
            boardIndex
        );
    }


}
