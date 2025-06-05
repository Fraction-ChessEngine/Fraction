using System.Linq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Engines;

using fraction;

namespace fraction.Benchmarks;

[SimpleJob(RunStrategy.Throughput)]
public class PerftBenchmark {
    [Params(4)]
    public int Depth { get; set; }
    [ParamsSource(nameof(Positions))]
    public Position Pos { get; set; }

    public IEnumerable<Position> Positions =>
        new string[]{
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            "rn2k2r/1pppppbp/2b1q3/p3P3/2P3p1/3B2P1/PPBP1PNP/R2QK2R w KQkq - 0 1",
            "1k6/n1q1P1q1/7q/3q4/8/2Q2Q2/1K6/R3RQ1N w - - 0 1",
            "1r1qk2r/p3p2p/2npbppb/2N1n3/3NP3/P2P2P1/1PPBQPB1/2KR3R w Kk - 0 1",
            "5k2/pp4pp/3bpp2/1P6/8/P2KP3/5PPP/2B5 b - - 0 1",
            "r2qkb1r/1p1bnppp/2npp3/pN6/3NP1P1/3BB3/PPPQ1P1P/R3K2R b KQkq - 1 10"
        }.Select(x => {
            if (FEN.TryParse(x, out var fen)) {
                return new Position(fen) { Name = x };
            }
            throw new ArgumentException();
        });

    [Benchmark]
    public ulong Normal() => Search.Perft(Pos, Depth);
}

public class Program {
    public static void Main() {
        var summary = BenchmarkRunner.Run<PerftBenchmark>();
    }
}
