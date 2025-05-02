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
        Assert.Equal(expectedSum, perftSum(new(), depth, true));
    }

    public static long perftSum(Chessboard board, int depth, bool whitesTurn) {
        Span<Chessboard> boards = (new MoveGen(board, whitesTurn)).GenerateBoards();
        long sum = 0;
        foreach (Chessboard cb in boards) {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            _ = minimax.Run(cb, depth - 1, !whitesTurn);
            sum += (long)minimax.Positions;
        }
        return sum;
    }

    [Theory]
    [ClassData(typeof(Ethereal))]
    public void ethereal(string fen, int depth, long expectedSum) {
        Assert.True(FEN.TryParse(fen, out var f));
        Assert.Equal(expectedSum, perftSum(new(f), depth, f.WhitesTurn));
    }
}
