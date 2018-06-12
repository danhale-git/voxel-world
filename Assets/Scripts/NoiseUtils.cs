using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseUtils
{
	//	All temporary for testing
	//  Generate 2D noise for ground height
	public static int GroundHeight(int x, int z, int maxHeight)
	{
		float height;
		height = BrownianMotion(x * 0.01f,z * 0.01f);

		return (int)Map(0, maxHeight, 0, 1, height);;
	}
	static float Map(float newmin, float newmax, float origmin, float origmax, float value)
    {
        return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));
    }

	//	Brownian Motion (Perlin Noise)
	public static float BrownianMotion(float x, float z, int octaves = 2, float persistance = 0.2f)
	{
		float total = 0;
		float frequency = 1;
		float amplitude = 1;
		float maxValue = 0;
		
		//	Layer noise to create more complex/natural waves
		for(int i = 0; i < octaves ; i++)
		{
			total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;

			maxValue += amplitude;
			amplitude *= persistance;
			frequency += 2;
		}

		return total/maxValue;
	}
}
