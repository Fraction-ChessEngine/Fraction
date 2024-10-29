using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

/*
    --Klasse die zum testen und benchmarken der software dient--
    -funktion die ein PGN ein ein array von fen strings umwandelt
    -funktionen die relevante funktionen von MoveGen und MoveSets benchmarken
*/
namespace fraction;
static class Testing {
    /// <summary>
    /// Liest das Textfile ein und generiert ein Array aus Plys in string form
    /// </summary>
    /// <returns></returns>
    public static string[][] GetPlysFromFile(string fileName) {
        //maximal 10000 PGNs
        string[][] plys = new string[10000][];
        string content = File.ReadAllText(fileName);

        string[] games = content.Split("sep");

        Console.WriteLine("|--- " + games.Length + " games read in, beginning to format ---|");

        int currIndex = 0;
        foreach (string game in games) {
            string[] moves = game.Split(".");

            for (int i = 0; i < moves.Length; i++) {
                if (moves[i].Length == 1) {
                    moves[i] = "";
                    continue;
                }

                moves[i] = moves[i].Remove(moves[i].Length - 2); //cuttet die beiden letzten chars um den index zu moves zu entfernen
                moves[i] = moves[i].Trim();
            }

            string[] gamePlys = string.Join(" ", moves).Split(" ");

            gamePlys[gamePlys.Length - 1] = ""; //um das 1-0 zu entfernen

            //um das # zu entfernen
            if (gamePlys.Length > 2) //um edgecases abzufangen
            {
                string lastPly = gamePlys[gamePlys.Length - 2];
                if (lastPly != "" && lastPly[lastPly.Length - 1] == '#') {
                    gamePlys[gamePlys.Length - 2] = gamePlys[gamePlys.Length - 2].Substring(
                        0,
                        gamePlys[gamePlys.Length - 2].Length - 1
                    );
                }
            }

            plys[currIndex] = gamePlys;
            currIndex++;
        }

        //entfertn das "+"
        foreach (string[] game in plys) {
            if (game == null)
                continue;
            for (int i = 0; i < game.Length; i++) {
                game[i] = RemovePlus(game[i]);
            }
        }

        return plys;
    }

    //string.contains methode funktioniert nicht, erkennt "+" nicht reliable, dh ersatzmethode muss geschrieben werden
    private static string RemovePlus(string str) {
        string retStr = "";
        for (int i = 0; i < str.Length; i++) {
            if (str[i] != '+' && str[i] != 'x')
                retStr += str[i];
        }
        return retStr;
    }

