using System;

namespace fraction;

public class Transposinfo {
    public int Depth { get; private set; } = -1;
    public Score Score { get; private set; }
    public Move[]? Moves { get; set; }

    public Transposinfo(Score score, int depth) {
        this.Score = score;
        this.Depth = depth;
    }

    public Score UpdateScore(Score score, int depth) {
        if (this.Score.IsMate) {
            if (!score.IsMate) return this.Score;
            if (Math.Abs(this.Score.Value) < Math.Abs(score.Value)) return this.Score;
            this.Score = score;
            this.Depth = depth;
            return this.Score;
        }

        if (score.IsMate || this.Depth < depth) {
            this.Score = score;
            this.Depth = depth;
            return this.Score;
        }

        return this.Score;
    }
}
