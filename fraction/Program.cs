using System;
using System.IO;

namespace fraction;
public class Program {
    private const string name = "fraction";
    private const string author = "ValleAkaJesus";

    public static void Go(string[] args) {
        switch(args[0]) {
            case "perft":
                break;
            default:
                break;
        }
    }

    public static void Main() {
        TextReader stdin = Console.In;
        TextWriter stdout = Console.Out;

        for (string? cmd = stdin.ReadLine(); cmd is not null; cmd = stdin.ReadLine()) {
            String[] args = cmd.Split(' ');
            switch (args[0]) {
                case "uci":
                    stdout.WriteLine($"id name {name}");
                    stdout.WriteLine($"id name {author}");
                    stdout.WriteLine("uciok");
                    break;
                case "isready":
                    stdout.WriteLine("readyok");
                    break;
                case "ucinewgame":
                    break;
                case "position":
                    break;
                case "go":
                    Go(args[1..^1]);
                    break;
                case "stop":
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }
        }
    }
}
