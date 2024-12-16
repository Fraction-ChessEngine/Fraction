using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace fraction;



public class Chessboard {
    public static readonly int WKingSide = 0;
    public static readonly int WQueenSide = 1;
    public static readonly int BKingSide = 2;
    public static readonly int BQueenSide = 3;
    public static int BoardCount {get; private set;} = 0;
    //dient dem tracken einzelner boards im perft tree beim debuggen
    public int boardIndex;
    public int parentIndex;
    public Move lastMove;

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
    public int enPassantSqr = -1;
    public bool isCheckMate = false;
    private int Rights = 0b1111;//only the 4 lsb contain data
    public bool GetCastlingRights(int side) {
        return ((1 << side) & Rights) != 0;
    }
    public void SetCastlingRightsNullAt(int side) {
        Rights &= ~(1 << side);
    }


    public static readonly ulong[] CastleSqrs ={
        0b01000000ul,0b100ul,0b01000000ul << 56, 0b100ul << 56, 0//null wert für optimisation
    };
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
        int parentIndex,
    int[] castlingRights
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

    //calculates new BBs for the controlled sqrs of the given color
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

            //a piece cannot cover itself
            attackSqrBB |= bb & ~(1ul << v.PosIndex);
        }

        if (forWhite) {
            WControlledSqrBB = attackSqrBB;
        } else {
            BControlledSqrBB = attackSqrBB;
        }
    }

    static bool hasCastl(string data) {
        return (data.Contains("k")) || (data.Contains("q"))
           || (data.Contains("K")) || (data.Contains("Q"));
    }

    static bool hasNumber(string data) {
        return data.Contains("1") || data.Contains("2") || data.Contains("3") || data.Contains("4") ||
        data.Contains("5") || data.Contains("6") || data.Contains("7") || data.Contains("8");
    }

    public static (Chessboard, bool) FromFEN(string fen) {
        string[] data = fen.Split(' ');
        data = data[0..(data.Length - 2)];//removes the plys necessary for 50move rule

        bool forWhite = true;

        Chessboard cb = new Chessboard(Utility.FENtoPosition(data[0]));

        bool[] castl = [false, false, false, false];

        for (int i = 1; i < data.Length; i++) {
            string s = data[i];

            if (s == "w") {
                forWhite = true;
            } else if (s == "b") {
                forWhite = false;
            } else if (hasCastl(s)) {

                if (s.Contains("K")) castl[Chessboard.WKingSide] = true;
                if (s.Contains("Q")) castl[Chessboard.WQueenSide] = true;
                if (s.Contains("k")) castl[Chessboard.BKingSide] = true;
                if (s.Contains("q")) castl[Chessboard.BQueenSide] = true;
            } else if (hasNumber(s)) {
                cb.enPassantSqr = Utility.ANtoPos(s);
            }
        }

        //if there are no rooks, castlingrights need to be revoked for the given side
        if (!cb.wRookBB[0]) castl[Chessboard.WQueenSide] = false;
        if (!cb.wRookBB[7]) castl[Chessboard.WKingSide] = false;
        if (!cb.bRookBB[56]) castl[Chessboard.BQueenSide] = false;
        if (!cb.bRookBB[63]) castl[Chessboard.BKingSide] = false;

        for (int i = 0; i < castl.Length; i++) {
            bool b = castl[i];
            if (!b) cb.SetCastlingRightsNullAt(i);
        }



        return (cb, forWhite);
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

    //Contains every line between possible pinSliders, and the king (both exclusive)
    //Order: Clockwise, starting with VertiTop
    //Updates when GeneratePinnedPieceBB() is called
    private readonly BitBoard[] PinLines = new BitBoard[8];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bb">BitBoard with only the pinnedPiece set  </param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public BitBoard GetPinLineBB(BitBoard bb) {
        for (int i = 0; i < 8; i++) {
            BitBoard line = PinLines[i];
            if ((line & bb) != 0) return line;
        }

        throw new Exception("Pinned piece was not found on any generated pinLines");
    }

    //kein unterschied zwischen weißen und schwarzen pins, weil sowieso nach jedem zug das BB aktualisiert werden muss
    /// <summary>
    /// forWhite = white is pinned
    /// </summary>
    /// <param name="forWhite"></param>
    public void GeneratePinnedPieceBB(bool forWhite) {
        //needs to be reset for edgy edge cases
        for (int i = 0; i < 8; i++) {
            PinLines[i] = 0;
        }

        int kingIndex;

        BitBoard rookSightlines;
        BitBoard bishopSightlines;

        //enemy pieces with sightlines at king, aka intersections of sightlines with pieces
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
            //pieces of the other color that block this pin
            enemyBlockers = forWhite ? BKnightBB | BBishopBB | BPawnBB | BKingBB : WKnightBB | WBishopBB | WPawnBB | WKingBB;

            BitBoard intersectionHoriWest = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex, kingIndex - x);
            intersectionHoriWest = intersectionHoriWest == 0 ? 0 : MoveSets.InterpolateHorizontal(kingIndex, intersectionHoriWest.BitScanReverse);
            intersectionHoriWest = ValidatePin(intersectionHoriWest, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionHoriEast = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex + (7 - x), kingIndex);
            intersectionHoriEast = intersectionHoriEast == 0 ? 0 : MoveSets.InterpolateHorizontal(intersectionHoriEast.LowestOne, kingIndex);
            intersectionHoriEast = ValidatePin(intersectionHoriEast, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionVertiBottom = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex, kingIndex - y * 8);
            intersectionVertiBottom = intersectionVertiBottom == 0 ? 0 : MoveSets.InterpolateVertical(kingIndex, intersectionVertiBottom.BitScanReverse);
            intersectionVertiBottom = ValidatePin(intersectionVertiBottom, sameColorPieces, enemyBlockers, kingIndex);


            BitBoard intersectionVertiTop = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex + (7 - y) * 8, kingIndex);
            intersectionVertiTop = intersectionVertiTop == 0 ? 0 : MoveSets.InterpolateVertical(intersectionVertiTop.LowestOne, kingIndex);
            intersectionVertiTop = ValidatePin(intersectionVertiTop, sameColorPieces, enemyBlockers, kingIndex);

            //if there are more or less than one piece of the own color on the pinLine -> no valid pin

            friendsInSightlines |= sameColorPieces & (intersectionHoriEast | intersectionHoriWest | intersectionVertiBottom | intersectionVertiTop);
            PinLines[0] = intersectionVertiTop;
            PinLines[2] = intersectionHoriEast;
            PinLines[4] = intersectionVertiBottom;
            PinLines[6] = intersectionHoriWest;
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

            PinLines[1] = intersectionDiagNE;
            PinLines[3] = intersectionDiagSE;
            PinLines[5] = intersectionDiagSW;
            PinLines[7] = intersectionDiagNW;
        }


        pinnedBB = friendsInSightlines & ~WKingBB & ~BKingBB;//damit niemand auf die idee kommt, dass der king gepinnt ist

    }

    public void Copy(Chessboard board) {
        this.pinnedBB = board.pinnedBB;
        this.boardIndex = board.boardIndex;
        this.Rights = board.Rights;
        this.bKingBB = board.bKingBB;
        this.bPawnBB = board.bPawnBB;
        this.bRookBB = board.bRookBB;
        this.wKingBB = board.wKingBB;
        this.wPawnBB = board.wPawnBB;
        this.wRookBB = board.wRookBB;
        this.bQueenBB = board.bQueenBB;
        this.lastMove = board.lastMove;
        this.wQueenBB = board.wQueenBB;
        this.bBishopBB = board.bBishopBB;
        this.bKnightBB = board.bKnightBB;
        this.wBishopBB = board.wBishopBB;
        this.wKnightBB = board.wKnightBB;
        this.isCheckMate = board.isCheckMate;
        this.parentIndex = board.parentIndex;
        this.enPassantSqr = board.enPassantSqr;
        this.bControlledSqrBB = board.bControlledSqrBB;
        this.wControlledSqrBB = board.wControlledSqrBB;
    }

    public Chessboard Clone() {
        Chessboard board = (Chessboard) this.MemberwiseClone();
        board.boardIndex = BoardCount++;
        return board;
    }



    public void MakeSimpleMove(int start, int end, Piece type, Piece promotion = Piece.wQueen) {

        AfterCapturePly = BlackPiecesBB[end] || WhitePiecesBB[end];

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
                    PromoteTo(end, promotion);
                }
                break;

            case Piece.bPawn:
                bPawnBB[start] = false;
                bPawnBB[end] = true;

                if (end < 8) {
                    PromoteTo(end, promotion);
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

    public void MakeMove(Move move)
        => MakeMove(move.Start, move.End, this.GetPieceAt(move.Start), move.Promotion ?? Piece.wQueen);
    public void MakeMove(int start, int end, Piece type, Piece promotion = Piece.wQueen) {
        var enPassantSqr = this.enPassantSqr;

        this.MakeSimpleMove(start, end, type, promotion);

        this.lastMove = new(start, end, promotion);


        this.SetCastlingRightsNullAt(GetSideOfRook(end));

        this.enPassantSqr = -1;//sqr is reset as EP is only possible directly after the doublemove was played

        //special behaviour such as castling, or en passant
        switch (type) {
            case Piece.wKing:
            case Piece.bKing:
                (bool isCastling, int rookStartIndex, int rookEndIndex, Piece rook)
                    = GetCastleRookData(start, end);

                if (type.IsWhite()) {
                    this.SetCastlingRightsNullAt(0);
                    this.SetCastlingRightsNullAt(1);
                } else {
                    this.SetCastlingRightsNullAt(2);
                    this.SetCastlingRightsNullAt(3);
                }

                if (!isCastling) return;


                this.MakeSimpleMove(rookStartIndex, rookEndIndex, rook);
                break;

            case Piece.wRook:
            case Piece.bRook:

                int side = GetSideOfRook(start);
                this.SetCastlingRightsNullAt(side);
                break;

            case Piece.wPawn:
            case Piece.bPawn:

                if (IsDoubleMove(start, end)) {
                    //if king is separated from a horizontal slider by only this pawn and an enemy pawn
                    //--> this must become -1 again
                    //can be made ineffient as this is an edge case
                    if (!this.hasSussyEnpassantPin(type.IsWhite(), end)) {
                        this.enPassantSqr = (start + end) / 2; //yes this works, i am a genius
                        break;
                    }
                }

                //pawn needs to be killed if EP is captured
                if (end == enPassantSqr) {
                    ulong bb = ~(1ul << GetEnPassantPawn(end));
                    if (type.IsWhite()) {
                        this.bPawnBB &= bb;
                    } else {
                        this.wPawnBB &= bb;
                    }
                }
                break;
        }

    }

    private void PromoteTo(int end, Piece type) {
        switch (type) {
            case Piece.wBishop:
                wPawnBB[end] = false;
                wBishopBB[end] = true;
                break;
            case Piece.wQueen:
                wPawnBB[end] = false;
                wQueenBB[end] = true;
                break;
            case Piece.wRook:
                wPawnBB[end] = false;
                wRookBB[end] = true;
                break;
            case Piece.wKnight:
                wPawnBB[end] = false;
                wKnightBB[end] = true;
                break;

            case Piece.bBishop:
                bPawnBB[end] = false;
                bBishopBB[end] = true;
                break;
            case Piece.bQueen:
                bPawnBB[end] = false;
                bQueenBB[end] = true;
                break;
            case Piece.bRook:
                bPawnBB[end] = false;
                bRookBB[end] = true;
                break;
            case Piece.bKnight:
                bPawnBB[end] = false;
                bKnightBB[end] = true;
                break;


            default:
                throw new ArgumentException("Invalid piece entered for promotion", nameof(type));
        }
    }



    //doesnt change the BB, only nullifies if necessary
    private BitBoard ValidatePin(BitBoard sightLine, BitBoard sameColorPieces, BitBoard enemyBlockers, int kingIndex) {
        sameColorPieces[kingIndex] = false;

        if ((sightLine & enemyBlockers) != 0) return 0;//enemy steht auf der pinLine

        return (sightLine & sameColorPieces).PopCount == 1 ? sightLine : 0;
    }

    /// <summary>
    /// Reihenfolge (nach Wert sortiert, aufsteigend): Pawn, Knight, Bishop, Rook, Queen
    /// </summary>
    readonly public BitBoard[] CheckPieceBBs = new BitBoard[5];

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
    public Chessboard GenerateBoardWithMove(int startIndex, int endIndex, Piece type, Piece promotion = Piece.wQueen) {
        Chessboard board = Clone();
        board.MakeMove(startIndex, endIndex, type, promotion);

        return board;
    }

    /* TODO:
    removes pawn, finds king has an pinnned pawn  despite the removed pawn not being part of the pin
     */

    //can technically be optimized, but not a bottlenecking function
    //usecase:  a double pawn move was just made, therefore we need to make sure the EP sqr is valid
    //its not valid if capturing it reveals an attack at the enemys king
    private bool hasSussyEnpassantPin(bool forWhite, int endIndexPawn) {
        Chessboard cb = Clone();
        ulong enemyPawnBB;

        //  Utility.PrintBitBoard(cb.wPawnBB);
        //pawn is nulled
        if (forWhite) {
            cb.wPawnBB &= ~(1ul << endIndexPawn);
            enemyPawnBB = bPawnBB;
        } else {
            cb.bPawnBB &= ~(1ul << endIndexPawn);
            enemyPawnBB = wPawnBB;
        }

        //is the enemy now pinned without the doubleMovePawn?
        //then they cannot capture
        cb.GeneratePinnedPieceBB(!forWhite);

        for (int i = 0; i < 2; i++) {
            //contains the whole line between king and slider
            //set to zero if more than one piece on this line
            BitBoard pinLine = cb.PinLines[i * 4 + 2];
            //there is a intersection, so without the doubleMovepawn, there would be a pinned pawn
            //the doubleMovePawn also needs be on the pinLine
            if ((pinLine & enemyPawnBB) != 0 && ((1ul << endIndexPawn) & pinLine) != 0) {
                return true;
            }
        }

        return false;
        /* 
        Idea: build version of this chessboard without this pawn, 
        see if now theres a pinline with a enemyPawn next to this missing pawn*/
    }

    //can technically be optimized with a full lookuptable, but that
    //saves 1 (extremely fast) operation that only happens in extremely rare cases 
    public static int GetEnPassantPawn(int endIndex) {
        switch (endIndex) {
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
                return endIndex + 8; //endIndex is the sqr behind the pawn

            case 40:
            case 41:
            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
                return endIndex - 8;

            default:
                Console.WriteLine("endIndex = " + endIndex);

                throw new Exception("No valid endIndex for capturing En Passant was entered in board with index = i dont fucking know");
        }
    }

    private static bool IsDoubleMove(int start, int end) {
        return (end == start + 16) || (end == start - 16);
    }

    //if the move is castling, if yes where the rooks supposed be go
    private static (bool, int, int, Piece rookType) GetCastleRookData(int startIndex, int endIndex) {
        switch (startIndex, endIndex) {
            case (4, 6)://white kingSide
                return (true, 7, 5, Piece.wRook);
            case (4, 2)://white queenside
                return (true, 0, 3, Piece.wRook);
            case (60, 62):
                return (true, 63, 61, Piece.bRook);
            case (60, 58):
                return (true, 56, 59, Piece.bRook);

            default:
                return (false, -1, -1, Piece.bKing);
        }
    }
    //to deny castlingRights on this side
    private static int GetSideOfRook(int posIndex) {
        switch (posIndex) {
            case 0:
                return WQueenSide;
            case 7:
                return WKingSide;
            case 56:
                return BQueenSide;
            case 63:
                return BKingSide;
            default:
                return 4;
        }

        throw new ArgumentOutOfRangeException("Kein valider posIndex für CastlingRights bei rook änderung");
    }
}
