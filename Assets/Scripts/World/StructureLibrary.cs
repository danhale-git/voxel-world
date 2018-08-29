using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StructureLibrary
{
	public enum Tiles {NONE, WALL, PATH};

	public class StructureTest
	{
		public int wallHeight = 5;


		public int divisor = 8;

		FastNoise noise = new FastNoise();

		public StructureTest()
		{
			noise.SetNoiseType(FastNoise.NoiseType.Value);
			noise.SetFrequency(0.6f);
			noise.SetInterp(FastNoise.Interp.Quintic);
			//noise.SetCellularJitter(0);
		}

		public float GetNoise(float x, float z)
		{
			int nx = Mathf.FloorToInt(x/divisor);
			int nz = Mathf.FloorToInt(z/divisor);
			return noise.GetNoise01(nx, nz);
		}

		public Tiles Tile(float noise)
		{
			if(noise > 0.8f && noise < .95f ||
				noise > 0.2 && noise < 0.4f ) return Tiles.WALL;
			else if(noise > 0.75f && noise < 1f ||
				noise > 0.15 && noise < 0.45f ) return Tiles.PATH;

			else return Tiles.NONE;
		}
	}

}
