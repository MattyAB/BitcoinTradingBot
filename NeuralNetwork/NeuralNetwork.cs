﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml.XPath;
using TradingLib;

namespace TradingBot
{
	public class NeuralNetwork
	{
		public int InputLayerSize = 7;
		public int HiddenLayerSize = 16;
		public int OutputLayerSize = 1;

		public List<Synapse> Synapses;
		public List<Neuron> Neurons;

		public NeuralNetwork()
		{
			Synapses = new List<Synapse>();
			Neurons = new List<Neuron>();

			for (int i = 1; i <= InputLayerSize; i++)
				Neurons.Add(new InputNeuron(1, i, 1));
			for (int i = 1; i <= HiddenLayerSize; i++)
				Neurons.Add(new HiddenNeuron(2, i, InputLayerSize));
			for (int i = 1; i <= OutputLayerSize; i++)
				Neurons.Add(new OutputNeuron(3, i, HiddenLayerSize));

			// Create first layer synapses
			for (int i = 1; i <= InputLayerSize; i++)
				for (int j = 1; j <= HiddenLayerSize; j++)
					Synapses.Add(new Synapse(1, i, j));
			// Create second layer synapses
			for (int i = 1; i <= HiddenLayerSize; i++)
				for (int j = 1; j <= OutputLayerSize; j++)
					Synapses.Add(new Synapse(2, i, j));
		}

	    public void TimeNetwork(List<DatabaseRow> inputAsDatabaseRows)
	    {
	        InputData X = new InputData(inputAsDatabaseRows);

            Stopwatch s = new Stopwatch();
            s.Start();
            // 10^16 times
            Parallel.For(0, 10000, j =>
	        {
                //if(j % 10000000 == 0)
                //    Console.WriteLine(j);
                TrainCost(X);
	        });
            s.Stop();
            Console.WriteLine(s.ElapsedMilliseconds/1000 + "s.");
        }

		public void TrainNetwork(List<DatabaseRow> inputAsDatabaseRows, int TrainLength)
		{
			InputData X = new InputData(inputAsDatabaseRows);

		    for (int i = 0; i < TrainLength; i++)
		    {
		        if (i % 10 == 0)
		        {
		            List<double> weights = new List<double>();
		            for (int j = 0; j < Synapses.Count; j++)
		            {
		                weights.Add(Synapses[j].Weight);
		            }

                    WriteWeights(weights);
                }

		        double prevCost = TrainCost(X);

                double[,] improvments = new double[Synapses.Count, 2];
		        Parallel.For(0, Synapses.Count, j =>
		        {
		            Synapses[j].Weight += 0.1;

		            improvments[j, 0] = prevCost - TrainCost(X);

		            Synapses[j].Weight -= 0.2;

		            improvments[j, 1] = prevCost - TrainCost(X);

		            Synapses[j].Weight += 0.1;
		        });

                // Get biggest improvment
		        int x = 0;
		        int y = 0;
                for (int j = 0; j < improvments.Length / 2; j++)
                {
                    if (improvments[j, 0] > improvments[x, y])
                    {
                        x = j;
                        y = 0;
                    }
                    if (improvments[j, 1] > improvments[x, y])
                    {
                        x = j;
                        y = 1;
                    }
                }

		        if (improvments[x, y] < 0)
		        {
		            Console.WriteLine("All done. Either give me more data or reset weights.");
		            break;
		        }

		        double bestCost = 1000000;
		        for (double j = -1; j <= 1; j += 0.1)
		        {
		            double oldWeight = Synapses[x].Weight;
                    Synapses[x].Weight = j;
		            if (TrainCost(X) < bestCost)
		            {
		                bestCost = TrainCost(X);
		            }
		            else
		            {
		                Synapses[x].Weight = oldWeight;
		            }
		        }

		        double newWeight = 0;


		        Console.WriteLine("Row " + i + ", Biggest improvement: " + x + ":" + y + ", " + TrainCost(X) + " (" + (prevCost - TrainCost(X)) + ")");
            }

            Console.WriteLine(TestCost(X));
	    }

        static void WriteWeights(List<double> weights)
	    {
	        string WeightSaveString = @"C:\Users\matth\OneDrive\Documents\Visual Studio 2017\Projects\TradingBot\weights.txt";

	        if (File.Exists(WeightSaveString))
	        {
	            // Delete file
	            File.Delete(WeightSaveString);
	        }

	        using (System.IO.StreamWriter file =
	            new System.IO.StreamWriter(WeightSaveString))
	        {
	            foreach (double weight in weights)
	            {
	                file.WriteLine(weight);
	            }
	        }
	    }

