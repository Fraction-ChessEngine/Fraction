using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace fraction;
public class Chessboard {
    public static int BoardCount = 0;
    //dient dem tracken einzelner boards im perft tree beim debuggen
    public int boardIndex;
    public int parentIndex;
    /* private BitBoard bRookBB = new(0b1000_0001, 0, 0, 0, 0, 0, 0, 0);
    private BitBoard wRookBB = new(0, 0, 0, 0, 0, 0, 0, 0b1000_0001);
    private BitBoard bBishopBB = new(0b0010_0100, 0, 0, 0, 0, 0, 0, 0);
    private BitBoard wBishopBB = new(0, 0, 0, 0, 0, 0, 0, 0b0010_0100);
    private BitBoard bKnightBB = new(0b0100_0010, 0, 0, 0, 0, 0, 0, 0);
    private BitBoard wKnightBB = new(0, 0, 0, 0, 0, 0, 0, 0b0100_0010);
    private BitBoard bQueenBB = new(0b0001_0000, 0, 0, 0, 0, 0, 0, 0);
    private BitBoard wQueenBB = new(0, 0, 0, 0, 0, 0, 0, 0b0001_0000);
    private BitBoard bKingBB = new(0b0000_1000, 0, 0, 0, 0, 0, 0, 0);
    private BitBoard wKingBB = new(0, 0, 0, 0, 0, 0, 0, 0b0000_1000);
    private BitBoard bPawnBB = new(0, 0b1111_1111, 0, 0, 0, 0, 0, 0);
    private BitBoard wPawnBB = new(0, 0, 0, 0, 0, 0, 0b1111_1111, 0); */

    private BitBoard bRookBB = 0b10000001ul << 56;
    private BitBoard wRookBB = 0b10000001ul;
    private BitBoard bBishopBB = 0b0100100ul << 56;
    private BitBoard wBishopBB = 0b0100100ul;
    private BitBoard bKnightBB = 0b01000010ul << 56;
    private BitBoard wKnightBB = 0b01000010ul;
    private BitBoard bQueenBB = 0b00001000ul << 56;
    private BitBoard wQueenBB = 0b00001000ul;
    private BitBoard bKingBB = 0b00010000ul << 56;
    private BitBoard wKingBB = 0b00010000ul;
    private BitBoard bPawnBB = 0b11111111ul << 48;
    private BitBoard wPawnBB = 0b11111111ul << 8;
    //private BitBoard whitePiecesBB = 0b0000000000000000000000000000000000000000000000001111111111111111;
    //private BitBoard blackPiecesBB = 0b1111111111111111000000000000000000000000000000000000000000000000;
    private BitBoard wControlledSqrBB = 0;// 0b11111111ul << 16;
    private BitBoard bControlledSqrBB = 0;//0b11111111ul << 40;

    //0 ist ganz rechts, 63 ist ganz links, 0=a1, 63=h8
    public BitBoard BRookBB { get => bRookBB; set => bRookBB = value; }
    public BitBoard WRookBB { get => wRookBB; set => wRookBB = value; }
    public BitBoard BBishopBB { get => bBishopBB; set => bBishopBB = value; }
    public BitBoard WBishopBB { get => wBishopBB; set => wBishopBB = value; }
    public BitBoard BKnightBB { get => bKnightBB; set => bKnightBB = value; }
    public BitBoard WKnightBB { get => wKnightBB; set => wKnightBB = value; }
    public BitBoard WQueenBB { get => wQueenBB; set => wQueenBB = value; }
    public BitBoard BQueenBB { get => bQueenBB; set => bQueenBB = value; }
    public BitBoard WKingBB { get => wKingBB; set => wKingBB = value; }
    public BitBoard BKingBB { get => bKingBB; set => bKingBB = value; }
    public BitBoard WPawnBB { get => wPawnBB; set => wPawnBB = value; }
    public BitBoard BPawnBB { get => bPawnBB; set => bPawnBB = value; }
    public BitBoard WControlledSqrBB { get => wControlledSqrBB; set => wControlledSqrBB = value; }
    public BitBoard BControlledSqrBB { get => bControlledSqrBB; set => bControlledSqrBB = value; }

