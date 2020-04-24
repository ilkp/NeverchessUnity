using System.Collections.Generic;
using UnityEngine;

public enum PieceCode
{
	EMPTY = 0b0000000,
	W_KING = 0b0000001,
	W_QUEEN = 0b0000010,
	W_PAWN = 0b0000100,
	W_KNIGHT = 0b0001000,
	W_BISHOP = 0b0010000,
	W_ROOK = 0b0100000,
	B_KING = 0b1000001,
	B_QUEEN = 0b1000010,
	B_PAWN = 0b1000100,
	B_KNIGHT = 0b1001000,
	B_BISHOP = 0b1010000,
	B_ROOK = 0b1100000
}

public class Chess
{
	public static int PIECE_CODE_LENGTH = 7;
	public static int BOARD_LENGTH = 8;
	public static int ANN_INPUT_LENGTH = BOARD_LENGTH * BOARD_LENGTH * PIECE_CODE_LENGTH;

	public static void InitBoardData(ref BoardStateData boardStateData)
	{
		boardStateData = new BoardStateData();
		boardStateData._turn = 0;
		boardStateData._enPassant = -1;
		boardStateData._kingMoved = new bool[2] { false, false };
		boardStateData._kRookMoved = new bool[2] { false, false };
		boardStateData._qRookMoved = new bool[2] { false, false };
		boardStateData._pieces = new PieceCode[8 * 8];
		for (int y = 2; y < BOARD_LENGTH - 2; ++y)
		{
			for (int x = 0; x < BOARD_LENGTH; ++x)
			{
				boardStateData._pieces[y * BOARD_LENGTH + x] = PieceCode.EMPTY;
			}
		}
		for (int x = 0; x < BOARD_LENGTH; ++x)
		{
			boardStateData._pieces[BOARD_LENGTH + x] = PieceCode.W_PAWN;
			boardStateData._pieces[BOARD_LENGTH * 6 + x] = PieceCode.B_PAWN;
		}
		boardStateData._pieces[0] = PieceCode.W_ROOK;
		boardStateData._pieces[1] = PieceCode.W_KNIGHT;
		boardStateData._pieces[2] = PieceCode.W_BISHOP;
		boardStateData._pieces[3] = PieceCode.W_QUEEN;
		boardStateData._pieces[4] = PieceCode.W_KING;
		boardStateData._pieces[5] = PieceCode.W_BISHOP;
		boardStateData._pieces[6] = PieceCode.W_KNIGHT;
		boardStateData._pieces[7] = PieceCode.W_ROOK;

		boardStateData._pieces[BOARD_LENGTH * 7 + 0] = PieceCode.B_ROOK;
		boardStateData._pieces[BOARD_LENGTH * 7 + 1] = PieceCode.B_KNIGHT;
		boardStateData._pieces[BOARD_LENGTH * 7 + 2] = PieceCode.B_BISHOP;
		boardStateData._pieces[BOARD_LENGTH * 7 + 3] = PieceCode.B_QUEEN;
		boardStateData._pieces[BOARD_LENGTH * 7 + 4] = PieceCode.B_KING;
		boardStateData._pieces[BOARD_LENGTH * 7 + 5] = PieceCode.B_BISHOP;
		boardStateData._pieces[BOARD_LENGTH * 7 + 6] = PieceCode.B_KNIGHT;
		boardStateData._pieces[BOARD_LENGTH * 7 + 7] = PieceCode.B_ROOK;
	}

	public static int PiecesSide(BoardStateData boardStateData, int x, int y)
	{
		return (int)boardStateData._pieces[y * BOARD_LENGTH + x] >> (PIECE_CODE_LENGTH - 1);
	}

	public static bool MoveIsLegal(MoveData move, BoardStateData boardStateData)
	{
		if (move.xStart < 0 || move.xStart >= BOARD_LENGTH || move.yStart < 0 || move.yStart >= BOARD_LENGTH
			|| move.xEnd < 0 || move.xEnd >= BOARD_LENGTH || move.yEnd < 0 || move.yEnd >= BOARD_LENGTH)
		{
			return false;
		}
		PieceCode pieceCode = boardStateData._pieces[move.yStart * BOARD_LENGTH + move.xStart];
		switch (pieceCode)
		{
			case PieceCode.W_KING:
			case PieceCode.B_KING:
				return MoveIsLegalKing(move, boardStateData._pieces, boardStateData._turn, boardStateData._kingMoved, boardStateData._kRookMoved, boardStateData._qRookMoved);

			case PieceCode.W_QUEEN:
			case PieceCode.B_QUEEN:
				return MoveIsLegalQueen(move, boardStateData._pieces, boardStateData._turn);

			case PieceCode.W_BISHOP:
			case PieceCode.B_BISHOP:
				return MoveIsLegalBishop(move, boardStateData._pieces, boardStateData._turn);

			case PieceCode.W_ROOK:
			case PieceCode.B_ROOK:
				return MoveIsLegalRook(move, boardStateData._pieces, boardStateData._turn);

			case PieceCode.W_KNIGHT:
			case PieceCode.B_KNIGHT:
				return MoveIsLegalKnight(move, boardStateData._pieces, boardStateData._turn);

			case PieceCode.W_PAWN:
			case PieceCode.B_PAWN:
				return MoveIsLegalPawn(move, boardStateData._pieces, boardStateData._turn, boardStateData._enPassant);
		}
		return false;
	}

