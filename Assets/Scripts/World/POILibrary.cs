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
			List<int>[] layers = new List<int>[4];

			layers[0] = new List<int>();


			if(lSystem.SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:40, maxWidth:50, minLength:50, maxLength:80))
				layers[0].Add(lSystem.CurrentIndex());
			else
			{
				Debug.Log("initial square creation failed at " + zone.POI.position);
				return;
			}


			int[] mainBounds = lSystem.currentBounds[layers[0][0]];

			int[] hDivs;
			int[] vDivs;

			lSystem.SegmentBounds(5, 0, mainBounds, zone.back, out hDivs, out vDivs);

			lSystem.DrawBoundsBorderWithSegments(mainBounds, hDivs, vDivs, zone.wallMatrix, 1);

			lSystem.DefineArea();


			if(lSystem.SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:5, maxWidth:10, minLength:50, maxLength:0))
			{

			}

			foreach(int[] bounds in lSystem.currentBounds)
			{
				lSystem.DrawPoint(LSystem.BoundsCenter(bounds), zone.debugMatrix, 2);
				lSystem.DrawBoundsBorder(bounds, zone.debugMatrix, 1);
			}
			foreach(Int2 point in lSystem.originPoints)
			{
				lSystem.DrawPoint(point, zone.debugMatrix, 3);
			}



			lSystem.ApplyMaps(this);
		}


	}

}
