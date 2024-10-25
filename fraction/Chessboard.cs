using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace fraction;
public class Chessboard {
    private BitBoard bRookBB = new(0b1000_0001, 0, 0, 0, 0, 0, 0, 0);
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
    private BitBoard wPawnBB = new(0, 0, 0, 0, 0, 0, 0b1111_1111, 0);
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
    public BitBoard WhitePiecesBB => wPawnBB | wBishopBB | wRookBB | wKnightBB | wKingBB | wQueenBB;
    public BitBoard BlackPiecesBB => bPawnBB | bBishopBB | bRookBB | bKnightBB | bKingBB | bQueenBB;

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


    public BitBoard WControlledSqrBB { get => wControlledSqrBB; set => wControlledSqrBB = value; }
    public BitBoard BControlledSqrBB { get => bControlledSqrBB; set => bControlledSqrBB = value; }

    public bool AfterCapturePly { get; set; } = false;

    //dient dem tracken einzelner boards im perft tree beim debuggen
    public int boardIndex = 0, parentIndex = 0;
    public static int BoardCount = 0;
    public ulong pinnedBB = 0;

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
        ulong wKingBB, ulong bKingBB,
        ulong wKnightBB, ulong bKnightBB,
        ulong wQueenBB, ulong bQueenBB,
        ulong wRookBB, ulong bRookBB,
        ulong wBishopBB, ulong bBishopBB,
        ulong wPawnBB, ulong bPawnBB,
        bool afterCapturePly,
        ulong wCtrlBB,
        ulong bCtrlBB,
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
        ulong attackSqrBB = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            ulong bb = v.MoveBB;

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
    /// After GeneratePinnedPieceBB(...) is called, this contains BBs to pinLines in alle directions in the following order:
    /// Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, TopLeft (<=> clockwise, starting with Top)
    /// </summary>


    //forWhite = white is pinned
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

        ulong sameColorPieces = forWhite ? WhitePiecesBB : BlackPiecesBB;


        if (intersectionsStraight != 0) {
            ulong intersectionHoriWest = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex, kingIndex - x);
            intersectionHoriWest = intersectionHoriWest == 0 ? 0 : MoveSets.InterpolateHorizontal(kingIndex, MoveSets.GetBiggestBit(intersectionHoriWest));
            intersectionHoriWest = MoveSets.CountSetBits(intersectionHoriWest & sameColorPieces) == 2 ? intersectionHoriWest : 0;//1x king, 1x piece, dh 2 bits

            ulong intersectionHoriEast = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex + (7 - x), kingIndex);
            intersectionHoriEast = intersectionHoriEast == 0 ? 0 : MoveSets.InterpolateHorizontal(MoveSets.GetSmallestBit(intersectionHoriEast), kingIndex);
            intersectionHoriEast = MoveSets.CountSetBits(intersectionHoriEast & sameColorPieces) == 2 ? intersectionHoriEast : 0;

            ulong intersectionVertiBottom = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex, kingIndex - y * 8);
            intersectionVertiBottom = intersectionVertiBottom == 0 ? 0 : MoveSets.InterpolateVertical(kingIndex, MoveSets.GetBiggestBit(intersectionVertiBottom));
            intersectionVertiBottom = MoveSets.CountSetBits(intersectionVertiBottom & sameColorPieces) == 2 ? intersectionVertiBottom : 0;

            ulong intersectionVertiTop = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex + (8 - y) * 8, kingIndex);
            intersectionVertiTop = intersectionVertiTop == 0 ? 0 : MoveSets.InterpolateVertical(MoveSets.GetSmallestBit(intersectionVertiTop), kingIndex);
            intersectionVertiTop = MoveSets.CountSetBits(intersectionVertiTop & sameColorPieces) == 2 ? intersectionVertiTop : 0;

            //wenn mehr oder weniger als ein piece der eigenen farbe auf der pinLine steht ist es kein pin

            friendsInSightlines |= sameColorPieces & (intersectionHoriEast | intersectionHoriWest | intersectionVertiBottom | intersectionVertiTop);

        }

        if (intersectionDiags != 0) {
            ulong antiDiag = MoveSets.GetAntiDiagonal(x, y);
            int nw = MoveSets.GetBiggestBit(antiDiag);
            ulong intersectionDiagNW = intersectionDiags & MoveSets.InterpolateAntiDiagonal(nw, kingIndex);
            intersectionDiagNW = intersectionDiagNW == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(MoveSets.GetSmallestBit(intersectionDiagNW), kingIndex);
            intersectionDiagNW = MoveSets.CountSetBits(intersectionDiagNW & sameColorPieces) == 2 ? intersectionDiagNW : 0;

            int se = MoveSets.GetSmallestBit(antiDiag);
            ulong intersectionDiagSE = intersectionDiags & MoveSets.InterpolateAntiDiagonal(kingIndex, se);
            intersectionDiagSE = intersectionDiagSE == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(kingIndex, MoveSets.GetBiggestBit(intersectionDiagSE));
            intersectionDiagSE = MoveSets.CountSetBits(intersectionDiagSE & sameColorPieces) == 2 ? intersectionDiagSE : 0;

            ulong diag = MoveSets.GetDiagonal(x, y);
            int ne = MoveSets.GetBiggestBit(diag);
            ulong intersectionDiagNE = intersectionDiags & MoveSets.InterpolateDiagonal(ne, kingIndex);
            intersectionDiagNE = intersectionDiagNE == 0 ? 0 : MoveSets.InterpolateDiagonal(MoveSets.GetSmallestBit(intersectionDiagNE), kingIndex);
            intersectionDiagNE = MoveSets.CountSetBits(intersectionDiagNE & sameColorPieces) == 2 ? intersectionDiagNE : 0;

            int sw = MoveSets.GetSmallestBit(diag);
            ulong intersectionDiagSW = intersectionDiags & MoveSets.InterpolateDiagonal(kingIndex, sw);
            intersectionDiagSW = intersectionDiagSW == 0 ? 0 : MoveSets.InterpolateDiagonal(kingIndex, MoveSets.GetBiggestBit(intersectionDiagSW));
            intersectionDiagSW = MoveSets.CountSetBits(intersectionDiagSW & sameColorPieces) == 2 ? intersectionDiagSW : 0;

            friendsInSightlines |= intersectionDiagNE | intersectionDiagNW | intersectionDiagSE | intersectionDiagSW;
        }

        /* TODO!!! sicherstellen dass nur EIN piece der eigenen farbe auf den linien steht */

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
