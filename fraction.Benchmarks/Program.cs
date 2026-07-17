using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using fraction;

namespace fractionBenchmarks;

public class MiniMaxBenchmark {
    private const int maxQSP = 4;

    [Params(0, 1, 2, 3, 4)]
    public int Depth { get; set; }

    private readonly Chessboard[] boards = MoveGen.GenerateBoards(new(), true).ToArray();
    private Minimax normal = new() {
        AlphaBetaPruning = false,
        MaxQuiescenceSearchPlies = 0,
        perft=true
    };

    private Minimax alphaBetaPruning = new() {
        AlphaBetaPruning = true,
        MaxQuiescenceSearchPlies = 0,
                perft=true

    };

    private Minimax qsp = new() {
        AlphaBetaPruning = false,
        MaxQuiescenceSearchPlies = maxQSP,
                perft=true

    };

    private Minimax both = new() {
        AlphaBetaPruning = true,
        MaxQuiescenceSearchPlies = maxQSP,
                perft=true

    };

    private float Perft(Minimax m) {
        float sum = 0;

        foreach (var board in boards) {
            sum += m.Run(board, Depth - 1, false);
        }

        return sum;
    }

    [Benchmark(Baseline = true)]
    public float Normal() => Perft(normal);
    [Benchmark]
    public float AlphaBetaPruning() => Perft(alphaBetaPruning);
    [Benchmark]
    public float QuiescenceSearchPlies() => Perft(qsp);
    [Benchmark]
    public float Both() => Perft(both);
}

public class Program {
    public static void Main() {
        var summary = BenchmarkRunner.Run<MiniMaxBenchmark>();
    }
}
