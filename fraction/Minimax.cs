using System;
using System.Timers;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;


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

        //only true, if i didnt find any legal moves and i am in check
        //It is my turn, i realise its mate, this is very bad
        if (pos.isCheckMate) return whitesTurn ? float.MinValue : float.MaxValue;

        //no legal moves, therefore draw, should be null values
        if (cbs.Length == 0) return 0;


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

    public static Move BestMove(Chessboard cb, bool whitesTurn, int depth) {
        Move currBestMove = new(-1, -1, Piece.wKing);
        float currBestEval = whitesTurn ? int.MinValue : int.MaxValue;

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


    /// <summary>
    /// Needs to be called AFTER BestMoveTime else gay
    /// </summary>
    public Move GetBestMove => bestMove;

    private Move bestMove;
    private CancellationTokenSource tokenSource;

    //todo: move ordering
    //basic idea: store child positions and their eval at current depth and sort them by eval
    private void BestMoveTime_(Chessboard cb, bool whitesTurn, int ms) {
        //use bestMove(depth) to get the best move with the given depth
        //search 1 depth deeper as soon as search is completed until time runs out

        int currDepth = 1;

        System.Timers.Timer aTimer = new();
        aTimer.Elapsed += new ElapsedEventHandler(cancel_Click);
        aTimer.Interval = ms;
        aTimer.Enabled = true;

        while (true) {
            bestMove = BestMove(cb, whitesTurn, currDepth);
            currDepth++;
        }
    }

    //very basic iterative deepening
    public void BestMoveTime(Chessboard cb, bool whitesTurn, int ms) {
        tokenSource = new CancellationTokenSource();
        Task.Factory.StartNew(() => BestMoveTime_(cb, whitesTurn, ms), tokenSource.Token);

        Thread.Sleep(ms);
    }

    private void cancel_Click(object sender, ElapsedEventArgs e) {
        tokenSource.Cancel();
    }
}