    public static string[] PlysToFENs(string[] plys) {
        // printStrings(plys);

        Dictionary<int, Piece> currPos = Utility.FENtoPosition(
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"
        );
        string[] FENs = new string[plys.Length];
        bool whiteToPlay = true;

        for (int i = 0; i < plys.Length; i++) {
            string currPly = plys[i];
            if (currPly == "")
                continue;

            //es ist ein pawnMove, wird nicht in getPlysFromFile() verschoben weil man hier whiteToPlay braucht
            if (currPly.Length == 2)
                currPly = (whiteToPlay ? "P" : "p") + currPly;

            //debug
            /* if (i == 14)
            {
                Console.WriteLine("ERROR HERE: " + currPly);
            } */

            /* Hier wird dc6 zu pc6 gemacht, dies entfernt aber relevante information über den PawnMove,
            in diesem Fall dass mit dem dPawn genommen wird
             */

            //pawn captures werden mit kleinbuchstaben für die ranks representiert
            if (currPly == currPly.ToLower() && currPly[0] != 'p') {
                // Console.WriteLine(currPly + " is a pawnMove, becomes " + (whiteToPlay ? "P" : "p") + currPly.Substring(0, currPly.Length));
                currPly = (whiteToPlay ? "P" : "p") + currPly.Substring(0, currPly.Length);
            }

            //pgn unterscheidet nicht zwischen verschiedenen farben bei der schreibweise
            if (!whiteToPlay)
                currPly = currPly.ToLower();

            //castlen muss abgefangen werden
            if (currPly == "O-O") //weiß, kingside short castle
            {
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 7, 5); //rook geht nach c1
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 4, 6);
            } else if (currPly == "o-o") //schwarz, kingside short castle
              {
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 63, 61);
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 60, 62);
            } else if (currPly == "O-O-O") //weiß, queenside long castle
              {
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 0, 3);
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 4, 2);
            } else if (currPly == "o-o-o") //schwarz, queenside long castle
              {
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 56, 59);
                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), 60, 58);
            } else {
                //normale piece bewegungen
                Piece piece = Utility.SymbolToPiece(currPly[0].ToString());

                // Console.WriteLine(currPly + ", Index = " + i);

                int endPos = GetEndPosOfPly(currPly);

                int startPos;
                if (currPly.Length == 4) //ein weirder move mit zusatzangaben
                {
                    char posInfo = currPly[1];

                    if ("12345678".Contains(posInfo)) //y koordinate ist gegeben
                    {
                        startPos = GetStartPosOfPly(
                            currPos,
                            piece,
                            endPos,
                            -1,
                            "12345678".IndexOf(posInfo)
                        );
                    } else //x koordinate ist gegeben
                      {
                        // Console.WriteLine("Found x = " + "abcdefgh".IndexOf(posInfo));
                        startPos = GetStartPosOfPly(
                            currPos,
                            piece,
                            endPos,
                            "abcdefgh".IndexOf(posInfo),
                            -1
                        );
                    }
                } else {
                    startPos = GetStartPosOfPly(currPos, piece, endPos);
                }

                //debug
                if (startPos == -1) {
                    return FENs; //weil en passant nicht implementiert wurde und daher oft "illegale" moves passieren
                    /* Console.WriteLine(piece);
                    Console.WriteLine(endPos);
                    Console.WriteLine(Utility.BoardToFEN(currPos));
                    Console.WriteLine(currPly); */
                }

                currPos = GeneratePosWithMove(Utility.BoardToFEN(currPos), startPos, endPos);
            }

            FENs[i] = Utility.BoardToFEN(currPos);
            whiteToPlay = !whiteToPlay;
        }

        return FENs;
    }

    //debuggen
    private static int GetStartPosOfPly(
        Dictionary<int, Piece> pos,
        Piece p,
        int endPos,
        int givenX = -1,
        int givenY = -1
    ) {
        //für moves wie Nge2
        if (givenX > -1) {
            for (int y = 0; y < 8; y++) {
                int index = Utility.PosToIndex(givenX, y);

                Piece currPiece;
                if (pos.TryGetValue(index, out currPiece)) //wenn hier ein piece existiert
                {
                    if (currPiece != p)
                        continue; //nur für die richtige pieceArt checken

                    BitBoard bb = MoveSets.GetPseudoLegalMoves(
                        new Chessboard(pos),
                        index, currPiece
                    );
                    if (bb[endPos])
                        return index;
                }
            }
        } else if (givenY > -1) {
            for (int x = 0; x < 8; x++) {
                int index = Utility.PosToIndex(x, givenY);
                Piece currPiece;
                if (pos.TryGetValue(index, out currPiece)) //wenn hier ein piece existiert
                {
                    if (currPiece != p)
                        continue; //nur für die richtige pieceArt checken
                    if (
                        MoveSets.GetPseudoLegalMoves(
                            new Chessboard(pos),
                            index, currPiece
                        )[endPos]
                    )
                        return index;
                }
            }
        } else {
            //für alle mgl Pieces der korrekten art überprüfen ob die endPos im bitboard im lookuptable enthalten ist
            for (int i = 0; i < 64; i++) {
                Piece currPiece;
                if (pos.TryGetValue(i, out currPiece)) //wenn hier ein piece existiert
                {
                    if (currPiece != p)
                        continue; //nur für die richtige pieceArt checken

                    BitBoard bb = MoveSets.GetPseudoLegalMoves(
                        new Chessboard(pos),
                        i, currPiece
                    );

                    if (bb[endPos])
                        return i;
                }
            }
        }

        /* Console.WriteLine(Utility.BoardToFEN(pos));
        Console.WriteLine("function failed at Piece: " + p); */
        return -1;
    }

    private static int GetEndPosOfPly(string ply) {
        if (ply == "O-O-O")
            return -3; //castlen hätte abgefangen werden müssen
        if (ply == "O-O")
            return -2;
        string plyPos = "";

        //greift die hinteren beiden chars ab, da diese immer die position sind
        for (int i = 0; i < 2; i++) {
            plyPos = ply[ply.Length - 1 - i] + plyPos;
        }

        return Utility.ANtoPos(plyPos);
    }

    public static Dictionary<int, Piece> GeneratePosWithMove(string posFEN, int start, int end) {
        Dictionary<int, Piece> pos = Utility.FENtoPosition(posFEN);
        Piece piece = pos[start];
        pos.Remove(start);
        pos[end] = piece;

        return pos;
    }

    public static void PrintStrings(string[] strings, bool newline = false) {
        for (int i = 0; i < strings.Length; i++) {
            Console.Write(strings[i] + " , " + (newline ? "\n" : ""));
        }

        Console.WriteLine("");
    }

    public static void BenchMark() {
        int n = 852000; //n<852000
        Chessboard[] chessboards = ReadFENsFromFile(n);
        Stopwatch sw = new Stopwatch();

        sw.Start();
        for (int i = 0; i < n; i++) {
            MoveGen.GenerateBoards(chessboards[i], true);
        }
        sw.Stop();

        Console.WriteLine(
            "1 Iteration on " + n + " different positions, Time elapsed={0}",
            sw.Elapsed
        );
        float t = sw.Elapsed.Seconds + (float)sw.Elapsed.Milliseconds / 1000f;
        Console.WriteLine((float)n / t + " Iterations per second");
    }

    public static void BenchMarkPins() {
        int n = 852000; //n<852000
        Chessboard[] chessboards = ReadFENsFromFile(n);
        Stopwatch sw = new Stopwatch();

        sw.Start();
        for (int i = 0; i < n; i++) {
            chessboards[i].GeneratePinnedPieceBB(true);
            chessboards[i].GeneratePinnedPieceBB(false);
        }
        sw.Stop();

        Console.WriteLine(
            "1 Iteration on " + n + " different positions, Time elapsed={0}",
            sw.Elapsed
        );
        float t = sw.Elapsed.Seconds + (float)sw.Elapsed.Milliseconds / 1000f;
        Console.WriteLine((float)n * 2 / t + " Iterations per second");
    }

    public static void BenchMarkMINIMAX() {
        Stopwatch sw = new Stopwatch();

        int depth = 7;

        sw.Start();

        Chessboard b = new Chessboard();
        Minimax minimax = new() { MaxQuiescenceSearchPlies = 3, AlphaBetaPruning = true };
        float eval = minimax.Run(b, depth, true);
        Console.WriteLine("Eval = " + (float)eval);
        Console.WriteLine("Depth = " + (float)depth);

        sw.Stop();

        Console.WriteLine("Time elapsed={0}", sw.Elapsed);

        float t = sw.Elapsed.Seconds + (float)sw.Elapsed.Milliseconds / 1000f;
    }

    public static void BenchMarkMoveSets() {
        BitBoard[] bbs =
        {
                0b0101010101001010001001111110101011101010010111100010011000111001,
                0b1000110001101101101111111110111100010101011101100000010110011110,
                0b1111110000001000001110101010100111100011011110000101010001110001,
                0b1101100011000101101000000111010000011010101111000000001011010000,
                0b0101001110110001000111001110010001011011101110010011010100111111,
                0b0011110011110000010110000010101000010111010111001001101011011000,
                0b1100001001011111011100101010100010100101011000010001110000100100,
                0b0101100011000000011110000011000101000101000010101001100001010010
            };

        int max = 100000;

        Stopwatch sw = new Stopwatch();

        sw.Start();

        for (int n = 0; n < max; n++) {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 64; j++) {
                    MoveSets.GetPseudoTargetSqrsRook(bbs[i], j);
                }
            }
        }

        sw.Stop();

        Console.WriteLine(
            "1 Iterations on " + max * 8 * 64 + " different positions, Time elapsed={0}",
            sw.Elapsed
        );
        float t = sw.Elapsed.Seconds + (float)sw.Elapsed.Milliseconds / 1000f;
        Console.WriteLine((float)max * 8f * 64f / t + " Iterations per second");
    }

    static Chessboard[] ReadFENsFromFile(int maxIndex) {
        string[] fens = File.ReadAllLines("FENdatabase.txt");
        Chessboard[] boards = new Chessboard[maxIndex];

        for (int i = 0; i < maxIndex; i++) {
            boards[i] = new Chessboard(Utility.FENtoPosition(fens[i]));
        }

        return boards;
    }

    //es gibt wohl nie mehr als 128 moves
    public static string[] perftmoves = new string[128];
    public static void PerftResults(Chessboard b, int d, bool whitesTurn) {
        Chessboard[] boards = MoveGen.GenerateBoards(b, whitesTurn, true);

        int sum = 0;
        for (int i = 0; i < boards.Length; i++) {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            minimax.Run(boards[i], d - 1, !whitesTurn);
            sum += minimax.Positions;

            Console.WriteLine(perftmoves[i] + ": " + minimax.Positions);
        }

        Console.WriteLine("Sum: " + sum);
    }

    public static void Checks() {
        Chessboard[] checkBoards = {
            Chessboard.FromFEN("rnbqkbnr/ppp1pppp/3p4/8/Q2P4/8/PPP1PPPP/RNB1KBNR"),
            Chessboard.FromFEN("rnbqkbnr/ppp1pppp/2Qp4/8/3P4/8/PPP1PPPP/RNB1KBNR"),
            Chessboard.FromFEN(""),
            Chessboard.FromFEN(""),
            Chessboard.FromFEN("")
    };
    }
}
