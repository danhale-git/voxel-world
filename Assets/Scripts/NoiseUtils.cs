using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseUtils
{
	//	Functions that return procedurally generated floats between 0 and 1 for generating height maps
	public static float TestGround(int x, int z)
	{
		float source = BrownianMotion(x * 0.01f, z * 0.01f, 3, 0.2f);

		return source;	
	}

	public static float LowLandsTest(int x, int z)
	{
		//	Height at which ground is levelled out
		float levelHeight = 0.3f;

		//	Hilly terrain
		float microHeight = BrownianMotion(x * 0.01f,z * 0.01f, 5, 0.5f);
		//	More hillyness
		float factor = BrownianMotion(x * 0.01f,z * 0.01f);
		float height = (microHeight * factor);

		//	Level out areas lower than levelHeight and return
		return LevelOutAtMin(levelHeight, 0.75f, height);
	}
	
	public static float HillyPlateusTest(int x, int z)
	{	
		float source = BrownianMotion(x * 0.01f, z * 0.01f, 3, 0.2f);
		float subtract = BrownianMotion(x * 0.012f, z * 0.012f, 3, 0.25f);
		float difference = 0;
		if(subtract > source)
		{
			difference = subtract - source;
		}

		float height = source - difference;

		return LevelOutAtMax(0.6f, 0.7f, height);
	}

	#region Utility

	//	Return rough shallow noise
	public static float Rough(int x, int z, float roughness)
	{
		float frequency = 0.01f * roughness;

		float value = BrownianMotion(x * frequency, z * frequency, 5, 1);

		return value;	
	}

	public static float ReverseTroughs(float value)
	{
		if(value < 0.5f)
		{
			return 0.5f + (0.5f - value);
		}
		else return value;
	}

	//	Level out terrain softly at below min height
	static float LevelOutAtMin(	float min,			//	Height after which terrain is levelled out
								float flatness,		//	Flatness of levelled areas (1 for completely flat, 0 for no flattening)
								float height)		//	Value to be checked and possibly levelled
	{
		if(height < min)
		{
			float factor = (min - height) * flatness;
			return height + factor;
		}

		return height;
	}

	//	Level out terrain softly above max height
	static float LevelOutAtMax(	float max,			//	Height after which terrain is levelled out
								float flatness,		//	Flatness of levelled areas (1 for completely flat, 0 for no flattening)
								float value)		//	Value to be checked and possibly levelled
	{
		if(value > max)
		{
			float factor = (value - max) * flatness;
			return value - factor;
		}

		return value;
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

	//	3D Brownian motion
	public static float BrownianMotion3D(float x, float y, float z, float frequency, int octaves)
    {
        float XY = BrownianMotion(x * frequency ,y * frequency, octaves, 0.5f);
        float YZ = BrownianMotion(y * frequency ,z * frequency, octaves, 0.5f);
        float XZ = BrownianMotion(x * frequency ,z * frequency, octaves, 0.5f);

        float YX = BrownianMotion(y * frequency ,x * frequency, octaves, 0.5f);
        float ZY = BrownianMotion(z * frequency ,y * frequency, octaves, 0.5f);
        float ZX = BrownianMotion(z * frequency ,x * frequency, octaves, 0.5f);

        return (XY+YZ+XZ+YX+ZY+ZX)/6.0f;
    }

	#endregion
}
