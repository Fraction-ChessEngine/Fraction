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
    //private BitBoard whitePiecesBB = 0b0000000000000000000000000000000000000000000000001111111111111111;
    //private BitBoard blackPiecesBB = 0b1111111111111111000000000000000000000000000000000000000000000000;

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
    }

    public Chessboard() { }

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

    private static int _canary = typeof(Chessboard).GetRuntimeFields().Count();
    public void Copy(Chessboard board) {
        // please add all fields here, otherwise, the canary will die
        if (_canary != 27)
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
    }

    public Chessboard Clone() {
        // please add all fields here, otherwise, the canary will die
        if (_canary != 27)
            throw new NotImplementedException($"A canary died at age of {_canary}, please revive it");
        Chessboard board = (Chessboard)this.MemberwiseClone();
        board.BoardIndex = BoardCount++;
        return board;
    }

    public void MakeSimpleMove(int start, int end, Piece type, Piece? promotion = null) {

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
    }

    public void MakeMove(Move move)
        => MakeMove(move.Start, move.End, this.GetPieceAt(move.Start), move.Promotion);
    public void MakeMove(int start, int end, Piece type, Piece? promotion = null) {
        var enPassantSqr = this.EnPassantSqr;

        this.MakeSimpleMove(start, end, type, promotion);

        this.LastMove = new(start, end, promotion);

        bool isCapture = type.IsWhite() ? BlackPiecesBB[end] : WhitePiecesBB[end];
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
                    //can be made ineffient as this is an edge case
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

    /// <summary>
    /// Generiert stumpf ein Board wo das Piece von StartIndex zu EndIndex bewegt wurde
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
        var pinLines = MoveGen.GetPinLines(cb, !forWhite);

        for (int i = 0; i < 2; i++) {
            //contains the whole line between king and slider
            //set to zero if more than one piece on this line
            BitBoard pinLine = pinLines[i * 4 + 2];
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
