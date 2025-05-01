using System;
using String = System.String;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fraction.UCI;
using UciEngine = Fraction.UCI.Engine;

namespace fraction;
public class Fraction : UciEngine {
    private const string name = "fraction";
    private const string author = "ValleAkaJesus";

    private CancellationTokenSource cts = new();
    private Task? bestMove = null;

    private Chessboard board = new();
    private bool WhitesTurn = true;

    public Fraction() : base() { }

    public bool Debug { get; private set; } = false;

    private void HandleUci() {
        this.Send(new Id(Id.Type.Name, name));
        this.Send(new Id(Id.Type.Author, author));
        this.Send(new UciOk());
    }

    private void HandleDebug(String[] args) {
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

    private void HandleIsReady() {
        if (cts.IsCancellationRequested) {
            bestMove?.Wait();
            bestMove = null;
        }
        this.Send(new ReadyOk());
    }

    private void HandleGo(IEnumerable<Go.ICommand> args) {

        bool infinite = false;
        int? moves = null;
        int? depth = null;
        int? time = null;
        int? etime = null;
        foreach (var arg in args) {
            switch (arg) {
                case Infinite:
                    infinite = true;
                    break;
                case MovesToGo a:
                    moves = moves ?? a.X;
                    break;
                case Depth a:
                    depth = depth ?? a.X;
                    break;
                case Time a:
                    if (a.Turn == (this.WhitesTurn ? Color.White : Color.Black))
                        time = time ?? a.TimeLeft;
                    else etime = etime ?? a.TimeLeft;
                    break;
                default:
                    this.Log(LogLevel.Warning, $"Not Handling {arg.Serialize()}");
                    break;
            }
        }

        if (bestMove is null) {
            bestMove = Minimax.BestMoveAsync(this.board, this.WhitesTurn, depth ?? -1, cts.Token)
                .ContinueWith((t) => {
                    Move result = t.GetAwaiter().GetResult();
                    result = result with { Promotion = result.Promotion | (Piece)8 };
                    this.bestMove = null;
                    this.Send(new BestMove(result.ToString()));
                    if (!cts.TryReset()) {
                        cts.Dispose();
                        cts = new();
                    }
                });

            if (!infinite && time is not null) {
                int x = time.Value;
                if (moves is not null) {
                    x /= moves.Value;
                } else if (etime is not null) {
                    x -= etime.Value;
                } else x = -1;
                if (x <= 0) x = 200;
                this.cts.CancelAfter(x);
            }
        }
    }

    private void HandleStop() {
        if (bestMove is not null) {
            cts.Cancel();
        }
    }

    protected override void Handle(ICommand command) {
        switch (command) {
            case Uci:
                this.HandleUci();
                break;

            case Debug c:
                if (c.State) {
                    this.MinLogLevel = LogLevel.Debug;
                    Debug = true;
                } else {
                    this.MinLogLevel = LogLevel.Warning;
                    Debug = false;
                }
                break;

            case IsReady:
                this.HandleIsReady();
                break;

            case UciNewGame:
                break;

            case Position c:
                if (c.Fen is not null) {
                    if (FEN.TryParse(c.Fen, out FEN? fen)) {
                        this.board = new(fen);
                        this.WhitesTurn = fen.WhitesTurn;
                    } else {
                        this.Log(LogLevel.Warning, $"fenparsing failed. Fen: {c.Fen}");
                        goto default;
                    }
                } else {
                    this.board = new();
                    this.WhitesTurn = true;
                }

                foreach (var move in c.moves) {
                    if (Move.TryParse(move, out Move m)) {
                        this.board.MakeMove(m);
                        this.WhitesTurn ^= true;
                        continue;
                    }
                    this.Log(LogLevel.Warning, $"Invalid Move '{move}' in command '{c.Serialize()}'");
                }

                break;

            case Go c:
                this.HandleGo(c.Commands);
                break;

            case Stop:
                this.HandleStop();
                break;

            case Quit:
                Environment.Exit(0);
                break;

            case Unknown:
                this.Log(LogLevel.Warning, $"Received unknown command '{command.Serialize()}'");
                break;

            default:
                this.Log(LogLevel.Warning, $"Not handling command '{command.Serialize()}'");
                break;
        }
    }
}
