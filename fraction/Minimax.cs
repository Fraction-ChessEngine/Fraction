using System;
using System.Diagnostics;

namespace fraction;
public sealed class Minimax : Search {
    private object lck = new();
    private ulong nodes = 0;
    private int depth = 0;
    private Move currMove = Move.Null;
    private Stopwatch sw = new();

    public bool AlphaBetaPruning { get; init; } = true;
    public int MaxQuiescenceSearchPlies { get; init; } = 3;
    public int NonQuietEndNodes { get; private set; } = 0;

    public override SearchHeuristics SearchHeuristics {
        get {
            ulong nodes;
            int depth;
            long time;
            Move move;
            lock (this.lck) {
                nodes = this.nodes;
                depth = this.depth;
                time = sw.ElapsedMilliseconds;
                move = this.currMove;
            }
            return new SearchHeuristics { Nodes = nodes, Depth = depth, Time = time, CurrMove = currMove };
        }
    }
    public Minimax() { }

    protected override void Reset() {
        this.nodes = 0;
        this.depth = 0;
        this.currMove = Move.Null;
        this.sw.Reset();
    }

    public Score Run(Position pos, int depth, bool afterCapture)
        => Run(pos, depth, Score.MinValue, Score.MaxValue, afterCapture, 0);

    private Score Run(
        Position pos,
        int depth,
        Score alpha,
        Score beta,
        bool afterCapture,
        int quiescenceSearchPlies
    ) {

        this.CancellationToken.ThrowIfCancellationRequested();

        //quiescence search, 3 as hard limit for depth increase
        if (afterCapture && quiescenceSearchPlies < MaxQuiescenceSearchPlies) {
            NonQuietEndNodes++;
            quiescenceSearchPlies++;
            depth++;
        }

        Score staticEval = Eval.BasicStaticEval(pos.Board);

        if (depth == 0) {
            lock (lck) {
                this.nodes++;
            }
            return staticEval;
        }

        MoveGen moveGen = new(pos);
        Span<Move> moves = moveGen.GenerateMoves();

        //only true, if i didnt find any legal moves and i am in check
        //It is my turn, i realise its mate, this is very bad
        if (moveGen.IsCheckMate) return new Score(ScoreType.Mate, 0);

        //no legal moves, therefore draw, should be null values
        if (moves.Length == 0) return new Score(ScoreType.Centipawns, 0);

        //fifty move rule is enforced
        if (pos.FiftyMovePlys >= 50) return new Score(ScoreType.Centipawns, 0);


        if (pos.WhitesTurn) {
            Score maxEval = Score.MinValue;
            foreach (var move in moves) {
                Position copy = new(pos);
                bool isCapture = copy.MakeMove(move);
                Score eval = Run(copy, depth - 1, alpha, beta, isCapture, quiescenceSearchPlies);

                if (eval.IsMate)
                    eval = eval.Value >= 0 ?
                        eval with { Value = eval.Value + 1 } :
                        eval with { Value = eval.Value - 1 };

                maxEval = Score.Max(maxEval, eval);

                if (AlphaBetaPruning) {
                    alpha = Score.Max(alpha, eval);

                    if (!(beta > alpha)) break;
                }
            }
            return maxEval;
        } else {
            Score minEval = Score.MaxValue;
            foreach (var move in moves) {
                Position copy = new(pos);
                bool isCapture = copy.MakeMove(move);
                Score eval = Run(copy, depth - 1, alpha, beta, isCapture, quiescenceSearchPlies);

                if (eval.IsMate)
                    eval = eval.Value <= 0 ?
                        eval with { Value = eval.Value - 1 } :
                        eval with { Value = eval.Value + 1 };

                minEval = Score.Min(minEval, eval);

                if (AlphaBetaPruning) {
                    beta = Score.Min(beta, eval);

                    if (!(beta > alpha)) break;
                }
            }
            return minEval;
        }
    }

    public Move BestMove(Position pos, int depth) {
        Move currBestMove = Move.Null;
        Score currBestEval = pos.WhitesTurn ? Score.MinValue : Score.MaxValue;

        Span<Move> children = (new MoveGen(pos)).GenerateMoves();

        foreach (Move currMove in children) {
            lock (this.lck) {
                this.currMove = currMove;
            }
            Position nextPos = new(pos);
            bool isCapture = nextPos.MakeMove(currMove);
            Score eval = this.Run(nextPos, depth - 1, isCapture);

            if (pos.WhitesTurn) {//we want to maximize eval
                if (eval > currBestEval) {
                    currBestEval = eval;
                    currBestMove = currMove;
                }
            } else {//we want to minimize eval
                if (eval < currBestEval) {
                    currBestEval = eval;
                    currBestMove = currMove;
                }
            }
        }

        return currBestMove;
    }

    protected override void Run(SearchArgs args) {
        lock (this.lck) { this.sw.Start(); }
        Move bestMove = Move.Null;

        for (
                int i = 1;
                !this.CancellationToken.IsCancellationRequested &&
                (args.Depth == -1 || i < args.Depth);
                i++) {

            lock (this.lck) {
                depth = i;
            }

            try {
                bestMove = this.BestMove(args.pos, i);
            } catch (OperationCanceledException) { }
        }

        lock (this.lck) { this.sw.Stop(); }
        this.OnFinished(new(bestMove));
    }
}

