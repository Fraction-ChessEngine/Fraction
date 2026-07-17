using System;
using System.Runtime.CompilerServices;

namespace fraction;
public static class MoveGen {

    static readonly Vision[] visionBuffer=new Vision[16];
    public static Span<Vision> GenerateVisions(Chessboard b, bool forWhite, bool includeCoverage = false) {
        //weil maximal 16 pieces die je ein "Moves" bekommen
        Vision[] possibleMoves = visionBuffer;
        BitBoard pinBB=b.GetPinnedPieceBB(forWhite);

        int currIndex = 0;

        if (forWhite) {
            GenerateMovesForPiece(b, b.WRookBB, ref possibleMoves, ref currIndex, Piece.wRook,pinBB, includeCoverage);
            GenerateMovesForPiece(b, b.WKnightBB, ref possibleMoves, ref currIndex, Piece.wKnight,pinBB, includeCoverage);
            GenerateMovesForPiece(b, b.WBishopBB, ref possibleMoves, ref currIndex, Piece.wBishop,pinBB, includeCoverage);
            GenerateMovesForPiece(b, b.WQueenBB, ref possibleMoves, ref currIndex, Piece.wQueen,pinBB, includeCoverage);

            GenerateMovesForPawns(b, b.WPawnBB, ref possibleMoves, ref currIndex, Piece.wPawn,pinBB, includeCoverage);
            GenerateMovesForKing(b, b.WKingBB, ref possibleMoves, ref currIndex, Piece.wKing,pinBB, includeCoverage);

        } else {
            GenerateMovesForPiece(b, b.BRookBB, ref possibleMoves, ref currIndex, Piece.bRook,pinBB, includeCoverage);
            GenerateMovesForPiece(b, b.BKnightBB, ref possibleMoves, ref currIndex, Piece.bKnight,pinBB, includeCoverage);
            GenerateMovesForPiece(b, b.BBishopBB, ref possibleMoves, ref currIndex, Piece.bBishop,pinBB, includeCoverage);
            GenerateMovesForPiece(b, b.BQueenBB, ref possibleMoves, ref currIndex, Piece.bQueen,pinBB, includeCoverage);

            GenerateMovesForPawns(b, b.BPawnBB, ref possibleMoves, ref currIndex, Piece.bPawn,pinBB, includeCoverage);
            GenerateMovesForKing(b, b.BKingBB, ref possibleMoves, ref currIndex, Piece.bKing,pinBB, includeCoverage);
        }

        return possibleMoves.AsSpan(0..currIndex);
    }

    private static void GenerateMovesForKing(Chessboard b, BitBoard kingBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece king,BitBoard pinBB, bool includeCoverage = false) {

        int kingIndex = kingBB.LowestOne;

        Vision vKing = GetVisionForPieceAt(b, kingIndex, king,pinBB, includeCoverage);

        if (vKing.MoveBB != 0) {
            possibleMoves[currIndex] = vKing;
            currIndex++;
        }
    }

