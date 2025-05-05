using System;
using System.Runtime.CompilerServices;

namespace fraction;
public class MoveGen {
    private BitBoard enemyControl = 0;
    private (BitBoard[] white, BitBoard[] black) pinLines;
    private BitBoard[] checkPieces = []; // tmp

    private bool isCheck => IsInCheck(this.WhitesTurn, this.checkPieces);

    public Chessboard Board { get; init; }
    public bool WhitesTurn { get; init; }
    public bool IsCheckMate { get; private set; }

    public MoveGen(Chessboard board, bool whitesTurn) {
        this.Board = board;
        this.WhitesTurn = whitesTurn;
        this.pinLines = (GetPinLines(board, true), GetPinLines(board, false));
    }

    //doesnt change the BB, only nullifies if necessary
    private static BitBoard ValidatePin(BitBoard sightLine, BitBoard sameColorPieces, BitBoard enemyBlockers, int kingIndex) {
        sameColorPieces[kingIndex] = false;

        if ((sightLine & enemyBlockers) != 0) return 0;//enemy steht auf der pinLine

        return (sightLine & sameColorPieces).PopCount == 1 ? sightLine : 0;
    }

    //forWhite = white is in check
    private static bool IsInCheck(bool forWhite, BitBoard[] checkPieces) {
        foreach (var checkPiece in checkPieces) {
            if (checkPiece != 0) return true;
        }
        return false;
    }

    private BitBoard[] CheckPieces() {
        BitBoard[] checkPieces = new BitBoard[5];

        BitBoard knightBB, queenBB, rookBB, bishopBB, pawnBB, sameColorPieces;
        BitBoard pawnDoub;//einzigen beiden bits wo pawns checken können
        int kingIndex;

        if (this.WhitesTurn) {
            sameColorPieces = this.Board.WhitePiecesBB;
            knightBB = this.Board.BKnightBB;
            queenBB = this.Board.BQueenBB;
            rookBB = this.Board.BRookBB;
            bishopBB = this.Board.BBishopBB;
            pawnBB = this.Board.BPawnBB;

            kingIndex = this.Board.WKingBB.LowestOne;

            int x = kingIndex & 7;
            int y = kingIndex >> 3;

            pawnDoub = BitBoard.HorizontalLine(y + 1) & (0b101ul << (kingIndex + 7)) & this.Board.BPawnBB;
        } else {
            sameColorPieces = this.Board.BlackPiecesBB;
            knightBB = this.Board.WKnightBB;
            queenBB = this.Board.WQueenBB;
            rookBB = this.Board.WRookBB;
            bishopBB = this.Board.WBishopBB;
            pawnBB = this.Board.WPawnBB;

            kingIndex = this.Board.BKingBB.LowestOne;

            int x = kingIndex & 7;
            int y = kingIndex >> 3;

            pawnDoub = BitBoard.HorizontalLine(y - 1) & (0b101ul << (kingIndex - 9)) & this.Board.WPawnBB;
        }


        //king perspective 
        BitBoard kingPersKnight = BB_Lookup.GetBBforPieceAtSqr(Piece.wKnight, kingIndex);
        /* MoveSets.GetPseudoTargetSqrsRook(BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex), kingIndex) */
        BitBoard kingPersRook = MoveSets.GetSliderPseudoLegalMoves(this.Board, kingIndex, sameColorPieces, Piece.wRook);
        BitBoard kingPersBishop = MoveSets.GetSliderPseudoLegalMoves(this.Board, kingIndex, sameColorPieces, Piece.wBishop);
        BitBoard kingPersQueen = kingPersBishop | kingPersRook;


        //hier sind bits gesetzt wo pieces stehen die check geben
        //wenn leer gibt kein piece check
        checkPieces[1] = knightBB & kingPersKnight;
        checkPieces[4] = queenBB & kingPersQueen;
        checkPieces[3] = rookBB & kingPersRook;
        checkPieces[2] = bishopBB & kingPersBishop;

        checkPieces[0] = pawnBB & pawnDoub;

        return checkPieces;
    }

    private static BitBoard GetPinLineBB(BitBoard bb, BitBoard[] pinLines) {
        for (int i = 0; i < 8; i++) {
            BitBoard line = pinLines[i];
            if ((line & bb) != 0) return line;
        }

        throw new Exception("Pinned piece was not found on any generated pinLines");
    }

    private static BitBoard GetPinnedPieceBB(Chessboard board, BitBoard[] pinLines) {
        BitBoard ret = 0;
        foreach (var p in pinLines) ret |= p;
        return ret & ~board.WKingBB & ~board.BKingBB;
    }

