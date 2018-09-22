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

			if(lSystem.SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:80, maxWidth:100, minLength:80, maxLength:100))
				//lSystem.DrawBoundsBorder(lSystem.currentBounds[0], zone.debugMatrix, 1);

			lSystem.GenerateRooms(lSystem.currentBounds[0]);

			foreach(int[] bounds in lSystem.currentBounds)
			{
				//lSystem.DrawPoint(LSystem.BoundsCenter(bounds), zone.debugMatrix, 2);
				lSystem.DrawBoundsBorder(bounds, zone.debugMatrix, 1);
			}

			lSystem.DrawRooms(zone.debugMatrix, 1);

			//lSystem.DrawCorridors(zone.debugMatrix, 1);
			foreach(Int2 point in lSystem.originPoints)
			{
				//lSystem.DrawPoint(point, zone.debugMatrix, 3);
			}

			lSystem.DefineArea();

			lSystem.ApplyMaps(this);
		}


	}

}
