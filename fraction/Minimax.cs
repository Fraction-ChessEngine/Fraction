using System;
using System.Threading;
using System.Threading.Tasks;


namespace fraction;
public sealed class Minimax {
    public bool AlphaBetaPruning { get; init; } = true;
    public int MaxQuiescenceSearchPlies { get; init; } = 3;
    public int Positions { get; private set; } = 0;
    public int NonQuietEndNodes { get; private set; } = 0;
    public CancellationToken CancellationToken { get; set; }

    public Minimax() { }
    public Minimax(CancellationToken ct) {
        CancellationToken = ct;
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

        CancellationToken.ThrowIfCancellationRequested();

        //quiescence search, 3 as hard limit for depth increase
        if (afterCapture && quiescenceSearchPlies < MaxQuiescenceSearchPlies) {
            NonQuietEndNodes++;
            quiescenceSearchPlies++;
            depth++;
        }

        Score staticEval = Eval.BasicStaticEval(pos.Board);

        if (depth == 0) {
            Positions++;
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

    public static Move BestMove(Position pos, int depth, CancellationToken cancellationToken = new()) {
        Move currBestMove = Move.Null;
        Score currBestEval = pos.WhitesTurn ? Score.MinValue : Score.MaxValue;

        Span<Move> children = (new MoveGen(pos)).GenerateMoves();

        foreach (Move currMove in children) {
            Position nextPos = new(pos);
            bool isCapture = nextPos.MakeMove(currMove);
            Minimax m = new(cancellationToken);
            Score eval = m.Run(nextPos, depth - 1, isCapture);

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

    public static Task<Move> BestMoveAsync(Position pos, int depth, CancellationToken ct) {
        return Task<Move>.Run(() => {
            Move bestMove = Move.Null;

            for (int i = 1; !ct.IsCancellationRequested && (depth == -1 || i < depth); i++) {
                try {
                    bestMove = BestMove(pos, i, ct);
                } catch (OperationCanceledException) { }
            }
            return bestMove;
        }, ct);
    }
}

