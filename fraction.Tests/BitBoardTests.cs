using fraction;
namespace fraction.Test;

public class BitBoardTests {
    [Fact]
    public void Konstructor_Chessboard_Returns0xaa55aa55aa55aa55() {
        BitBoard bb = new(
            0b01010101,
            0b10101010,
            0b01010101,
            0b10101010,
            0b01010101,
            0b10101010,
            0b01010101,
            0b10101010
        );

        Assert.Equal(0xaa55aa55aa55aa55ul, (ulong)bb);
    }

    [Fact]
    public void Konstructor_BigChessboard_Returns0xf00ff00ff00ff00f() {
        BitBoard bb = new(
            0b00001111,
            0b11110000,
            0b00001111,
            0b11110000,
            0b00001111,
            0b11110000,
            0b00001111,
            0b11110000
        );

        Assert.Equal(0xf00ff00ff00ff00f, (ulong)bb);
    }

    [Theory]
    [InlineData(0xffff_ffff_ffff_fffful, 64)]
    [InlineData(0x0ul, 0)]
    [InlineData(0xaaaa_aaaa_aaaa_aaaaul, 32)]
    public void Count_ReturnsPopCount(ulong bb, int popCount) {
        Assert.Equal(popCount, ((BitBoard)bb).PopCount);
    }

    [Fact]
    public void Indexer_Int_ReturnsTrueWhenBitSet() {
        BitBoard bb = 0xaaaa_aaaa_aaaa_aaaaul;

        for (int i = 0; i < 64; i++) {
            if (i % 2 == 1)
                Assert.True(bb[i]);
            else
                Assert.False(bb[i]);
        }
    }

    [Fact]
    public void Indexer_B5_ReturnsTrue() {
        BitBoard bb = new(0, 0, 0, 0x40, 0, 0, 0, 0);

        Assert.True(bb["b5"]);
        Assert.True(bb["B5"]);
    }

    [Fact]
    public void Diagonal_int_SetsBitN() {
        for (int i = 0; i < 64; i++) {
            Assert.True(BitBoard.Diagonal(i)[i], $"i: {i}");
        }
    }

    [Fact]
    public void Diagonal_HorizontalBottom_SetsDiagonalBits() {
        for (int i = 0; i < 8; i++) {
            BitBoard bb = BitBoard.Diagonal(i, 0);
            for (int j = 0; j < 8 - i; j++)
                Assert.True(bb[j + i, j], $"i: {i}, j: {j}");
        }
    }

    [Fact]
    public void Diagonal_VerticalLeft_SetsDiagonalBits() {
        for (int i = 0; i < 8; i++) {
            BitBoard bb = BitBoard.Diagonal(0, i);
            for (int j = 0; j < 8 - i; j++)
                Assert.True(bb[j, j + i], $"i: {i}, j: {j}");
        }
    }

    [Fact]
    public void AntiDiagonal_int_SetsBitN() {
        for (int i = 0; i < 64; i++) {
            Assert.True(BitBoard.AntiDiagonal(i)[i], $"i: {i}");
        }
    }
    [Fact]
    public void AntiDiagonal_HorizontalTop_SetsDiagonalBits() {
        for (int i = 0; i < 8; i++) {
            BitBoard bb = BitBoard.AntiDiagonal(i, 7);
            for (int j = 0; j < 8 - i; j++)
                Assert.True(bb[j + i, 7 - j], $"i: {i}, j: {j}");
        }
    }

    [Fact]
    public void AntiDiagonal_VerticalLeft_SetsDiagonalBits() {
        for (int i = 0; i < 8; i++) {
            BitBoard bb = BitBoard.AntiDiagonal(0, 7 - i);
            for (int j = 0; j < 8 - i; j++)
                Assert.True(bb[j, 7 - (j + i)], $"i: {i}, j: {j}");
        }
    }

    [Fact]
    public void HorizontalLine_Ranks_SetsHorizontalLine() {
        for (int i = 0; i < 8; i++) {
            BitBoard bb = BitBoard.HorizontalLine(i);
            for (int j = 0; j < 8; j++)
                Assert.True(bb[j, i]);
        }
    }

    [Fact]
    public void VerticalLine_Ranks_SetsVerticalLine() {
        for (int i = 0; i < 8; i++) {
            BitBoard bb = BitBoard.VerticalLine(i);
            for (int j = 0; j < 8; j++)
                Assert.True(bb[i, j]);
        }
    }

    [Theory]
    [InlineData(0b11001010, 0xcacacacacacacacaul)]
    public void VerticalLines_Files_SetsVerticalLines(byte files, ulong expected) {
        BitBoard actual = BitBoard.VerticalLines(files);

        Assert.Equal(expected, (ulong)actual);
    }
}
