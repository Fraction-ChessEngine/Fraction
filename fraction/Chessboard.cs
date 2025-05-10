using System;
using System.Linq;
using System.Diagnostics;

namespace fraction;

public class Chessboard {
    // for every piece on every square
    private static readonly Zobrist zobrist;
    private const int wPawnZO = 0 * 64;
    private const int wRookZO = 1 * 64;
    private const int wKnightZO = 2 * 64;
    private const int wBishopZO = 3 * 64;
    private const int wQueenZO = 4 * 64;
    private const int wKingZO = 5 * 64;
    private const int bPawnZO = 6 * 64;
    private const int bRookZO = 7 * 64;
    private const int bKnightZO = 8 * 64;
    private const int bBishopZO = 9 * 64;
    private const int bQueenZO = 10 * 64;
    private const int bKingZO = 11 * 64;

    static Chessboard() {
        zobrist = new(12 * 64);
        Startpos = new();
    }
    public static readonly Chessboard Startpos;

    private int hashcode;
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
                    this.hashcode ^= CalculateBitBoardHash(wPawnBB, wPawnZO);
                    wPawnBB = value;
                    this.hashcode ^= CalculateBitBoardHash(wPawnBB, wPawnZO);
                    break;

                case Piece.wBishop:
                    this.hashcode ^= CalculateBitBoardHash(wBishopBB, wBishopZO);
                    wBishopBB = value;
                    this.hashcode ^= CalculateBitBoardHash(wBishopBB, wBishopZO);
                    break;

                case Piece.wKnight:
                    this.hashcode ^= CalculateBitBoardHash(wKnightBB, wKnightZO);
                    wKnightBB = value;
                    this.hashcode ^= CalculateBitBoardHash(wKnightBB, wKnightZO);
                    break;

                case Piece.wRook:
                    this.hashcode ^= CalculateBitBoardHash(wRookBB, wRookZO);
                    wRookBB = value;
                    this.hashcode ^= CalculateBitBoardHash(wRookBB, wRookZO);
                    break;

                case Piece.wKing:
                    this.hashcode ^= CalculateBitBoardHash(wKingBB, wKingZO);
                    wKingBB = value;
                    this.hashcode ^= CalculateBitBoardHash(wKingBB, wKingZO);
                    break;

                case Piece.wQueen:
                    this.hashcode ^= CalculateBitBoardHash(wQueenBB, wQueenZO);
                    wQueenBB = value;
                    this.hashcode ^= CalculateBitBoardHash(wQueenBB, wQueenZO);
                    break;

                case Piece.bPawn:
                    this.hashcode ^= CalculateBitBoardHash(bPawnBB, bPawnZO);
                    bPawnBB = value;
                    this.hashcode ^= CalculateBitBoardHash(bPawnBB, bPawnZO);
                    break;

                case Piece.bBishop:
                    this.hashcode ^= CalculateBitBoardHash(bBishopBB, bBishopZO);
                    bBishopBB = value;
                    this.hashcode ^= CalculateBitBoardHash(bBishopBB, bBishopZO);
                    break;

                case Piece.bKnight:
                    this.hashcode ^= CalculateBitBoardHash(bKnightBB, bKnightZO);
                    bKnightBB = value;
                    this.hashcode ^= CalculateBitBoardHash(bKnightBB, bKnightZO);
                    break;

                case Piece.bRook:
                    this.hashcode ^= CalculateBitBoardHash(bRookBB, bRookZO);
                    bRookBB = value;
                    this.hashcode ^= CalculateBitBoardHash(bRookBB, bRookZO);
                    break;

                case Piece.bKing:
                    this.hashcode ^= CalculateBitBoardHash(bKingBB, bKingZO);
                    bKingBB = value;
                    this.hashcode ^= CalculateBitBoardHash(bKingBB, bKingZO);
                    break;

