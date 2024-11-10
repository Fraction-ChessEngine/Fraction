using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

/* To do:

-pawn attack lookup table generieren
-movegen kann mit den neuen Utility funktionen krank optimiert werden
-auch evaluation kann stark verbessert werden weil jetzt access auf die einzelnen indices mgl ist
-transposition table


*/

namespace fraction;
public class Program {
    /// <summary>
    /// Printed das board
    /// </summary>
    /// <param name="board"></param>
    public static void DisplayBoard(Chessboard b) {
        if (b == null)
            return;
        for (int y = 7; y >= 0; y--) {
            string currRank = "";
            for (int x = 0; x < 8; x++) {
                int posIndex = y * 8 + x;

                if (b.HasPieceAt(posIndex)) {
                    currRank += b.GetPieceAt(posIndex).GetSymbol() + " | ";
                } else {
                    currRank += "  | ";
                }
            }

            Console.WriteLine(currRank);
            Console.WriteLine("-------------------------------");
        }
        //  Console.WriteLine("");
    }

    static void DisplayBoard(string fen) {
        DisplayBoard(new Chessboard(Utility.FENtoPosition(fen)));
    }

    static string GetPlayerMove() {
        Console.Write("My move is: \n");
        return Console.ReadLine() ?? "";
    }

    /// <summary>
    /// Nimmt einen string der form "e4 f6" an, gibt bei 0 die erste Pos an, bei 1 die zweite
    /// </summary>
    /// <param name="an"></param>
    static int[] TranslateMove(string an) {
        int pos1 = Utility.ANtoPos(an.Substring(0, 2));
        int pos2 = Utility.ANtoPos(an.Substring(3, 2));

        return new int[] { pos1, pos2 };
    }

    /// <summary>
    /// Die rekursive Funktion die das game am laufen hält
    /// </summary>
    static void MainLoop() {
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

        // Utility.printBitBoard(prof.wPawnBB);
        // PGN_Formatter.CreateFormattedFile("mvl.txt", "gamesCarlsen.txt");
        // PGN_Formatter.InsertPGNFileToFENdatabase("gamesCarlsen.txt", "carlsenfen.txt");
        // PGN_Formatter.RemoveDuplicates("FENdatabase.txt");

        Testing.BenchMark();

        return;
    }


    private static void perft(int d) {
        Chessboard b1 = new Chessboard(
            Utility.FENtoPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")
        );
        string l = "abcdefgh";

        int sum = 0;

        {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };

            var n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("b1"),
                Utility.ANtoPos("a3"),
                Piece.wKnight
            );
            minimax.Run(n1, d, false);
            Console.WriteLine("Na3: " + minimax.Positions);
            sum += minimax.Positions;
        }

        {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            var n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("b1"),
                Utility.ANtoPos("c3"),
                Piece.wKnight
            );
            minimax.Run(n1, d, false);
            Console.WriteLine("Nc3: " + minimax.Positions);
            sum += minimax.Positions;
        }

        {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            var n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("g1"),
                Utility.ANtoPos("f3"),
                Piece.wKnight
            );
            minimax.Run(n1, d, false);
            Console.WriteLine("Nf3: " + minimax.Positions);
            sum += minimax.Positions;
        }

        {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            var n1 = b1.GenerateBoardWithMove(
                Utility.ANtoPos("g1"),
                Utility.ANtoPos("h3"),
                Piece.wKnight
            );
            minimax.Run(n1, d, false);
            Console.WriteLine("Nh3: " + minimax.Positions);
            sum += minimax.Positions;
        }

        foreach (char c in l) {
            {
                Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
                var a3 = b1.GenerateBoardWithMove(
                    Utility.ANtoPos(c + "2"),
                    Utility.ANtoPos(c + "3"),
                    Piece.wPawn
                );
                minimax.Run(a3, d, false);
                Console.WriteLine(c + "3: " + minimax.Positions);
                sum += minimax.Positions;
            }

            {
                Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
                var a3 = b1.GenerateBoardWithMove(
                    Utility.ANtoPos(c + "2"),
                    Utility.ANtoPos(c + "4"),
                    Piece.wPawn
                );
                minimax.Run(a3, d, false);
                Console.WriteLine(c + "4: " + minimax.Positions);
                sum += minimax.Positions;
            }
        }

        Console.WriteLine("Sum: " + sum + " with depth = " + d);
    }

    public static Chessboard errorComp = new();
    static Chessboard? visualBoard; //board auf dem die "wahre" position gespeichert wird
    public static bool debug = false;
    static void Main(string[] args) {
        //8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1 ;D4 43238 ;D5 674624 ;D6 11030083

        (Chessboard cb, bool whiteStarts) = Chessboard.FromFEN("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
        // cb = Testing.BuildPosition(cb, "b4c4 h4g5 c4c6 g5f6 e2e4");
        //f4e3 wird nicht generated
        //  Console.WriteLine(cb.enPassantSqr);

        // cb.Print();
        /* 
        DisplayBoard(cb);
        Testing.PerftResults(cb, 6, true); 
        */
        //Console.WriteLine(Testing.perftSum(cb, 1, false));


        Testing.LoadAndTest();
        /* 
        en passant bug handling
        https://peterellisjones.com/posts/generating-legal-chess-moves-efficiently/

         */
    }
}
