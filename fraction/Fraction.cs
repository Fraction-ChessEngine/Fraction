using System;
using String = System.String;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Fraction.UCI;
using UciEngine = Fraction.UCI.Engine;
using PositionCommand = Fraction.UCI.Position;

namespace fraction;
public class Fraction : UciEngine {
    private const string name = "fraction";
    private const string author = "ValleAkaJesus";

    private Search search = null!;
    private Search Search {
        get => this.search;
        set {
            this.search.Finished -= SearchFinishedHandler;
            this.search = value;
            this.search.Finished += SearchFinishedHandler;
        }
    }

    private CancellationTokenSource cts = new();
    private Task? heuristics = null;

    private Position pos = new(Position.Startpos);

    public Fraction() : base() {
        this.search = new Minimax();
        this.search.Finished += SearchFinishedHandler;
    }

    public bool Debug { get; private set; } = false;

    private void SearchFinishedHandler(Object? sender, SearchResult e) {
        if (this.heuristics is not null) {
            this.cts.Cancel();
            this.heuristics.GetAwaiter().GetResult();
            this.heuristics.Dispose();
            this.heuristics = null;
            this.cts.Dispose();
            this.cts = new();
        }

        Move result = e.BestMove;
        result = result with { Promotion = result.Promotion | (Piece)8 };
        this.Send(new BestMove(result.ToString()));
    }

    private async Task Heuristics(CancellationToken ct) {
        ulong lastNodes = 0;
        long lastTime = -1;
        while (!ct.IsCancellationRequested) {
            var sh = this.search.SearchHeuristics;
            ulong nps = ((sh.Nodes.Value - lastNodes) * 1000) / (ulong)(sh.Time.Value - lastTime);
            lastNodes = sh.Nodes.Value;
            lastTime = sh.Time.Value;
            this.Send(new Info($"depth {sh.Depth} time {sh.Time} nodes {sh.Nodes} currmove {sh.CurrMove} nps {nps}"));
            await Task.Delay(1000);
        }
    }

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
        this.Send(new ReadyOk());
    }

    private void HandleGo(IEnumerable<Go.ICommand> args) {
        if (search.IsRunning) {
            this.Log(LogLevel.Warning, "There is already a search running");
            return;
        }

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
                    if (a.Turn == (this.pos.WhitesTurn ? Color.White : Color.Black))
                        time = time ?? a.TimeLeft;
                    else etime = etime ?? a.TimeLeft;
                    break;
                default:
                    this.Log(LogLevel.Warning, $"Not Handling {arg.Serialize()}");
                    break;
            }
        }

        search.Start(new(this.pos, depth ?? -1, -1, -1, []));
        this.heuristics = this.Heuristics(cts.Token);

        if (!infinite && time is not null) {
            int x = time.Value;
            if (moves is not null) {
                x /= moves.Value;
            } else if (etime is not null) {
                x -= etime.Value;
            } else x = -1;
            if (x <= 0) x = 200;
            Task.Delay(x).ContinueWith(_ => this.search.Stop());
        }
    }

    private void HandleStop() {
        search.Stop();
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

            case PositionCommand c:
                if (search.IsRunning) {
                    this.Log(LogLevel.Warning, "wait until the search is complete");
                    break;
                }
                if (c.Fen is not null) {
                    if (FEN.TryParse(c.Fen, out FEN? fen)) {
                        this.pos = new(fen);
                    } else {
                        this.Log(LogLevel.Warning, $"fenparsing failed. Fen: {c.Fen}");
                        goto default;
                    }
                } else {
                    this.pos = new(Position.Startpos);
                }

                foreach (var move in c.moves) {
                    if (Move.TryParse(move, out Move m)) {
                        this.pos.MakeMove(m);
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