                case Piece.bQueen:
                    this.hashcode ^= CalculateBitBoardHash(bQueenBB, bQueenZO);
                    bQueenBB = value;
                    this.hashcode ^= CalculateBitBoardHash(bQueenBB, bQueenZO);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }

    private Chessboard() {
        this.hashcode = this.CalculateHash();
    }

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
        this.hashcode = board.hashcode;
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
        this.hashcode = this.CalculateHash();
    }

    private static Piece[] allPieces = [
        Piece.wPawn, Piece.wRook, Piece.wKnight, Piece.wBishop, Piece.wQueen, Piece.wKing,
        Piece.bPawn, Piece.bRook, Piece.bKnight, Piece.bBishop, Piece.bQueen, Piece.bKing,
    ];

    private static int CalculateBitBoardHash(BitBoard bb, int segment) {
        int hash = 0;
        while (bb.PopCount > 0) {
            var offset = bb.LowestOne;
            bb[offset] = false;
            hash ^= zobrist[segment + offset];
        }
        return hash;
    }

    private int CalculateHash() {
        int hash = 0;
        for (int i = 0; i < 12; i++) {
            var segment = i * 64;
            hash = CalculateBitBoardHash(this[allPieces[i]], segment);
        }
        return hash;
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
        for (int i = 0; i < 12; i++) {
            if (this[allPieces[i]][end]) this.hashcode ^= zobrist[i * 64 + end];
        }
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
                this.hashcode ^= zobrist[wPawnZO + start];
                wPawnBB[end] = true;
                this.hashcode ^= zobrist[wPawnZO + end];
                //promotion
                if (end >= 56) {
                    PromoteTo(end, (promotion & (Piece)~8) ?? Piece.wQueen);
                }
                break;

            case Piece.bPawn:
                bPawnBB[start] = false;
                this.hashcode ^= zobrist[bPawnZO + start];
                bPawnBB[end] = true;
                this.hashcode ^= zobrist[bPawnZO + end];

                if (end < 8) {
                    PromoteTo(end, (promotion | (Piece)8) ?? Piece.bQueen);
                }
                break;

            //funktioniert weil es nur einen king geben darf
            case Piece.wKing:
                this.hashcode ^= zobrist[wKingZO + start];
                wKingBB = 1ul << end;
                this.hashcode ^= zobrist[wKingZO + end];
                break;

            case Piece.bKing:
                this.hashcode ^= zobrist[bKingZO + start];
                bKingBB = 1ul << end;
                this.hashcode ^= zobrist[bKingZO + end];
                break;

            default:
                BitBoard bb = this[type];
                var zoi = Array.IndexOf(allPieces, type) * 64;
                bb[start] = false;
                this.hashcode ^= zobrist[zoi + start];
                bb[end] = true;
                this.hashcode ^= zobrist[zoi + end];
                this[type] = bb;
                break;
        }
    }

    private void PromoteTo(int end, Piece type) {
        this.hashcode ^= zobrist[wPawnZO + end];
        if (type.IsWhite()) {
            this.hashcode ^= zobrist[wPawnZO + end];
            wPawnBB[end] = false;
        } else {
        this.hashcode ^= zobrist[bPawnZO + end];
            bPawnBB[end] = false;
        }

        switch (type) {
            case Piece.wBishop:
                wBishopBB[end] = true;
                this.hashcode ^= zobrist[wBishopZO + end];
                break;
            case Piece.wQueen:
                wQueenBB[end] = true;
                this.hashcode ^= zobrist[wQueenZO + end];
                break;
            case Piece.wRook:
                wRookBB[end] = true;
                this.hashcode ^= zobrist[wRookZO + end];
                break;
            case Piece.wKnight:
                wKnightBB[end] = true;
                this.hashcode ^= zobrist[wKnightZO + end];
                break;
            case Piece.bBishop:
                bBishopBB[end] = true;
                this.hashcode ^= zobrist[bBishopZO + end];
                break;
            case Piece.bQueen:
                bQueenBB[end] = true;
                this.hashcode ^= zobrist[bQueenZO + end];
                break;
            case Piece.bRook:
                bRookBB[end] = true;
                this.hashcode ^= zobrist[bRookZO + end];
                break;
            case Piece.bKnight:
                bKnightBB[end] = true;
                this.hashcode ^= zobrist[bKnightZO + end];
                break;
            default:
                throw new ArgumentException("Invalid piece entered for promotion", nameof(type));
        }
    }

    public override bool Equals(object? obj) {
        if (obj is not Chessboard) return false;
        if (obj.GetHashCode() != this.GetHashCode()) return false;
        Chessboard board = (Chessboard)obj;
        foreach (var piece in allPieces) {
            if (this[piece] != board[piece]) return false;
        }
        return true;
    }

    public override int GetHashCode() {
        Debug.Assert(this.hashcode == CalculateHash());
        return this.hashcode;
    }

    public static bool operator ==(Chessboard lhs, Chessboard rhs) {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Chessboard lhs, Chessboard rhs) {
        return !lhs.Equals(rhs);
    }
}
