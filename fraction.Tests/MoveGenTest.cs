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
    public void perft(int depth, ulong expectedSum) {
        Span<Chessboard> boards = MoveGen.GenerateBoards(new(), true, true);
        ulong sum = 0;
        foreach (Chessboard board in boards) {
            Minimax minimax = new() { MaxQuiescenceSearchPlies = 0, AlphaBetaPruning = false };
            _ = minimax.Run(board, depth - 1, false);
            sum += (ulong)minimax.Positions;
        }

        Assert.Equal(expectedSum, sum);
    }

    [Theory]
    [ClassData(typeof(Ethereal))]
    public void ethereal(string fen, int depth, long expectedSum) {
        (var board, var whitesTurn) = Chessboard.FromFEN(fen);
        Assert.Equal(expectedSum, Testing.perftSum(board, depth, whitesTurn));
    }
}
