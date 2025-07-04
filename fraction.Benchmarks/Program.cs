using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

using fraction;

namespace fraction.Benchmarks;

public record TestPosition(string Name, Position Pos) {
    public override string ToString() => this.Name;
}

[SimpleJob(RunStrategy.Throughput)]
public class PerftBenchmark {
    [Params(4)]
    public int Depth { get; set; }

    [ParamsSource(nameof(Positions))]
    public TestPosition Testpos { get; set; }

    private Position Pos => Testpos.Pos;

    public IEnumerable<TestPosition> Positions =>
        new (string name, string fen)[]{
            ("startpos", "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"),
            ("rn2k2r", "rn2k2r/1pppppbp/2b1q3/p3P3/2P3p1/3B2P1/PPBP1PNP/R2QK2R w KQkq - 0 1"),
            ("queens", "1k6/n1q1P1q1/7q/3q4/8/2Q2Q2/1K6/R3RQ1N w - - 0 1"),
            ("1r1qk2r", "1r1qk2r/p3p2p/2npbppb/2N1n3/3NP3/P2P2P1/1PPBQPB1/2KR3R w Kk - 0 1"),
            ("pawns", "5k2/pp4pp/3bpp2/1P6/8/P2KP3/5PPP/2B5 b - - 0 1"),
            ("r2qkdb1r", "r2qkb1r/1p1bnppp/2npp3/pN6/3NP1P1/3BB3/PPPQ1P1P/R3K2R b KQkq - 1 10")
        }.Select(x => {
            if (FEN.TryParse(x.fen, out var fen)) {
                return new TestPosition(x.name, new(fen));
            }
            throw new ArgumentException();
        });

    [Benchmark]
    public ulong Normal() => Search.Perft(Pos, Depth);

    [Benchmark]
    public ulong WithTranspos() => perftWithtranspos(new(), Pos, Depth);

    private ulong perftWithtranspos(Transpostable tp, Position pos, int depth) {
        if (depth <= 0) return 1;
        if (tp.ContainsKey(pos)) return 1;

        MoveGen moveGen = new(pos);
        Span<Move> moves = moveGen.GenerateMoves();

        ulong sum = 0;
        Position next = new(pos);

        foreach (var move in moves) {
            next.Copy(pos);
            next.MakeMove(move);
            tp.Add(new(next), new());
            sum += perftWithtranspos(tp, next, depth - 1);
        }
        return sum;
    }
}

public class Program {
    public static void Main() {
        var summary = BenchmarkRunner.Run<PerftBenchmark>();
    }
}
