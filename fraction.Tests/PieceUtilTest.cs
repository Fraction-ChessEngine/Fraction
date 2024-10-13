using Xunit;
using fraction;

namespace fraction.Test;
public class PieceUtilTest
{
    [Theory]
    [InlineData(Piece.wPawn)]
    [InlineData(Piece.wBishop)]
    [InlineData(Piece.wKnight)]
    [InlineData(Piece.wRook)]
    [InlineData(Piece.wKing)]
    [InlineData(Piece.wQueen)]
    public void IsWhite_WhitePices_ReturnTrue(Piece piece)
    {
        Assert.True(piece.IsWhite(), "White Pieces should be white");
    }

    [Theory]
    [InlineData(Piece.bPawn)]
    [InlineData(Piece.bBishop)]
    [InlineData(Piece.bKnight)]
    [InlineData(Piece.bRook)]
    [InlineData(Piece.bKing)]
    [InlineData(Piece.bQueen)]
    public void IsWhite_BlackPices_ReturnFalse(Piece piece)
    {
        Assert.False(piece.IsWhite(), "Black Pieces should not be white");
    }

    [Theory]
    [InlineData(Piece.wPawn, "P")]
    [InlineData(Piece.wBishop, "B")]
    [InlineData(Piece.wKnight, "N")]
    [InlineData(Piece.wRook, "R")]
    [InlineData(Piece.wKing, "K")]
    [InlineData(Piece.wQueen, "Q")]
    [InlineData(Piece.bPawn, "p")]
    [InlineData(Piece.bBishop, "b")]
    [InlineData(Piece.bKnight, "n")]
    [InlineData(Piece.bRook, "r")]
    [InlineData(Piece.bKing, "k")]
    [InlineData(Piece.bQueen, "q")]
    public void GetSymbol_Piece_ReturnsSymbol(Piece piece, string symbol)
    {
        Assert.Equal(symbol, piece.GetSymbol());
    }
}
