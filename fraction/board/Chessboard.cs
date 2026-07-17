using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace fraction;

public class Chessboard {
    public const int WKingSide = 0;
    public const int WQueenSide = 1;
    public const int BKingSide = 2;
    public const int BQueenSide = 3;
    public static int BoardCount { get; private set; } = 0;
    public int FiftyMovePlys { get; private set; } = 0;

    //dient dem tracken einzelner boards im perft tree beim debuggen
    public int BoardIndex { get; private set; }
    public int ParentIndex { get; private set; }
    public Move LastMove { get; private set; }

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
    private BitBoard wControlledSqrBB = 0;// 0b11111111ul << 16;
    private BitBoard bControlledSqrBB = 0;//0b11111111ul << 40;
    private int rights = 0b1111;//only the 4 lsb contain data

    public int EnPassantSqr { get; private set; } = -1;
    public bool IsCheckMate { get; set; } = false;

    public bool GetCastlingRights(int side) {
        return ((1 << side) & rights) != 0;
    }

    public void SetCastlingRightsNullAt(int side) {
        rights &= ~(1 << side);
    }


    public static readonly ulong[] CastleSqrs = {
        0b01000000ul,0b100ul,0b01000000ul << 56, 0b100ul << 56, 0//null wert für optimisation
    };
    //occupancy: wird einmal pro move neu berechnet (RefreshOccupancy) statt bei jedem zugriff
    private BitBoard whitePiecesBB;
    private BitBoard blackPiecesBB;
    private BitBoard allPiecesBB;

    //0 ist ganz rechts, 63 ist ganz links, 0=a1, 63=h8
    //die 12 piece-setter sind cold (FEN, tests), deswegen ist das refresh hier egal
    public BitBoard BRookBB { get => bRookBB; set { bRookBB = value; RefreshOccupancy(); } }
    public BitBoard WRookBB { get => wRookBB; set { wRookBB = value; RefreshOccupancy(); } }
    public BitBoard BBishopBB { get => bBishopBB; set { bBishopBB = value; RefreshOccupancy(); } }
    public BitBoard WBishopBB { get => wBishopBB; set { wBishopBB = value; RefreshOccupancy(); } }
    public BitBoard BKnightBB { get => bKnightBB; set { bKnightBB = value; RefreshOccupancy(); } }
    public BitBoard WKnightBB { get => wKnightBB; set { wKnightBB = value; RefreshOccupancy(); } }
    public BitBoard WQueenBB { get => wQueenBB; set { wQueenBB = value; RefreshOccupancy(); } }
    public BitBoard BQueenBB { get => bQueenBB; set { bQueenBB = value; RefreshOccupancy(); } }
    public BitBoard WKingBB { get => wKingBB; set { wKingBB = value; RefreshOccupancy(); } }
    public BitBoard BKingBB { get => bKingBB; set { bKingBB = value; RefreshOccupancy(); } }
    public BitBoard WPawnBB { get => wPawnBB; set { wPawnBB = value; RefreshOccupancy(); } }
    public BitBoard BPawnBB { get => bPawnBB; set { bPawnBB = value; RefreshOccupancy(); } }
    public BitBoard WControlledSqrBB { get => wControlledSqrBB; set => wControlledSqrBB = value; }
    public BitBoard BControlledSqrBB { get => bControlledSqrBB; set => bControlledSqrBB = value; }

    public BitBoard WhitePiecesBB => whitePiecesBB;
    public BitBoard BlackPiecesBB => blackPiecesBB;
    public BitBoard AllPiecesBB => allPiecesBB;

    /// <summary>
    /// Muss nach JEDER mutation der 12 piece-BBs gecalled werden.
    /// Wird am ende von MakeSimpleMove und MakeMove gecalled, dh der ganze search-pfad ist abgedeckt.
    /// </summary>
    private void RefreshOccupancy() {
        whitePiecesBB = wPawnBB | wBishopBB | wRookBB | wKnightBB | wKingBB | wQueenBB;
        blackPiecesBB = bPawnBB | bBishopBB | bRookBB | bKnightBB | bKingBB | bQueenBB;
        allPiecesBB = whitePiecesBB | blackPiecesBB;
    }

