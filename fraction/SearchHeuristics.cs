using System;
using System.Threading;

namespace fraction;

public class SearchHeuristics {
    public int? Depth { get; init; }
    public int? SelDepth { get; init; }
    public long? Time { get; init; }
    public ulong? Nodes { get; init; }
    public Move[]? PV { get; init; }
    public Score? Score { get; init; }
    public Move? CurrMove { get; init; }
    public int? CurrMoveNumber { get; init; }
    public int? HashFull { get; init; }
}