    public BitBoard WhitePiecesBB => wPawnBB | wBishopBB | wRookBB | wKnightBB | wKingBB | wQueenBB;
    public BitBoard BlackPiecesBB => bPawnBB | bBishopBB | bRookBB | bKnightBB | bKingBB | bQueenBB;

    public bool AfterCapturePly { get; set; } = false;

    public BitBoard pinnedBB = 0;

    public BitBoard this[Piece type] {
        get => type switch {
            Piece.wPawn => wPawnBB,
            Piece.wBishop => wBishopBB,
            Piece.wKnight => wKnightBB,
            Piece.wRook => wRookBB,
            Piece.wKing => wKingBB,
            Piece.wQueen => wQueenBB,
            Piece.bPawn => bPawnBB,
            Piece.bBishop => bBishopBB,
            Piece.bKnight => bKnightBB,
            Piece.bRook => bRookBB,
            Piece.bKing => bKingBB,
            Piece.bQueen => bQueenBB,
            _ => throw new UnreachableException(),
        };
        set {
            switch (type) {
                case Piece.wPawn:
                    wPawnBB = value;
                    break;

                case Piece.wBishop:
                    wBishopBB = value;
                    break;

                case Piece.wKnight:
                    wKnightBB = value;
                    break;

                case Piece.wRook:
                    wRookBB = value;
                    break;

                case Piece.wKing:
                    wKingBB = value;
                    break;

                case Piece.wQueen:
                    wQueenBB = value;
                    break;

                case Piece.bPawn:
                    bPawnBB = value;
                    break;

                case Piece.bBishop:
                    bBishopBB = value;
                    break;

                case Piece.bKnight:
                    bKnightBB = value;
                    break;

                case Piece.bRook:
                    bRookBB = value;
                    break;

                case Piece.bKing:
                    bKingBB = value;
                    break;

                case Piece.bQueen:
                    bQueenBB = value;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

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

        //whitePiecesBB = wPawnBB | wBishopBB | wKingBB | wKnightBB | wRookBB | wQueenBB;
        //blackPiecesBB = bPawnBB | bBishopBB | bKingBB | bKnightBB | bRookBB | bQueenBB;
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

        //this.whitePiecesBB = wKingBB | wKnightBB | wQueenBB | wRookBB | wBishopBB | wPawnBB;
        //this.blackPiecesBB = bKingBB | bKnightBB | bQueenBB | bRookBB | bBishopBB | bPawnBB;

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
            if (v.PieceType == Piece.wPawn || v.PieceType == Piece.bPawn) {
                bb &= ~BitBoard.VerticalLine(v.PosIndex % 8);

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
        if (WPawnBB[posIndex])
            return Piece.wPawn;
        if (BPawnBB[posIndex])
            return Piece.bPawn;
        if (WKingBB[posIndex])
            return Piece.wKing;
        if (BKingBB[posIndex])
            return Piece.bKing;
        if (WKnightBB[posIndex])
            return Piece.wKnight;
        if (BKnightBB[posIndex])
            return Piece.bKnight;
        if (WQueenBB[posIndex])
            return Piece.wQueen;
        if (BQueenBB[posIndex])
            return Piece.bQueen;
        if (WRookBB[posIndex])
            return Piece.wRook;
        if (BRookBB[posIndex])
            return Piece.bRook;
        if (WBishopBB[posIndex])
            return Piece.wBishop;
        if (BBishopBB[posIndex])
            return Piece.bBishop;

        return 0;
    }

    public bool HasPieceAt(int posIndex) {
        return WhitePiecesBB[posIndex] || BlackPiecesBB[posIndex];
    }

    /// <summary>
    /// Kann benutzt werden um die Farbe eines Pieces auf einem Sqr zu checken, Davor muss überprüft werden ob hier überhaupt ein Piece existiert !!!
    /// </summary>
    /// <returns></returns>
    public bool HasWhitePieceAt(int index) {
        return WhitePiecesBB[index];
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
            kingIndex = wKingBB.LowestOne;

            rookSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex);
            bishopSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wBishop, kingIndex);


            intersectionsStraight = rookSightlines & (BRookBB | BQueenBB);
            intersectionDiags = bishopSightlines & (BBishopBB | BQueenBB);
        } else {
            kingIndex = bKingBB.LowestOne;

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
            intersectionHoriWest = intersectionHoriWest == 0 ? 0 : MoveSets.InterpolateHorizontal(kingIndex, intersectionHoriWest.BitScanReverse);
            intersectionHoriWest = ValidatePin(intersectionHoriWest, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionHoriEast = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex + (7 - x), kingIndex);
            intersectionHoriEast = intersectionHoriEast == 0 ? 0 : MoveSets.InterpolateHorizontal(intersectionHoriEast.LowestOne, kingIndex);
            intersectionHoriEast = ValidatePin(intersectionHoriEast, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionVertiBottom = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex, kingIndex - y * 8);
            intersectionVertiBottom = intersectionVertiBottom == 0 ? 0 : MoveSets.InterpolateVertical(kingIndex, intersectionVertiBottom.BitScanReverse);
            intersectionVertiBottom = ValidatePin(intersectionVertiBottom, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionVertiTop = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex + (8 - y) * 8, kingIndex);
            intersectionVertiTop = intersectionVertiTop == 0 ? 0 : MoveSets.InterpolateVertical(intersectionVertiTop.LowestOne, kingIndex);
            intersectionVertiTop = ValidatePin(intersectionVertiTop, sameColorPieces, enemyBlockers, kingIndex);

            //wenn mehr oder weniger als ein piece der eigenen farbe auf der pinLine steht ist es kein pin

            friendsInSightlines |= sameColorPieces & (intersectionHoriEast | intersectionHoriWest | intersectionVertiBottom | intersectionVertiTop);
        }

        if (intersectionDiags != 0) {
            enemyBlockers = forWhite ? BKnightBB | BRookBB | BPawnBB : WKnightBB | WRookBB | WPawnBB;

            BitBoard antiDiag = BitBoard.AntiDiagonal(x, y);
            int nw = antiDiag.BitScanReverse;
            BitBoard intersectionDiagNW = intersectionDiags & MoveSets.InterpolateAntiDiagonal(nw, kingIndex);
            intersectionDiagNW = intersectionDiagNW == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(intersectionDiagNW.LowestOne, kingIndex);
            intersectionDiagNW = ValidatePin(intersectionDiagNW, sameColorPieces, enemyBlockers, kingIndex);

            int se = antiDiag.LowestOne;
            BitBoard intersectionDiagSE = intersectionDiags & MoveSets.InterpolateAntiDiagonal(kingIndex, se);
            intersectionDiagSE = intersectionDiagSE == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(kingIndex, intersectionDiagSE.BitScanReverse);
            intersectionDiagSE = ValidatePin(intersectionDiagSE, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard diag = BitBoard.Diagonal(x, y);
            int ne = diag.BitScanReverse;
            BitBoard intersectionDiagNE = intersectionDiags & MoveSets.InterpolateDiagonal(ne, kingIndex);
            intersectionDiagNE = intersectionDiagNE == 0 ? 0 : MoveSets.InterpolateDiagonal(intersectionDiagNE.LowestOne, kingIndex);
            intersectionDiagNE = ValidatePin(intersectionDiagNE, sameColorPieces, enemyBlockers, kingIndex);

            int sw = diag.LowestOne;
            BitBoard intersectionDiagSW = intersectionDiags & MoveSets.InterpolateDiagonal(kingIndex, sw);
            intersectionDiagSW = intersectionDiagSW == 0 ? 0 : MoveSets.InterpolateDiagonal(kingIndex, intersectionDiagSW.BitScanReverse);
            intersectionDiagSW = ValidatePin(intersectionDiagSW, sameColorPieces, enemyBlockers, kingIndex);

            friendsInSightlines |= intersectionDiagNE | intersectionDiagNW | intersectionDiagSE | intersectionDiagSW;
        }

        pinnedBB = friendsInSightlines & ~WKingBB & ~BKingBB;//damit niemand auf die idee kommt, dass der king gepinnt ist
    }

    public Chessboard Clone() {
        return new() {
            wPawnBB = wPawnBB,
            wKingBB = wKingBB,
            wRookBB = wRookBB,
            wQueenBB = wQueenBB,
            wBishopBB = wBishopBB,
            wKnightBB = wKnightBB,
            //whitePiecesBB = whitePiecesBB,
            wControlledSqrBB = wControlledSqrBB,
            bPawnBB = bPawnBB,
            bKingBB = bKingBB,
            bRookBB = bRookBB,
            bQueenBB = bQueenBB,
            bBishopBB = bBishopBB,
            bKnightBB = bKnightBB,
            //blackPiecesBB = blackPiecesBB,
            bControlledSqrBB = bControlledSqrBB,
            AfterCapturePly = AfterCapturePly,
            pinnedBB = pinnedBB,
            boardIndex = ++BoardCount,
            parentIndex = boardIndex,
        };
    }

    public void Move(int start, int end, Piece type) {
        AfterCapturePly = BlackPiecesBB[end] || WhitePiecesBB[end];

        // essential for checkmate detection
        wKingBB[end] = false;
        bKingBB[end] = false;

        wKnightBB[end] = false;
        bKnightBB[end] = false;
        wQueenBB[end] = false;
        bQueenBB[end] = false;
        wRookBB[end] = false;
        bRookBB[end] = false;
        wBishopBB[end] = false;
        bBishopBB[end] = false;
        wPawnBB[end] = false;
        bPawnBB[end] = false;

        switch (type) {
            case Piece.wPawn:
                wPawnBB[start] = false;
                wPawnBB[end] = true;
                //auto queen
                if (end > 55) {
                    wPawnBB[end] = false;
                    wQueenBB[end] = true;
                }
                break;

            case Piece.bPawn:
                bPawnBB[start] = false;
                bPawnBB[end] = true;

                if (end < 8) {
                    bPawnBB[end] = false;
                    bQueenBB[end] = true;
                }
                break;

            //funktioniert weil es nur einen king geben darf
            case Piece.wKing:
                wKingBB = 1ul << end;
                break;

            case Piece.bKing:
                bKingBB = 1ul << end;
                break;

            default:
                BitBoard bb = this[type];
                bb[start] = false;
                bb[end] = true;
                this[type] = bb;
                break;
        }
    }


    private BitBoard ValidatePin(BitBoard sightLine, BitBoard sameColorPieces, BitBoard enemyBlockers, int kingIndex) {
        sameColorPieces[kingIndex] = false;

        if ((sightLine & enemyBlockers) != 0) return 0;//enemy steht auf der pinLine

        return (sightLine & sameColorPieces).PopCount == 1 ? sightLine : 0;
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

            kingIndex = wKingBB.LowestOne;

            int x = kingIndex & 7;
            int y = kingIndex >> 3;

            pawnDoub = BitBoard.HorizontalLine(y + 1) & (0b101ul << (kingIndex + 7)) & BPawnBB;
        } else {
            sameColorPieces = BlackPiecesBB;
            knightBB = WKnightBB;
            queenBB = WQueenBB;
            rookBB = WRookBB;
            bishopBB = WBishopBB;
            pawnBB = WPawnBB;

            kingIndex = bKingBB.LowestOne;

            int x = kingIndex & 7;
            int y = kingIndex >> 3;

            pawnDoub = BitBoard.HorizontalLine(y - 1) & (0b101ul << (kingIndex - 9)) & WPawnBB;
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
        Chessboard board = Clone();
        board.Move(startIndex, endIndex, type);
        return board;
    }
}
