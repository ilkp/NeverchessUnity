using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.EventSystems;
using TMPro;

public class BoardManager : MonoBehaviour
{
	[SerializeField] private TMPro.TMP_Dropdown _whiteAiSelect;
	[SerializeField] private TMPro.TMP_Dropdown _blackAiSelect;
	[SerializeField] private UnityEngine.UI.Toggle _waitOnAiToggle;
	[SerializeField] private UnityEngine.UI.Image _readyImage;
	[SerializeField] private TMP_Text _winText;
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
	private Dictionary<ulong, int> hashPositions;
	private bool[] _sideIsPlayer = new bool[2] { false, false};
	private bool _active = false;
	private bool _processingAI = false;
	private bool _waitOnAi = true;
	private ANNetwork _annWhite;
	private ANNetwork _annBlack;
	private ChessAI _ai;
	private Coroutine _aiCoroutine = null;

	private Camera _camera;
	private GameObject _board;
	private GameObject[] _pieceObjects;
	private GameObject _selectedPiece;
	private Coroutine _lerping;
	private int _selectedX;
	private int _selectedY;
	private int _xhit;
	private int _yhit;

	// Start is called before the first frame update
	void Start()
	{
		_camera = Camera.main;
		_ai = new ChessAI();
		_annWhite = new ANNetwork();
		_annBlack = new ANNetwork();
		hashPositions = new Dictionary<ulong, int>();
		Chess.InitBoardData(ref _boardData);
		InitBoard();
		Restart();
	}

    // Update is called once per frame
    void Update()
    {
		_waitOnAi = _waitOnAiToggle.isOn;
		if (!_active)
			return;
		HandleTurn();
    }

	public void Restart()
	{
		Stop();
		EventSystem.current.SetSelectedGameObject(null);
		_active = false;
		_processingAI = false;
		_winText.enabled = false;
		hashPositions.Clear();
		string path = Path.GetFullPath("./") + "Ann\\";
		Chess.InitBoardData(ref _boardData);
		_sideIsPlayer[0] = false;
		_sideIsPlayer[1] = false;
		switch (_whiteAiSelect.value)
		{
			case 0:
				_sideIsPlayer[0] = true;
				break;
			case 1:
				_annWhite.ReadANN(path + "ann100.ann");
				break;
			case 2:
				_annWhite.ReadANN(path + "ann500.ann");
				break;
			case 3:
				_annWhite.ReadANN(path + "ann2500.ann");
				break;
			case 4:
				_annWhite.ReadANN(path + "ann12500.ann");
				break;
			case 5:
				_annWhite.ReadANN(path + "ann16384.ann");
				break;
			case 6:
				_annWhite.ReadANN(path + "ann32768.ann");
				break;
		}
		switch (_blackAiSelect.value)
		{
			case 0:
				_sideIsPlayer[1] = true;
				break;
			case 1:
				_annBlack.ReadANN(path + "ann100.ann");
				break;
			case 2:
				_annBlack.ReadANN(path + "ann500.ann");
				break;
			case 3:
				_annBlack.ReadANN(path + "ann2500.ann");
				break;
			case 4:
				_annBlack.ReadANN(path + "ann12500.ann");
				break;
			case 5:
				_annBlack.ReadANN(path + "ann16384.ann");
				break;
			case 6:
				_annBlack.ReadANN(path + "ann32768.ann");
				break;
		}
		UpdateGraphics();
		_active = true;
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
				DeSelect();
			// DESELECT WHEN CLICKING ANOTHER OWN PIECE
			else if (_boardData._pieces[_yhit * Chess.BOARD_LENGTH + _xhit] != PieceCode.EMPTY && Chess.PiecesSide(_boardData, _xhit, _yhit) == _boardData._turn)
			{
				DeSelect();
				Select(_xhit, _yhit);
			}
			// MAKE MOVE
			else if (_selectedPiece != null && (_boardData._pieces[_yhit * Chess.BOARD_LENGTH + _xhit] == PieceCode.EMPTY || Chess.PiecesSide(_boardData, _xhit, _yhit) != _boardData._turn))
			{
				MoveData move = MakeMove(_selectedX, _selectedY, _xhit, _yhit);
				if (Chess.MoveIsLegal(move, _boardData) && Chess.MoveIsSafe(move, _boardData))
					HandleMove(move);
				else
					DeSelect();
			}
		}
	}

	private void HandleTurn()
	{
		if (_processingAI)
			return;
		if (!_sideIsPlayer[_boardData._turn])
			_aiCoroutine = StartCoroutine(HandleAI());
		else if (Input.GetMouseButtonDown(0))
			OnClick();
	}

	private int PositionAppeared()
	{
		ulong hash = _boardData.ZobristHash();
		if (!hashPositions.ContainsKey(hash))
		{
			hashPositions.Add(hash, 1);
			return 1;
		}
		else
			return ++hashPositions[hash];
	}

	private void Stop()
	{
		if (_aiCoroutine != null)
			StopCoroutine(_aiCoroutine);
		if (_ai != null)
			_ai.Stop();
		_readyImage.color = Color.red;
	}

	private IEnumerator HandleAI()
	{
		_processingAI = true;
		if (_boardData._turn == 0)
			_ai._ann = _annWhite;
		else
			_ai._ann = _annBlack;
		_ai._boardStateData = new BoardStateData(_boardData);
		_ai.Start();

		while (!_ai._ready)
			yield return null;

		MoveData result = _ai._result;
		if (_waitOnAi)
		{
			_readyImage.color = Color.green;
			while (!Input.GetKeyDown(KeyCode.Space))
				yield return null;
		}
		HandleMove(result);
		_readyImage.color = Color.red;
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
			_active = false;
			_winText.enabled = true;
			if (_boardData._turn == 0)
				_winText.text = "White win";
			else
				_winText.text = "Black win";
		}
		int npos = PositionAppeared();
		if (npos > 2)
		{
			_winText.enabled = true;
			_winText.text = "Draw by repetition";
			_active = false;
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
			StopCoroutine(_lerping);
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
		move.upgrade = PieceCode.EMPTY;

		PieceCode piece = _boardData._pieces[yStart * Chess.BOARD_LENGTH + xStart];
		if (piece == PieceCode.W_KING || piece == PieceCode.B_KING)
		{
			if (xEnd == xStart + 2)
				move.shortCastle = true;
			else if (xEnd == xStart - 2)
				move.longCastle = true;
		}
		else if (piece == PieceCode.B_PAWN || piece == PieceCode.W_PAWN)
		{
			if (yEnd == yStart + (_boardData._turn == 0 ? 2 : -2))
				move.doublePawnMove = true;
			else if (yEnd == yStart + (_boardData._turn == 0 ? 1 : -1)
				&& (xEnd == xStart + 1 || xEnd == xStart - 1)
				&& _boardData._pieces[yEnd * Chess.BOARD_LENGTH + xEnd] == PieceCode.EMPTY)
			{
				move.enPassant = true;
			}
			else if (_boardData._turn == 0 && yEnd == Chess.BOARD_LENGTH - 1)
				move.upgrade = PieceCode.W_QUEEN;
			else if (_boardData._turn == 1 && yEnd == 0)
				move.upgrade = PieceCode.B_QUEEN;
		}
		return move;
	}

	private void UpdateGraphics()
	{
		for (int y = 0; y < Chess.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Chess.BOARD_LENGTH; ++x)
				SetSquareGraphic(x, y);
		}
	}

	public void QuitApp()
	{
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}
}
