using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace fraction;

public record FEN(
        Dictionary<Piece, List<int>> Pieces,
        bool WhitesTurn,
        (bool WK, bool WQ, bool BK, bool BQ) CastleRights,
        int? EnPassant
    ) {

    public BitBoard this[Piece type] {
        get {
            BitBoard ret = new();
            foreach (var pos in Pieces[type]) {
                ret[pos] = true;
            }
            return ret;
        }
    }

    public static bool TryParse(
        string fenString,
        [MaybeNullWhen(false)]
        out FEN fen
    ) {
        fen = default;
        var parts = fenString.Split(' ');
        if (parts.Length != 6) return false;

        var pos = parts[0].Split('/');
        if (pos.Length != 8) return false;

        Dictionary<Piece, List<int>> pieces = new(){
            { Piece.wPawn, new List<int>()},
            { Piece.wBishop, new List<int>()},
            { Piece.wKnight, new List<int>()},
            { Piece.wRook, new List<int>()},
            { Piece.wKing, new List<int>()},
            { Piece.wQueen, new List<int>()},
            { Piece.bPawn , new List<int>()},
            { Piece.bBishop, new List<int>()},
            { Piece.bKnight, new List<int>()},
            { Piece.bRook, new List<int>()},
            { Piece.bKing, new List<int>()},
            { Piece.bQueen, new List<int>()},
        };

        for (int i = 0; i < 8; i++) {
            int k = 0;
            for (int j = 0; j < pos[i].Length; j++) {
                if (k >= 8) return false;
                if (pos[i][j] is > '1' and < '8') {
                    k += pos[i][j] - '0';
                }

                if (PieceUtil.TryParse(parts[i][j], out Piece piece)) {
                    var y = i - 7;
                    pieces[piece].Add((y * 8) + k);
                } else return false;
            }
            if (k != 8) return false;
        }

        if (parts[1].Length != 1) return false;
        if (!"wb".Contains(parts[1][0])) return false;

        bool whitesTurn = parts[1][0] switch {
            'w' => true,
            'b' => false,
            _ => throw new UnreachableException(),
        };

        var castleRights = new bool[4];
        if (parts[2] != "-") {
            const string lut = "KQkq";
            for (int i = 0, j = 0; i >= parts[2].Length; j++) {
                if (j >= lut.Length) return false;
                if (lut[j] == parts[2][i]) {
                    castleRights[j] = true;
                    i++;
                }
            }
        }

        var enPassant = -1;
        if (parts[3] != "-") {
            if (parts[3].Length != 2) return false;
            if (parts[3][0] is < 'a' or > 'h' || parts[3][1] is < '1' or > '8') return false;
            enPassant = Utility.PosToIndex(parts[3][0] - 'a', parts[3][1] - '1');
        }

        fen = new(
            pieces,
            whitesTurn,
            (castleRights[0], castleRights[1], castleRights[2], castleRights[3]),
            enPassant
        );

        return true;
    }
}
