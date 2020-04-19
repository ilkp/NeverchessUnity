using System.Collections;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
	private const int BOARDLENGTH = 8;

	[SerializeField] private Sprite _spriteBoard;
	[SerializeField] private Sprite _spriteWK;
	[SerializeField] private Sprite _spriteBK;
	[SerializeField] private Sprite _spriteWQ;
	[SerializeField] private Sprite _spriteBQ;
	[SerializeField] private Sprite _spriteWB;
	[SerializeField] private Sprite _spriteBB;
	[SerializeField] private Sprite _spriteWR;
	[SerializeField] private Sprite _spriteBR;
	[SerializeField] private Sprite _spriteWk;
	[SerializeField] private Sprite _spriteBk;
	[SerializeField] private Sprite _spriteWP;
	[SerializeField] private Sprite _spriteBP;

	private BoardStateData _boardData;

	private Camera _camera;
	private GameObject _board;
	private GameObject[] _pieceObjects;
	private GameObject _selectedPiece;
	private int _selectedX;
	private int _selectedY;
	private Coroutine _lerping;
	ANNetwork ai;

	// Start is called before the first frame update
	void Start()
    {
		ai = new ANNetwork();
		ai.ReadANN("C:\\Projektit\\Neverchess\\Neverchess\\Release\\testANN.ann");
		_camera = Camera.main;
		_boardData = new BoardStateData();
		InitBoard();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
		{
			OnClick();
		}
    }

	private void OnClick()
	{
		RaycastHit2D hit = Physics2D.Raycast(_camera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
		if (hit.collider != null)
		{
			int xhit = (int)(hit.point.x + 0.5f);
			int yhit = (int)(hit.point.y + 0.5f);
			if (_selectedX == xhit && _selectedY == yhit)
			{
				DeSelect();
			}
			else if (_selectedPiece != null
				&& (_boardData._pieces[yhit * BOARDLENGTH + xhit] == PieceCode.EMPTY
				|| ((int)_boardData._pieces[yhit * BOARDLENGTH + xhit] >> (Constants.PIECE_CODE_LENGTH - 1)) != _boardData._turn))
			{
				MoveData move = MakeMove(_selectedX, _selectedY, xhit, yhit);
				if (MoveIsLegal(move))
				{
					MovePiece(move);
				}
				DeSelect();
			}
			else if (_boardData._pieces[yhit * BOARDLENGTH + xhit] != PieceCode.EMPTY
				&& ((int)_boardData._pieces[yhit * BOARDLENGTH + xhit] >> (Constants.PIECE_CODE_LENGTH - 1)) == _boardData._turn)
			{
				DeSelect();
				Select(xhit, yhit);
			}
		}
		else
		{
			DeSelect();
		}
	}

	private void Select(int x, int y)
	{
		DeSelect();
		_selectedPiece = _pieceObjects[y * Constants.BOARD_LENGTH + x];
		_selectedPiece.GetComponent<SpriteRenderer>().sortingOrder = 2;
		_lerping = StartCoroutine(LerpSelectedUp(_selectedPiece));
		_selectedX = x;
		_selectedY = y;
	}

	private void DeSelect()
	{
		if (_lerping != null)
		{
			StopCoroutine(_lerping);
		}
		if (_selectedPiece != null)
		{
			_selectedPiece.transform.localScale = Vector3.one;
			_selectedPiece.GetComponent<SpriteRenderer>().sortingOrder = 1;
			_selectedPiece = null;
			_selectedX = -1;
			_selectedY = -1;
		}
	}

	private IEnumerator LerpSelectedUp(GameObject piece)
	{
		float timer = 0f;
		float maxTime = 2f;
		while (timer < maxTime && _selectedPiece == piece)
		{
			piece.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.4f, 1.4f, 0.0f), FastRising(timer / maxTime));
			timer += Time.deltaTime;
			yield return null;
		}
	}

	private float FastRising(float x)
	{
		return Mathf.Pow(x, 0.2f);
	}

	private void InitBoard()
	{
		_boardData._pieces = new PieceCode[8 * 8];
		for (int y = 2; y < Constants.BOARD_LENGTH - 2; ++y)
		{
			for (int x = 0; x < Constants.BOARD_LENGTH; ++x)
			{
				_boardData._pieces[y * Constants.BOARD_LENGTH + x] = PieceCode.EMPTY;
			}
		}
		for (int x = 0; x < Constants.BOARD_LENGTH; ++x)
		{
			_boardData._pieces[Constants.BOARD_LENGTH + x] = PieceCode.W_PAWN;
			_boardData._pieces[Constants.BOARD_LENGTH * 6 + x] = PieceCode.B_PAWN;
		}
		_boardData._pieces[0] = PieceCode.W_ROOK;
		_boardData._pieces[1] = PieceCode.W_KNIGHT;
		_boardData._pieces[2] = PieceCode.W_BISHOP;
		_boardData._pieces[3] = PieceCode.W_QUEEN;
		_boardData._pieces[4] = PieceCode.W_KING;
		_boardData._pieces[5] = PieceCode.W_BISHOP;
		_boardData._pieces[6] = PieceCode.W_KNIGHT;
		_boardData._pieces[7] = PieceCode.W_ROOK;

		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 0] = PieceCode.B_ROOK;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 1] = PieceCode.B_KNIGHT;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 2] = PieceCode.B_BISHOP;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 3] = PieceCode.B_QUEEN;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 4] = PieceCode.B_KING;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 5] = PieceCode.B_BISHOP;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 6] = PieceCode.B_KNIGHT;
		_boardData._pieces[Constants.BOARD_LENGTH * 7 + 7] = PieceCode.B_ROOK;

		Destroy(_board);
		_board = new GameObject();
		_board.AddComponent<SpriteRenderer>();
		_board.GetComponent<SpriteRenderer>().sortingOrder = 0;
		_board.GetComponent<SpriteRenderer>().sprite = _spriteBoard;
		_board.AddComponent<BoxCollider2D>();
		_board.GetComponent<BoxCollider2D>().size = new Vector2(Constants.BOARD_LENGTH, Constants.BOARD_LENGTH);
		_board.GetComponent<BoxCollider2D>().offset = new Vector2(Constants.BOARD_LENGTH * 0.5f - 0.5f, Constants.BOARD_LENGTH * 0.5f - 0.5f);

		if (_pieceObjects != null)
		{
			foreach (GameObject go in _pieceObjects)
			{
				Destroy(go);
			}
		}
		_pieceObjects = new GameObject[Constants.BOARD_LENGTH * Constants.BOARD_LENGTH];
		for (int y = 0; y < Constants.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Constants.BOARD_LENGTH; ++x)
			{
				_pieceObjects[y * Constants.BOARD_LENGTH + x] = new GameObject();
				_pieceObjects[y * Constants.BOARD_LENGTH + x].transform.position = new Vector3(x, y, 0);
				_pieceObjects[y * Constants.BOARD_LENGTH + x].AddComponent<SpriteRenderer>();
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sortingOrder = 1;
				SetSquareGraphic(x, y);
			}
		}
	}

	private void SetSquareGraphic(int x, int y)
	{
		PieceCode pieceCode = _boardData._pieces[y * Constants.BOARD_LENGTH + x];
		switch (pieceCode)
		{
			case PieceCode.EMPTY:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = null;
				break;
			case PieceCode.W_KING:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWK;
				break;
			case PieceCode.B_KING:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBK;
				break;
			case PieceCode.W_QUEEN:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWQ;
				break;
			case PieceCode.B_QUEEN:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBQ;
				break;
			case PieceCode.W_BISHOP:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWB;
				break;
			case PieceCode.B_BISHOP:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBB;
				break;
			case PieceCode.W_ROOK:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWR;
				break;
			case PieceCode.B_ROOK:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBR;
				break;
			case PieceCode.W_KNIGHT:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWk;
				break;
			case PieceCode.B_KNIGHT:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBk;
				break;
			case PieceCode.W_PAWN:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWP;
				break;
			case PieceCode.B_PAWN:
				_pieceObjects[y * Constants.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBP;
				break;
		}
	}

	private MoveData MakeMove(int xStart, int yStart, int xEnd, int yEnd)
	{
		MoveData move = new MoveData();
		move.xStart = xStart;
		move.yStart = yStart;
		move.xEnd = xEnd;
		move.yEnd = yEnd;

		PieceCode piece = _boardData._pieces[yStart * BOARDLENGTH + xStart];
		if (piece == PieceCode.W_KING || piece == PieceCode.B_KING)
		{
			if (xEnd == xStart + 2)
			{
				move.shortCastle = true;
			}
			else if (xEnd == xStart - 2)
			{
				move.longCastle = true;
			}
		}
		else if (piece == PieceCode.B_PAWN || piece == PieceCode.W_PAWN)
		{
			if (yEnd == yStart + (_boardData._turn == 0 ? 2 : -2))
			{
				move.doublePawnMove = true;
			}
			else if (yEnd == yStart + (_boardData._turn == 0 ? 1 : -1)
				&& (xEnd == xStart + 1 || xEnd == xStart - 1)
				&& _boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY)
			{
				move.enPassant = true;
			}
		}
		move.upgrade = PieceCode.EMPTY;
		return move;
	}

	private void MovePiece(MoveData move)
	{
		int turn = _boardData._turn;
		_boardData._enPassant = -1;
		if (move.shortCastle)
		{
			int row = turn == 0 ? 0 : 7;
			_boardData._pieces[row * BOARDLENGTH + 5] = turn == 0 ? PieceCode.W_ROOK : PieceCode.B_ROOK;
			_boardData._pieces[row * BOARDLENGTH + 6] = turn == 0 ? PieceCode.W_KING : PieceCode.B_KING;
			_boardData._pieces[row * BOARDLENGTH + 4] = PieceCode.EMPTY;
			_boardData._pieces[row * BOARDLENGTH + 7] = PieceCode.EMPTY;
			_boardData._kingMoved[turn] = true;
			_boardData._kRookMoved[turn] = true;
		}
		else if (move.longCastle)
		{
			int row = turn == 0 ? 0 : 7;
			_boardData._pieces[(turn == 0 ? 0 : 7) * BOARDLENGTH + 3] = turn == 0 ? PieceCode.W_ROOK : PieceCode.B_ROOK;
			_boardData._pieces[(turn == 0 ? 0 : 7) * BOARDLENGTH + 2] = turn == 0 ? PieceCode.W_KING : PieceCode.B_KING;
			_boardData._pieces[row * BOARDLENGTH + 4] = PieceCode.EMPTY;
			_boardData._pieces[row * BOARDLENGTH + 7] = PieceCode.EMPTY;
			_boardData._kingMoved[turn] = true;
			_boardData._qRookMoved[turn] = true;
		}
		else if (move.enPassant)
		{
			_boardData._pieces[move.yEnd * BOARDLENGTH + move.xStart] = PieceCode.EMPTY;
			_boardData._pieces[move.yEnd * BOARDLENGTH + move.xEnd] = _boardData._pieces[move.yStart * BOARDLENGTH + move.xStart];
			_boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] = PieceCode.EMPTY;
		}
		else
		{
			_boardData._pieces[move.yEnd * BOARDLENGTH + move.xEnd] = _boardData._pieces[move.yStart * BOARDLENGTH + move.xStart];
			_boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] = PieceCode.EMPTY;
			if (move.doublePawnMove)
			{
				_boardData._enPassant = move.xStart;
			}
			else if (_boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] == PieceCode.W_KING || _boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] == PieceCode.B_KING)
			{
				_boardData._kingMoved[turn] = true;
			}
			// CHECK IF ROOKS ARE MOVED
			else if (move.xStart == 0 && move.yStart == (turn == 0 ? 0 : 7)
				&& (_boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] == PieceCode.W_ROOK
				|| _boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] == PieceCode.B_ROOK))
			{
				_boardData._qRookMoved[turn] = true;
			}
			else if (move.xStart == 7 && move.yStart == (turn == 0 ? 0 : 7)
				&& (_boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] == PieceCode.W_ROOK
				|| _boardData._pieces[move.yStart * BOARDLENGTH + move.xStart] == PieceCode.B_ROOK))
			{
				_boardData._kRookMoved[turn] = true;
			}
		}
		if (move.upgrade != PieceCode.EMPTY)
		{
			_boardData._pieces[move.yEnd * BOARDLENGTH + move.xEnd] = move.upgrade;
		}
		_boardData._turn = (_boardData._turn + 1) % 2;
		UpdateGraphics();
	}

	private void UpdateGraphics()
	{
		for (int y = 0; y < Constants.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Constants.BOARD_LENGTH; ++x)
			{
				SetSquareGraphic(x, y);
			}
		}
	}

	private bool MoveIsLegal(MoveData move)
	{
		if (move.xStart < 0 || move.xStart >= BOARDLENGTH || move.yStart < 0 || move.yStart >= BOARDLENGTH
			|| move.xEnd < 0 || move.xEnd >= BOARDLENGTH || move.yEnd < 0 || move.yEnd >= BOARDLENGTH)
		{
			return false;
		}
		PieceCode pieceCode = _boardData._pieces[move.yStart * Constants.BOARD_LENGTH + move.xStart];
		switch (pieceCode)
		{
			case PieceCode.W_KING:
			case PieceCode.B_KING:
				return MoveIsLegalKing(move);

			case PieceCode.W_QUEEN:
			case PieceCode.B_QUEEN:
				return MoveIsLegalQueen(move);

			case PieceCode.W_BISHOP:
			case PieceCode.B_BISHOP:
				return MoveIsLegalBishop(move);

			case PieceCode.W_ROOK:
			case PieceCode.B_ROOK:
				return MoveIsLegalRook(move);

			case PieceCode.W_KNIGHT:
			case PieceCode.B_KNIGHT:
				return MoveIsLegalKnight(move);

			case PieceCode.W_PAWN:
			case PieceCode.B_PAWN:
				return MoveIsLegalPawn(move);
		}
		return false;
	}

	private bool MoveIsLegalKing(MoveData move)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		int row = _boardData._turn == 0 ? 0 : 7;
		// SHORT CASTLE
		if (move.shortCastle
			&& yStart == row
			&& yEnd == yStart
			&& xStart == 4
			&& (xEnd == xStart + 2)
			&& !_boardData._kingMoved[_boardData._turn]
			&& !_boardData._kRookMoved[_boardData._turn]
			&& _boardData._pieces[row * BOARDLENGTH + 5] == PieceCode.EMPTY
			&& _boardData._pieces[row * BOARDLENGTH + 6] == PieceCode.EMPTY
			&& !SquareThreatened(4, row)
			&& !SquareThreatened(5, row)
			&& !SquareThreatened(6, row))
		{
			return true;
		}
		// LONG CASTLE
		else if (move.longCastle
			&& xStart == row
			&& yEnd == yStart
			&& xEnd == xStart - 2
			&& !_boardData._kingMoved[_boardData._turn]
			&& !_boardData._qRookMoved[_boardData._turn]
			&& _boardData._pieces[row * BOARDLENGTH + 1] == PieceCode.EMPTY
			&& _boardData._pieces[row * BOARDLENGTH + 2] == PieceCode.EMPTY
			&& _boardData._pieces[row * BOARDLENGTH + 3] == PieceCode.EMPTY
			&& !SquareThreatened(2, row)
			&& !SquareThreatened(3, row)
			&& !SquareThreatened(4, row))
		{
			return true;
		}
		else
		{
			return (Mathf.Abs(xEnd - xStart) == 0 || Mathf.Abs(xEnd - xStart) == 1)
				&& (Mathf.Abs(yEnd - yStart) == 0 || Mathf.Abs(yEnd - yStart) == 1)
				&& (_boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY
				|| ((int)_boardData._pieces[yEnd * BOARDLENGTH + xEnd] >> (Constants.PIECE_CODE_LENGTH - 1)) != _boardData._turn);
		}
	}

	private bool MoveIsLegalQueen(MoveData move)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (!(_boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY
			|| ((int)_boardData._pieces[yEnd * BOARDLENGTH + xEnd] >> (Constants.PIECE_CODE_LENGTH - 1)) != _boardData._turn))
		{
			return false;
		}
		if (!(xStart == xEnd || yStart == yEnd || (Mathf.Abs(xEnd - xStart) == Mathf.Abs(yEnd - yStart))))
		{
			return false;
		}
		return SquaresAreEmpty(xStart, yStart, xEnd, yEnd);
	}

	private bool MoveIsLegalBishop(MoveData move)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (!(_boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY
			|| ((int)_boardData._pieces[yEnd * BOARDLENGTH + xEnd] >> (Constants.PIECE_CODE_LENGTH - 1)) != _boardData._turn))
		{
			return false;
		}
		if (Mathf.Abs(xEnd - xStart) != Mathf.Abs(yEnd - yStart))
		{
			return false;
		}
		return SquaresAreEmpty(xStart, yStart, xEnd, yEnd);
	}

	private bool MoveIsLegalRook(MoveData move)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (!(_boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY
			|| ((int)_boardData._pieces[yEnd * BOARDLENGTH + xEnd] >> (Constants.PIECE_CODE_LENGTH - 1)) != _boardData._turn))
		{
			return false;
		}
		if (!(xStart == xEnd || yStart == yEnd))
		{
			return false;
		}
		return SquaresAreEmpty(xStart, yStart, xEnd, yEnd);
	}

	private bool MoveIsLegalKnight(MoveData move)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		if (!(_boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY
			|| ((int)_boardData._pieces[yEnd * BOARDLENGTH + xEnd] >> (Constants.PIECE_CODE_LENGTH - 1)) != _boardData._turn))
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

	private bool MoveIsLegalPawn(MoveData move)
	{
		int xStart = move.xStart;
		int yStart = move.yStart;
		int xEnd = move.xEnd;
		int yEnd = move.yEnd;
		int dir = _boardData._turn == 0 ? 1 : -1;
		// EN PASSANT
		if (move.enPassant
			&& xStart == _boardData._enPassant
			&& (xEnd == xStart + 1 || xEnd == xStart - 1)
			&& yEnd == yStart + dir
			&& move.yStart == (_boardData._turn == 0 ? 3 : 4))
		{
			return true;
		}
		// DOUBLE MOVE
		else if (move.doublePawnMove
			&& yStart == (_boardData._turn == 0 ? 1 : 6)
			&& xEnd == xStart
			&& yEnd == yStart + 2 * dir
			&& _boardData._pieces[(yStart + dir) * BOARDLENGTH + xStart] == PieceCode.EMPTY
			&& _boardData._pieces[(yStart + 2 * dir) * BOARDLENGTH + xStart] == PieceCode.EMPTY)
		{
			return true;
		}
		// TAKE
		else if ((xEnd == xStart + 1 || xEnd == xStart - 1)
			&& yEnd == yStart + dir
			&& _boardData._pieces[yEnd * BOARDLENGTH + xEnd] != PieceCode.EMPTY
			&& ((int)_boardData._pieces[yEnd * BOARDLENGTH + xEnd] >> Constants.PIECE_CODE_LENGTH - 1) != _boardData._turn)
		{
			return true;
		}
		// NORMAL MOVE
		else if (xEnd == xStart && yEnd == yStart + dir
			&& _boardData._pieces[yEnd * BOARDLENGTH + xEnd] == PieceCode.EMPTY)
		{
			return true;
		}
		return false;
	}

	private bool SquaresAreEmpty(int xStart, int yStart, int xEnd, int yEnd)
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
		else
		{
			xDir = (xEnd - xStart) / Mathf.Abs(xEnd - xStart);
			yDir = (yEnd - yStart) / Mathf.Abs(yEnd - yStart);
		}
		int x = xStart + xDir;
		int y = yStart + yDir;
		while (!(x == xEnd && y == yEnd))
		{
			if (_boardData._pieces[y * BOARDLENGTH + x] != PieceCode.EMPTY)
			{
				return false;
			}
			x += xDir;
			y += yDir;
		}
		return true;
	}

	private bool SquareThreatened(int x, int y)
	{
		return false;
	}

	bool PieceCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		PieceCode piece = _boardData._pieces[pieceY * BOARDLENGTH + pieceX];
		switch (piece)
		{
			case PieceCode.W_KING:
			case PieceCode.B_KING:
				return kingCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_QUEEN:
			case PieceCode.B_QUEEN:
				return queenCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_ROOK:
			case PieceCode.B_ROOK:
				return rookCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_BISHOP:
			case PieceCode.B_BISHOP:
				return bishopCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_KNIGHT:
			case PieceCode.B_KNIGHT:
				return knightCanThreatenSquare(pieceX, pieceY, targetX, targetY);
			case PieceCode.W_PAWN:
			case PieceCode.B_PAWN:
				return pawnCanThreatenSquare(pieceX, pieceY, targetX, targetY);
		}
		return false;
	}

	bool kingCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		return Mathf.Abs(targetX - pieceX) <= 1 && Mathf.Abs(targetY - pieceY) <= 1;
	}

	bool queenCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		if (!(targetX == pieceX || targetY == pieceY || (Mathf.Abs(targetX - pieceX) == Mathf.Abs(targetY - pieceY))))
		{
			return false;
		}
		return SquaresAreEmpty(pieceX, pieceY, targetX, targetY);
	}

	bool bishopCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		if (Mathf.Abs(targetX - pieceX) != Mathf.Abs(targetY - pieceY))
		{
			return false;
		}
		return SquaresAreEmpty(pieceX, pieceY, targetX, targetY);
	}

	bool knightCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		if (Mathf.Abs(targetX - pieceX) == 2 && Mathf.Abs(targetY - pieceY) == 1
			|| Mathf.Abs(targetX - pieceX) == 1 && Mathf.Abs(targetY - pieceY) == 2)
		{
			return true;
		}
		return false;
	}

	bool rookCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		if (!(targetX == pieceX || targetY == pieceY))
		{
			return false;
		}
		return SquaresAreEmpty(pieceX, pieceY, targetX, targetY);
	}

	bool pawnCanThreatenSquare(int pieceX, int pieceY, int targetX, int targetY)
	{
		int dir = _boardData._turn == 0 ? 1 : -1;
		if (pieceY + dir == targetY && Mathf.Abs(targetX - pieceX) == 1)
		{
			return true;
		}
		return false;
	}
}
