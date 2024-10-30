using System;
using System.Text;

using static System.Numerics.BitOperations;

namespace fraction;

// implemented with lsb = a1 and msb = h8 first increasing file then rank
public struct BitBoard {
    private ulong _value; // must have 64b!

    public int PopCount => PopCount(_value);
    public int TrailingZeroCount => TrailingZeroCount(_value);
    public int LeadingZeroCount => LeadingZeroCount(_value);
    public int LowestOne => TrailingZeroCount;
    public int HighestOne => 63 - LeadingZeroCount;
    public int BitScanForward => LowestOne;
    /// Deprecated, use HighestOne, which returns -1 on POPCount == 0
    public int BitScanReverse => int.Max(0, HighestOne);

    public bool this[int bit] {
        get => (_value & (1ul << bit)) != 0;
        set {
            if (value)
                _value |= (1ul << bit);
            else
                _value &= ~(1ul << bit);
        }
    }

    public bool this[int x, int y] {
        get => this[(y * 8) + x];
        set => this[(y * 8) + x] = value;
    }

    public bool this[string pos] {
        get => this[((pos[1] - '1') * 8) + ((pos[0] | 0x60) - 'a')];
        set => this[((pos[1] - '1') * 8) + ((pos[0] | 0x60) - 'a')] = value;
    }

    public BitBoard() { }

    // draw a BitBoard with a1 in msb of a...
    public BitBoard(byte h, byte g, byte f, byte e, byte d, byte c, byte b, byte a) {
        _value = Reverse(h);
        _value <<= 8;
        _value += Reverse(g);
        _value <<= 8;
        _value += Reverse(f);
        _value <<= 8;
        _value += Reverse(e);
        _value <<= 8;
        _value += Reverse(d);
        _value <<= 8;
        _value += Reverse(c);
        _value <<= 8;
        _value += Reverse(b);
        _value <<= 8;
        _value += Reverse(a);
    }


    public Span<int> FindOnes(Span<int> posBuf) {
        int i = 0;
        for (BitBoard bb = this; i < int.Min(posBuf.Length, PopCount); i++) {
            posBuf[i] = bb.LowestOne;
            bb[posBuf[i]] = false;
        }

        return posBuf[0..i];
    }

    //https://stackoverflow.com/questions/3587826/is-there-a-built-in-function-to-reverse-bit-order
    private static byte Reverse(byte x)
        => (byte)(((x * 0x80200802ul) & 0x0884422110ul) * 0x0101010101ul >> 32);

    private static readonly BitBoard[] diagonals =
    {
        0x0000000000000080ul,
        0x0000000000008040ul,
        0x0000000000804020ul,
        0x0000000080402010ul,
        0x0000008040201008ul,
        0x0000804020100804ul,
        0x0080402010080402ul,
        0x8040201008040201ul,
        0x4020100804020100ul,
        0x2010080402010000ul,
        0x1008040201000000ul,
        0x0804020100000000ul,
        0x0402010000000000ul,
        0x0201000000000000ul,
        0x0100000000000000ul,
        0x0000000000000080ul,
    };

    public static BitBoard Diagonal(string pos)
        => Diagonal(pos[0] - 'a', pos[1] - '1');

    public static BitBoard Diagonal(int bit)
        => Diagonal(bit & 0x7, bit >> 3);

    public static BitBoard Diagonal(int x, int y)
        => diagonals[y - x + 7];


    private static readonly BitBoard[] antiDiagonals =
    {
        0x0000000000000001ul,
        0x0000000000000102ul,
        0x0000000000010204ul,
        0x0000000001020408ul,
        0x0000000102040810ul,
        0x0000010204081020ul,
        0x0001020408102040ul,
        0x0102040810204080ul,
        0x0204081020408000ul,
        0x0408102040800000ul,
        0x0810204080000000ul,
        0x1020408000000000ul,
        0x2040800000000000ul,
        0x4080000000000000ul,
        0x8000000000000000ul,
        0x0000000000000001ul,
    };

    public static BitBoard AntiDiagonal(string pos)
        => AntiDiagonal(pos[0] - 'a', pos[1] - '1');

    public static BitBoard AntiDiagonal(int bit)
        => AntiDiagonal(bit & 0x7, bit >> 3);

    public static BitBoard AntiDiagonal(int x, int y)
        => antiDiagonals[x + y];

    public static BitBoard HorizontalLine(int rank)
        => 0xfful << (8 * rank);

    public static BitBoard VerticalLine(char file)
        => VerticalLine(file - 'a');

    public static BitBoard VerticalLine(int file)
        => 0x101010101010101ul << file;

    // lsb is 'a' file
    public static BitBoard VerticalLines(byte files)
        => 0x101010101010101ul * files;

    public static implicit operator BitBoard(ulong val) => new() { _value = val };
    public static implicit operator ulong(BitBoard val) => val._value;
    public static BitBoard operator ~(BitBoard bb) => ~(ulong)bb;
    public static BitBoard operator &(BitBoard left, BitBoard right) => (ulong)left & (ulong)right;
    public static BitBoard operator |(BitBoard left, BitBoard right) => (ulong)left | (ulong)right;
    public static BitBoard operator ^(BitBoard left, BitBoard right) => (ulong)left ^ (ulong)right;
    public static bool operator ==(BitBoard left, BitBoard right) => (ulong)left == (ulong)right;
    public static bool operator !=(BitBoard left, BitBoard right) => (ulong)left != (ulong)right;

    public override bool Equals(object? obj)
        => ((ulong)this).Equals(obj);

    public override int GetHashCode()
        => ((ulong)this).GetHashCode();

    // do not use Console.WriteLine(bb);
    // use Console.WriteLine(bb.ToString());
    public override string ToString() {
        StringBuilder sb = new();
        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++)
                sb.Append((this[(8 - i) * 8 + j]) ? '1' : '0').Append(' ');
            sb.AppendLine();
        }

        sb.Remove(sb.Length - 1, 1);

        return sb.ToString();
    }

    public void Print() {
        Console.WriteLine(ToString());
    }
}
