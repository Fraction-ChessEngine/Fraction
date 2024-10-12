using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

/* To do:

-movegen kann mit den neuen Utility funktionen krank optimiert werden
-auch evaluation kann stark verbessert werden weil jetzt access auf die einzelnen indices mgl ist
-transposition table


*/


namespace fraction
{
    public enum Piece : int
    {
        wPawn,
        wBishop,
        wKnight,
        wRook,
        wKing,
        wQueen,
        bPawn,
        bBishop,
        bKnight,
        bRook,
        bKing,
        bQueen,
    }

    public class Program
    {
        /// <summary>
        /// Printed das board
        /// </summary>
        /// <param name="board"></param>
        public static void DisplayBoard(Chessboard b)
        {
            if (b == null)
                return;
            for (int y = 7; y >= 0; y--)
            {
                string currRank = "";
                for (int x = 0; x < 8; x++)
                {
                    int posIndex = y * 8 + x;

                    if (b.HasPieceAt(posIndex))
                    {
                        currRank += b.GetPieceAt(posIndex).GetSymbol() + " | ";
                    }
                    else
                    {
                        currRank += "  | ";
                    }
                }

                Console.WriteLine(currRank);
                Console.WriteLine("-------------------------------");
            }
            //  Console.WriteLine("");
        }

        static void DisplayBoard(string fen)
        {
            DisplayBoard(new Chessboard(Utility.FENtoPosition(fen)));
        }

        static string GetPlayerMove()
        {
            Console.Write("My move is: \n");
            return Console.ReadLine();
        }

        /// <summary>
        /// Nimmt einen string der form "e4 f6" an, gibt bei 0 die erste Pos an, bei 1 die zweite
        /// </summary>
        /// <param name="an"></param>
        static int[] TranslateMove(string an)
        {
            int pos1 = Utility.ANtoPos(an.Substring(0, 2));
            int pos2 = Utility.ANtoPos(an.Substring(3, 2));

            return new int[] { pos1, pos2 };
        }

        /// <summary>
        /// Die rekursive Funktion die das game am laufen hält
        /// </summary>
        static void MainLoop()
        {
            Chessboard b1 = new Chessboard(
                Utility.FENtoPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")
            );
            Chessboard h4 = new Chessboard(
                Utility.FENtoPosition("rnbqkbnr/pppppppp/8/8/7P/8/PPPPPPP1/RNBQKBNR")
            );
            Chessboard h3 = new Chessboard(
                Utility.FENtoPosition("rnbqkbnr/pppppppp/8/8/8/7P/PPPPPPP1/RNBQKBNR")
            );
            Chessboard sus = new Chessboard(
                Utility.FENtoPosition("rnbqkbnr/1ppppppp/8/8/p7/5PK1/PPPPP1PP/RNBQ1BNR")
            );
            Chessboard mateIn2 = new Chessboard(Utility.FENtoPosition("k7/ppp5/ppp5/8/8/8/8/KR6"));
            Chessboard error = new Chessboard(
                Utility.FENtoPosition("r1bqkbnr/pppppppp/B7/8/4P3/8/PPPP1PPP/RNBQK1NR")
            );
            var prof = Chessboard.FromFEN("r1bqkb2/ppp1pnp1/2np4/5p1r/2B1P3/3P4/PPPN1PPP/RNB1K2R");
            var evalB1 = Chessboard.FromFEN("5r2/1p6/2p1k3/4Q2p/3P4/8/5PK1/q7");

            // Utility.printBitBoard(prof.wPawnBB);
            // PGN_Formatter.CreateFormattedFile("mvl.txt", "gamesCarlsen.txt");
            // PGN_Formatter.InsertPGNFileToFENdatabase("gamesCarlsen.txt", "carlsenfen.txt");
            // PGN_Formatter.RemoveDuplicates("FENdatabase.txt");

            Testing.BenchMark();

            return;
        }

