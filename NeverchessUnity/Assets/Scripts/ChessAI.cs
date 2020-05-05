using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AlphaBetaEvaluation
{
	public MoveData move = new MoveData();
	public float evaluatedValue;
}

public class ChessAI
{
	public MoveData _result;
	public BoardStateData _boardStateData;
	public ANNetwork _ann;
	public int _depth = 2;
	public bool _ready = true;

	private System.Threading.Thread _thread = null;

	private ulong[] zobristPieceValues = new ulong[Chess.BOARD_LENGTH * Chess.BOARD_LENGTH * 12];
	private ulong[] zobristTurnValues = new ulong[2];
	private ulong[] zobristKingMovedValues = new ulong[2];
	private ulong[] zobristQRookMovedValues = new ulong[2];
	private ulong[] zobristKRookMovedValues = new ulong[2];
	private ulong[] zobristEnPassantValues = new ulong[9];
	private Dictionary<ulong, AlphaBetaEvaluation> boardEvaluations = new Dictionary<ulong, AlphaBetaEvaluation>();
	private Dictionary<ulong, int> hashPositions = new Dictionary<ulong, int>();

	public ChessAI()
	{
		calculateZobristValues();
	}

	public void Start()
	{
		_ready = false;
		_thread = new System.Threading.Thread(Process);
		_thread.Start();
	}

	public void Process()
	{
		AlphaBetaEvaluation eval = alphaBeta(_boardStateData, _depth, -1000.0f, 1000.0f);
		_result = eval.move;
		_ready = true;
	}

	AlphaBetaEvaluation alphaBeta(BoardStateData boardStateData, int depth, float alpha, float beta)
	{
		bool savePosition = false;
		AlphaBetaEvaluation evaluation = new AlphaBetaEvaluation();
		//ulong zHash = zobristHash(boardStateData);
		//if (boardEvaluations.ContainsKey(zHash))
		//{
		//	return boardEvaluations[zHash];
		//}
		//else
		//{
		//	savePosition = true;
		//}

		List<MoveData> moves = Chess.GenRawMoves(boardStateData);
		List<BoardStateData> newStates = Chess.FilterMoves(boardStateData, ref moves);

		if (moves.Count == 0)
		{
			evaluate(boardStateData, ref evaluation, true);
			return evaluation;
		}
		if (depth <= 0)
		{
			evaluate(boardStateData, ref evaluation, false);
			return evaluation;
		}

		float abValue;
		evaluation.move = moves[0];
		evaluation.evaluatedValue = boardStateData._turn == 0 ? 1000.0f : -1000.0f;

		for (int i = 0; i < moves.Count; ++i)
		{
			abValue = alphaBeta(newStates[newStates.Count - i - 1], depth - 1, alpha, beta).evaluatedValue;
			if (boardStateData._turn == 0)
			{
				if (abValue < evaluation.evaluatedValue)
				{
					evaluation.evaluatedValue = abValue;
					evaluation.move = moves[i];
				}
				beta = Math.Min(beta, evaluation.evaluatedValue);
			}
			else
			{
				if (abValue > evaluation.evaluatedValue)
				{
					evaluation.evaluatedValue = abValue;
					evaluation.move = moves[i];
				}
				alpha = Math.Max(alpha, evaluation.evaluatedValue);
			}
			if (alpha >= beta)
			{
				break;
			}
		}
		//if (savePosition)
		//{
		//	boardEvaluations[zHash] = evaluation;
		//}
		return evaluation;
	}

	private void calculateZobristValues()
	{
		Random rand = new Random();
		ulong v;
		ulong left;
		for (int i = 0; i < Chess.BOARD_LENGTH * Chess.BOARD_LENGTH * 12; ++i)
		{
			do
			{
				v = (ulong)rand.Next(0, int.MaxValue) << 32;
				left = (ulong)rand.Next(0, int.MaxValue);
				v = v | left;
			} while (zobristValueExists(v));
			zobristPieceValues[i] = v;
		}
		for (int i = 0; i < Chess.BOARD_LENGTH + 1; ++i)
		{
			do
			{
				v = (ulong)rand.Next(0, int.MaxValue) << 32;
				left = (ulong)rand.Next(0, int.MaxValue);
				v = v | left;
			} while (zobristValueExists(v));
			zobristEnPassantValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = (ulong)rand.Next(0, int.MaxValue) << 32;
				left = (ulong)rand.Next(0, int.MaxValue);
				v = v | left;
			} while (zobristValueExists(v));
			zobristTurnValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = (ulong)rand.Next(0, int.MaxValue) << 32;
				left = (ulong)rand.Next(0, int.MaxValue);
				v = v | left;
			} while (zobristValueExists(v));
			zobristKingMovedValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = (ulong)rand.Next(0, int.MaxValue) << 32;
				left = (ulong)rand.Next(0, int.MaxValue);
				v = v | left;
			} while (zobristValueExists(v));
			zobristQRookMovedValues[i] = v;
		}
		for (int i = 0; i < 2; ++i)
		{
			do
			{
				v = (ulong)rand.Next(0, int.MaxValue) << 32;
				left = (ulong)rand.Next(0, int.MaxValue);
				v = v | left;
			} while (zobristValueExists(v));
			zobristKRookMovedValues[i] = v;
		}
	}

	private void evaluate(BoardStateData boardStateData, ref AlphaBetaEvaluation evaluation, bool noMoves)
	{
		if (noMoves)
		{
			if (boardStateData._turn == 1)
			{
				evaluation.evaluatedValue = -1000.0f;
			}
			else
			{
				evaluation.evaluatedValue = 1000.0f;
			}
		}
		else
		{
			_ann.SetInput(boardStateData);
			_ann.PropagateForward();
			evaluation.evaluatedValue = _ann._outputLayer._outputs[0];
		}
	}

	private ulong zobristHash(BoardStateData boardStateData)
	{
		ulong h = 0;
		PieceCode piece;
		int intPiece = 0;
		h = h ^ zobristTurnValues[(int)boardStateData._turn];
		for (int i = 0; i < Chess.BOARD_LENGTH * Chess.BOARD_LENGTH; ++i)
		{
			if (boardStateData._pieces[i] != PieceCode.EMPTY)
			{
				piece = boardStateData._pieces[i];
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
			int enPassant = boardStateData._enPassant + 1;
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

	private bool zobristValueExists(ulong v)
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

	private void increasePositionMap(BoardStateData boardStateData)
	{
		ulong h = zobristHash(boardStateData);
		if (!hashPositions.ContainsKey(h))
		{
			hashPositions.Add(h, 1);
		}
		else
		{
			hashPositions[h]++;
		}
	}

}