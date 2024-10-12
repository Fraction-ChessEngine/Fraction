using System;

namespace fraction;
sealed class Minimax
{
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
    )
    {
        //checkmate detection
        float staticEval = Eval.BasicStaticEval(pos);

        if (Math.Abs(staticEval) > 9000)
        {
            return staticEval;
        }

        //quiescence search, 3 als hard limit für depth increase
        if (pos.afterCapturePly && quiescenceSearchPlies < MaxQuiescenceSearchPlies)
        {
            NonQuietEndNodes++;
            quiescenceSearchPlies++;
            depth++;
        }

        if (depth == 0)
        {
            Positions++;
            return staticEval;
        }

        Chessboard[] cbs = MoveGen.GenerateBoards(pos, whitesTurn);

        if (cbs.Length == 0)
            return staticEval;

        if (whitesTurn)
        {
            float maxEval = float.MinValue;
            foreach (Chessboard c in cbs)
            {
                float eval = Run(c, depth - 1, alpha, beta, false, quiescenceSearchPlies);
                maxEval = Math.Max(maxEval, eval);

                if (AlphaBetaPruning)
                {
                    alpha = Math.Max(alpha, eval);

                    if (beta <= alpha) break;
                }
            }
            return maxEval;
        }
        else
        {
            float minEval = float.MaxValue;
            foreach (Chessboard c in cbs)
            {
                float eval = Run(c, depth - 1, alpha, beta, true, quiescenceSearchPlies);
                minEval = Math.Min(minEval, eval);

                if (AlphaBetaPruning)
                {
                    beta = Math.Min(beta, eval);

                    if (beta <= alpha) break;
                }
            }
            return minEval;
        }
    }
}