	public static bool MoveIsLegalKing(MoveData move, PieceCode[] pieces, int turn, bool[] kingMoved, bool[] kRookMoved, bool[] qRookMoved)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		int row = turn == 0 ? 0 : 7;
		// SHORT CASTLE
		if (move.shortCastle
			&& yStart == row
			&& yEnd == row
			&& xStart == 4
			&& (xEnd == xStart + 2)
			&& !kingMoved[turn]
			&& !kRookMoved[turn]
			&& pieces[row * BOARD_LENGTH + 5] == PieceCode.EMPTY
			&& pieces[row * BOARD_LENGTH + 6] == PieceCode.EMPTY
			&& !SquareThreatened(pieces, turn, 4, row)
			&& !SquareThreatened(pieces, turn, 5, row)
			&& !SquareThreatened(pieces, turn, 6, row))
		{
			return true;
		}
		// LONG CASTLE
		else if (move.longCastle
			&& yStart == row
			&& yEnd == row
			&& xStart == 4
			&& xEnd == xStart - 2
			&& !kingMoved[turn]
			&& !qRookMoved[turn]
			&& pieces[row * BOARD_LENGTH + 1] == PieceCode.EMPTY
			&& pieces[row * BOARD_LENGTH + 2] == PieceCode.EMPTY
			&& pieces[row * BOARD_LENGTH + 3] == PieceCode.EMPTY
			&& !SquareThreatened(pieces, turn, 2, row)
			&& !SquareThreatened(pieces, turn, 3, row)
			&& !SquareThreatened(pieces, turn, 4, row))
		{
			return true;
		}
		else
		{
			return (Mathf.Abs(xEnd - xStart) == 0 || Mathf.Abs(xEnd - xStart) == 1)
				&& (Mathf.Abs(yEnd - yStart) == 0 || Mathf.Abs(yEnd - yStart) == 1)
				&& ((pieces[yEnd * BOARD_LENGTH + xEnd] == PieceCode.EMPTY || ((int)pieces[yEnd * BOARD_LENGTH + xEnd] >> (PIECE_CODE_LENGTH - 1) != turn)));
		}
	}

	public static bool MoveIsLegalQueen(MoveData move, PieceCode[] pieces, int turn)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (pieces[yEnd * BOARD_LENGTH + xEnd] != PieceCode.EMPTY && ((int)pieces[yEnd * BOARD_LENGTH + xEnd] >> (PIECE_CODE_LENGTH - 1)) == turn)
		{
			return false;
		}
		if ((xStart == xEnd ^ yStart == yEnd) || (Mathf.Abs(xEnd - xStart) == Mathf.Abs(yEnd - yStart)))
		{
			return SquaresAreEmpty(pieces, xStart, yStart, xEnd, yEnd);

		}
		return false;
	}

	public static bool MoveIsLegalBishop(MoveData move, PieceCode[] pieces, int turn)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (pieces[yEnd * BOARD_LENGTH + xEnd] != PieceCode.EMPTY && ((int)pieces[yEnd * BOARD_LENGTH + xEnd] >> (PIECE_CODE_LENGTH - 1)) == turn)
		{
			return false;
		}
		if (Mathf.Abs(xEnd - xStart) == Mathf.Abs(yEnd - yStart))
		{
			return SquaresAreEmpty(pieces, xStart, yStart, xEnd, yEnd);
		}
		return false;
	}

	public static bool MoveIsLegalRook(MoveData move, PieceCode[] pieces, int turn)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (pieces[yEnd * BOARD_LENGTH + xEnd] != PieceCode.EMPTY && ((int)pieces[yEnd * BOARD_LENGTH + xEnd] >> (PIECE_CODE_LENGTH - 1)) == turn)
		{
			return false;
		}
		if (((xStart == xEnd) && (yStart != yEnd) || (xStart != xEnd) && (yStart == yEnd)))
		{
			return SquaresAreEmpty(pieces, xStart, yStart, xEnd, yEnd);
		}
		return false;
	}

	public static bool MoveIsLegalKnight(MoveData move, PieceCode[] pieces, int turn)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (pieces[yEnd * BOARD_LENGTH + xEnd] != PieceCode.EMPTY && ((int)pieces[yEnd * BOARD_LENGTH + xEnd] >> (PIECE_CODE_LENGTH - 1)) == turn)
		{
			return false;
		}
		if (xEnd == xStart + 2 || xEnd == xStart - 2)
		{
			if (yEnd == yStart + 1 || yEnd == yStart - 1)
			{
				return true;
			}
		}
		else if (xEnd == xStart + 1 || xEnd == xStart - 1)
		{
			if (yEnd == yStart + 2 || yEnd == yStart - 2)
			{
				return true;
			}
		}
		return false;
	}

	public static bool MoveIsLegalPawn(MoveData move, PieceCode[] pieces, int turn, int enPassant)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		int dir = turn == 0 ? 1 : -1;
		// EN PASSANT
		if (move.enPassant
			&& xEnd == enPassant
			&& (xEnd == xStart + 1 || xEnd == xStart - 1)
			&& yEnd == yStart + dir
			&& move.yStart == (turn == 0 ? 4 : 3))
		{
			return true;
		}
		// DOUBLE MOVE
		else if (move.doublePawnMove
			&& yStart == (turn == 0 ? 1 : 6)
			&& xEnd == xStart
			&& yEnd == yStart + 2 * dir
			&& pieces[(yStart + dir) * BOARD_LENGTH + xStart] == PieceCode.EMPTY
			&& pieces[(yStart + 2 * dir) * BOARD_LENGTH + xStart] == PieceCode.EMPTY)
		{
			return true;
		}
		// TAKE
		else if ((xEnd == xStart + 1 || xEnd == xStart - 1)
			&& yEnd == yStart + dir
			&& pieces[yEnd * BOARD_LENGTH + xEnd] != PieceCode.EMPTY
			&& ((int)pieces[yEnd * BOARD_LENGTH + xEnd] >> (PIECE_CODE_LENGTH - 1)) != turn)
		{
			return true;
		}
		// NORMAL MOVE
		else if (xEnd == xStart && yEnd == yStart + dir
			&& pieces[yEnd * BOARD_LENGTH + xEnd] == PieceCode.EMPTY)
		{
			return true;
		}
		return false;
	}

	public static bool SquareThreatened(PieceCode[] pieces, int turn, int targetX, int targetY)
	{
		for (int yBoard = 0; yBoard < BOARD_LENGTH; ++yBoard)
		{
			for (int xBoard = 0; xBoard < BOARD_LENGTH; ++xBoard)
			{
				if (pieces[yBoard * BOARD_LENGTH + xBoard] != PieceCode.EMPTY
					&& (int)pieces[yBoard * BOARD_LENGTH + xBoard] >> (PIECE_CODE_LENGTH - 1) != turn
					&& PieceCanThreatenSquare(pieces, turn, xBoard, yBoard, targetX, targetY))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool PieceCanThreatenSquare(PieceCode[] pieces, int turn, int pieceX, int pieceY, int targetX, int targetY)
	{
		PieceCode piece = pieces[pieceY * BOARD_LENGTH + pieceX];
		switch (piece)
		{
			case PieceCode.W_KING:
			case PieceCode.B_KING:
				return KingCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_QUEEN:
			case PieceCode.B_QUEEN:
				return QueenCanThreatenSquare(pieces, pieceX, pieceY, targetX, targetY);
			case PieceCode.W_ROOK:
			case PieceCode.B_ROOK:
				return RookCanThreatenSquare(pieces, pieceX, pieceY, targetX, targetY);
			case PieceCode.W_BISHOP:
			case PieceCode.B_BISHOP:
				return BishopCanThreatenSquare(pieces, pieceX, pieceY, targetX, targetY);
			case PieceCode.W_KNIGHT:
			case PieceCode.B_KNIGHT:
				return KnightCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_PAWN:
			case PieceCode.B_PAWN:
				return PawnCanThreatenSquare(turn, pieceX, pieceY, targetX, targetY);
		}
		return false;
	}

	public static bool KingCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		return Mathf.Abs(targetX - pieceX) <= 1 && Mathf.Abs(targetY - pieceY) <= 1;
	}

	public static bool QueenCanThreatenSquare(PieceCode[] pieces, int pieceX, int pieceY, int targetX, int targetY)
	{
		if (!(targetX == pieceX || targetY == pieceY || (Mathf.Abs(targetX - pieceX) == Mathf.Abs(targetY - pieceY))))
		{
			return false;
		}
		int xDir = 0;
		int yDir = 0;
		if (targetX == pieceX)
		{
			yDir = (targetY - pieceY) / Mathf.Abs(targetY - pieceY);
		}
		else if (targetY == pieceY)
		{
			xDir = (targetX - pieceX) / Mathf.Abs(targetX - pieceX);
		}
		else
		{
			xDir = (targetX - pieceX) / Mathf.Abs(targetX - pieceX);
			yDir = (targetY - pieceY) / Mathf.Abs(targetY - pieceY);
		}
		int x = pieceX + xDir;
		int y = pieceY + yDir;
		while (!(x == targetX && y == targetY))
		{
			if (pieces[y * BOARD_LENGTH + x] != PieceCode.EMPTY)
			{
				return false;
			}
			x += xDir;
			y += yDir;
		}
		return true;
	}

	public static bool BishopCanThreatenSquare(PieceCode[] pieces, int pieceX, int pieceY, int targetX, int targetY)
	{
		if (Mathf.Abs(targetX - pieceX) != Mathf.Abs(targetY - pieceY))
		{
			return false;
		}
		int xDir = (targetX - pieceX) / Mathf.Abs(targetX - pieceX);
		int yDir = (targetY - pieceY) / Mathf.Abs(targetY - pieceY);
		int x = pieceX + xDir;
		int y = pieceY + yDir;
		while (!(x == targetX && y == targetY))
		{
			if (pieces[y * BOARD_LENGTH + x] != PieceCode.EMPTY)
			{
				return false;
			}
			x += xDir;
			y += yDir;
		}
		return true;
	}

	public static bool KnightCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		if (Mathf.Abs(targetX - pieceX) == 2 && Mathf.Abs(targetY - pieceY) == 1
			|| Mathf.Abs(targetX - pieceX) == 1 && Mathf.Abs(targetY - pieceY) == 2)
		{
			return true;
		}
		return false;
	}

	public static bool RookCanThreatenSquare(PieceCode[] pieces, int pieceX, int pieceY, int targetX, int targetY)
	{
		if (!(targetX == pieceX || targetY == pieceY))
		{
			return false;
		}
		int xDir = 0;
		int yDir = 0;
		if (targetX != pieceX)
		{
			xDir = (targetX - pieceX) / Mathf.Abs(targetX - pieceX);
		}
		if (targetY != pieceY)
		{
			yDir = (targetY - pieceY) / Mathf.Abs(targetY - pieceY);
		}
		int x = pieceX + xDir;
		int y = pieceY + yDir;
		while (!(x == targetX && y == targetY))
		{
			if (pieces[y * BOARD_LENGTH + x] != PieceCode.EMPTY)
			{
				return false;
			}
			x += xDir;
			y += yDir;
		}
		return true;
	}

	public static bool PawnCanThreatenSquare(int turn, int pieceX, int pieceY, int targetX, int targetY)
	{
		int yDir = turn == 1 ? 1 : -1;
		if (pieceY + yDir == targetY && Mathf.Abs(targetX - pieceX) == 1)
		{
			return true;
		}
		return false;
	}

	public static bool SquaresAreEmpty(PieceCode[] pieces, int xStart, int yStart, int xEnd, int yEnd)
	{
		int xDir = 0;
		int yDir = 0;
		if (xEnd == xStart)
		{
			yDir = (yEnd - yStart) / Mathf.Abs(yEnd - yStart);
		}
		else if (yEnd == yStart)
		{
			xDir = (xEnd - xStart) / Mathf.Abs(xEnd - xStart);
		}
		else if (Mathf.Abs(xEnd - xStart) == Mathf.Abs(yEnd - yStart))
		{
			xDir = (xEnd - xStart) / Mathf.Abs(xEnd - xStart);
			yDir = (yEnd - yStart) / Mathf.Abs(yEnd - yStart);
		}
		else
		{
			return false;
		}
		int x = xStart + xDir;
		int y = yStart + yDir;
		while (x != xEnd || y != yEnd)
		{
			if (pieces[y * BOARD_LENGTH + x] != PieceCode.EMPTY)
			{
				return false;
			}
			x += xDir;
			y += yDir;
		}
		return true;
	}

	public static bool MoveIsSafe(MoveData move, BoardStateData boardStateData)
	{
		BoardStateData temp = new BoardStateData(boardStateData);
		PlayMove(ref temp, move);
		int[] kingCoord = FindKing(temp._pieces, boardStateData._turn);
		return !SquareThreatened(temp._pieces, boardStateData._turn, kingCoord[0], kingCoord[1]);
	}

	public static List<BoardStateData> FilterMoves(BoardStateData boardStateData, ref List<MoveData> moves)
	{
		BoardStateData temp;
		List<BoardStateData> newStates = new List<BoardStateData>();
		PieceCode kingCode = boardStateData._turn == 1 ? PieceCode.B_KING : PieceCode.W_KING;
		bool kingThreatened;
		int[] kingPos = FindKing(boardStateData._pieces, boardStateData._turn);
		int[] kingPosMoved;
		for (int i = moves.Count - 1; i > -1; --i)
		{
			temp = new BoardStateData(boardStateData);
			PlayMove(ref temp, moves[i]);
			if (temp._pieces[kingPos[0] + kingPos[1] * BOARD_LENGTH] == kingCode)
			{
				kingThreatened = SquareThreatened(temp._pieces, boardStateData._turn, kingPos[0], kingPos[1]);
			}
			else
			{
				kingPosMoved = FindKing(temp._pieces, boardStateData._turn);
				kingThreatened = SquareThreatened(temp._pieces, boardStateData._turn, kingPosMoved[0], kingPosMoved[1]);
			}
			if (kingThreatened)
			{
				moves.RemoveAt(i);
			}
			else
			{
				newStates.Add(temp);
			}
		}
		return newStates;
	}

	public static int[] FindKing(PieceCode[] pieces, int turn)
	{
		int[] coordinates = new int[2] { -1, -1 };
		PieceCode king = turn == 0 ? PieceCode.W_KING : PieceCode.B_KING;
		for (int y = 0; y < BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < BOARD_LENGTH; ++x)
			{
				if (pieces[y * BOARD_LENGTH + x] != PieceCode.EMPTY && pieces[y * BOARD_LENGTH + x] == king)
				{
					coordinates[0] = x;
					coordinates[1] = y;
					return coordinates;
				}
			}
		}
		return coordinates;
	}

	public static void PlayMove(ref BoardStateData boardStateData, MoveData move)
	{
		boardStateData._enPassant = -1;
		if (move.enPassant)
		{
			boardStateData._pieces[move.yStart * BOARD_LENGTH + move.xEnd] = PieceCode.EMPTY;
		}
		else if (move.doublePawnMove)
		{
			boardStateData._enPassant = move.xStart;
		}
		if (move.longCastle)
		{
			int y = boardStateData._turn == 1 ? BOARD_LENGTH - 1 : 0;
			boardStateData._pieces[y * BOARD_LENGTH + 2] = boardStateData._pieces[y * BOARD_LENGTH + 4];
			boardStateData._pieces[y * BOARD_LENGTH + 3] = boardStateData._pieces[y * BOARD_LENGTH];
			boardStateData._pieces[y * BOARD_LENGTH + 4] = PieceCode.EMPTY;
			boardStateData._pieces[y * BOARD_LENGTH] = PieceCode.EMPTY;
			boardStateData._kingMoved[boardStateData._turn] = true;
			boardStateData._qRookMoved[boardStateData._turn] = true;
		}
		else if (move.shortCastle)
		{
			int y = boardStateData._turn == 1 ? BOARD_LENGTH - 1 : 0;
			boardStateData._pieces[y * BOARD_LENGTH + 6] = boardStateData._pieces[y * BOARD_LENGTH + 4];
			boardStateData._pieces[y * BOARD_LENGTH + 5] = boardStateData._pieces[y * BOARD_LENGTH + 7];
			boardStateData._pieces[y * BOARD_LENGTH + 4] = PieceCode.EMPTY;
			boardStateData._pieces[y * BOARD_LENGTH + 7] = PieceCode.EMPTY;
			boardStateData._kingMoved[boardStateData._turn] = true;
			boardStateData._kRookMoved[boardStateData._turn] = true;
		}
		else
		{
			PieceCode piece = boardStateData._pieces[move.yStart * BOARD_LENGTH + move.xStart];
			if (piece == PieceCode.W_KING || piece == PieceCode.B_KING)
			{
				boardStateData._kingMoved[boardStateData._turn] = true;
			}
			else if (piece == PieceCode.W_ROOK || piece == PieceCode.B_ROOK)
			{
				if (move.xStart == 0)
				{
					boardStateData._qRookMoved[boardStateData._turn] = true;
				}
				if (move.xStart == 7)
				{
					boardStateData._kRookMoved[boardStateData._turn] = true;
				}
			}
			boardStateData._pieces[move.yEnd * BOARD_LENGTH + move.xEnd] = boardStateData._pieces[move.yStart * BOARD_LENGTH + move.xStart];
			boardStateData._pieces[move.yStart * BOARD_LENGTH + move.xStart] = PieceCode.EMPTY;
			if (move.upgrade != PieceCode.EMPTY)
			{
				boardStateData._pieces[move.yEnd * BOARD_LENGTH + move.xEnd] = move.upgrade;
			}
		}
		boardStateData._turn = (boardStateData._turn + 1) % 2;
	}

	public static bool ShortCastleAvailable(BoardStateData boardStateData)
	{
		int row = boardStateData._turn == 1 ? (BOARD_LENGTH - 1) : 0;
		return !boardStateData._kingMoved[boardStateData._turn]
			&& !boardStateData._kRookMoved[boardStateData._turn]
			&& boardStateData._pieces[row * BOARD_LENGTH + 5] == PieceCode.EMPTY
			&& boardStateData._pieces[row * BOARD_LENGTH + 6] == PieceCode.EMPTY
			&& !SquareThreatened(boardStateData._pieces, boardStateData._turn, 4, row)
			&& !SquareThreatened(boardStateData._pieces, boardStateData._turn, 5, row)
			&& !SquareThreatened(boardStateData._pieces, boardStateData._turn, 6, row);
	}

	public static bool LongCastleAvailable(BoardStateData boardStateData)
	{
		int row = boardStateData._turn == 1 ? (BOARD_LENGTH - 1) : 0;
		return !boardStateData._kingMoved[boardStateData._turn]
			&& !boardStateData._qRookMoved[boardStateData._turn]
			&& boardStateData._pieces[row * BOARD_LENGTH + 1] == PieceCode.EMPTY
			&& boardStateData._pieces[row * BOARD_LENGTH + 2] == PieceCode.EMPTY
			&& boardStateData._pieces[row * BOARD_LENGTH + 3] == PieceCode.EMPTY
			&& !SquareThreatened(boardStateData._pieces, boardStateData._turn, 2, row)
			&& !SquareThreatened(boardStateData._pieces, boardStateData._turn, 3, row)
			&& !SquareThreatened(boardStateData._pieces, boardStateData._turn, 4, row);
	}

	public static List<MoveData> GenRawMoves(BoardStateData boardStateData)
	{
		List<MoveData> moves = new List<MoveData>();
		PieceCode piece;
		for (int y = 0; y < BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < BOARD_LENGTH; ++x)
			{
				piece = boardStateData._pieces[y * BOARD_LENGTH + x];
				if (piece == PieceCode.EMPTY)
				{
					continue;
				}
				if (((int)piece >> (PIECE_CODE_LENGTH - 1)) != boardStateData._turn)
				{
					continue;
				}
				GenRawPieceMoves(boardStateData, ref moves, x, y);
			}
		}
		return moves;
	}

	public static void GenRawPieceMoves(BoardStateData boardStateData, ref List<MoveData> moves, int x, int y)
	{
		switch (boardStateData._pieces[y * BOARD_LENGTH + x])
		{
			case PieceCode.EMPTY:
				break;
			case PieceCode.W_KING:
			case PieceCode.B_KING:
				GenRawMovesKing(ref moves, boardStateData, x, y);
				break;
			case PieceCode.W_QUEEN:
			case PieceCode.B_QUEEN:
				GenRawMovesQueen(ref moves, boardStateData._pieces, boardStateData._turn, x, y);
				break;
			case PieceCode.W_ROOK:
			case PieceCode.B_ROOK:
				GenRawMovesRook(ref moves, boardStateData._pieces, boardStateData._turn, x, y);
				break;
			case PieceCode.W_BISHOP:
			case PieceCode.B_BISHOP:
				GenRawMovesBishop(ref moves, boardStateData._pieces, boardStateData._turn, x, y);
				break;
			case PieceCode.W_KNIGHT:
			case PieceCode.B_KNIGHT:
				GenRawMovesKnight(ref moves, boardStateData._pieces, boardStateData._turn, x, y);
				break;
			case PieceCode.W_PAWN:
			case PieceCode.B_PAWN:
				GenRawMovesPawn(ref moves, boardStateData._pieces, boardStateData._enPassant, boardStateData._turn, x, y);
				break;
		}
	}

	public static void GenRawMovesKing(ref List<MoveData> moves, BoardStateData boardStateData, int pieceX, int pieceY)
	{
		for (int y = -1; y < 2; ++y)
		{
			for (int x = -1; x < 2; ++x)
			{
				if (!(x == 0 && y == 0)
					&& pieceX + x >= 0
					&& pieceX + x < BOARD_LENGTH
					&& pieceY + y >= 0
					&& pieceY + y < BOARD_LENGTH
					&& (boardStateData._pieces[(pieceY + y) * BOARD_LENGTH + pieceX + x] == PieceCode.EMPTY
						|| ((int)boardStateData._pieces[(pieceY + y) * BOARD_LENGTH + pieceX + x] >> (PIECE_CODE_LENGTH - 1)) != boardStateData._turn)
					)
				{
					moves.Add(new MoveData(pieceX, pieceY, pieceX + x, pieceY + y));
				}
			}
		}
		// Castles
		int row = boardStateData._turn == 1 ? 7 : 0;
		if (ShortCastleAvailable(boardStateData))
		{
			MoveData sc = new MoveData(4, row, 6, row);
			sc.shortCastle = true;
			moves.Add(sc);
		}
		if (LongCastleAvailable(boardStateData))
		{
			MoveData lc = new MoveData(4, row, 2, row);
			lc.longCastle = true;
			moves.Add(lc);
		}
	}

	public static void GenRawMovesQueen(ref List<MoveData> moves, PieceCode[] pieces, int turn, int pieceX, int pieceY)
	{
		GenMovesDir(ref moves, pieces, -1, 0,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 0, 1,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 1, 0,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 0, -1,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, -1, -1,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, -1, 1,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 1, 1,	turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 1, -1,	turn, pieceX, pieceY);
	}

	public static void GenRawMovesBishop(ref List<MoveData> moves, PieceCode[] pieces, int turn, int pieceX, int pieceY)
	{
		GenMovesDir(ref moves, pieces, -1, -1, turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, -1, 1, turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 1, 1, turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 1, -1, turn, pieceX, pieceY);
	}

	public static void GenRawMovesRook(ref List<MoveData> moves, PieceCode[] pieces, int turn, int pieceX, int pieceY)
	{
		GenMovesDir(ref moves, pieces, -1, 0, turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 0, 1, turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 1, 0, turn, pieceX, pieceY);
		GenMovesDir(ref moves, pieces, 0, -1, turn, pieceX, pieceY);
	}

	public static void GenRawMovesKnight(ref List<MoveData> moves, PieceCode[] pieces, int turn, int pieceX, int pieceY)
	{
		int x = pieceX - 2;
		int y = pieceY - 1;
		if (x > -1 && y > -1 && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		y = pieceY + 1;
		if (x > -1 && y < BOARD_LENGTH && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		x = pieceX + 2;
		if (x < BOARD_LENGTH && y < BOARD_LENGTH && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		y = pieceY - 1;
		if (x < BOARD_LENGTH && y < -1 && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		x = pieceX - 1;
		y = pieceY - 2;
		if (x > -1 && y > -1 && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		x = pieceX + 1;
		if (x < BOARD_LENGTH && y > -1 && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		y = pieceY + 2;
		if (x < BOARD_LENGTH && y < BOARD_LENGTH && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}

		x = pieceX - 1;
		if (x > -1 && y < BOARD_LENGTH && SquareIsEmptyOrOpponent(pieces[y * BOARD_LENGTH + x], turn))
		{
			moves.Add(new MoveData(pieceX, pieceY, x, y));
		}
	}

	public static void GenRawMovesPawn(ref List<MoveData> moves, PieceCode[] pieces, int enPassant, int turn, int pieceX, int pieceY)
	{
		int yDir = turn == 1 ? -1 : 1;
		if (pieces[(pieceY + yDir) * BOARD_LENGTH + pieceX] == PieceCode.EMPTY)
		{
			// regular move
			MoveData baseMove = new MoveData(pieceX, pieceY, pieceX, pieceY + yDir);
			if ((turn == 1 && pieceY + yDir == 0) || (turn == 0 && pieceY + yDir == BOARD_LENGTH - 1))
			{
				// upgrade if at the end of the board
				baseMove.upgrade = turn == 1 ? PieceCode.B_QUEEN : PieceCode.W_QUEEN;
				MoveData upgradeToKnight = new MoveData(pieceX, pieceY, pieceX, pieceY + yDir);
				upgradeToKnight.upgrade = turn == 1 ? PieceCode.B_KNIGHT : PieceCode.W_KNIGHT;
				moves.Add(upgradeToKnight);
			}
			moves.Add(baseMove);
			if ((turn == 0 && pieceY == 1 || turn == 1 && pieceY == 6) && pieces[(pieceY + 2 * yDir) * BOARD_LENGTH + pieceX] == PieceCode.EMPTY)
			{
				// double move
				MoveData moveDouble = new MoveData(pieceX, pieceY, pieceX, pieceY + 2 * yDir);
				moveDouble.doublePawnMove = true;
				moves.Add(moveDouble);
			}
		}
		if (pieceX > 0 && pieces[(pieceY + yDir) * BOARD_LENGTH + pieceX - 1] != PieceCode.EMPTY && ((int)pieces[(pieceY + yDir) * BOARD_LENGTH + pieceX - 1] >> (PIECE_CODE_LENGTH - 1)) != turn)
		{
			// take towards low x coord
			MoveData move = new MoveData(pieceX, pieceY, pieceX - 1, pieceY + yDir);
			if ((turn == 1 && pieceY + yDir == 0) || (turn == 0 && pieceY + yDir == 7))
			{
				// upgrade if at the end of the board
				move.upgrade = turn == 1 ? PieceCode.B_QUEEN : PieceCode.W_QUEEN;
				MoveData upgradeToKnight = new MoveData(pieceX, pieceY, pieceX - 1, pieceY + yDir);
				upgradeToKnight.upgrade = turn == 1 ? PieceCode.B_KNIGHT : PieceCode.W_KNIGHT;
				moves.Add(upgradeToKnight);
			}
			moves.Add(move);
		}
		if (pieceX < BOARD_LENGTH - 1 && pieces[(pieceY + yDir) * BOARD_LENGTH + pieceX + 1] != PieceCode.EMPTY && ((int)pieces[(pieceY + yDir) * BOARD_LENGTH + pieceX + 1] >> (PIECE_CODE_LENGTH - 1)) != turn)
		{
			// take towards high x coord
			MoveData move = new MoveData(pieceX, pieceY, pieceX + 1, pieceY + yDir);
			if ((turn == 1 && pieceY + yDir == 0) || (turn == 0 && pieceY + yDir == 7))
			{
				// upgrade if at the end of the board
				move.upgrade = turn == 1 ? PieceCode.B_QUEEN : PieceCode.W_QUEEN;
				MoveData upgradeToKnight = new MoveData(pieceX, pieceY, pieceX + 1, pieceY + yDir);
				upgradeToKnight.upgrade = turn == 1 ? PieceCode.B_KNIGHT : PieceCode.W_KNIGHT;
				moves.Add(upgradeToKnight);
			}
			moves.Add(move);
		}
		if (enPassant != -1)
		{
			if (turn == 1 && pieceY == 3 && (pieceX == enPassant - 1 || pieceX == enPassant + 1))
			{
				MoveData move = new MoveData(pieceX, pieceY, enPassant, pieceY + yDir);
				move.enPassant = true;
				moves.Add(move);
			}
			else if (turn == 0 && pieceY == 4 && (pieceX == enPassant - 1 || pieceX == enPassant + 1))
			{
				MoveData move = new MoveData(pieceX, pieceY, enPassant, pieceY + yDir);
				move.enPassant = true;
				moves.Add(move);
			}
		}
	}

	public static void GenMovesDir(ref List<MoveData> moves, PieceCode[] pieces, int xDir, int yDir, int turn, int pieceX, int pieceY)
	{
		int x = pieceX + xDir;
		int y = pieceY + yDir;
		while (x > -1 && x < BOARD_LENGTH && y > -1 && y < BOARD_LENGTH)
		{
			if (pieces[y * BOARD_LENGTH + x] != PieceCode.EMPTY)
			{
				if ((int)pieces[y * BOARD_LENGTH + x] >> (PIECE_CODE_LENGTH - 1) != turn)
				{
					moves.Add(new MoveData(pieceX, pieceY, x, y));
				}
				break;
			}
			moves.Add(new MoveData(pieceX, pieceY, x, y));
			x += xDir;
			y += yDir;
		}
	}

	public static bool PieceIsOpponent(PieceCode piece, int turn)
	{
		return piece != PieceCode.EMPTY && ((int)piece >> (PIECE_CODE_LENGTH - 1)) != turn;
	}

	public static bool SquareIsEmptyOrOpponent(PieceCode piece, int turn)
	{
		return piece == PieceCode.EMPTY || ((int)piece >> (PIECE_CODE_LENGTH - 1)) != turn;
	}
}