    private static void GenerateMovesForPawns(
            Chessboard b, BitBoard pawnBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece pawn,BitBoard pinBB, bool includeCoverage = false) {

        Span<int> pawns = stackalloc int[pawnBB.PopCount];
        _ = pawnBB.FindOnes(pawns);

        for (int i = 0; i < pawns.Length; i++) {
            int posIndex = pawns[i];
            Vision v = GetVisionForPieceAt(b, posIndex, pawn,pinBB, includeCoverage);

            if (v.MoveBB == 0) {
                continue;
            }
            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }


    private static void GenerateMovesForPiece(Chessboard b, BitBoard pieceBB,
            ref Vision[] possibleMoves, ref int currIndex, Piece type, BitBoard pinBB, bool includeCoverage = false) {

        Span<int> pieces = stackalloc int[pieceBB.PopCount];
        _ = pieceBB.FindOnes(pieces);

        for (int i = 0; i < pieces.Length; i++) {
            Vision v = GetVisionForPieceAt(b, pieces[i], type,pinBB, includeCoverage);

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
    public static Span<Vision> GenerateMovesForCheck(Chessboard b, bool forWhite) {
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
        BitBoard enemyControlSqrs = forWhite ? b.BControlledSqrBB : b.WControlledSqrBB;
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
            return GenerateMovesForSingleCheck(b, forWhite, combined, posIndex);
        }
    }

    //muss mit checkmates getestet werden
    /// <summary>
    /// Wird in GenerateMovesForCheck gecalled wenn es nur ein Piece check gibt
    /// </summary>
    /// <returns></returns>
    private static Span<Vision> GenerateMovesForSingleCheck(Chessboard b, bool forWhite, BitBoard combined, int kingIndex) {
        Span<Vision> pseudoLegal = GenerateVisions(b, forWhite);

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


    static readonly int[] intBuffer=new int[27];//max amount if moves a single piece can have
    public static Span<Move> GenerateMoves(Chessboard b, bool whitesTurn, Span<Move> buffer) {

        //provisorische lösung
        Span<Vision> attackVisions = GenerateVisions(b, !whitesTurn, true);
        b.UpdateAttackedSqrBB(attackVisions, !whitesTurn);

        //we already computed attackSqrs, so checking for check doesnt need an extra call
        //when were not in check. that way, we know for sure if we arent
        BitBoard enemyAttacks = whitesTurn ? b.BControlledSqrBB : b.WControlledSqrBB;
        BitBoard myKing = whitesTurn ? b.WKingBB : b.BKingBB;
        bool isCheck = (enemyAttacks & myKing) != 0 && b.IsInCheck(whitesTurn);

        Span<Vision> visions = isCheck ? GenerateMovesForCheck(b, whitesTurn) : GenerateVisions(b, whitesTurn);

       // Move[] ret = new Move[96];
        int index = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            Span<int> moves = intBuffer;//stackalloc int[v.MoveBB.PopCount];
            _ = v.MoveBB.FindOnes(moves);

            for (int j = 0; j < v.MoveBB.PopCount; j++) {
                int end = moves[j];

                bool promo = IsPromoting(end, v.PieceType);
                bool isCapture;

                if (whitesTurn) {
                    isCapture = HandleMoveBuilding(buffer, end, Piece.wQueen, Piece.wRook, Piece.wBishop, Piece.wKnight, promo, b,ref index, v);
                } else {
                    isCapture = HandleMoveBuilding(buffer, end, Piece.bQueen, Piece.bRook, Piece.bBishop, Piece.bKnight, promo, b,ref index, v);
                }

                //frisst hoffentlich nicht zu viel performance
                /* if (perft) {
                    Testing.perftmoves[index - 1] = new Move(v.PosIndex, end).ToString();
                } */
            }
        }

        return buffer[0..index];
       // return moveBuffer.AsSpan(0..index);
    }

    static bool HandleMoveBuilding(Span<Move> moves, int end, Piece queen, Piece rook,
        Piece bishop, Piece knight, bool promo, Chessboard b, ref int index, Vision v) {

        BitBoard enemyPieces=v.PieceType.IsWhite() ? b.BlackPiecesBB : b.WhitePiecesBB;
        bool isCapture = ((1ul << end) & enemyPieces) != 0;

        if (promo) {
            moves[index] = new(v.PosIndex, end, queen, isCapture);
            moves[index + 1] = new(v.PosIndex, end, rook, isCapture);
            moves[index + 2] = new(v.PosIndex, end, bishop, isCapture);
            moves[index + 3] = new(v.PosIndex, end, knight, isCapture);
            index += 4;
        } else {
            moves[index++] = new(v.PosIndex, end, isCapture);
        }

        return isCapture;
    }

    public static Chessboard[] GenerateBoards(Chessboard b, bool whitesTurn, bool perft = false) {
        Move[] buf=new Move[64];
        Span<Move> moves = GenerateMoves(b, whitesTurn,buf);
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
    public static Vision GetVisionForPieceAt(Chessboard b, int i, Piece type,BitBoard pinBB, bool includeCoverage = false) {
        BitBoard bb = MoveSets.GetPseudoLegalMoves(b, i, type, includeCoverage);

        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (pinBB[i] && !includeCoverage) {
            BitBoard pinLine = b.GetPinLineBB(1ul << i);
            bb &= pinLine;
        }

        return new Vision(i, bb, type);
    }
}
