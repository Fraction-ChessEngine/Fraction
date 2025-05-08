using System;
using System.Threading;

namespace fraction;

public class Heuristics : EventArgs {
    public int? Depth { get; init; }
    public int? SelDepth { get; init; }
    public long? Time { get; init; }
    public long? Nodes { get; init; }
    public Move[]? PV { get; init; }
    public int? MultiPV { get; init; }
    public Score? Score { get; init; }
    public Move? CurrMove { get; init; }
    public int? CurrMoveNumber { get; init; }
    public int? HashFull { get; init; }
    public int? TBHits { get; init; }
    public Move[]? CurrLine { get; init; }
    public int? ThreadID { get; init; }
}
