
using System;

public class BoardStateData
{
	public PieceCode[] _pieces;
	public int _turn;
	public bool[] _kingMoved;
	public bool[] _kRookMoved;
	public bool[] _qRookMoved;
	public int _enPassant;

	private ulong[] zobristPieceValues = new ulong[Chess.BOARD_LENGTH * Chess.BOARD_LENGTH * 12];
	private ulong[] zobristTurnValues = new ulong[2];
	private ulong[] zobristKingMovedValues = new ulong[2];
	private ulong[] zobristQRookMovedValues = new ulong[2];
	private ulong[] zobristKRookMovedValues = new ulong[2];
	private ulong[] zobristEnPassantValues = new ulong[9];

	public BoardStateData()
	{
		GenerateZobristValues();
	}

	public BoardStateData (BoardStateData rhs)
	{
		_pieces = new PieceCode[Chess.BOARD_LENGTH * Chess.BOARD_LENGTH];
		_kingMoved = new bool[2];
		_kRookMoved = new bool[2];
		_qRookMoved = new bool[2];
		for (int y = 0; y < Chess.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Chess.BOARD_LENGTH; ++x)
			{
				_pieces[y * Chess.BOARD_LENGTH + x] = rhs._pieces[y * Chess.BOARD_LENGTH + x];
			}
		}
		_enPassant = rhs._enPassant;
		_kingMoved[0] = rhs._kingMoved[0];
		_kingMoved[1] = rhs._kingMoved[1];
		_kRookMoved[0] = rhs._kRookMoved[0];
		_kRookMoved[1] = rhs._kRookMoved[1];
		_qRookMoved[0] = rhs._qRookMoved[0];
		_qRookMoved[1] = rhs._qRookMoved[1];
		_turn = rhs._turn;
	}

	public static bool operator == (BoardStateData lhs, BoardStateData rhs)
	{
		for (int y = 0; y < Chess.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Chess.BOARD_LENGTH; ++x)
			{
				if (lhs._pieces[y * Chess.BOARD_LENGTH + x] != rhs._pieces[y * Chess.BOARD_LENGTH + x])
				{
					return false;
				}
			}
		}
		return lhs._turn == rhs._turn
			&& lhs._enPassant == rhs._enPassant
			&& lhs._kingMoved[0] == rhs._kingMoved[0]
			&& lhs._kingMoved[1] == rhs._kingMoved[1]
			&& lhs._kRookMoved[0] == rhs._kRookMoved[0]
			&& lhs._kRookMoved[1] == rhs._kRookMoved[1]
			&& lhs._qRookMoved[0] == rhs._qRookMoved[0]
			&& lhs._qRookMoved[1] == rhs._qRookMoved[1];
	}

	private void GenerateZobristValues()
	{
		Random rand = new Random();
		ulong v = 0;
		for (int i = 0; i < Chess.BOARD_LENGTH * Chess.BOARD_LENGTH * 12; ++i)
		{
			do
			{
				v = Convert.ToUInt64(rand.Next(Int32.MaxValue));
				v = v << 32;
				v = v | (uint)rand.Next(Int32.MaxValue);
			} while (ZobristValueExists(v));
			zobristPieceValues[i] = v;
		}
		for (int i = 0; i < Chess.BOARD_LENGTH + 1; ++i)
		{
			do
			{
				v = Convert.ToUInt64(rand.Next(Int32.MaxValue));
				v = v << 32;
				v = v | (uint)rand.Next(Int32.MaxValue);
			} while (ZobristValueExists(v));
			zobristEnPassantValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = Convert.ToUInt64(rand.Next(Int32.MaxValue));
				v = v << 32;
				v = v | (uint)rand.Next(Int32.MaxValue);
			} while (ZobristValueExists(v));
			zobristTurnValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = Convert.ToUInt64(rand.Next(Int32.MaxValue));
				v = v << 32;
				v = v | (uint)rand.Next(Int32.MaxValue);
			} while (ZobristValueExists(v));
			zobristKingMovedValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = Convert.ToUInt64(rand.Next(Int32.MaxValue));
				v = v << 32;
				v = v | (uint)rand.Next(Int32.MaxValue);
			} while (ZobristValueExists(v));
			zobristQRookMovedValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = Convert.ToUInt64(rand.Next(Int32.MaxValue));
				v = v << 32;
				v = v | (uint)rand.Next(Int32.MaxValue);
			} while (ZobristValueExists(v));
			zobristKRookMovedValues[i] = v;
		}
	}

	public ulong ZobristHash()
	{
		ulong h = 0;
		PieceCode piece;
		int intPiece = 0;
		h = h ^ zobristTurnValues[(int)_turn];
		for (int i = 0; i < Chess.BOARD_LENGTH * Chess.BOARD_LENGTH; ++i)
		{
			if (_pieces[i] != PieceCode.EMPTY)
			{
				piece = _pieces[i];
				switch (piece)
				{
					case PieceCode.W_KING:
						intPiece = 0;
						break;
					case PieceCode.W_QUEEN:
						intPiece = 1;
						break;
					case PieceCode.W_PAWN:
						intPiece = 2;
						break;
					case PieceCode.W_KNIGHT:
						intPiece = 3;
						break;
					case PieceCode.W_BISHOP:
						intPiece = 4;
						break;
					case PieceCode.W_ROOK:
						intPiece = 5;
						break;
					case PieceCode.B_KING:
						intPiece = 6;
						break;
					case PieceCode.B_QUEEN:
						intPiece = 7;
						break;
					case PieceCode.B_PAWN:
						intPiece = 8;
						break;
					case PieceCode.B_KNIGHT:
						intPiece = 9;
						break;
					case PieceCode.B_BISHOP:
						intPiece = 10;
						break;
					case PieceCode.B_ROOK:
						intPiece = 11;
						break;
				}
				h = h ^ zobristPieceValues[(i * 12) + intPiece];
			}
		}
		for (int i = 0; i < Chess.BOARD_LENGTH + 1; ++i)
		{
			int enPassant = _enPassant + 1;
			h = h ^ zobristEnPassantValues[enPassant];
		}
		h = h ^ zobristKingMovedValues[0];
		h = h ^ zobristKingMovedValues[1];
		h = h ^ zobristQRookMovedValues[0];
		h = h ^ zobristQRookMovedValues[1];
		h = h ^ zobristQRookMovedValues[0];
		h = h ^ zobristQRookMovedValues[1];

		return h;
	}

	private bool ZobristValueExists(ulong v)
	{
		for (int i = 0; i < Chess.BOARD_LENGTH * Chess.BOARD_LENGTH * 12; ++i)
		{
			if (zobristPieceValues[i] == v)
			{
				return true;
			}
		}
		for (int i = 0; i < Chess.BOARD_LENGTH + 1; ++i)
		{
			if (zobristEnPassantValues[i] == v)
			{
				return true;
			}
		}
		if (zobristTurnValues[0] == v || zobristTurnValues[1] == v)
		{
			return true;
		}
		if (zobristKingMovedValues[0] == v || zobristKingMovedValues[1] == v)
		{
			return true;
		}
		if (zobristQRookMovedValues[0] == v || zobristQRookMovedValues[1] == v)
		{
			return true;
		}
		if (zobristKRookMovedValues[0] == v || zobristKRookMovedValues[1] == v)
		{
			return true;
		}
		return false;
	}

	public static bool operator != (BoardStateData lhs, BoardStateData rhs)
	{
		return !(lhs == rhs);
	}
}