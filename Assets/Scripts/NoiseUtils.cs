using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseUtils
{
	public static void noisetest()
	{
		float frequency = 1;
		float amplitude = 1;

		/*for(int x = 0; x < 10; x++)
			for(int z = 0; z < 10; z++)
			{	
				float _x = x*0.01f;
				float _z = z*0.01f;

				float value = Mathf.PerlinNoise(_x * frequency, _z * frequency) * amplitude;
				Debug.Log(value);
			}*/
	}

	//
	public static int TestGround(int x, int z)
	{
		float frequency = 0.0025f;
		float maxHeight = 300;
		float bigHills = LevelOutAtMin(.2f, .8f, Mathf.PerlinNoise(x * frequency, z * frequency) * 0.5f);

		//Rough = 

		return RidgyHills(x, z, .5f);	
	}

	

	#region Terrains

	public static int RidgyHills(int x, int z, float ridgyness = 0.5f)	
	{
		float frequency = 0.0025f;
		float maxHeight = 300;
		float bigHillsSource = LevelOutAtMin(.2f, .8f, Mathf.PerlinNoise(x * frequency, z * frequency) * 0.5f);
		float bigHills = Mathf.Lerp(0, maxHeight, bigHillsSource);


		float hillModifier = ridgyness * (1- bigHillsSource);
		float roughFrequency = 0.01f * hillModifier;
		ridgyness = BrownianMotion(x * roughFrequency, z * roughFrequency, 5, 1);

		return (int) (((bigHills * ridgyness) + bigHills) / 2);	
	}

	public static int LowLands(int x, int z)
	{
		//	Maximum ground height
		int maxHeight = 50;
		//	Height at which ground is levelled out
		int levelHeight = 15;

		//	Hilly terrain
		float microHeight = Mathf.Lerp(10, maxHeight, BrownianMotion(x * 0.01f,z * 0.01f, 5, 0.5f));
		//	More hillyness
		float factor = Mathf.Lerp(0, 1, BrownianMotion(x * 0.01f,z * 0.01f));
		int height = (int) (microHeight * factor);

		//	Level out areas lower than levelHeight and return
		return (int) LevelOutAtMin(levelHeight, 0.75f, height);
	}
	
	public static int HillyPlateus(int x, int z)
	{
		int maxHeight = 70;
		float main = Mathf.Lerp(0, maxHeight, BrownianMotion(x * 0.01f, z * 0.01f, 3, 0.2f)) + maxHeight;
		float subtract = Mathf.Lerp(0, maxHeight, BrownianMotion(x * 0.012f, z * 0.012f, 3, 0.25f)) + maxHeight;
		float difference = 0;
		if(subtract > main)
		{
			difference = subtract - main;
		}

		return (int)LevelOutAtMax(95, 0.7f, main - difference);	
	}

	#endregion

	#region Utility

	//	Return rough shallow noise
	public static float Rough(int x, int z, float roughness)
	{
		float frequency = 0.01f * roughness;

		float value = BrownianMotion(x * frequency, z * frequency, 5, 1);

		return value;	
	}

	//	Level out terrain softly at below min height
	static float LevelOutAtMin(float min, float flatness, float height)
	{
		if(height < min)
		{
			float factor = (min - height) * flatness;
			return height + factor;
		}

		return height;
	}

	//	Level out terrain softly above max height
	static float LevelOutAtMax(	int max,			//	Height after which terrain is levelled out
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

	#endregion

	#region Testing

	//	Raw Brownian
	public static int BrownianGround(float x, float z, int octaves, float persistance, float factor, int maxHeight)
	{
		float value = BrownianMotion(x * factor, z * factor, octaves, persistance);

		return (int)Mathf.Lerp(0, maxHeight, value);	
	}

	//	Raw perlin
	public static int Perlin(int x, int z, int maxHeight, float frequency = 0.01f, float amplitude = 1)
	{
		float value = Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;

		return (int)Mathf.Lerp(0, maxHeight, value);	
	}

	//	Draw big stone patches
	public static Blocks.Types Biome(int x, int z)
	{
		float biomeRoll = (float)Util.RoundToDP(BrownianMotion(x * 0.001f, z * 0.001f, 10), 2);
		if(biomeRoll < .5f)
		{
			return Blocks.Types.STONE;
		}
			return Blocks.Types.DIRT;
	}
	

	public static int GroundHeight(int x, int z, int maxHeight)
	{
		float height;
		height = BrownianMotion(x * 0.05f,z * 0.05f);

		return (int)Map(0, maxHeight, 0, 1, height);;
	}
	static float Map(float newmin, float newmax, float origmin, float origmax, float value)
    {
        return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));
    }

	#endregion
}
