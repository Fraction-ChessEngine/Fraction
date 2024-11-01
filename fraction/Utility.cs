using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Security.Cryptography;

namespace fraction;
static class Utility {
    public static void PrintBitBoard(BitBoard bb, int specialIndex = -1)
    {
        byte[] bytes = BitConverter.GetBytes(bb);
        int[] pos = specialIndex >= 0 ? Utility.IndexToPos(specialIndex) : new int[2];
        for (int i = 0; i < 8; i++)
        {
            byte x = bytes[7 - i];
            string s = System.Convert.ToString(x, 2);
            if (x < 128)
                s = "0" + s;
            if (x < 64)
                s = "0" + s;
            if (x < 32)
                s = "0" + s;
            if (x < 16)
                s = "0" + s;
            if (x < 8)
                s = "0" + s;
            if (x < 4)
                s = "0" + s;
            if (x < 2)
                s = "0" + s;
            if (specialIndex >= 0 && 7 - i == pos[1])
            {
                int a = 7 - pos[0];
                s = s.Substring(0, a) + "X" + s.Substring(a + 1, 7 - a);
            }
            Console.WriteLine(Reverse(s));
        }
        Console.WriteLine(" ");
    }

    public static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    
    /// <summary>
    /// Konvertiert einen FEN-String zu einer Position die einem Board gegeben werden kann
    /// </summary>
    /// <param name="fen"></param>
    /// <returns></returns>
    public static Dictionary<int, Piece> FENtoPosition(string fen) {
        Dictionary<int, Piece> pos = new Dictionary<int, Piece> { };
        int currPos = 0;

        foreach (char c in fen) {
            switch (c) {
                case 'r':
                    pos[Convert(currPos)] = Piece.bRook;
                    break;
                case 'R':
                    pos[Convert(currPos)] = Piece.wRook;
                    break;
                case 'n':
                    pos[Convert(currPos)] = Piece.bKnight;
                    break;
                case 'N':
                    pos[Convert(currPos)] = Piece.wKnight;
                    break;
                case 'b':
                    pos[Convert(currPos)] = Piece.bBishop;
                    break;
                case 'B':
                    pos[Convert(currPos)] = Piece.wBishop;
                    break;
                case 'k':
                    pos[Convert(currPos)] = Piece.bKing;
                    break;
                case 'K':
                    pos[Convert(currPos)] = Piece.wKing;
                    break;
                case 'q':
                    pos[Convert(currPos)] = Piece.bQueen;
                    break;
                case 'Q':
                    pos[Convert(currPos)] = Piece.wQueen;
                    break;
                case 'p':
                    pos[Convert(currPos)] = Piece.bPawn;
                    break;
                case 'P':
                    pos[Convert(currPos)] = Piece.wPawn;
                    break;
                case '/':
                    currPos--;
                    break;

                default: //c ist eine zahl
                    currPos += c - '0' - 1;
                    break;
            }

            currPos++;
        }

        return pos;
    }

    /// <summary>
    /// Konvertiert einen index auf dem board zu einem weirden FEN-Index (0=a8, 63=h1)
    /// </summary>
    /// <returns></returns>
    private static int Convert(int i) {
        return (i % 8) * 2 + 56 - i;
    }

    /// <summary>
    /// Generiert den FEN string zu einer entsprechenden Position, "positionToFEN()"
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static string BoardToFEN(Dictionary<int, Piece> pos) {
        string fen = "";
        int currNum = 0;

        for (int i = 0; i < 64; i++) {
            //das sqr mit dem FEN-Index das wir abrufen wollen, muss konvertiert wegen der komischen FEN-Konvention
            int sqr = Convert(i);

            //wenn ein linebreak gehittet wurde
            if (i % 8 == 0 && i > 0) {
                if (currNum > 0)
                    fen += currNum;
                fen += "/";
                currNum = 0;
            }

            Piece piece;
            if (pos.TryGetValue(sqr, out piece)) {
                //das piece an den fen-string appenden
                if (currNum > 0)
                    fen += currNum;
                fen += piece.GetSymbol();
                currNum = 0;
            } else {
                currNum++;
            }
        }

        if (currNum > 0)
            fen += currNum;
        return fen;
    }

    /// <summary>
    /// Konvertiert einen index (0-63) zu einem 2er array mit x,y (0-7)
    /// </summary>
    /// <param name="index">Der index des Pieces in einem 64er array</param>
    /// <returns></returns>
    public static int[] IndexToPos(int index) {
        //y * 8 + x = index

        int y = index >> 3;
        int x = index & 7;
        return new int[] { x, y };
    }

    /// <summary>
    /// Konvertiert ein 2er array mit x,y zu einem index in einem 64er array
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static int PosToIndex(int[] pos) {
        return pos[1] * 8 + pos[0];
    }

    public static int PosToIndex(int i1, int i2) {
        return 8 * i2 + i1;
    }


    public static Piece SymbolToPiece(string symbol) {
        switch (symbol) {
            case "r":
                return Piece.bRook;
            case "n":
                return Piece.bKnight;
            case "b":
                return Piece.bBishop;
            case "q":
                return Piece.bQueen;
            case "k":
                return Piece.bKing;
            case "p":
                return Piece.bPawn;

            case "R":
                return Piece.wRook;
            case "N":
                return Piece.wKnight;
            case "B":
                return Piece.wBishop;
            case "Q":
                return Piece.wQueen;
            case "K":
                return Piece.wKing;
            case "P":
                return Piece.wPawn;

            default:
                Console.WriteLine(
                    "Something went terribly wrong in (Utility.SymbolToPiece(...)"
                );
                return Piece.bKing;
        }
    }

    /// <summary>
    /// position zu Algebraic Notation; hat bis jetzt nur den zweck einer leichteren visualisierung
    /// </summary>
    /// <returns></returns>
    public static string PosToAN(int[] pos) {
        return "abcdefgh"[pos[0]].ToString() + pos[1]; //funktionert, frag nicht wieso
    }

    public static string PosToAN(int pos) {
        return "abcdefgh"[pos & 7].ToString() + ((pos >> 3) + 1);
        //das sieht echt bodenlos aus, aber es klappt
    }

    /// <summary>
    /// Algebraic Notation -> position als int
    /// </summary>
    /// <param name="AN"></param>
    /// <returns></returns>
    public static int ANtoPos(string AN) {
        int x = "abcdefgh".IndexOf(AN[0]);
        int y = "12345678".IndexOf(AN[1]);

        return 8 * y + x;
    }

    /* public static void printIntArr(int[] sqrs) {
        string str = "";
        for (int i = 0; i < sqrs.Length; i++) {
            str += sqrs[i] + ", ";
        }

        Console.WriteLine(str);
    } */

    /// <summary>
    /// Generiert das Bitboard einer pieceArt aus der positionsdarstellung
    /// </summary>
    /// <param name="pieces"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static BitBoard GetBBofPosition(Dictionary<int, Piece> pieces, Piece p) {
        BitBoard retBB = 0;
        for (int i = 0; i < 64; i++) {
            Piece currPiece;
            if (pieces.TryGetValue(i, out currPiece)) {
                if (currPiece == p) {
                    retBB |= 1ul << i;
                }
            }
        }

        return retBB;
    }

    public static void DisplayAllBoards(Chessboard[][] b) {
        foreach (var b1 in b) {
            if (b1 == null) {
                continue;
            }

            foreach (var b2 in b1) {
                Program.DisplayBoard(b2);
            }
        }
    }
}
