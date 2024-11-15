using System;

namespace fraction {
    public struct Move {
        public readonly int Start, End;
        public readonly Piece promotion;

        public Move(int s, int e, Piece p = Piece.wKing) {
            Start = s;
            End = e;
            promotion = p;
        }


        public override string ToString() {
            return "{" + Start + " -> " + End + ", Promotion?: " + promotion + "}";
        }

    }
}