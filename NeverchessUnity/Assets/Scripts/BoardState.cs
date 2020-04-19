
public class Constants
{
	public static int PIECE_CODE_LENGTH = 7;
	public static int BOARD_LENGTH = 8;
	public static int ANN_INPUT_LENGTH = BOARD_LENGTH * BOARD_LENGTH * PIECE_CODE_LENGTH;
}

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

public class BoardStateData
{
	public PieceCode[] _pieces;
	public int _turn = 0;
	public bool[] _kingMoved = new bool[2] { false, false };
	public bool[] _kRookMoved = new bool[2] { false, false };
	public bool[] _qRookMoved = new bool[2] { false, false };
	public int _enPassant = -1;

	public static bool operator == (BoardStateData lhs, BoardStateData rhs)
	{
		for (int y = 0; y < Constants.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Constants.BOARD_LENGTH; ++x)
			{
				if (lhs._pieces[y * Constants.BOARD_LENGTH + x] != rhs._pieces[y * Constants.BOARD_LENGTH + x])
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

	public static bool operator != (BoardStateData lhs, BoardStateData rhs)
	{
		return !(lhs == rhs);
	}
}