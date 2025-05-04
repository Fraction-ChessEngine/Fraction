using System.Diagnostics;

namespace fraction;

public static class CastleSquares {
    private const ulong wKingSide = 0b_01000000ul;
    private const ulong wQueenSide = 0b_00000100ul;
    private const ulong bKingSide = wKingSide << 56;
    private const ulong bQueenSide = wQueenSide << 56;
    public static BitBoard Get(CastleRights side) => side switch {
        CastleRights.None => 0,
        CastleRights.WKingSide => wKingSide,
        CastleRights.WQueenSide => wQueenSide,
        CastleRights.BKingSide => bKingSide,
        CastleRights.BQueenSide => bQueenSide,
        CastleRights.White => wKingSide | wQueenSide,
        CastleRights.Black => bKingSide | bQueenSide,
        CastleRights.KingSide => wKingSide | bKingSide,
        CastleRights.QueenSide => wQueenSide | bQueenSide,
        CastleRights.All => wKingSide | wQueenSide | bKingSide | bQueenSide,
        _ => throw new UnreachableException(),
    };
}
