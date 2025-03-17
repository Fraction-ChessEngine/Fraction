using System;
using String = System.String;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using fraction.UCI;
using UciEngine = fraction.UCI.Engine;

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
        if (bestMove is null) {
            if (!cts.TryReset()) {
                cts.Dispose();
                cts = new();
            }
        }
        this.Send(new ReadyOk());
    }

    private void HandleGo() {
        if (bestMove is null) {
            bestMove = Minimax.BestMoveAsync(this.board, this.WhitesTurn, -1, cts.Token)
                .ContinueWith((t) => {
                    Move result = t.GetAwaiter().GetResult();
                    this.bestMove = null;
                    this.Send(new BestMove(result.ToString()));
                });
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

            case UCI.Debug c:
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
                    if (FEN.TryParse(c.Fen, out FEN fen)) {
                        this.board = new(fen);
                        this.WhitesTurn = fen.WhitesTurn;
                    } else goto default;
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
                if (c.Commands.Length > 1) goto default;
                if (c.Commands.Length > 0 && c.Commands[0] is not Infinite) goto default;

                this.HandleGo();
                break;

            case Stop:
                this.HandleStop();
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
