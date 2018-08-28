using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureLibrary
{
	public enum Tiles {NONE, WALL, PATH};

	public class StructureTest
	{
		public int height = 2;

		FastNoise noise = new FastNoise();

		public StructureTest()
		{
			noise.SetNoiseType(FastNoise.NoiseType.Value);
			noise.SetFrequency(0.3f);
			noise.SetInterp(FastNoise.Interp.Quintic);
		}

		public float GetNoise(int x, int z)
		{
			return noise.GetNoise(x, z);
		}

		public Tiles Tile(float noise)
		{
			if(noise < 0.7) return Tiles.WALL;
			else return Tiles.NONE;
		}
	}

}
