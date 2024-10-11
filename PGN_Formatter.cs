using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace fraction
{
    static class PGN_Formatter
    {
        public static void CreateFormattedFile(string pgnName, string fileName)
        {
            string[] lines = File.ReadAllLines(pgnName);

            using (
                StreamWriter outputFile = new StreamWriter(
                    ("C:\\Users\\valen\\Desktop\\Coding\\C#\\chessbot\\" + fileName)
                )
            )
            {
                string currentPGN = "";
                foreach (string line in lines)
                {
                    if (line == "")
                    {
                        if (currentPGN != "")
                        {
                            outputFile.WriteLine(currentPGN);
                            outputFile.WriteLine("sep");
                            currentPGN = "";
                        }
                        ;

                        continue;
                    }
                    ;

                    if (line[0] == '[')
                        continue;

                    if (line[0] == '1' && currentPGN == "")
                    {
                        currentPGN = line;
                        continue;
                    }

                    //muss appended werden
                    if ("123456789".IndexOf(line[0]) != -1)
                    {
                        currentPGN += " " + line;
                    }
                }
            }
        }

        /// <summary>
        /// Nimmt ein formatiertesPGNFile entgegen und sortiert es in die FEN-Datenbank
        /// </summary>
        /// <param name="fileName"></param>
        public static void InsertPGNFileToFENdatabase(string fileName, string targetFileName)
        {
            // string[] lines = File.ReadAllLines(fileName);
            string[][] fens = new string[5000][];

            string[][] games = Testing.GetPlysFromFile(fileName);

            for (int i = 0; i < 4000; i++)
            {
                fens[i] = Testing.PlysToFENs(games[i]);
            }
            /*  fens[0] = Testing.plysToFENs(games[9]);*/




            using (
                StreamWriter outputFile = new StreamWriter(
                    ("C:\\Users\\valen\\Desktop\\Coding\\C#\\chessbot\\" + targetFileName)
                )
            )
            {
                foreach (string[] strings in fens)
                {
                    if (strings == null)
                        continue;
                    foreach (string fen in strings)
                    {
                        // if (ContainsString(targetFileName, fen)) continue;
                        outputFile.WriteLine(fen);
                    }
                }
            }
        }

        public static void RemoveDuplicates(string fenFileName)
        {
            string[] content = File.ReadAllLines(fenFileName);
            string[] distinct = content.Distinct().ToArray();

            using (
                StreamWriter outputFile = new StreamWriter(
                    ("C:\\Users\\valen\\Desktop\\Coding\\C#\\chessbot\\" + fenFileName)
                )
            )
            {
                foreach (string str in distinct)
                {
                    outputFile.WriteLine(str);
                }
            }
        }

        public static bool ContainsString(string fileName, string str)
        {
            string[] content = File.ReadAllLines(fileName);

            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == str)
                    return true;
            }

            return false;
        }
    }
}
