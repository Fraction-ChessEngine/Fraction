using System;
using System.Text;

namespace fraction;

public readonly struct Score {
    public static readonly Score MaxValue = new Score(ScoreType.Mate, 1);
    public static readonly Score MinValue = new Score(ScoreType.Mate, -1);

    public ScoreType Type { get; init; }
    public int Value { get; init; }

    public bool IsMate => this.Type == ScoreType.Mate;

    public Score(ScoreType type, int val) {
        this.Type = type;
        this.Value = val;
    }

    public static Score Max(Score x, Score y) {
        return x > y ? x : y;
    }

    public static Score Min(Score x, Score y) {
        return x < y ? x : y;
    }

    public override string ToString() {
            StringBuilder sb = new();
            if (this.Type.HasFlag(ScoreType.Bound))
                sb.Append(this.Type.HasFlag(ScoreType.UpperBound) ? "upperbound " : "lowerbound ");
            sb.Append(this.Type.HasFlag(ScoreType.Mate) ? "mate " : "cp ");
            sb.Append(this.Value);
            return sb.ToString();
        }
    public static bool operator <(Score lhs, Score rhs) {
        return (lhs.Type, rhs.Type) switch {
            (ScoreType.Centipawns, ScoreType.Centipawns) => lhs.Value < rhs.Value,
            (ScoreType.Mate, ScoreType.Mate) => int.MaxValue - lhs.Value < int.MaxValue - rhs.Value,
            (ScoreType.Mate, ScoreType.Centipawns) => (Math.Sign(lhs.Value) * 2) < Math.Sign(rhs.Value),
            (ScoreType.Centipawns, ScoreType.Mate) => Math.Sign(lhs.Value) < (Math.Sign(rhs.Value) * 2),
            _ => throw new NotImplementedException(),
        };
    }

    public static bool operator >(Score lhs, Score rhs) {
        return (lhs.Type, rhs.Type) switch {
            (ScoreType.Centipawns, ScoreType.Centipawns) => lhs.Value > rhs.Value,
            (ScoreType.Mate, ScoreType.Mate) => int.MaxValue / lhs.Value > int.MaxValue / rhs.Value,
            (ScoreType.Mate, ScoreType.Centipawns) => (Math.Sign(lhs.Value) * 2) > Math.Sign(rhs.Value),
            (ScoreType.Centipawns, ScoreType.Mate) => Math.Sign(lhs.Value) > (Math.Sign(rhs.Value) * 2),
            _ => throw new NotImplementedException(),
        };
    }
}

[Flags]
public enum ScoreType {
    Centipawns = 0,
    Mate = 1,
    Bound = 2,
    UpperBound = 4,
}
