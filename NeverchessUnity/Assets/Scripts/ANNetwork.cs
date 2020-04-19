using System.Threading;

public class ANNetwork
{
	private ANNLayer _inputLayer;
	private ANNLayer _outputLayer;

	public void ReadANN(string file)
	{
		Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		string[] lines = System.IO.File.ReadAllLines(file);
		int line = 0;
		int inputSize = int.Parse(lines[line]);
		int hiddenSize = int.Parse(lines[++line]);
		int outputSize = int.Parse(lines[++line]);
		int nHiddenLayers = int.Parse(lines[++line]);

		// CREATE ANNETWORK OBJECT
		_inputLayer = new ANNLayer(inputSize, Sigmoid);
		_inputLayer._previousLayer = null;
		ANNLayer[] hiddenLayers = new ANNLayer[nHiddenLayers];
		hiddenLayers[0] = new ANNLayer(hiddenSize, _inputLayer, Sigmoid);
		for (int i = 1; i < nHiddenLayers; ++i)
		{
			hiddenLayers[i] = new ANNLayer(hiddenSize, hiddenLayers[i - 1], Sigmoid);
			hiddenLayers[i - 1]._nextLayer = hiddenLayers[i];
		}
		_inputLayer._nextLayer = hiddenLayers[0];
		_outputLayer = new ANNLayer(outputSize, hiddenLayers[hiddenLayers.Length - 1], Sigmoid);
		_outputLayer._nextLayer = null;
		hiddenLayers[hiddenLayers.Length - 1] = _outputLayer;


		// READ WEIGHTS
		ANNLayer l = _inputLayer._nextLayer;
		while (l != null)
		{
			for (int i = 0; i < l._layerSize; ++i)
			{
				for (int j = 0; j < l._previousLayer._layerSize; ++j)
				{
					l._weights[i * l._previousLayer._layerSize + j] = float.Parse(lines[++line]);
				}
			}
			l = l._nextLayer;
		}

		// READ BIASES
		l = _inputLayer._nextLayer;
		while (l != null)
		{
			for (int i = 0; i < l._layerSize; ++i)
			{
				l._biases[i] = float.Parse(lines[++line]);
			}
			l = l._nextLayer;
		}
	}

	public void PropagateForward()
	{
		ANNLayer l = _inputLayer._nextLayer;
		while (l != null)
		{
			l.PropagateForward();
			l = l._nextLayer;
		}
	}

	public float GetOutput()
	{
		return _outputLayer._outputs[0];
	}

	public void SetInput(BoardStateData boardStateData)
	{
		float[] input = new float[Constants.ANN_INPUT_LENGTH];
		input[0] = boardStateData._turn;
		if (!boardStateData._kingMoved[0])
		{
			if (!boardStateData._qRookMoved[0])
			{
				input[1] = 1.0f;
			}
			if (!boardStateData._kRookMoved[0])
			{
				input[2] = 1.0f;
			}
		}
		if (!boardStateData._kingMoved[1])
		{
			if (!boardStateData._qRookMoved[1])
			{
				input[3] = 1.0f;
			}
			if (!boardStateData._kRookMoved[1])
			{
				input[4] = 1.0f;
			}
		}
		if (boardStateData._enPassant != -1)
		{
			int epMask = 1 << boardStateData._enPassant;
			for (int i = 0; i < 8; ++i)
			{
				input[5 + i] = (epMask >> i == 1) ? 1.0f : 0.0f;
			}
		}

		for (int y = 0; y < Constants.BOARD_LENGTH; ++y)
		{
			for (int x = 0; x < Constants.BOARD_LENGTH; ++x)
			{
				for (int i = 0; i < Constants.PIECE_CODE_LENGTH; ++i)
				{
					input[13 + (y * Constants.BOARD_LENGTH + x) * Constants.PIECE_CODE_LENGTH + i]
						= (float)(((int)boardStateData._pieces[y * Constants.BOARD_LENGTH + x] >> i) & 1);
				}
			}
		}

		_inputLayer._outputs = input;
	}

	private float ReLu(float x)
	{
		if (x < 0.0f)
		{
			return 0;
		}
		return x;
	}

	private float dRelu(float x)
	{
		if (x < 0.0f)
		{
			return 0.0f;
		}
		return 1.0f;
	}

	private float Sigmoid(float x)
	{
		return 1.0f / (1 + UnityEngine.Mathf.Exp(-x));
	}

	private float DSigmoid(float x)
	{
		return Sigmoid(x) * (1 - Sigmoid(x));
	}
}