        /*
Na3: 8885
Nc3: 9757 2 zu viel
Nf3: 9754 6 zu viel
Nh3: 8883 2 zu viel
a3: 8457
a4: 9329
b3: 9347 2 zu viel
b4: 9332
c3: 9347 75 zu viel
c4: 9818 74 zu viel
d3: 11961 2 zu viel
d4: 12437 2 zu viel
e3: 13280 147 zu viel
e4: 13308 148 zu viel
f3: 8457
f4: 8929
g3: 9347 2 zu viel
g4: 9328
h3: 8457
h4: 9329
Sum: 197742
 */

        private static void perft(int d)
        {
            Chessboard b1 = new Chessboard(
                Utility.FENtoPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")
            );
            string l = "abcdefgh";

            int sum = 0;

            Minimax.positions = 0;
            var n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("b1"),
                Utility.ANtoPos("a3"),
                Piece.wKnight
            );
            Minimax.MiniMax(n1, d, float.MinValue, float.MaxValue, false);
            Console.WriteLine("Na3: " + Minimax.positions);
            sum += Minimax.positions;

            Minimax.positions = 0;
            n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("b1"),
                Utility.ANtoPos("c3"),
                Piece.wKnight
            );
            Minimax.MiniMax(n1, d, float.MinValue, float.MaxValue, false);
            Console.WriteLine("Nc3: " + Minimax.positions);
            sum += Minimax.positions;

            Minimax.positions = 0;
            n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("g1"),
                Utility.ANtoPos("f3"),
                Piece.wKnight
            );
            Minimax.MiniMax(n1, d, float.MinValue, float.MaxValue, false);
            Console.WriteLine("Nf3: " + Minimax.positions);
            sum += Minimax.positions;

            Minimax.positions = 0;
            n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("g1"),
                Utility.ANtoPos("h3"),
                Piece.wKnight
            );
            Minimax.MiniMax(n1, d, float.MinValue, float.MaxValue, false);
            Console.WriteLine("Nh3: " + Minimax.positions);
            sum += Minimax.positions;

            foreach (char c in l)
            {
                Minimax.positions = 0;
                var a3 = b1.GenerateBoardWithMove(
                    Utility.ANtoPos(c + "2"),
                    Utility.ANtoPos(c + "3"),
                    Piece.wPawn
                );
                Minimax.MiniMax(a3, d, float.MinValue, float.MaxValue, false);
                Console.WriteLine(c + "3: " + Minimax.positions);
                sum += Minimax.positions;

                Minimax.positions = 0;
                a3 = b1.GenerateBoardWithMove(
                    Utility.ANtoPos(c + "2"),
                    Utility.ANtoPos(c + "4"),
                    Piece.wPawn
                );
                Minimax.MiniMax(a3, d, float.MinValue, float.MaxValue, false);
                Console.WriteLine(c + "4: " + Minimax.positions);
                sum += Minimax.positions;
            }

            Console.WriteLine("Sum: " + sum + " with depth = " + d);
        }

        static Chessboard visualBoard; //board auf dem die "wahre" position gespeichert wird

        static void Main(string[] args)
        {
            visualBoard = new Chessboard();

 
            Chessboard b1 = new Chessboard(
                Utility.FENtoPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")
            );

            perft(4);

            /* Utility.PrintBitBoard(visualBoard.wControlledSqrBB);
            Utility.PrintBitBoard(visualBoard.bControlledSqrBB); */
           // DisplayBoard(visualBoard);
           /* b1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("b1"),
                Utility.ANtoPos("c3"),
                Piece.wKnight
            );

            b1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("e7"),
                Utility.ANtoPos("e5"),
                Piece.bPawn
            );

            b1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("c3"),
                Utility.ANtoPos("d5"),
                Piece.wKnight
            );

            
            response nodes after Nc3
            Pawn e7 e5 has 657 , expected 656 (+1) 
            Pawn e7 e6 has 658 , expected 657 (+1)

            response nodes after e5
            knight c3 d5 has 29 , expected 28
            

            Testing.PerftResults(b1, 1, false);
            
            */
        }
    }
}
