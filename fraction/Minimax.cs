using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;


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

    public float Run(Chessboard pos, int depth, bool whitesTurn, bool afterCapture)
        => Run(pos, depth, float.MinValue, float.MaxValue, whitesTurn, afterCapture, 0);

    private float Run(
        Chessboard pos,
        int depth,
        float alpha,
        float beta,
        bool whitesTurn,
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

        float staticEval = Eval.BasicStaticEval(pos);

        if (depth == 0) {
            Positions++;
            return staticEval;
        }

        Span<Move> moves = (new MoveGen(pos, whitesTurn)).GenerateMoves();

        //only true, if i didnt find any legal moves and i am in check
        //It is my turn, i realise its mate, this is very bad
        if (pos.IsCheckMate) return whitesTurn ? int.MinValue : int.MaxValue;

        //no legal moves, therefore draw, should be null values
        if (moves.Length == 0) return 0;

        //fifty move rule is enforced
        if (pos.FiftyMovePlys >= 50) return 0;

        Chessboard copy = pos.Clone();

        if (whitesTurn) {
            float maxEval = float.MinValue;
            foreach (var move in moves) {
                copy.Copy(pos);
                bool isCapture = copy.MakeMove(move);
                float eval = Run(copy, depth - 1, alpha, beta, false, isCapture, quiescenceSearchPlies);
                maxEval = Math.Max(maxEval, eval);

                if (AlphaBetaPruning) {
                    alpha = Math.Max(alpha, eval);

                    if (beta <= alpha) break;
                }
            }
            return maxEval;
        } else {
            float minEval = float.MaxValue;
            foreach (var move in moves) {
                copy.Copy(pos);
                bool isCapture = copy.MakeMove(move);
                float eval = Run(copy, depth - 1, alpha, beta, true, isCapture, quiescenceSearchPlies);
                minEval = Math.Min(minEval, eval);

                if (AlphaBetaPruning) {
                    beta = Math.Min(beta, eval);

                    if (beta <= alpha) break;
                }
            }
            return minEval;
        }
    }

    public static Move BestMove(Chessboard cb, bool whitesTurn, int depth, CancellationToken cancellationToken = new()) {
        Move currBestMove = Move.Null;
        float currBestEval = whitesTurn ? float.MinValue : float.MaxValue;

        Span<Move> children = (new MoveGen(cb, whitesTurn)).GenerateMoves();

        foreach (Move currMove in children) {
            Chessboard nextPos = cb.Clone();
            bool isCapture = nextPos.MakeMove(currMove);
            Minimax m = new(cancellationToken);
            float eval = m.Run(nextPos, depth - 1, !whitesTurn, isCapture);

            if (whitesTurn) {//we want to maximize eval
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

    public static Task<Move> BestMoveAsync(Chessboard cb, bool whitesTurn, int depth, CancellationToken ct) {
        return Task<Move>.Run(() => {
            Move bestMove = Move.Null;

            for (int i = 1; !ct.IsCancellationRequested && (depth == -1 || i < depth); i++) {
                try {
                    bestMove = BestMove(cb, whitesTurn, i, ct);
                } catch (OperationCanceledException) { }
            }
            return bestMove;
        }, ct);
    }
}

