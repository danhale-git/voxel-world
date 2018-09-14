using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POILibrary
{
	public enum Tiles {NONE, WALL, PATH};

	public class POI
	{
		public int wallHeight = 8;

		public int divisor = 8;

		FastNoise noise = new FastNoise();

		public POI()
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

		public void GenerateMatrixes(LSystem lSystem, Zone zone)
		{

			if(lSystem.SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:40, maxWidth:50, minLength:50, maxLength:80))
				//lSystem.DrawBoundsBorder(lSystem.currentBounds[0], zone.debugMatrix, 1);

			if(lSystem.ConnectedSquare(zone.bufferedBounds, 0, bestSide:true, minWidth:10, maxWidth:20, minLength:10, maxLength:20))
				//lSystem.DrawBoundsBorder(lSystem.currentBounds[1], zone.debugMatrix, 1);


			if(lSystem.ConnectedSquare(zone.bufferedBounds, 0, bestSide:true, minWidth:10, maxWidth:20, minLength:10, maxLength:20))
				//lSystem.DrawBoundsBorder(lSystem.currentBounds[2], zone.debugMatrix, 1);

			foreach(int[] bounds in lSystem.currentBounds)
			{
				//lSystem.DrawPoint(LSystem.BoundsCenter(bounds), zone.debugMatrix, 2);
				lSystem.DrawBoundsBorder(bounds, zone.debugMatrix, 1);
			}
			foreach(Int2 point in lSystem.originPoints)
			{
				//lSystem.DrawPoint(point, zone.debugMatrix, 3);
			}

			lSystem.DefineArea();

			lSystem.ApplyMaps(this);
		}


	}

}
