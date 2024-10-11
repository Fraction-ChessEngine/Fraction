using System;
using System.Collections.Generic;

namespace fraction
{
    static class Minimax
    {
        public static int positions = 0, nonQuietEndNodes = 0;

        public static float miniMax(Chessboard pos, int depth, float alpha, float beta, bool whitesTurn)
        {
            //checkmate detection
            float staticEval = Eval.basicStaticEval(pos);
            if (Math.Abs(staticEval) > 9000)
            {
                return staticEval;
            }

            //quiescence search, 3 als hard limit für depth increase
            if (pos.afterCapturePly && pos.quiescenceSearchPlies < 3)
            {
                nonQuietEndNodes++;
                pos.quiescenceSearchPlies++;
                depth++;
            }
/* ayo das hier ist ein commit test, wenn du ihn siehst entfern ihn */

            if (depth == 0)
            {
                positions++;
                return staticEval;
            }


            Chessboard[] cbs = MoveGen.generateBoards(pos, whitesTurn);
            if (cbs.Length == 0) return staticEval;

            if (whitesTurn)
            {
                float maxEval = float.MinValue;
                foreach (Chessboard c in cbs)
                {
                    float eval = miniMax(c, depth - 1, alpha, beta, false);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);

                    if (beta <= alpha) break;
                }
                return maxEval;
            }
            else
            {
                float minEval = float.MaxValue;
                foreach (Chessboard c in cbs)
                {
                    float eval = miniMax(c, depth - 1, alpha, beta, true);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);

                    if (beta <= alpha) break;
                }
                return minEval;
            }
        }
    }
}