	    // Calculates how accurate network currently is
	    private double Cost(double[,] X)
	    {
	        double cost = 0;

	        for (int i = 0; i < X.Length / 8; i++)
	        {
	            double[] input =
	            {
	                X[i, 0], X[i, 1], X[i, 2], X[i, 3], X[i, 4], X[i, 5], X[i, 6], X[i, 7]
                };

	            double output = Forward(input); // Propogate network

	            double e = Math.Abs(output - X[i, 7]); // Difference between yHat and y

	            cost += (e * e) / 2; // 1/2 * e^2
	        }

	        return cost / X.Length / 8 * 1000;
            //return cost;
        }

	    // Runs cost method on train data
	    private double TrainCost(InputData X)
	    {
	        return Cost(X.xTrain);
	    }

	    // Runs cost method on train data
	    private double TestCost(InputData X)
	    {
	        return Cost(X.xTest);
	    }

        // Gradient of a sigmoid
        double SigmoidPrime(double z)
	    {
	        double exp = Math.Exp(-z);
	        double tmp = Math.Pow((1 + exp), 2);
	        return exp / tmp;
	    }

	    public static T[] GetRow<T>(T[,] matrix, int row)
	    {
	        var columns = matrix.GetLength(1);
	        var array = new T[columns];
	        for (int i = 0; i < columns; ++i)
	            array[i] = matrix[row, i];
	        return array;
        }

        double Forward()
		{
		    #region z2Propogate
		    foreach (Neuron n in Neurons)
		    {
		        if (n.Layer == 1) // If it's in the input layer
		        {
		            foreach (Synapse s in Synapses)
		            {
		                if (s.Layer == 1 && s.InNeuron == n.Height) // If it's Neuron N
		                {
		                    s.InValue = n.OutValue;
		                }
		            }
		        }
		    }
            #endregion
            
		    #region a2Propogate
            /**
		    foreach (Synapse s in Synapses)
		    {
		        if (s.Layer == 1)
		        {
		            foreach (Neuron n in Neurons)
		            {
		                if (n.Layer == 2) // If it's in the hidden layer
		                {
		                    n.InValue[s.InNeuron - 1] = s.OutValue;
		                }
		            }
		        }
		    }**/

		    foreach (Neuron n in Neurons)
		    {
		        if (n.Layer == 2) // If it's in the hidden layer
		        {
		            foreach (Synapse s in Synapses)
		            {
		                if (s.Layer == 1 && s.OutNeuron == n.Height)
		                {
		                    n.InValue[s.InNeuron - 1] = s.OutValue;
		                }
		            }
                }
            }

		    foreach (Neuron n in Neurons)
		    {
		        if (n.Layer == 2)
		            n.Propogate();
		    }
            #endregion

		    #region z3Propogate
		    foreach (Neuron n in Neurons)
		    {
		        if (n.Layer == 2) // If it's in the hidden layer
		        {
		            foreach (Synapse s in Synapses)
		            {
		                if (s.Layer == 2 && s.InNeuron == n.Height) // If it's Neuron N
		                {
		                    s.InValue = n.OutValue;
		                }
		            }
		        }
		    }
            #endregion

		    #region yHatPropogate
		    foreach (Synapse s in Synapses)
		    {
		        if (s.Layer == 2)
		        {
		            foreach (Neuron n in Neurons)
		            {
		                if (n.Layer == 3) // If it's in the hidden layer
		                {
		                    n.InValue[s.InNeuron - 1] = s.OutValue;
		                }
		            }
		        }
		    }
            #endregion

		    foreach (Neuron n in Neurons)
		    {
		        if (n.Layer == 3)
		        {
		            n.Propogate();
		            return n.OutValue;
		        }
		    }

		    throw new Exception("Output neuron cannot be found.");
		}

	    public double Forward(double[] input)
	    {
	        double[] saveData = new double[7];

            // Set neurons to input data
	        foreach (Neuron n in Neurons)
	        {
	            if (n.Layer == 1)
	            {
	                if (n.Height == 1)
	                    n.InValue = new double[] { input[0] };
	                if (n.Height == 2)
	                    n.InValue = new double[] { input[1] };
	                if (n.Height == 3)
	                    n.InValue = new double[] { input[2] };
	                if (n.Height == 4)
	                    n.InValue = new double[] { input[3] };
	                if (n.Height == 5)
	                    n.InValue = new double[] { input[4] };
	                if (n.Height == 6)
	                    n.InValue = new double[] { input[5] };
	                if (n.Height == 7)
	                    n.InValue = new double[] { input[6] };
	            }
	        }

	        Forward();
	        double output = 0;
	        foreach (Neuron n in Neurons)
	        {
	            if (n.Layer == 3)
	            {
	                output = n.OutValue;
	            }
	        }

	        return output;
	    }

	    public void ImportWeights(List<double> weights)
	    {
	        try
	        {
	            for (int i = 0; i < weights.Count; i++)
	            {
	                Synapses[i].Weight = weights[i];
	            }
            }
	        catch (NullReferenceException e)
	        {
	            Console.WriteLine("No weights found at file. Generating new.");
	        }
	    }
	}
}