    public static BitBoard[] GetPinLines(Chessboard board, bool forWhite) {
        BitBoard[] pinLines = new BitBoard[8];
        int kingIndex;

        BitBoard rookSightlines;
        BitBoard bishopSightlines;

        //enemy pieces with sightlines at king, aka intersections of sightlines with pieces
        BitBoard intersectionsStraight;
        BitBoard intersectionDiags;


        if (forWhite) {
            kingIndex = board.WKingBB.LowestOne;

            rookSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex);
            bishopSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wBishop, kingIndex);

            intersectionsStraight = rookSightlines & (board.BRookBB | board.BQueenBB);
            intersectionDiags = bishopSightlines & (board.BBishopBB | board.BQueenBB);
        } else {
            kingIndex = board.BKingBB.LowestOne;

            rookSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wRook, kingIndex);
            bishopSightlines = BB_Lookup.GetBBforPieceAtSqr(Piece.wBishop, kingIndex);

            intersectionsStraight = rookSightlines & (board.WRookBB | board.WQueenBB);
            intersectionDiags = bishopSightlines & (board.WBishopBB | board.WQueenBB);
        }

        int y = kingIndex >> 3;
        int x = kingIndex & 7;

        BitBoard sameColorPieces = forWhite ? board.WhitePiecesBB : board.BlackPiecesBB;
        BitBoard enemyBlockers; //muss in funktion angepasst werden da auch bRook bBishop blocken kann


        if (intersectionsStraight != 0) {
            //pieces of the other color that block this pin
            enemyBlockers = forWhite ? board.BKnightBB | board.BBishopBB | board.BPawnBB | board.BKingBB : board.WKnightBB | board.WBishopBB | board.WPawnBB | board.WKingBB;

            BitBoard intersectionHoriWest = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex, kingIndex - x);
            intersectionHoriWest = intersectionHoriWest == 0 ? 0 : MoveSets.InterpolateHorizontal(kingIndex, intersectionHoriWest.BitScanReverse);
            intersectionHoriWest = ValidatePin(intersectionHoriWest, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionHoriEast = intersectionsStraight & MoveSets.InterpolateHorizontal(kingIndex + (7 - x), kingIndex);
            intersectionHoriEast = intersectionHoriEast == 0 ? 0 : MoveSets.InterpolateHorizontal(intersectionHoriEast.LowestOne, kingIndex);
            intersectionHoriEast = ValidatePin(intersectionHoriEast, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard intersectionVertiBottom = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex, kingIndex - y * 8);
            intersectionVertiBottom = intersectionVertiBottom == 0 ? 0 : MoveSets.InterpolateVertical(kingIndex, intersectionVertiBottom.BitScanReverse);
            intersectionVertiBottom = ValidatePin(intersectionVertiBottom, sameColorPieces, enemyBlockers, kingIndex);


            BitBoard intersectionVertiTop = intersectionsStraight & MoveSets.InterpolateVertical(kingIndex + (7 - y) * 8, kingIndex);
            intersectionVertiTop = intersectionVertiTop == 0 ? 0 : MoveSets.InterpolateVertical(intersectionVertiTop.LowestOne, kingIndex);
            intersectionVertiTop = ValidatePin(intersectionVertiTop, sameColorPieces, enemyBlockers, kingIndex);

            pinLines[0] = intersectionVertiTop;
            pinLines[2] = intersectionHoriEast;
            pinLines[4] = intersectionVertiBottom;
            pinLines[6] = intersectionHoriWest;
        }

        if (intersectionDiags != 0) {
            enemyBlockers = forWhite ? board.BKnightBB | board.BRookBB | board.BPawnBB : board.WKnightBB | board.WRookBB | board.WPawnBB;

            BitBoard antiDiag = BitBoard.AntiDiagonal(x, y);
            int nw = antiDiag.BitScanReverse;
            BitBoard intersectionDiagNW = intersectionDiags & MoveSets.InterpolateAntiDiagonal(nw, kingIndex);
            intersectionDiagNW = intersectionDiagNW == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(intersectionDiagNW.LowestOne, kingIndex);
            intersectionDiagNW = ValidatePin(intersectionDiagNW, sameColorPieces, enemyBlockers, kingIndex);

            int se = antiDiag.LowestOne;
            BitBoard intersectionDiagSE = intersectionDiags & MoveSets.InterpolateAntiDiagonal(kingIndex, se);
            intersectionDiagSE = intersectionDiagSE == 0 ? 0 : MoveSets.InterpolateAntiDiagonal(kingIndex, intersectionDiagSE.BitScanReverse);
            intersectionDiagSE = ValidatePin(intersectionDiagSE, sameColorPieces, enemyBlockers, kingIndex);

            BitBoard diag = BitBoard.Diagonal(x, y);
            int ne = diag.BitScanReverse;
            BitBoard intersectionDiagNE = intersectionDiags & MoveSets.InterpolateDiagonal(ne, kingIndex);
            intersectionDiagNE = intersectionDiagNE == 0 ? 0 : MoveSets.InterpolateDiagonal(intersectionDiagNE.LowestOne, kingIndex);
            intersectionDiagNE = ValidatePin(intersectionDiagNE, sameColorPieces, enemyBlockers, kingIndex);

            int sw = diag.LowestOne;
            BitBoard intersectionDiagSW = intersectionDiags & MoveSets.InterpolateDiagonal(kingIndex, sw);
            intersectionDiagSW = intersectionDiagSW == 0 ? 0 : MoveSets.InterpolateDiagonal(kingIndex, intersectionDiagSW.BitScanReverse);
            intersectionDiagSW = ValidatePin(intersectionDiagSW, sameColorPieces, enemyBlockers, kingIndex);

            pinLines[1] = intersectionDiagNE;
            pinLines[3] = intersectionDiagSE;
            pinLines[5] = intersectionDiagSW;
            pinLines[7] = intersectionDiagNW;
        }

        return pinLines;
    }

    private static BitBoard GetAttackedSqrBB(Span<Vision> visions, bool forWhite) {
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

    private Span<Vision> GenerateVisions(bool forWhite, bool includeCoverage = false) {
        //weil maximal 16 pieces die je ein "Moves" bekommen
        Vision[] possibleMoves = new Vision[16];

        int currIndex = 0;

        if (forWhite) {
            this.GenerateMovesForPiece(this.Board.WRookBB, ref possibleMoves, ref currIndex, Piece.wRook, includeCoverage);
            this.GenerateMovesForPiece(this.Board.WKnightBB, ref possibleMoves, ref currIndex, Piece.wKnight, includeCoverage);
            this.GenerateMovesForPiece(this.Board.WBishopBB, ref possibleMoves, ref currIndex, Piece.wBishop, includeCoverage);
            this.GenerateMovesForPiece(this.Board.WQueenBB, ref possibleMoves, ref currIndex, Piece.wQueen, includeCoverage);
            this.GenerateMovesForPawns(this.Board.WPawnBB, ref possibleMoves, ref currIndex, Piece.wPawn, includeCoverage);
            this.GenerateMovesForKing(this.Board.WKingBB, ref possibleMoves, ref currIndex, Piece.wKing, includeCoverage);

        } else {
            this.GenerateMovesForPiece(this.Board.BRookBB, ref possibleMoves, ref currIndex, Piece.bRook, includeCoverage);
            this.GenerateMovesForPiece(this.Board.BKnightBB, ref possibleMoves, ref currIndex, Piece.bKnight, includeCoverage);
            this.GenerateMovesForPiece(this.Board.BBishopBB, ref possibleMoves, ref currIndex, Piece.bBishop, includeCoverage);
            this.GenerateMovesForPiece(this.Board.BQueenBB, ref possibleMoves, ref currIndex, Piece.bQueen, includeCoverage);
            this.GenerateMovesForPawns(this.Board.BPawnBB, ref possibleMoves, ref currIndex, Piece.bPawn, includeCoverage);
            this.GenerateMovesForKing(this.Board.BKingBB, ref possibleMoves, ref currIndex, Piece.bKing, includeCoverage);
        }

        return possibleMoves.AsSpan(0..currIndex);
    }

    private void GenerateMovesForKing(BitBoard kingBB, ref Vision[] possibleMoves,
            ref int currIndex, Piece king, bool includeCoverage = false) {
        int kingIndex = kingBB.LowestOne;

        Vision vKing = this.GetVisionForPieceAt(kingIndex, king, includeCoverage);

        if (vKing.MoveBB != 0) {
            possibleMoves[currIndex] = vKing;
            currIndex++;
        }
    }

    private void GenerateMovesForPawns(BitBoard pawnBB, ref Vision[] possibleMoves, ref int currIndex, Piece pawn, bool includeCoverage = false) {
        Span<int> pawns = stackalloc int[pawnBB.PopCount];
        _ = pawnBB.FindOnes(pawns);

        for (int i = 0; i < pawns.Length; i++) {
            int posIndex = pawns[i];
            Vision v = this.GetVisionForPieceAt(posIndex, pawn, includeCoverage);

            if (v.MoveBB == 0) {
                continue;
            }
            possibleMoves[currIndex] = v;
            currIndex++;
        }
    }


    private void GenerateMovesForPiece(BitBoard pieceBB, ref Vision[] possibleMoves, ref int currIndex, Piece type, bool includeCoverage = false) {

        Span<int> pieces = stackalloc int[pieceBB.PopCount];
        _ = pieceBB.FindOnes(pieces);

        for (int i = 0; i < pieces.Length; i++) {
            Vision v = this.GetVisionForPieceAt(pieces[i], type, includeCoverage);

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
    private Span<Vision> GenerateMovesForCheck() {
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
        foreach (BitBoard bb in this.checkPieces) combined |= bb;
        int amount = combined.PopCount;

        int posIndex = (this.WhitesTurn ? this.Board.WKingBB : this.Board.BKingBB).LowestOne;
        BitBoard sameColorPieces = this.WhitesTurn ? this.Board.WhitePiecesBB : this.Board.BlackPiecesBB;
        Piece king = this.WhitesTurn ? Piece.wKing : Piece.bKing;

        //weil ein kingmove immer ein valider ausweg ist, castleSqrs=0 weil eh nicht gecastled werden darf
        Vision kingVision = new(
            posIndex,
            MoveSets.GetKingPseudoLegalMoves(posIndex, sameColorPieces, this.enemyControl, 0),
            king
            );
        // kingMobile - der neue anbieter fuer Mobilfunk auf dem Schachbrett
        bool kingMobile = kingVision.MoveBB != 0;//wenn 0: kingVision darf nicht returnt werden

        //double check, king muss bewegt werden
        if (amount > 1) {
            if (kingMobile) return new Vision[] { kingVision };
            this.IsCheckMate = true;
            return [];//entspricht checkmate

        } else {//nur ein piece checkt den king
            return GenerateMovesForSingleCheck(combined, posIndex);
        }
    }

    //muss mit checkmates getestet werden
    /// <summary>
    /// Wird in GenerateMovesForCheck gecalled wenn es nur ein Piece check gibt
    /// </summary>
    /// <returns></returns>
    private Span<Vision> GenerateMovesForSingleCheck(BitBoard combined, int kingIndex) {
        Span<Vision> pseudoLegal = GenerateVisions(this.WhitesTurn);

        Vision[] legal = new Vision[16];
        int currIndex = 0;

        if ((this.checkPieces[0] | this.checkPieces[1]) != 0) {//es ist ein knight oder pawn, dh kein piece kann blocken

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
                    this.checkPieces[0] != 0 && (v.PieceType == Piece.wPawn || v.PieceType == Piece.bPawn)
                    && this.Board.EnPassantSqr > 0
                    && v.MoveBB[this.Board.EnPassantSqr];

                    if (enPassantPawnGivesCheckAndCanBeCapturedByCurrentPawn) {//confirmed pawn

                        //possible EP is on the board
                        int checkPieceIndex = combined.HighestOne;
                        int EPpawnIndex = Chessboard.GetEnPassantPawn(this.Board.EnPassantSqr);

                        //en passant pawn gave check
                        if (EPpawnIndex == checkPieceIndex) {
                            v.MoveBB &= combined;
                            v.MoveBB |= 1ul << this.Board.EnPassantSqr;
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
                this.IsCheckMate = true;
                return [];
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
                this.IsCheckMate = true;
                return [];
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
    private static BitBoard GetCheckLine(int k, int c) {
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


    public Span<Move> GenerateMoves() {

        Span<Vision> attackVisions = GenerateVisions(!this.WhitesTurn, true);
        this.enemyControl = GetAttackedSqrBB(attackVisions, !this.WhitesTurn);

        this.checkPieces = CheckPieces();

        Span<Vision> visions = this.isCheck ? this.GenerateMovesForCheck() : GenerateVisions(this.WhitesTurn);

#pragma warning disable CA2014
        Move[] ret = new Move[96];
        int index = 0;

        for (int i = 0; i < visions.Length; i++) {
            Vision v = visions[i];
            Span<int> moves = stackalloc int[v.MoveBB.PopCount];
            _ = v.MoveBB.FindOnes(moves);

            for (int j = 0; j < v.MoveBB.PopCount; j++) {
                int end = moves[j];

                if (IsPromoting(end, v.PieceType)) {

                    if (this.WhitesTurn) {
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
            }
        }
#pragma warning restore CA2014

        return ret[0..index];
    }

    private static bool IsPromoting(int end, Piece type) {
        return type switch {
            Piece.wPawn => end > 55,
            Piece.bPawn => end < 8,
            _ => false,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vision GetVisionForPieceAt(int i, Piece type, bool includeCoverage = false) {
        BitBoard bb = MoveSets.GetPseudoLegalMoves(this.Board, i, type, this.enemyControl, this.isCheck, includeCoverage);
        var pinLines = type.IsWhite() ? this.pinLines.white : this.pinLines.black;

        //wenn das piece auf dem pinBB liegt, dh es ist gepinnt
        if (GetPinnedPieceBB(this.Board, pinLines)[i] && !includeCoverage) {
            BitBoard pinLine = GetPinLineBB(1ul << i, pinLines);
            bb &= pinLine;
        }

        return new Vision(i, bb, type);
    }
}
