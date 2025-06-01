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
        Assert.Equal(expectedSum, Search.Perft(new(Position.Startpos), depth));
    }

    [Theory]
    [ClassData(typeof(Ethereal))]
    public void ethereal(string fen, int depth, ulong expectedSum) {
        Assert.True(FEN.TryParse(fen, out var f));
        Assert.Equal(expectedSum, Search.Perft(new(f), depth));
    }
}
