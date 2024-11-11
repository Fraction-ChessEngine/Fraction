using System;

namespace fraction;
public sealed class Minimax {
    public bool AlphaBetaPruning { get; init; } = true;
    public int MaxQuiescenceSearchPlies { get; init; } = 3;
    public int Positions { get; private set; } = 0;
    public int NonQuietEndNodes { get; private set; } = 0;

    public Minimax() { }

    public float Run(Chessboard pos, int depth, bool whitesTurn)
        => Run(pos, depth, float.MinValue, float.MaxValue, whitesTurn, 0);

    private float Run(
        Chessboard pos,
        int depth,
        float alpha,
        float beta,
        bool whitesTurn,
        int quiescenceSearchPlies
    ) {

        //prone for error, might be inverse, weird logical things are happening here
        if (pos.isCheckMate) return whitesTurn ? float.MaxValue : float.MinValue;

        //quiescence search, 3 as hard limit for depth increase
        if (pos.AfterCapturePly && quiescenceSearchPlies < MaxQuiescenceSearchPlies) {
            NonQuietEndNodes++;
            quiescenceSearchPlies++;
            depth++;
        }

        float staticEval = Eval.BasicStaticEval(pos);

        if (depth == 0) {
            Positions++;
            return staticEval;
        }

        Span<Chessboard> cbs = MoveGen.GenerateBoards(pos, whitesTurn);

        if (cbs.Length == 0) return staticEval;


        if (whitesTurn) {
            float maxEval = float.MinValue;
            foreach (Chessboard c in cbs) {
                float eval = Run(c, depth - 1, alpha, beta, false, quiescenceSearchPlies);
                maxEval = Math.Max(maxEval, eval);

                if (AlphaBetaPruning) {
                    alpha = Math.Max(alpha, eval);

                    if (beta <= alpha) break;
                }
            }
            return maxEval;
        } else {
            float minEval = float.MaxValue;
            foreach (Chessboard c in cbs) {
                float eval = Run(c, depth - 1, alpha, beta, true, quiescenceSearchPlies);
                minEval = Math.Min(minEval, eval);

                if (AlphaBetaPruning) {
                    beta = Math.Min(beta, eval);

                    if (beta <= alpha) break;
                }
            }
            return minEval;
        }
    }

    public static (int, int) BestMove(Chessboard cb, bool whitesTurn, int depth) {
        (int, int) currBestMove = (-1, -1);
        float currBestEval = whitesTurn ? -10_000 : 10_000;

        Span<Chessboard> children = MoveGen.GenerateBoards(cb, whitesTurn);

        foreach (Chessboard currCB in children) {
            Minimax m = new();
            float eval = m.Run(currCB, depth - 1, !whitesTurn);

            if (whitesTurn) {//we want to maximize eval
                if (eval > currBestEval) {
                    currBestEval = eval;
                    currBestMove = currCB.lastMove;
                }
            } else {//we want to minimize eval
                if (eval < currBestEval) {
                    currBestEval = eval;
                    currBestMove = currCB.lastMove;
                }
            }
        }

        return currBestMove;
    }

}

