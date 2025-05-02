using System;
using System.Runtime.CompilerServices;

namespace fraction;
public static class MoveGen {
    public static BitBoard GetAttackedSqrBB(Span<Vision> visions, bool forWhite) {
        BitBoard attackSqrBB = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            BitBoard bb = v.MoveBB;

            //pawns müssen gesondert berechnet werden wegen des unterschieds zwischen bewegung und schlagzug
            if (v.PieceType == Piece.wPawn || v.PieceType == Piece.bPawn) {
                bb &= ~BitBoard.VerticalLine(v.PosIndex % 8);

                int y = v.PosIndex >> 3;
                int x = v.PosIndex & 7;
                bb |= BB_Lookup.GetPawnAttackSqrs(x, y, forWhite);
            }

            //a piece cannot cover itself
            attackSqrBB |= bb & ~(1ul << v.PosIndex);
        }

        return attackSqrBB;
    }

    public static Span<Vision> GenerateVisions(Chessboard b, bool forWhite, BitBoard enemyControlSqrs, bool includeCoverage = false) {
        //weil maximal 16 pieces die je ein "Moves" bekommen
        Vision[] possibleMoves = new Vision[16];
        b.GetPinnedPieceBB(forWhite);

        int currIndex = 0;

        if (forWhite) {
            GenerateMovesForPiece(b, b.WRookBB, ref possibleMoves, ref currIndex, Piece.wRook, enemyControlSqrs, includeCoverage);
            GenerateMovesForPiece(b, b.WKnightBB, ref possibleMoves, ref currIndex, Piece.wKnight, enemyControlSqrs, includeCoverage);
            GenerateMovesForPiece(b, b.WBishopBB, ref possibleMoves, ref currIndex, Piece.wBishop, enemyControlSqrs, includeCoverage);
            GenerateMovesForPiece(b, b.WQueenBB, ref possibleMoves, ref currIndex, Piece.wQueen, enemyControlSqrs, includeCoverage);

            GenerateMovesForPawns(b, b.WPawnBB, ref possibleMoves, ref currIndex, Piece.wPawn, enemyControlSqrs, includeCoverage);
            GenerateMovesForKing(b, b.WKingBB, ref possibleMoves, ref currIndex, Piece.wKing, enemyControlSqrs, includeCoverage);

        } else {
            GenerateMovesForPiece(b, b.BRookBB, ref possibleMoves, ref currIndex, Piece.bRook, enemyControlSqrs, includeCoverage);
            GenerateMovesForPiece(b, b.BKnightBB, ref possibleMoves, ref currIndex, Piece.bKnight, enemyControlSqrs, includeCoverage);
            GenerateMovesForPiece(b, b.BBishopBB, ref possibleMoves, ref currIndex, Piece.bBishop, enemyControlSqrs, includeCoverage);
            GenerateMovesForPiece(b, b.BQueenBB, ref possibleMoves, ref currIndex, Piece.bQueen, enemyControlSqrs, includeCoverage);

            GenerateMovesForPawns(b, b.BPawnBB, ref possibleMoves, ref currIndex, Piece.bPawn, enemyControlSqrs, includeCoverage);
            GenerateMovesForKing(b, b.BKingBB, ref possibleMoves, ref currIndex, Piece.bKing, enemyControlSqrs, includeCoverage);
        }

        return possibleMoves.AsSpan(0..currIndex);
    }

    private static void GenerateMovesForKing(Chessboard b, BitBoard kingBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece king, BitBoard enemyControlSqrs, bool includeCoverage = false) {

        int kingIndex = kingBB.LowestOne;

        Vision vKing = GetVisionForPieceAt(b, kingIndex, king, enemyControlSqrs, includeCoverage);

        if (vKing.MoveBB != 0) {
            possibleMoves[currIndex] = vKing;
            currIndex++;
        }
    }

    private static void GenerateMovesForPawns(
            Chessboard b, BitBoard pawnBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece pawn, BitBoard enemyControlSqrs, bool includeCoverage = false) {

        Span<int> pawns = stackalloc int[pawnBB.PopCount];
        _ = pawnBB.FindOnes(pawns);

        for (int i = 0; i < pawns.Length; i++) {
            int posIndex = pawns[i];
            Vision v = GetVisionForPieceAt(b, posIndex, pawn, enemyControlSqrs, includeCoverage);

            if (v.MoveBB == 0) {
                continue;
            }
            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }


    private static void GenerateMovesForPiece(Chessboard b, BitBoard pieceBB,
            ref Vision[] possibleMoves, ref int currIndex, Piece type, BitBoard enemyControlSqrs, bool includeCoverage = false) {

        Span<int> pieces = stackalloc int[pieceBB.PopCount];
        _ = pieceBB.FindOnes(pieces);

        for (int i = 0; i < pieces.Length; i++) {
            Vision v = GetVisionForPieceAt(b, pieces[i], type, enemyControlSqrs, includeCoverage);

            if (v.MoveBB == 0) continue;

            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }


    /// <summary>
    /// wird gecalled wenn eindeutig ein check vorhanden ist, forWhite = white is in check
    /// </summary>
    /// <param name="b"></param>
    /// <param name="forWhite"></param>
    /// <returns></returns>
    public static Span<Vision> GenerateMovesForCheck(Chessboard b, bool forWhite, BitBoard enemyControlSqrs) {
        /* 
        Algo: rausfinden ob doubleCheck
        Wenn doubleCheck:
            king bewegen
        
        ansonsten: 
            wenn knight:
                king bewegen oder knight nehmen
            ansonsten:
                king bewegen, piece nehmen, blocken

        */
        BitBoard combined = 0;
        foreach (BitBoard bb in b.CheckPieceBBs) combined |= bb;
        int amount = combined.PopCount;

        int posIndex = (forWhite ? b.WKingBB : b.BKingBB).LowestOne;
        BitBoard sameColorPieces = forWhite ? b.WhitePiecesBB : b.BlackPiecesBB;
        Piece king = forWhite ? Piece.wKing : Piece.bKing;

        //weil ein kingmove immer ein valider ausweg ist, castleSqrs=0 weil eh nicht gecastled werden darf
        Vision kingVision = new(
            posIndex,
            MoveSets.GetKingPseudoLegalMoves(posIndex, sameColorPieces, enemyControlSqrs, 0),
            king
            );

        bool kingMobile = kingVision.MoveBB != 0;//wenn 0: kingVision darf nicht returnt werden


        b.GetPinnedPieceBB(forWhite);

        //double check, king muss bewegt werden
        if (amount > 1) {
            if (kingMobile) return new Vision[] { kingVision };
            b.IsCheckMate = true;
            return null;//entspricht checkmate

        } else {//nur ein piece checkt den king
            return GenerateMovesForSingleCheck(b, forWhite, combined, posIndex, enemyControlSqrs);
        }
    }

    //muss mit checkmates getestet werden
    /// <summary>
    /// Wird in GenerateMovesForCheck gecalled wenn es nur ein Piece check gibt
    /// </summary>
    /// <returns></returns>
    private static Span<Vision> GenerateMovesForSingleCheck(Chessboard b, bool forWhite, BitBoard combined, int kingIndex, BitBoard enemyControlSqrs) {
        Span<Vision> pseudoLegal = GenerateVisions(b, forWhite, enemyControlSqrs);

        Vision[] legal = new Vision[16];
        int currIndex = 0;

        if ((b.CheckPieceBBs[0] | b.CheckPieceBBs[1]) != 0) {//es ist ein knight oder pawn, dh kein piece kann blocken

            //überprüfen ob knight/pawn genommen werden kann
            for (int i = 0; i < pseudoLegal.Length; i++) {
                Vision v = pseudoLegal[i];

                //der king dürfte auch andere moves ausser captures machen
                if (v.PosIndex != kingIndex) {
                    /* 
                    logic to check if EP sqr is behind the checkPiece,
                     combined is just a bitboard the checkPieceBit set 
                     */
                    bool enPassantPawnGivesCheckAndCanBeCapturedByCurrentPawn =
                    b.CheckPieceBBs[0] != 0 && (v.PieceType == Piece.wPawn || v.PieceType == Piece.bPawn)
                    && b.EnPassantSqr > 0
                    && v.MoveBB[b.EnPassantSqr];

                    if (enPassantPawnGivesCheckAndCanBeCapturedByCurrentPawn) {//confirmed pawn

                        //possible EP is on the board
                        int checkPieceIndex = combined.HighestOne;
                        int EPpawnIndex = Chessboard.GetEnPassantPawn(b.EnPassantSqr);

                        //en passant pawn gave check
                        if (EPpawnIndex == checkPieceIndex) {
                            v.MoveBB &= combined;
                            v.MoveBB |= 1ul << b.EnPassantSqr;
                        } else {
                            v.MoveBB &= combined;//nur moves die das checkPiece capturen werden zugelassen
                        }

                    } else {
                        v.MoveBB &= combined;//nur moves die das checkPiece capturen werden zugelassen
                    }
                }

                if (v.MoveBB != 0) {
                    legal[currIndex] = v;
                    currIndex++;
                }

            }
            //no legal moves found in check, therefore checkmate
            if (currIndex == 0) {
                b.IsCheckMate = true;
                return null;
            }

            return legal.AsSpan(0..currIndex);

        } else {//es ist ein slider
            BitBoard checkLine = GetCheckLine(kingIndex, combined.LowestOne) & ~(1ul << kingIndex);


            for (int i = 0; i < pseudoLegal.Length; i++) {
                Vision v = pseudoLegal[i];

                //der king darf nicht blocken
                if (v.PosIndex != kingIndex) {
                    v.MoveBB &= checkLine;
                }

                if (v.MoveBB != 0) {
                    legal[currIndex] = v;
                    currIndex++;
                }
            }
            //no legal moves found in check, therefore checkmate
            if (currIndex == 0) {
                b.IsCheckMate = true;
                return null;
            }
            return legal.AsSpan(0..currIndex);
        }
    }


    /// <summary>
    /// kingIndex, checkPieceIndex, enthält noch beide PieceSqrs 
    ///(king muss nicht entfernt werden weil ergebnis sowieso mit pseudoLegalmoves ver-und-et wird)
    /// </summary>
    /// <param name="k"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static BitBoard GetCheckLine(int k, int c) {
        int yKing = k >> 3;
        int xKing = k & 7;

        int yCheck = c >> 3;
        int xCheck = c & 7;

        int xDiff = xCheck - xKing;
        int yDiff = yCheck - yKing;

        int big, small;
        if (k > c) {
            big = k;
            small = c;
        } else {
            big = c;
            small = k;
        }

        if (yKing == yCheck) return MoveSets.InterpolateHorizontal(big, small);
        if (xKing == xCheck) return MoveSets.InterpolateVertical(big, small);
        if (xDiff == yDiff) return MoveSets.InterpolateDiagonal(big, small);
        if (xDiff == -yDiff) return MoveSets.InterpolateAntiDiagonal(big, small);

        throw new Exception("GetCheckLine macht Probleme, korrekte Interpolation nicht eindeutig");
    }


    public static Span<Move> GenerateMoves(Chessboard b, bool whitesTurn, bool perft = false) {

        //provisorische lösung
        Span<Vision> attackVisions = GenerateVisions(b, !whitesTurn, 0, true);
        BitBoard enemyControlSqrs = GetAttackedSqrBB(attackVisions, !whitesTurn);

        bool isCheck = b.IsInCheck(whitesTurn);
        Span<Vision> visions = isCheck ? GenerateMovesForCheck(b, whitesTurn, enemyControlSqrs) : GenerateVisions(b, whitesTurn, enemyControlSqrs);

        Move[] ret = new Move[96];
        int index = 0;


#pragma warning disable CA2014

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            Span<int> moves = stackalloc int[v.MoveBB.PopCount];
            _ = v.MoveBB.FindOnes(moves);

            for (int j = 0; j < v.MoveBB.PopCount; j++) {
                int end = moves[j];

                if (IsPromoting(end, v.PieceType)) {

                    if (whitesTurn) {
                        ret[index] = new(v.PosIndex, end, Piece.wQueen);
                        ret[index + 1] = new(v.PosIndex, end, Piece.wRook);
                        ret[index + 2] = new(v.PosIndex, end, Piece.wBishop);
                        ret[index + 3] = new(v.PosIndex, end, Piece.wKnight);
                        index += 4;
                    } else {
                        ret[index] = new(v.PosIndex, end, Piece.bQueen);
                        ret[index + 1] = new(v.PosIndex, end, Piece.bRook);
                        ret[index + 2] = new(v.PosIndex, end, Piece.bBishop);
                        ret[index + 3] = new(v.PosIndex, end, Piece.bKnight);
                        index += 4;
                    }

                } else {
                    ret[index++] = new(v.PosIndex, end);
                }

                //frisst hoffentlich nicht zu viel performance
                if (perft) {
                    Testing.perftmoves[index - 1] = new Move(v.PosIndex, end).ToString();
                }
            }
        }
#pragma warning restore CA2014

        return ret[0..index];
    }

    public static Chessboard[] GenerateBoards(Chessboard b, bool whitesTurn, bool perft = false) {
        Span<Move> moves = GenerateMoves(b, whitesTurn, perft);
        Chessboard[] ret = new Chessboard[moves.Length];
        for (int i = 0; i < ret.Length; i++) {
            ret[i] = b.Clone();
            ret[i].MakeMove(moves[i]);
        }
        return ret;
    }

    private static bool IsPromoting(int end, Piece type) {
        return type switch {
            Piece.wPawn => end > 55,
            Piece.bPawn => end < 8,
            _ => false,
        };
    }




    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vision GetVisionForPieceAt(Chessboard b, int i, Piece type, BitBoard enemyControlSqrs, bool includeCoverage = false) {
        BitBoard bb = MoveSets.GetPseudoLegalMoves(b, i, type, enemyControlSqrs, includeCoverage);

        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (b.GetPinnedPieceBB(type.IsWhite())[i] && !includeCoverage) {
            BitBoard pinLine = b.GetPinLineBB(1ul << i);
            bb &= pinLine;
        }

        return new Vision(i, bb, type);
    }
}
