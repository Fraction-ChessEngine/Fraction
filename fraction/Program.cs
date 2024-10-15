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

    static Chessboard? visualBoard; //board auf dem die "wahre" position gespeichert wird

    static void Main(string[] args) {

        /* 
            b2b3: 9345 vs b2b3: 9347
            c2c3: 9272 vs c2c3: 9313
            d2d3: 11959 vs d2d3: 11961
            e2e3: 13134 vs e2e3: 13164
            g2g3: 9345 vs g2g3: 9347
            c2c4: 9744 vs c2c4: 9784
            d2d4: 12435 vs d2d4: 12437
            e2e4: 13160 vs e2e4: 13193
            b1c3: 9755 vs b1c3: 9757
            g1f3: 9748 vs g1f3: 9754
            g1h3: 8881 vs g1h3: 8883 
        */
        visualBoard = new();
        DisplayBoard(visualBoard);

        /* visualBoard = visualBoard.GenerateBoardWithMove(Utility.ANtoPos("c1"), Utility.ANtoPos("a3"), Piece.wBishop);
        MoveGen.GenerateBoards(visualBoard, false);
        MoveGen.GenerateBoards(visualBoard, true);

        /* TODO bugfixing, perft updatet die scheisse aus irgendeinem grund nicht selbst, bitte perften um das zu beheben */


        Testing.PerftResults(visualBoard, 6, true);

    }
}
