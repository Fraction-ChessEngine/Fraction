using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace fraction;
public enum Piece : int {
    wPawn,
    wBishop,
    wKnight,
    wRook,
    wKing,
    wQueen,
    bPawn = 8,
    bBishop,
    bKnight,
    bRook,
    bKing,
    bQueen,
}

public static class PieceUtil {
    public static bool IsWhite(this Piece p) {
        return (int)p < (int)Piece.bPawn;
    }

    private const string symbols = "PBNRKQ  pbnrkq";
    //kovertiert pieces.irgendwas zu symbol
    public static string GetSymbol(this Piece p) {
        int n = (int)p;
        //string symbols2 = "♙♗♘♖♔♕♟♝♞♜♚♛"; coole idee, kann er aber nicht printen
        return symbols[n].ToString();
    }

    public static bool TryParse(string s, out Piece p) {
        p = Piece.wPawn;
        if (s.Length != 1) return false;
        return TryParse(s[0], out p);
    }
    public static bool TryParse(char c, out Piece p) {
        p = Piece.wPawn;
        if (c == ' ') return false;
        int i = symbols.IndexOf(c);
        if (i < 0) return false;
        p = (Piece)i;
        return true;
    }
}
