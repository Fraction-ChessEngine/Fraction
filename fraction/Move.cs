using System;

namespace fraction;

public readonly struct Move {
    public readonly int Start { get; }
    public readonly int End { get; }
    public readonly Piece? Promotion { get; } = null;

    public Move(int from, int to) {
        this.Start = from;
        this.End = to;
    }

    public Move(int from, int to, Piece? promotion) : this(from, to) {
        this.Promotion = promotion;
    }

    public static bool TryParse(string s, out Move m) {
        m = new(0, 0);
        if (s.Length is not 4 or 5) return false;
        if (int.Max(s[0], s[2]) > 'h') return false;
        if (int.Min(s[0], s[2]) < 'a') return false;
        if (!char.IsDigit(s[1])) return false;
        if (!char.IsDigit(s[3])) return false;

        (int row, int col) from = (s[1] - '1', s[0] - 'a');
        (int row, int col) to = (s[3] - '1', s[2] - 'a');
        m = new((from.row * 8) + from.col, (to.row * 8) + to.col);
        if (s.Length == 4) return true;
        if (!PieceUtil.TryParse(s[4], out Piece p)) return false;
        m = new(m.Start, m.End, p);
        return true;
    }

    public override string ToString() {
        Console.WriteLine("Promotion is: " + Promotion);
        return $"{(char)((Start % 8) + 'a')}{(char)((Start / 8) + '1')}{(char)((End % 8) + 'a')}{(char)((End / 8) + '1')}{Promotion?.GetSymbol() ?? ""}";
        //return "{" + Start + " -> " + End + ", Promotion?: " + Promotion + "}";
    }

}
