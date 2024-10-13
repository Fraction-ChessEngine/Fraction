using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace fraction;
public enum Piece : int
{
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

public static class PieceUtil
{
    public static bool IsWhite(this Piece p)
    {
        return (int)p < (int)Piece.bPawn;
    }

    //kovertiert pieces.irgendwas zu symbol
    public static string GetSymbol(this Piece p)
    {
        int n = (int)p;
        string symbols = "PBNRKQ  pbnrkq";
        //string symbols2 = "♙♗♘♖♔♕♟♝♞♜♚♛"; coole idee, kann er aber nicht printen
        return symbols[n].ToString();
    }
}
