using System;
using System.Diagnostics;

using static System.Threading.Interlocked;

namespace fraction;
public sealed class Minimax : Search {
    private object lck = new();
    private long nodes = 0;
    private long depth = 0;
    private long starttime = -1;
    private Move currMove = Move.Null;

    public override long Nodes => Read(ref this.nodes);
    public override long Depth => Read(ref this.depth);
    public override long Time => (Stopwatch.GetTimestamp() - this.starttime) * 1000 / Stopwatch.Frequency;

    public bool AlphaBetaPruning { get; init; } = true;
    public int MaxQuiescenceSearchPlies { get; init; } = 3;
    public int NonQuietEndNodes { get; private set; } = 0;

    public Minimax() { }

    protected override void Reset() {
        this.nodes = 0;
        this.depth = 0;
        this.currMove = Move.Null;
        this.starttime = -1;
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
            Increment(ref this.nodes);
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
            this.currMove = currMove;
            this.OnNewHeuristics(new() { Time = this.Time, Depth = (int)this.depth, Nodes = this.nodes, CurrMove = currMove });
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
        this.OnNewHeuristics(new() { Time = this.Time, Depth = (int)this.depth, Nodes = this.nodes, Score = currBestEval });

        return currBestMove;
    }

    protected override void Run(SearchArgs args) {
        this.starttime = Stopwatch.GetTimestamp();
        Move bestMove = Move.Null;

        for (
                int i = 1;
                !this.CancellationToken.IsCancellationRequested &&
                (args.Depth == -1 || i < args.Depth);
                i++) {

            Exchange(ref this.depth, i);
            this.OnNewHeuristics(new() { Time = this.Time, Depth = (int)this.depth, Nodes = this.nodes });

            try {
                bestMove = this.BestMove(args.pos, i);
            } catch (OperationCanceledException) { }
        }

        this.OnFinished(new(bestMove));
    }
}

