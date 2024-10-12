using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace fraction
{
    static class PieceUtil
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
}
