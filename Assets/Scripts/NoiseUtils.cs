using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseUtils
{
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

	#region Terrains

	public static int RidgyHills(int x, int z, float ridgyness = 0.5f)	
	{
		//	Simple heightmap with large rounded hills
		float frequency = 0.0025f;
		float maxHeight = 300;
		float bigHillsSource = LevelOutAtMin(.2f, .8f, Mathf.PerlinNoise(x * frequency, z * frequency) * 0.5f);
		float bigHills = Mathf.Lerp(0, maxHeight, bigHillsSource);

		//	Secondary hightmap creates ridges when multiplied by main heightmap noise
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
	
	public static int HillyPlateus(int x, int z, int maxHeight, bool ridgify = false)
	{
		float source = BrownianMotion(x * 0.01f, z * 0.01f, 3, 0.2f);
		float subtract = BrownianMotion(x * 0.012f, z * 0.012f, 3, 0.25f);
		float difference = 0;
		if(subtract > source)
		{
			difference = subtract - source;
		}

		int height = (int) ( (Mathf.Lerp(0, maxHeight, source) + maxHeight) - Mathf.Lerp(0, maxHeight, difference) );

		if(!ridgify)
		{
			return (int)LevelOutAtMax(95, 0.7f, height);	
		}
		else
		{
			return Ridgify(x, z, source, LevelOutAtMax(95, 0.7f, height), ridgyness: 0.1f);
		}
	}

	public static int Ridgify(int x, int z, float originalSource, float originalHeight, float ridgyness = 0.05f)
	{
		//	Secondary hightmap creates ridges when multiplied by main heightmap noise
		float hillModifier = ridgyness * (1- originalSource);
		float roughFrequency = 0.01f * hillModifier;
		ridgyness = BrownianMotion(x * roughFrequency, z * roughFrequency, 5, 1);

		return (int) (((originalHeight * ridgyness) + originalHeight) / 2);	
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
