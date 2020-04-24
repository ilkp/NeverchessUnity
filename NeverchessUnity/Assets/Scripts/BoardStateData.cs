
public class BoardStateData
{
	public PieceCode[] _pieces;
	public int _turn;
	public bool[] _kingMoved;
	public bool[] _kRookMoved;
	public bool[] _qRookMoved;
	public int _enPassant;

	public BoardStateData() { }

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

	public static bool operator != (BoardStateData lhs, BoardStateData rhs)
	{
		return !(lhs == rhs);
	}
}