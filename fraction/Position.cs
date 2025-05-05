using System;

namespace fraction;

public class Position {
    public static readonly Position Startpos = new();

    public Chessboard Board { get; }
    public bool WhitesTurn { get; private set; }
    public int EnPassantSqr { get; private set; }
    public CastleRights Rights { get; private set; }
    public int FiftyMovePlys { get; private set; }

    private Position() {
        this.Board = Chessboard.Startpos;
        this.WhitesTurn = true;
        this.EnPassantSqr = -1;
        this.Rights = CastleRights.All;
        this.FiftyMovePlys = 0;
    }

    public Position(Position pos) {
        this.Board = new(pos.Board);
        this.WhitesTurn = pos.WhitesTurn;
        this.EnPassantSqr = pos.EnPassantSqr;
        this.Rights = pos.Rights;
        this.FiftyMovePlys = pos.FiftyMovePlys;
    }

    public Position(FEN fen) {
        this.Board = new(fen);
        this.WhitesTurn = fen.WhitesTurn;
        this.EnPassantSqr = fen.EnPassant ?? -1;
        this.Rights = CastleRights.All;

        if (!fen.CastleRights.WK) this.ClearRights(CastleRights.WKingSide);
        if (!fen.CastleRights.WQ) this.ClearRights(CastleRights.WQueenSide);
        if (!fen.CastleRights.BK) this.ClearRights(CastleRights.BKingSide);
        if (!fen.CastleRights.BQ) this.ClearRights(CastleRights.BQueenSide);
    }

    public Position(
            Chessboard board,
            bool whitesTurn,
            int fiftyMovePlys,
            int enPassantSqr,
            CastleRights rights
            ) {
        this.Board = board;
        this.WhitesTurn = whitesTurn;
        this.EnPassantSqr = enPassantSqr;
        this.Rights = rights;
        this.FiftyMovePlys = fiftyMovePlys;
    }

    public void ClearRights(CastleRights side) {
        this.Rights &= ~(side);
    }

    public bool MakeMove(Move move)
        => MakeMove(move.Start, move.End, this.Board.GetPieceAt(move.Start), move.Promotion);
    public bool MakeMove(int start, int end, Piece type, Piece? promotion = null) {
        this.WhitesTurn = !this.WhitesTurn;
        var enPassantSqr = this.EnPassantSqr;

        this.Board.MakeSimpleMove(start, end, type, promotion);

        bool isCapture = type.IsWhite() ? this.Board.BlackPiecesBB[end] : this.Board.WhitePiecesBB[end];
        if (isCapture) {
            this.FiftyMovePlys = 0;
        } else {
            this.FiftyMovePlys++;
        }

        this.ClearRights(GetSideOfRook(end));

        this.EnPassantSqr = -1;//sqr is reset as EP is only possible directly after the doublemove was played

        //special behaviour such as castling, or en passant
        switch (type) {
            case Piece.wKing:
            case Piece.bKing:
                (bool isCastling, int rookStartIndex, int rookEndIndex, Piece rook)
                    = GetCastleRookData(start, end);

                if (type.IsWhite()) {
                    this.ClearRights(CastleRights.White);
                } else {
                    this.ClearRights(CastleRights.Black);
                }

                if (!isCastling) return isCapture;


                this.Board.MakeSimpleMove(rookStartIndex, rookEndIndex, rook);
                break;

            case Piece.wRook:
            case Piece.bRook:

                CastleRights side = GetSideOfRook(start);
                this.ClearRights(side);
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
                        this.Board.BPawnBB &= bb;
                    } else {
                        this.Board.WPawnBB &= bb;
                    }
                }
                break;
            default:
                break;
        }
        return isCapture;
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

    /* TODO:
    removes pawn, finds king has an pinnned pawn  despite the removed pawn not being part of the pin
     */

    //can technically be optimized, but not a bottlenecking function
    //usecase:  a double pawn move was just made, therefore we need to make sure the EP sqr is valid
    //its not valid if capturing it reveals an attack at the enemys king
    private bool hasSussyEnpassantPin(bool forWhite, int endIndexPawn) {
        Chessboard cb = new(this.Board);
        ulong enemyPawnBB;

        //  Utility.PrintBitBoard(cb.wPawnBB);
        //pawn is nulled
        if (forWhite) {
            cb.WPawnBB &= ~(1ul << endIndexPawn);
            enemyPawnBB = cb.BPawnBB;
        } else {
            cb.BPawnBB &= ~(1ul << endIndexPawn);
            enemyPawnBB = cb.WPawnBB;
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
    private static CastleRights GetSideOfRook(int posIndex) {
        return posIndex switch {
            0 => CastleRights.WQueenSide,
            7 => CastleRights.WKingSide,
            56 => CastleRights.BQueenSide,
            63 => CastleRights.BKingSide,
            _ => CastleRights.None,
        };
    }
}
