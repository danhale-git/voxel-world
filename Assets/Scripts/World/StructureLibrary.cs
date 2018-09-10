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

		public void Generate(LSystem lSystem, Zone zone)
		{
			lSystem.SquareInBounds(zone.bounds, zone.back, positionOnSide: 0.5f, minWidth:40, maxWidth:50, minLength:50, maxLength:80);
			int sq1 = lSystem.ConnectedSquare(zone.bounds, 0, bestSide:true, minWidth:10, maxWidth:40, minLength:20, maxLength:80);
			lSystem.ConnectedSquare(zone.bounds, 0, bestSide:true, minWidth:20, maxWidth:40, minLength:20, maxLength:40);

			lSystem.SquareInBounds(lSystem.allBounds[sq1], Zone.Side.BOTTOM, minWidth:10, maxWidth:20, minLength:10, maxLength:20);

	








			lSystem.DrawBlockMatrix();
		}


	}

}
