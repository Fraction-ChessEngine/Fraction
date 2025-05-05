using System;
using System.Diagnostics;

namespace fraction;

public class Chessboard {
    public static readonly Chessboard Startpos = new();

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

    private Chessboard() { }

    public Chessboard(Chessboard board) {
        this.wPawnBB = board.wPawnBB;
        this.wRookBB = board.wRookBB;
        this.wKnightBB = board.wKnightBB;
        this.wBishopBB = board.wBishopBB;
        this.wQueenBB = board.wQueenBB;
        this.wKingBB = board.wKingBB;
        this.bPawnBB = board.bPawnBB;
        this.bRookBB = board.bRookBB;
        this.bKnightBB = board.bKnightBB;
        this.bBishopBB = board.bBishopBB;
        this.bQueenBB = board.bQueenBB;
        this.bKingBB = board.bKingBB;
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

    public void MakeSimpleMove(int start, int end, Piece type, Piece? promotion = null) {
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
}
