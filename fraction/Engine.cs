using System;
using System.Collections.Generic;
using System.IO;
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
        bool startpos = false;
        int start = 0;
        foreach (var arg in args) {
            switch (arg.ToLower()) {
                case "startpos":
                    startpos = true;
                    break;
                case "moves":
                    break;
                default:
                    start++;
                    continue;
            }
            break;
        }

        foreach (var arg in args[start..^1]) {
            switch (arg.ToLower()) {
                case "moves":
                    break;
                default:
                    start++;
                    continue;
            }
            break;
        }

        Span<Move> movesBuf = stackalloc Move[args.Length - start];
        int count = 0;
        for (int i = start; i < args.Length; i++) {
            if (Move.TryParse(args[i].ToLower(), out Move m)) {
                movesBuf[count] = m;
                count++;
            }
        }

        for (int i = 0; i < count; i++) {
            board.MakeMove(
                movesBuf[i].Start,
                movesBuf[i].End,
                board.GetPieceAt(movesBuf[i].Start),
                movesBuf[i].Promotion ?? Piece.wQueen
            );
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
            switch (args[0].ToLower()) {
                case "uci":
                    HandleUci();
                    break;
                case "debug":
                    HandleDebug(args);
                    break;
                case "isready":
                    HandleIsReady();
                    break;
                case "ucinewgame":
                    HandleUciNewGame();
                    break;
                case "position":
                    HandlePosition(args);
                    break;
                case "go":
                    HandleGo(args);
                    break;
                case "stop":
                    HandleStop();
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }
            stdout.Flush();
        }
    }
}