    public bool AfterCapturePly { get; set; } = false;

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
            //KEIN RefreshOccupancy hier: MakeSimpleMove benutzt diesen setter im hot path
            //und refresht selbst am ende. cold caller (ctors) refreshen explizit.
        }
    }

    public Chessboard(FEN fen) {
        BPawnBB = fen[Piece.bPawn];
        WPawnBB = fen[Piece.wPawn];
        BBishopBB = fen[Piece.bBishop];
        WBishopBB = fen[Piece.wBishop];
        BQueenBB = fen[Piece.bQueen];
        WQueenBB = fen[Piece.wQueen];
        BKingBB = fen[Piece.bKing];
        WKingBB = fen[Piece.wKing];
        BKnightBB = fen[Piece.bKnight];
        WKnightBB = fen[Piece.wKnight];
        BRookBB = fen[Piece.bRook];
        WRookBB = fen[Piece.wRook];

        EnPassantSqr = fen.EnPassant ?? -1;

        if (!fen.CastleRights.WK) SetCastlingRightsNullAt(WKingSide);
        if (!fen.CastleRights.WQ) SetCastlingRightsNullAt(WQueenSide);
        if (!fen.CastleRights.BK) SetCastlingRightsNullAt(BKingSide);
        if (!fen.CastleRights.BQ) SetCastlingRightsNullAt(BQueenSide);

        RefreshOccupancy();
    }

    /// <summary>
    /// Hiermit kann durch FENtoPos funktionen ein board gebaut werden
    /// </summary>
    /// <param name="pieces_"></param>
    public Chessboard(Dictionary<int, Piece> pieces_) {
        //bitboards müssen generiert werden

        //whitePiecesBB = wPawnBB | wBishopBB | wKingBB | wKnightBB | wRookBB | wQueenBB;
        //blackPiecesBB = bPawnBB | bBishopBB | bKingBB | bKnightBB | bRookBB | bQueenBB;
        //die felder initialisieren auf die startposition, müssen also erst geleert werden
        wPawnBB = 0; wKnightBB = 0; wBishopBB = 0; wRookBB = 0; wQueenBB = 0; wKingBB = 0;
        bPawnBB = 0; bKnightBB = 0; bBishopBB = 0; bRookBB = 0; bQueenBB = 0; bKingBB = 0;

        foreach (var (pos, type) in pieces_) {
            BitBoard bb = this[type];
            bb[pos] = true;
            this[type] = bb;
        }

        RefreshOccupancy();
    }


    //die BB-felder initialisieren auf die startposition, occupancy muss dazu passen
    public Chessboard() {
        RefreshOccupancy();
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
                cb.EnPassantSqr = Utility.ANtoPos(s);
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
    /// Returnt 0 wenn auf dem sqr kein piece steht
    /// </summary>
    /// <param name="posIndex"></param>
    /// <returns></returns>
    public Piece GetPieceAt(int posIndex) {
        //occupancy entscheidet die farbe, danach reichen 6 statt 12 probes.
        //pawns zuerst weil am häufigsten
        if (whitePiecesBB[posIndex]) {
            if (wPawnBB[posIndex]) return Piece.wPawn;
            if (wKnightBB[posIndex]) return Piece.wKnight;
            if (wBishopBB[posIndex]) return Piece.wBishop;
            if (wRookBB[posIndex]) return Piece.wRook;
            if (wQueenBB[posIndex]) return Piece.wQueen;
            if (wKingBB[posIndex]) return Piece.wKing;
        } else {
            if (bPawnBB[posIndex]) return Piece.bPawn;
            if (bKnightBB[posIndex]) return Piece.bKnight;
            if (bBishopBB[posIndex]) return Piece.bBishop;
            if (bRookBB[posIndex]) return Piece.bRook;
            if (bQueenBB[posIndex]) return Piece.bQueen;
            if (bKingBB[posIndex]) return Piece.bKing;
        }

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
    private readonly BitBoard[] pinLines = new BitBoard[8];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bb">BitBoard with only the pinnedPiece set  </param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public BitBoard GetPinLineBB(BitBoard bb) {
        for (int i = 0; i < 8; i++) {
            BitBoard line = pinLines[i];
            if ((line & bb) != 0) return line;
        }

        throw new Exception("Pinned piece was not found on any generated pinLines");
    }

    //kein unterschied zwischen weißen und schwarzen pins, weil sowieso nach jedem zug das BB aktualisiert werden muss
    /// <summary>
    /// forWhite = white is pinned; updates pinLines as side effect
    /// </summary>
    /// <param name="forWhite"></param>
    public BitBoard GetPinnedPieceBB(bool forWhite) {
        //needs to be reset for edgy edge cases
        for (int i = 0; i < 8; i++) {
            pinLines[i] = 0;
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
            pinLines[0] = intersectionVertiTop;
            pinLines[2] = intersectionHoriEast;
            pinLines[4] = intersectionVertiBottom;
            pinLines[6] = intersectionHoriWest;
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

            pinLines[1] = intersectionDiagNE;
            pinLines[3] = intersectionDiagSE;
            pinLines[5] = intersectionDiagSW;
            pinLines[7] = intersectionDiagNW;
        }


        return friendsInSightlines & ~WKingBB & ~BKingBB;//damit niemand auf die idee kommt, dass der king gepinnt ist
    }

    private static int _canary = typeof(Chessboard).GetRuntimeFields().Count();
    public void Copy(Chessboard board) {
        // please add all fields here, otherwise, the canary will die
        if (_canary != 34)
            throw new NotImplementedException($"A canary died at age of {_canary}, please revive it");
        this.FiftyMovePlys = board.FiftyMovePlys;
        this.BoardIndex = board.BoardIndex;
        this.rights = board.rights;
        this.bKingBB = board.bKingBB;
        this.bPawnBB = board.bPawnBB;
        this.bRookBB = board.bRookBB;
        this.wKingBB = board.wKingBB;
        this.wPawnBB = board.wPawnBB;
        this.wRookBB = board.wRookBB;
        this.bQueenBB = board.bQueenBB;
        this.LastMove = board.LastMove;
        this.wQueenBB = board.wQueenBB;
        this.bBishopBB = board.bBishopBB;
        this.bKnightBB = board.bKnightBB;
        this.wBishopBB = board.wBishopBB;
        this.wKnightBB = board.wKnightBB;
        this.IsCheckMate = board.IsCheckMate;
        this.ParentIndex = board.ParentIndex;
        this.EnPassantSqr = board.EnPassantSqr;
        this.bControlledSqrBB = board.bControlledSqrBB;
        this.wControlledSqrBB = board.wControlledSqrBB;
        //kopieren statt RefreshOccupancy(): 3 loads sind billiger als 10 ORs
        this.whitePiecesBB = board.whitePiecesBB;
        this.blackPiecesBB = board.blackPiecesBB;
        this.allPiecesBB = board.allPiecesBB;
    }

    public Chessboard Clone() {
        // please add all fields here, otherwise, the canary will die
        if (_canary != 34)
            throw new NotImplementedException($"A canary died at age of {_canary}, please revive it");
        Chessboard board = (Chessboard)this.MemberwiseClone();
        board.BoardIndex = BoardCount++;
        return board;
    }

    public void MakeSimpleMove(int start, int end, Piece type, Piece? promotion = null) {

        bool capturedBlack=BlackPiecesBB[end];
        bool capturedWhite=WhitePiecesBB[end];
        AfterCapturePly = capturedBlack || capturedWhite;

        //optimization: we only need to clear if its a capture 
        //and if its the right color
        if (AfterCapturePly) {
            if (capturedBlack) {
                bKnightBB[end] = false;
                bQueenBB[end] = false;
                bRookBB[end] = false;
                bBishopBB[end] = false;
                bPawnBB[end] = false;
            } else {
                wKnightBB[end] = false;
                wQueenBB[end] = false;
                wRookBB[end] = false;
                wBishopBB[end] = false;
                wPawnBB[end] = false;
            }
        }
        

        switch (type) {
            case Piece.wPawn:
                wPawnBB[start] = false;
                wPawnBB[end] = true;
                //promotion
                if (end >= 56) {
                    PromoteTo(end, (promotion & (Piece)~8) ?? Piece.wQueen);
                }
                break;

            case Piece.bPawn:
                bPawnBB[start] = false;
                bPawnBB[end] = true;

                if (end < 8) {
                    PromoteTo(end, (promotion | (Piece)8) ?? Piece.bQueen);
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

        //hier und nicht erst am ende von MakeMove, damit hasSussyEnpassantPin
        //eine aktuelle occupancy sieht
        RefreshOccupancy();
    }

    public void MakeMove(Move move)
        => MakeMove(move.Start, move.End, this.GetPieceAt(move.Start), move.Promotion);
    public void MakeMove(int start, int end, Piece type, Piece? promotion = null) {
        var enPassantSqr = this.EnPassantSqr;

        this.MakeSimpleMove(start, end, type, promotion);

        this.LastMove = new(start, end, promotion);

        //NICHT BlackPiecesBB[end] testen: MakeSimpleMove hat das geschlagene piece schon
        //gekillt, dh der test war immer false. AfterCapturePly wird in MakeSimpleMove
        //VOR dem loeschen gesetzt und ist der wert den wir hier eig wollen
        bool isCapture = AfterCapturePly;
        if (isCapture) {
            FiftyMovePlys = 0;
        } else {
            FiftyMovePlys++;
        }

        this.SetCastlingRightsNullAt(GetSideOfRook(end));

        this.EnPassantSqr = -1;//sqr is reset as EP is only possible directly after the doublemove was played

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
                FiftyMovePlys = 0;//gets reset after pawn moves

                if (IsDoubleMove(start, end)) {
                    //if king is separated from a horizontal slider by only this pawn and an enemy pawn
                    //--> this must become -1 again
                    if (!this.hasSussyEnpassantPin(type.IsWhite(), end)) {
                        this.EnPassantSqr = (start + end) / 2; //yes this works, i am a genius
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
                    //einziger BB-write in MakeMove der nicht ueber MakeSimpleMove laeuft
                    RefreshOccupancy();
                }
                break;
            default:
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
    private static BitBoard ValidatePin(BitBoard sightLine, BitBoard sameColorPieces, BitBoard enemyBlockers, int kingIndex) {
        sameColorPieces[kingIndex] = false;

        if ((sightLine & enemyBlockers) != 0) return 0;//enemy steht auf der pinLine

        return (sightLine & sameColorPieces).PopCount == 1 ? sightLine : 0;
    }

    /// <summary>
    /// Reihenfolge (nach Wert sortiert, aufsteigend): Pawn, Knight, Bishop, Rook, Queen
    /// </summary>
    public BitBoard[] CheckPieceBBs { get; } = new BitBoard[5];

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

    public bool getCheckPiecesEmpty() {
        return (CheckPieceBBs[0] | CheckPieceBBs[1] | CheckPieceBBs[2] | CheckPieceBBs[3] | CheckPieceBBs[4]) == 0;
    }




    /// <summary>
    /// Generiert stumpf ein Board wo das Piece von StartIndex zu EndIndex bewegt wurde
    /// Ineffizient und slow
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    public Chessboard GenerateBoardWithMove(int startIndex, int endIndex, Piece type, Piece? promotion = null) {
        Chessboard board = Clone();
        board.MakeMove(startIndex, endIndex, type, promotion);

        return board;
    }

    /* TODO:
    removes pawn, finds king has an pinnned pawn  despite the removed pawn not being part of the pin
     */

    //usecase:  a double pawn move was just made, therefore we need to make sure the EP sqr is valid
    //its not valid if capturing it reveals an attack at the enemys king
    private bool hasSussyEnpassantPin(bool forWhite, int endIndexPawn) {
        //early return, if no king/ slider on the rank, there cant be a discovered check
        BitBoard rank = BitBoard.HorizontalLine(endIndexPawn >> 3);
        BitBoard victimKing = forWhite ? bKingBB : wKingBB;
        BitBoard mySliders = forWhite ? (wRookBB | wQueenBB) : (bRookBB | bQueenBB);
        if ((rank & victimKing) == 0 || (rank & mySliders) == 0) return false;

        ulong enemyPawnBB, tmp;

        //pawn is nulled
        //necessary for GetPinnedPieceBB which uses this CB's BBs
        if (forWhite) {
            tmp=wPawnBB;
            wPawnBB &= ~(1ul << endIndexPawn);
            enemyPawnBB = bPawnBB;
        } else {
            tmp=bPawnBB;
            bPawnBB &= ~(1ul << endIndexPawn);
            enemyPawnBB = wPawnBB;
        }

        //is the enemy now pinned without the doubleMovePawn?
        //then they cannot capture
        this.GetPinnedPieceBB(!forWhite);


        bool retValue=false;
        for (int i = 0; i <= 1; i++) {
            //contains the whole line between king and slider
            //set to zero if more than one piece on this line
            BitBoard pinLine = this.pinLines[i * 4 + 2];//2 and 6
            //there is a intersection, so without the doubleMovepawn, there would be a pinned pawn
            //the doubleMovePawn also needs be on the pinLine
            if ((pinLine & enemyPawnBB) != 0 && ((1ul << endIndexPawn) & pinLine) != 0) {
                retValue= true;
                break;
            }
        }

        if (forWhite) {
            wPawnBB=tmp;
        } else {
            bPawnBB=tmp;
        }

        return retValue;
        /* 
        Idea: build version of this chessboard without this pawn, 
        see if now theres a pinline with a enemyPawn next to this missing pawn*/
    }

    //can technically be optimized with a full lookuptable, but that
    //saves 1 (extremely fast) operation that only happens in extremely rare cases 
    /* ignore all of the above */
    public static int GetEnPassantPawn(int endIndex) {
        return endIndex switch {
            (>= 16) and (<= 23) => endIndex + 8,
            (>= 40) and (<= 47) => endIndex - 8,
            _ => throw new ArgumentOutOfRangeException(nameof(endIndex), endIndex, "No valid Index for capturing en passant"),
        };
    }

    private static bool IsDoubleMove(int start, int end) {
        return (end == start + 16) || (end == start - 16); // 16 == Math.Abs(start - end);
    }

    //if the move is castling, if yes where the rooks supposed be go
    private static (bool, int, int, Piece rookType) GetCastleRookData(int startIndex, int endIndex) {
        return (startIndex, endIndex) switch {
            (4, 6) => (true, 7, 5, Piece.wRook),
            (4, 2) => (true, 0, 3, Piece.wRook),
            (60, 62) => (true, 63, 61, Piece.bRook),
            (60, 58) => (true, 56, 59, Piece.bRook),
            _ => (false, -1, -1, Piece.bKing),
        };
    }
    //to deny castlingRights on this side
    private static int GetSideOfRook(int posIndex) {
        return posIndex switch {
            0 => WQueenSide,
            7 => WKingSide,
            56 => BQueenSide,
            63 => BKingSide,
            _ => 4,
        };
    }
}
