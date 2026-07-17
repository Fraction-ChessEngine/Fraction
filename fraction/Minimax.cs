using System;
using System.Collections;
using System.Collections.Generic;
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
    public bool perft=false;

    // HashSet<Chessboard>

    public Minimax() { }
    public Minimax(CancellationToken ct) {
        CancellationToken = ct;
    }

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

        CancellationToken.ThrowIfCancellationRequested();

        

        bool isCheck=!pos.getCheckPiecesEmpty();

        //quiescence search, 3 as hard limit for depth increase
        if ((isCheck || (pos.AfterCapturePly && quiescenceSearchPlies < MaxQuiescenceSearchPlies))&&!perft) {
            NonQuietEndNodes++;
            quiescenceSearchPlies++;
            depth++;
        }

        if (depth == 0) {
            Positions++;
            float staticEval = Eval.BasicStaticEval(pos);
            return staticEval;
        }

        //KEIN moveBuffers[depth]: die quiescence extension oben macht depth++, dh ein kind
        //kann den gleichen index kriegen wie sein parent, der seine moves noch iteriert.
        //braucht einen echten ply-zaehler bevor das hier einen shared buffer benutzen darf
        Span<Move> moves = MoveGen.GenerateMoves(pos, whitesTurn, new Move[256]);
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
                copy.MakeMove(move);
                float eval = Run(copy, depth - 1, alpha, beta, false, quiescenceSearchPlies);
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
                copy.MakeMove(move);
                float eval = Run(copy, depth - 1, alpha, beta, true, quiescenceSearchPlies);
                minEval = Math.Min(minEval, eval);

                if (AlphaBetaPruning) {
                    beta = Math.Min(beta, eval);

                    if (beta <= alpha) break;
                }
            }
            return minEval;
        }
    }

    //one row per recursion depth, weil der parent seine moves noch iteriert waehrend das kind generiert
    public readonly Move[][] moveBuffers = CreateMoveBuffers();
    public static readonly Chessboard[] boardBuffer=new Chessboard[64];

    //218 ist das maximum an legalen moves in einer legalen stellung, 256 als puffer
    private static Move[][] CreateMoveBuffers() {
        var buffers = new Move[64][];
        for (int i = 0; i < buffers.Length; i++) buffers[i] = new Move[256];
        return buffers;
    }
    public float runPerft(
        Chessboard pos,
        int depth,
        bool whitesTurn) {
          //  CancellationToken.ThrowIfCancellationRequested();

        

        if (depth <= 0) {
            Positions++;
            return 0;
        }
        
        Span<Move> moves = MoveGen.GenerateMoves(pos, whitesTurn,moveBuffers[depth]);

        if (pos.IsCheckMate) return whitesTurn ? int.MinValue : int.MaxValue;
        if (moves.Length == 0) return 0;
        if (pos.FiftyMovePlys >= 50) return 0;

       Chessboard copy=boardBuffer[depth];

        if (whitesTurn) {
            float maxEval = float.MinValue;
            foreach (var move in moves) {
                copy.Copy(pos);
                copy.MakeMove(move);
                float eval = runPerft(copy, depth - 1, false);
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        } else {
            float minEval = float.MaxValue;
            foreach (var move in moves) {
                copy.Copy(pos);
                copy.MakeMove(move);
                float eval = runPerft(copy, depth - 1, true);
                minEval = Math.Min(minEval, eval);
            }
            return minEval;
        }
        
    }

    public static Move BestMove(Chessboard cb, bool whitesTurn, int depth, CancellationToken cancellationToken = new()) {
        Move currBestMove = Move.Null;
        float currBestEval = whitesTurn ? float.MinValue : float.MaxValue;

        Chessboard[] children = MoveGen.GenerateBoards(cb, whitesTurn);

        foreach (Chessboard currCB in children) {
            Minimax m = new(cancellationToken);
            float eval = m.Run(currCB, depth - 1, !whitesTurn);

            if (whitesTurn) {//we want to maximize eval
                if (eval > currBestEval) {
                    currBestEval = eval;
                    currBestMove = currCB.LastMove;
                }
            } else {//we want to minimize eval
                if (eval < currBestEval) {
                    currBestEval = eval;
                    currBestMove = currCB.LastMove;
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

