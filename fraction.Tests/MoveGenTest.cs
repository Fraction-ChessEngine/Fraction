using fraction;
namespace fraction.Tests;
public class MoveGenTest {
    [Theory]
    [InlineData(1, 20ul)]
    [InlineData(2, 400ul)]
    [InlineData(3, 8902ul)]
    [InlineData(4, 197281ul)]
    [InlineData(5, 4865609ul)]
    [InlineData(6, 119060324ul)]
    //[InlineData(7, 3195901860ul)]
    public void perft(int depth, long expectedSum) {
        Assert.Equal(expectedSum, perftSum(new(Position.Startpos), depth));
    }

    public static long perftSum(Position pos, int depth) {
        Span<Move> moves = (new MoveGen(pos)).GenerateMoves();
        long sum = 0;
        foreach (Move m in moves) {
            Position b = new(pos);
            var isCapture = b.MakeMove(m);
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            _ = minimax.Run(b, depth - 1, isCapture);
            sum += (long)minimax.Nodes;
        }
        return sum;
    }

    [Theory]
    [ClassData(typeof(Ethereal))]
    public void ethereal(string fen, int depth, long expectedSum) {
        Assert.True(FEN.TryParse(fen, out var f));
        Assert.Equal(expectedSum, perftSum(new(f), depth));
    }
}
