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

		public void Generate(LSystem lSystem)
		{
			lSystem.FirstRoom(positionOnStartSide: 0.5f, minWidth:40, maxWidth:60, minLength:50, maxLength:90);
			lSystem.GenerateRooms(0, 1, parentSide:Zone.Side.RIGHT, bestSide:false, randomSide:false, minWidth:10, maxWidth:40, minLength:10, maxLength:40);




			lSystem.DrawBlockMatrix();
		}


	}

}
