using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
	private bool _processingAI = false;
	private bool[] _sideIsPlayer = new bool[2] { true, false };

	private Camera _camera;
	private GameObject _board;
	private GameObject[] _pieceObjects;
	private GameObject _selectedPiece;
	private int _selectedX;
	private int _selectedY;
	private Coroutine _lerping;
	private ANNetwork ann;
	private ChessAI ai;
	private int _xhit;
	private int _yhit;
	private bool _waitForAI = false;
	private bool _whiteVictory = false;
	private bool _blackVictory = false;

	// Start is called before the first frame update
	void Start()
    {
		Chess.InitBoardData(ref _boardData);
		InitBoard();
		ann = new ANNetwork();
		ann.ReadANN("C:\\Projektit\\Neverchess\\Neverchess\\Release\\testANN.ann");
		ai = new ChessAI(ann);
		_camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
		if (_whiteVictory)
		{
			Debug.Log("White victory");
		}
		else if (_blackVictory)
		{
			Debug.Log("Black victory");
		}
        if (Input.GetMouseButtonDown(0))
		{
			OnClick();
		}
    }

	private void OnClick()
	{
		RaycastHit2D hit = Physics2D.Raycast(_camera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

		if (!_processingAI && hit.collider != null && hit.collider.gameObject == _board)
		{
			_xhit = (int)(hit.point.x + 0.5f);
			_yhit = (int)(hit.point.y + 0.5f);
			// DESELECT WHEN CLICKING SAME PIECE
			if (_selectedX == _xhit && _selectedY == _yhit)
			{
				DeSelect();
			}
			// DESELECT WHEN CLICKING ANOTHER OWN PIECE
			else if (_boardData._pieces[_yhit * BOARDLENGTH + _xhit] != PieceCode.EMPTY && Chess.PiecesSide(_boardData, _xhit, _yhit) == _boardData._turn)
			{
				DeSelect();
				Select(_xhit, _yhit);
			}
			// MAKE MOVE
			else if (_selectedPiece != null && (_boardData._pieces[_yhit * BOARDLENGTH + _xhit] == PieceCode.EMPTY || Chess.PiecesSide(_boardData, _xhit, _yhit) != _boardData._turn))
			{
				MoveData move = MakeMove(_selectedX, _selectedY, _xhit, _yhit);
				if (Chess.MoveIsLegal(move, _boardData) && Chess.MoveIsSafe(move, _boardData))
				{
					HandleMove(move);
					if (!_sideIsPlayer[_boardData._turn])
					{
						StartCoroutine(HandleAI());
					}
				}
				else
				{
					DeSelect();
				}
			}
		}
	}

	private IEnumerator HandleAI()
	{
		_processingAI = true;
		ai._boardStateData = new BoardStateData(_boardData);
		ai.Start();

		while (!ai._ready)
		{
			yield return null;
		}

		MoveData result = ai._result;

		if (Chess.MoveIsLegal(result, _boardData) && Chess.MoveIsSafe(result, _boardData))
		{
			HandleMove(result);
		}
		else
		{
			Debug.LogWarning("AI GAVE ILLEGAL MOVE\n"
				+ "(" + result.xStart + ", " + result.yStart + ") -> (" + result.xEnd + ", " + result.yEnd + ")\n"
				+ "en passant: " + result.enPassant + "\n"
				+ "double pawn move: " + result.doublePawnMove + "\n"
				+ "short castle: " + result.shortCastle + "\n"
				+ "long castle: " + result.longCastle);
		}
		_processingAI = false;
	}

	private void HandleMove(MoveData move)
	{
		Chess.PlayMove(ref _boardData, move);
		UpdateGraphics();
		CheckForVictory();
	}

	private void CheckForVictory()
	{
		PieceCode king = _boardData._turn == 0 ? PieceCode.W_KING : PieceCode.B_KING;
		int[] kingCoord = Chess.FindKing(_boardData._pieces, _boardData._turn);
		List<MoveData> moves = Chess.GenRawMoves(_boardData);
		Chess.FilterMoves(_boardData, ref moves);
		if (moves.Count == 0)
		{
			if (_boardData._turn == 0)
			{
				_blackVictory = true;
			}
			else
			{
				_whiteVictory = true;
			}
		}
	}

	private void Select(int x, int y)
	{
		DeSelect();
		_selectedPiece = _pieceObjects[y * Chess.BOARD_LENGTH + x];
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
		Destroy(_board);
		_board = new GameObject();
		_board.AddComponent<SpriteRenderer>();
		_board.GetComponent<SpriteRenderer>().sortingOrder = 0;
		_board.GetComponent<SpriteRenderer>().sprite = _spriteBoard;
		_board.AddComponent<BoxCollider2D>();
		_board.GetComponent<BoxCollider2D>().size = new Vector2(Chess.BOARD_LENGTH, Chess.BOARD_LENGTH);
		_board.GetComponent<BoxCollider2D>().offset = new Vector2(Chess.BOARD_LENGTH * 0.5f - 0.5f, Chess.BOARD_LENGTH * 0.5f - 0.5f);

		if (_pieceObjects != null)
		{
			foreach (GameObject go in _pieceObjects)
			{
				Destroy(go);
			}
		}
		_pieceObjects = new GameObject[Chess.BOARD_LENGTH * Chess.BOARD_LENGTH];
		for (int y = 0; y < Chess.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Chess.BOARD_LENGTH; ++x)
			{
				_pieceObjects[y * Chess.BOARD_LENGTH + x] = new GameObject();
				_pieceObjects[y * Chess.BOARD_LENGTH + x].transform.position = new Vector3(x, y, 0);
				_pieceObjects[y * Chess.BOARD_LENGTH + x].AddComponent<SpriteRenderer>();
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sortingOrder = 1;
				SetSquareGraphic(x, y);
			}
		}
	}

	private void SetSquareGraphic(int x, int y)
	{
		PieceCode pieceCode = _boardData._pieces[y * Chess.BOARD_LENGTH + x];
		switch (pieceCode)
		{
			case PieceCode.EMPTY:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = null;
				break;
			case PieceCode.W_KING:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWK;
				break;
			case PieceCode.B_KING:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBK;
				break;
			case PieceCode.W_QUEEN:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWQ;
				break;
			case PieceCode.B_QUEEN:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBQ;
				break;
			case PieceCode.W_BISHOP:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWB;
				break;
			case PieceCode.B_BISHOP:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBB;
				break;
			case PieceCode.W_ROOK:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWR;
				break;
			case PieceCode.B_ROOK:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBR;
				break;
			case PieceCode.W_KNIGHT:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWk;
				break;
			case PieceCode.B_KNIGHT:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBk;
				break;
			case PieceCode.W_PAWN:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteWP;
				break;
			case PieceCode.B_PAWN:
				_pieceObjects[y * Chess.BOARD_LENGTH + x].GetComponent<SpriteRenderer>().sprite = _spriteBP;
				break;
		}
	}

	private MoveData MakeMove(int xStart, int yStart, int xEnd, int yEnd)
	{
		MoveData move = new MoveData(xStart, yStart, xEnd, yEnd);

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

	private void UpdateGraphics()
	{
		for (int y = 0; y < Chess.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Chess.BOARD_LENGTH; ++x)
			{
				SetSquareGraphic(x, y);
			}
		}
	}
}
