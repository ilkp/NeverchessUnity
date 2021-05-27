using System;

public class ANNLayer
{
	public int _layerSize;
	public float[] _weights;
	public float[] _biases;
	public float[] _outputs;
	public Func<float, float> _activationFunction;
	public ANNLayer _previousLayer;
	public ANNLayer _nextLayer;

	public ANNLayer(int layerSize, Func<float, float> activationFunction)
	{
		_activationFunction = activationFunction;
		_layerSize = layerSize;
		_outputs = new float[layerSize];
	}

	public ANNLayer(int layerSize, ANNLayer previousLayer, Func<float, float> activationFunction)
	{
		_activationFunction = activationFunction;
		_previousLayer = previousLayer;
		_layerSize = layerSize;
		_weights = new float[previousLayer._layerSize * layerSize];
		_biases = new float[layerSize];
		_outputs = new float[layerSize];
	}

	public void PropagateForward()
	{
		for (int i = 0; i < _layerSize; ++i)
		{
			_outputs[i] = 0.0f;
			for (int j = 0; j < _previousLayer._layerSize; ++j)
				_outputs[i] += _weights[i * _previousLayer._layerSize + j] * _previousLayer._outputs[j];
			_outputs[i] += _biases[i];
			_outputs[i] = _activationFunction(_outputs[i]);
		}
	}
}
