using System;

namespace fraction;

[Flags]
public enum CastleRights {
    None = 0,
    WKingSide = 1 << 0,
    WQueenSide = 1 << 1,
    BKingSide = 1 << 2,
    BQueenSide = 1 << 3,
    White = WKingSide | WQueenSide,
    Black = BKingSide | BQueenSide,
    KingSide = WKingSide | BKingSide,
    QueenSide = WQueenSide | BQueenSide,
    All = Black | White,
}
