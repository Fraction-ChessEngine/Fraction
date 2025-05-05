using System;

namespace fraction;

public class SearchResult : EventArgs {
    public Move BestMove { get; init; }

    public SearchResult(Move bestMove) {
        this.BestMove = bestMove;
    }
}
