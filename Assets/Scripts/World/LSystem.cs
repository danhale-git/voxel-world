using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSystem
{
	FastNoise noiseGen = new FastNoise();

	PointOfInterest POI;
	Zone zone;
	float noise;

	public LSystem(PointOfInterest POI, Zone zone, float noise)
	{
		this.POI = POI;
		this.zone = zone;
		this.noise = noise;

		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);
		noiseGen.SetSeed(85646465);

		DrawBlockMatrix();
	}


	void DrawBlockMatrix()
	{
		int width = zone.size * World.chunkSize;
		int height = zone.size * World.chunkSize;

		Int2 originPoint = RandomPointAtEdge(zone.back, width, height, noise);
		Vector3 startGlobal = MatrixToGlobal(originPoint);
		noise = noiseGen.GetNoise01(originPoint.x, originPoint.z);


		
		//	Create and build block matrix
		zone.blockMatrix = new int[width,height];
		zone.blockMatrix[originPoint.x,originPoint.z] = 1;

		int[] bounds = GenerateRoom(originPoint, zone.back, true);

		DrawRoom(bounds);

		List<Int2> originPoints = new List<Int2>();
		List<Zone.Side> originSides = new List<Zone.Side>();
		List<int[]> roomBounds = new List<int[]>();

		for(int i = 0; i < 10; i++)

		

		SetColumnMaps();
	}

	int[] GenerateRoom(Int2 originPoint, Zone.Side originSide, bool large = false)
	{
		int[] bounds = new int[4];
		for(int i = 0; i < 1; i++)
		{
			for(int s = 0; s < 4; s++)
			{
				int min = s < 2 ? originPoint.x : originPoint.z;
				int side = (int)originSide;
				if(side == s)
				{
					bounds[s] = min;
				}
				else
				{
					bounds[s] = RandomRange(min, zone.bounds[s], large);
				}
			}
		}
		return bounds;
	}

	void DrawRoom(int[] bounds)
	{
		for(int x = bounds[1]; x <= bounds[0]; x++)
		{
			zone.blockMatrix[x, bounds[3]] = 1;
			zone.blockMatrix[x, bounds[2]] = 1;
		}

		for(int z = bounds[3]; z <= bounds[2]; z++)
		{
			zone.blockMatrix[bounds[1], z] = 1;
			zone.blockMatrix[bounds[0], z] = 1;
		}
	}

	
	int RandomRange(int a, int b, bool large = false)
	{
		float noise = noiseGen.GetNoise01(a, b);
		if(large) noise = Mathf.Lerp(0.5f, 1f, noise);

		if(a < b)
			return Mathf.RoundToInt(a + ((b - a) * noise));
		else
			return Mathf.RoundToInt(b + ((a - b) * (1 - noise)));
	}




	void RoadBetweenPoints(Int2 startPoint, Int2 endPoint)
	{
		Vector2 differenceVector = new Vector2(endPoint.x, endPoint.z) - new Vector2(startPoint.x, startPoint.z);

		Int2 difference = new Int2((int)differenceVector.x, (int)differenceVector.y);

		int xSign = (int)Mathf.Sign(difference.x);
		int zSign = (int)Mathf.Sign(difference.z);

		int zRoad = startPoint.z;

		for(int xRoad = startPoint.x; DynamicCompare(xRoad, endPoint.x, xSign); xRoad+=xSign)
		{
			if(xRoad == Mathf.Abs(difference.x / 2))
			{
				for(zRoad = startPoint.z; DynamicCompare(zRoad, endPoint.z, zSign); zRoad+=zSign)
				{
					zone.blockMatrix[xRoad,zRoad] = 1;
				}
				zRoad -= zSign;
			}
			else
			{
				zone.blockMatrix[xRoad,zRoad] = 1;
			}
		}
	}
	

	bool DynamicCompare(int value, int compareTo, int sign)
	{
		if(sign > 0)
			return value <= compareTo;
		else
			return value >= compareTo;
	}

	Int2 RandomPointAtEdge(Zone.Side edge, int width, int height, float noise)
	{
		int top = height-1;
		int right = width-1;
		//	Pick starting point for main building
		switch(edge)
		{
			case Zone.Side.BOTTOM:
				return new Int2(Mathf.FloorToInt(right * noise), 0);
			case Zone.Side.TOP:
				return new Int2(Mathf.FloorToInt(right * noise), top);
			case Zone.Side.LEFT:
				return new Int2(0, Mathf.FloorToInt(top * noise));
			case Zone.Side.RIGHT:
				return new Int2(right, Mathf.FloorToInt(top * noise));
			default:
				return new Int2(0, 0);
		}
	}
	
	Vector3 MatrixToGlobal(Int2 local)
	{
		return new Vector3(	(int)POI.position.x + (zone.x*World.chunkSize) + local.x,
							0,
							(int)POI.position.z + (zone.z*World.chunkSize) + local.z);
	}

	void SetColumnMaps()
	{
		int chunkSize = World.chunkSize;

		for(int x = 0; x < zone.size; x++)
			for(int z = 0; z < zone.size; z++)
			{
				Column column = POI.columnMatrix[x+zone.x,z+zone.z];
				column.POIMap = new int[chunkSize,chunkSize];

				for(int cx = 0; cx < chunkSize; cx++)
					for(int cz = 0; cz < chunkSize; cz++)
					{
						int mx = cx + (x*chunkSize);
						int mz = cz + (z*chunkSize);
	
						column.POIMap[cx,cz] = zone.blockMatrix[mx,mz];
					}
			}
	}
}
