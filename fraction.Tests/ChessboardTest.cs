using fraction;
namespace fraction.Test;

public class ChessboardTest {
    [Fact]
    void Clone_ReturnsDeepCopy() {
        Chessboard a = new(Chessboard.Startpos);
        a.BRookBB = 0xff_ff_ff_ff_ff_ff_ff_fful;
        Assert.True(a.BRookBB[1]);
        Chessboard b = new(a);
        b.BRookBB = 0ul;
        Assert.False(b.BRookBB[1]);
    }

    [Theory]
    [InlineData(Piece.wPawn)]
    [InlineData(Piece.wBishop)]
    [InlineData(Piece.wRook)]
    [InlineData(Piece.wKnight)]
    [InlineData(Piece.wKing)]
    [InlineData(Piece.wQueen)]
    [InlineData(Piece.bPawn)]
    [InlineData(Piece.bBishop)]
    [InlineData(Piece.bRook)]
    [InlineData(Piece.bKnight)]
    [InlineData(Piece.bKing)]
    [InlineData(Piece.bQueen)]
    void Indexer_Works(Piece type) {
        Chessboard a = new(Chessboard.Startpos);
        a[type] = 0ul;
        Assert.Equal(0ul, (ulong)a[type]);
        a[type] = 1ul;
        Assert.Equal(1ul, (ulong)a[type]);
    }
}
