using System;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;

namespace fraction
{
	static class PieceUtil
	{

		public static bool isWhite(this Piece p)
		{
			return (int)p < 6;
		}


		//kovertiert pieces.irgendwas zu symbol
		public static string getSymbol(this Piece p)
		{
			int n = (int)p;
			string symbols = "PBNRKQpbnrkq";
			//string symbols2 = "♙♗♘♖♔♕♟♝♞♜♚♛"; coole idee, kann er aber nicht printen
			return symbols[n].ToString();
		}
	}
}