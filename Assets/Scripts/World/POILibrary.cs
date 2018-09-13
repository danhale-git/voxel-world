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
			int mainArea = lSystem.SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:40, maxWidth:50, minLength:50, maxLength:80);
			int subArea1 = lSystem.ConnectedSquare(zone.bufferedBounds, 0, bestSide:true, positionOnSide:0.8f, minWidth:10, maxWidth:40, minLength:20, maxLength:80);
			int subArea2 = lSystem.ConnectedSquare(zone.bufferedBounds, 0, bestSide:true, positionOnSide:0.8f, minWidth:20, maxWidth:40, minLength:20, maxLength:40);

			

			foreach(int[] bounds in lSystem.currentBounds)
			{
				int[] vert;
				int[] horiz;
				lSystem.SegmentBounds(12, 12, bounds, zone.back, out horiz, out vert);
			
				//lSystem.DrawBoundsBorderWithSegments(bounds, horiz, vert, zone.wallMatrix, 1);


				lSystem.DrawPoint(lSystem.BoundsCenter(bounds), zone.debugMatrix, 2);
				lSystem.DrawBoundsBorder(bounds, zone.debugMatrix, 1);
			}
			foreach(Int2 point in lSystem.originPoints)
			{
				lSystem.DrawPoint(point, zone.debugMatrix, 3);
			}

			lSystem.DefineArea();

			//lSystem.SquareInBounds(lSystem.allBounds[sq1], Zone.Side.BOTTOM, minWidth:10, maxWidth:20, minLength:10, maxLength:20);

			lSystem.ApplyMaps(this);
		}


	}

}
