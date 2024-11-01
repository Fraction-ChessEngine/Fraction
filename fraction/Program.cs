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
        var prof = Chessboard.FromFEN("r1bqkb2/ppp1pnp1/2np4/5p1r/2B1P3/3P4/PPPN1PPP/RNB1K2R");
        var evalB1 = Chessboard.FromFEN("5r2/1p6/2p1k3/4Q2p/3P4/8/5PK1/q7");

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
        /* Chessboard[] checks = new Chessboard[]{
            Chessboard.FromFEN("rnb1kbnr/pp1ppppp/2p5/q7/3P4/8/PPP1PPPP/RNBQKBNR"),
            Chessboard.FromFEN("3k4/8/8/4n3/8/r1PK4/8/8"),
            Chessboard.FromFEN("3k3r/8/b7/8/8/2PK4/7b/8")
        };

        Chessboard[] noChecks = new Chessboard[]{
            Chessboard.FromFEN("r1bqr1k1/pp1n1pbp/2pp1np1/4p3/P1BP1B2/4PN1P/1PPN1PP1/R2Q1RK1"),
            Chessboard.FromFEN("1r1q1rk1/2p2ppp/2np1n2/2bNp3/1pB1P1b1/2P2N2/1P1PQPPP/R1B2RK1"),
            Chessboard.FromFEN("rnbqkb1r/pp3ppp/4pn2/2ppN3/3P1B2/8/PPP1PPPP/RN1QKB1R")
         };Chessboard[] checkMates ={
            Chessboard.FromFEN("8/8/8/8/8/2k5/1q6/1K6"),
            Chessboard.FromFEN("1k5R/6R1/8/8/8/8/8/1K6"),
            Chessboard.FromFEN("1k6/2P5/Q2P4/8/8/8/8/1K6"),
            Chessboard.FromFEN("r1bqkb1r/pppp1Qpp/2n2n2/4p3/2B1P3/8/PPPP1PPP/RNB1K1NR")
         }; */

        Chessboard cb = new();
        cb = cb.GenerateBoardWithMove((Utility.ANtoPos("d2")), (Utility.ANtoPos("d3")), Piece.wPawn);
        /*   cb = cb.GenerateBoardWithMove((Utility.ANtoPos("d7")), (Utility.ANtoPos("d5")), Piece.bPawn);
          cb = cb.GenerateBoardWithMove((Utility.ANtoPos("c2")), (Utility.ANtoPos("c4")), Piece.wPawn);
          cb = cb.GenerateBoardWithMove((Utility.ANtoPos("d5")), (Utility.ANtoPos("c4")), Piece.bPawn);
          cb = cb.GenerateBoardWithMove((Utility.ANtoPos("e1")), (Utility.ANtoPos("d2")), Piece.wKing);
          cb = cb.GenerateBoardWithMove((Utility.ANtoPos("e8")), (Utility.ANtoPos("d7")), Piece.bKing);
             cb = cb.GenerateBoardWithMove((Utility.ANtoPos("g1")), (Utility.ANtoPos("f3")), Piece.wKnight);
           cb = cb.GenerateBoardWithMove((Utility.ANtoPos("d7")), (Utility.ANtoPos("d2")), Piece.bQueen); 
        cb.Print();*/

        for (int i = 2; i <= 6; i++) {
            Testing.BenchmarkPERFT(i);
          //  Testing.BenchmarkPERFT(i);
        }

        // Testing.PerftResults(new(), 7, true);

        //  Utility.PrintBitBoard(cb.pinnedBB);
        /*Perft begin:  3246355418 nodes
                         -50262561 (fixed bug where king castles out of check)
                            -14397 (fixed bug where pins where calculated incorrectly) 
                          -1060059 (fixed bug where controlledSqrs considered pins wrong)
                               -10 (fixed bug where king didnt block own sliders sightlines at enemyKing) = 3195018411

              */
    }
}
