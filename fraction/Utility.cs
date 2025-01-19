using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Security.Cryptography;

namespace fraction;
static class Utility {
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

    public static int PosToIndex(int i1, int i2) {
        return 8 * i2 + i1;
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
}
