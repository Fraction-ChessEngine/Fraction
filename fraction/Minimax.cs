using System;
using System.Timers;
using System;
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

    public static (int, int, Piece) BestMove(Chessboard cb, bool whitesTurn, int depth) {
        (int, int, Piece) currBestMove = (-1, -1, Piece.wKing);
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


    public static (int, int, Piece) bestFound = (-1, -1, Piece.wKing);
    public static (int, int, Piece) BestMoveTime(Chessboard cb, bool whitesTurn, float ms) {
        //use bestMove(depth) to get the best move with the given depth
        //search 1 depth deeper as soon as search is completed until time runs out

        int currDepth = 1;

        try {
            System.Timers.Timer aTimer = new();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = ms;
            aTimer.Enabled = true;

            while (true) {
                bestFound = BestMove(cb, whitesTurn, currDepth);
                Console.WriteLine("debug: completed depth " + currDepth);
                currDepth++;
            }

        } catch (System.Exception) {
            //damit der compiler ruhe gibt, hier wird nichts returnt weil der process davor abgetötet wird
            return bestFound;
        }
    }


    private static void OnTimedEvent(object source, ElapsedEventArgs e) {
        Process.GetCurrentProcess().Kill();
        throw new Exception();
    }

}


