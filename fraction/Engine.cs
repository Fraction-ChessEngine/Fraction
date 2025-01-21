using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fraction;
public static class Engine {
    private const string name = "fraction";
    private const string author = "ValleAkaJesus";

    private static TextReader stdin = Console.In;
    private static TextWriter stdout = Console.Out;

    private static CancellationTokenSource cts = new();
    private static Task? bestMove = null;

    private static Chessboard board = new();
    private static bool WhitesTurn = true;

    public static bool Debug { get; private set; } = false;

    private static void HandleUci() {
        stdout.WriteLine($"id name {name}");
        stdout.WriteLine($"id name {author}");
        stdout.WriteLine("uciok");
    }

    private static void HandleDebug(String[] args) {
        for (int i = 1; i < args.Length; i++) {
            switch (args[i].ToLower()) {
                case "on":
                    Debug = true;
                    return;
                case "off":
                    Debug = false;
                    return;
                default:
                    break;
            }
        }
    }

    private static void HandleIsReady() {
        if (cts.IsCancellationRequested) {
            bestMove?.Wait();
            bestMove = null;
        }
        if (bestMove is null) {
            if (!cts.TryReset()) {
                cts.Dispose();
                cts = new();
            }
        }
        stdout.WriteLine("readyok");
    }

    private static void HandleUciNewGame() { }

    private static void HandlePosition(String[] args) {
        var start = 0;
        var op = 0;
        for (; op == 0 && start < args.Length; start++) {
            op = args[start].ToLower() switch {
                "startpos" => 1,
                "fen" => 2,
                "moves" => 3,
                _ => 0,
            };
        }

        switch (op) {
            case 0: return;
            case 1:
                (board, WhitesTurn) = (new(), true);
                start++;
                break;
            case 2:
                FEN? fen = null;
                for (; start < args.Length - 6 && !FEN.TryParse(string.Join(' ', args, start, start + 6), out fen); start++) ;
                if (fen is null) return;
                (board, WhitesTurn) = (new(fen), fen.WhitesTurn);
                start += 6;
                break;
            case 3:
                break;
        }

        for (; start < args.Length; start++) {
            if (args[start].ToLower() == "moves") return;
        }

        if (++start >= args.Length) return;

        for (; start < args.Length; start++) {
            if (Move.TryParse(args[start], out var move)) {
                var moves = MoveGen.GenerateMoves(board, WhitesTurn).ToArray();
                if (moves.Contains(move)) {
                    board.MakeMove(move);
                    WhitesTurn = !WhitesTurn;
                }
            }
        }
    }

    private static void HandleGo(String[] args) {
        if (bestMove is null) {
            bestMove = Minimax.BestMoveAsync(new(), true, -1, cts.Token)
                .ContinueWith(static (t) => {
                    Move result = t.GetAwaiter().GetResult();
                    bestMove = null;
                    stdout.WriteLine($"bestMove {result}");
                });
        }
    }

    private static void HandleStop() {
        if (bestMove is not null) {
            cts.Cancel();
        }
    }

    public static void Run() {
        TextReader stdin = Console.In;
        TextWriter stdout = Console.Out;

        for (string? cmd = stdin.ReadLine(); cmd is not null; cmd = stdin.ReadLine()) {
            String[] args = cmd.Split(' ');
            for (int i = 0; i < args.Length; i++) {
                switch (args[i].ToLower()) {
                    case "uci":
                        HandleUci();
                        break;
                    case "debug":
                        HandleDebug(args[i..]);
                        break;
                    case "isready":
                        HandleIsReady();
                        break;
                    case "ucinewgame":
                        HandleUciNewGame();
                        break;
                    case "position":
                        HandlePosition(args[i..]);
                        break;
                    case "go":
                        HandleGo(args[i..]);
                        break;
                    case "stop":
                        HandleStop();
                        break;
                    case "quit":
                        Environment.Exit(0);
                        break;
                    default:
                        i = args.Length;
                        break;
                }
            }
            stdout.Flush();
        }
    }
}
