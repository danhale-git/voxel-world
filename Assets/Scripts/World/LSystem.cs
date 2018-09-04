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

		Int2 startPoint = RandomPointAtEdge(zone.back, width, height, noise);
		Vector3 startGlobal = MatrixToGlobal(startPoint);
		noise = noiseGen.GetNoise01(startPoint.x, startPoint.z);


		
		//	Create and build block matrix
		zone.blockMatrix = new int[width,height];
		zone.blockMatrix[startPoint.x,startPoint.z] = 1;

		//int[] bounds = GenerateRoom(originPoint, zone.back, true);


		List<Int2> originPoints = new List<Int2>();
		List<Zone.Side> originSides = new List<Zone.Side>();
		List<int[]> roomBounds = new List<int[]>();

		originPoints.Add(startPoint);
		originSides.Add(zone.back);

		for(int i = 0; i < 4; i++)
		{
			if(i == originSides.Count) break;

			roomBounds.Add(GenerateRoom(originPoints, originSides, roomBounds, i));
		}

		foreach(int[] room in roomBounds)
		{
			DrawRoom(room);
		}

		

		SetColumnMaps();
	}

	/*//	TODO does this need the side checks?
	int[] MaxRoomBounds(int[] bounds, Zone.Side originSide, List<int[]> other, int index)
	{
		int side = (int)originSide;

		for(int i = 0; i < index; i++)
		{
			if(other[i][3] < bounds[2] || other[i][2] > bounds[3])	//	other room overlap horizontal
			{
				if(side != 1 && other[i][0] > bounds[1] && other[i][1] < bounds[0])			//	other right > left
					bounds[1] = other[i][0];

				else if(side != 0 && other[i][1] < bounds[0] && other[i][0] > bounds[1])		//	left < right
					bounds[0] = other[i][1];
			}
			if(other[i][1] < bounds[0] || other[i][0] > bounds[1])	//	other room overlap vertical
			{
				if(side != 3 && other[i][2] > bounds[3] && other[i][3] < bounds[2])		//	other top > bottom
				{
					Debug.Log("top > bottom");
					bounds[3] = other[i][2];
				}

				else if(side != 2 && other[i][3] < bounds[2] && other[i][2] > bounds[3])	//	other bottom < top
				{
					Debug.Log("bottom < top");
					bounds[2] = other[i][3]; 
				}
			}
		}

		return bounds;
	}*/

	//	How to detect if overlapping room is above/below/left/right?
	void CorrectBounds(int[] bounds, Zone.Side originSide, Int2 originPoint, List<int[]> other, int index)
	{
		int side = (int)originSide;

		for(int i = 0; i < index; i++)
		{
			if(index == 3) Debug.Log(": "+i);
			if(InOrEitherSide(other[i][1], other[i][0], bounds[1], bounds[0]))
			{
				Debug.Log("vertical overlap");
				if(side != 3 && other[i][2] > bounds[3])
				{
					Debug.Log("other top > bottom");
					bounds[3] = other[i][2];
				}
				else if(side != 2 && other[i][3] < bounds[2])
					bounds[2] = other[i][3];
				
			}
			else if(InOrEitherSide(other[i][2], other[i][3], bounds[2], bounds[3]))		//	wrong order?
			{

				//	vertical overlap
			}
		}
	}

	bool InOrEitherSide(int a1, int a2, int b1, int b2)
	{
		if(	(a1 > b1 && a1 < b2)||
			(a2 > b1 && a2 < b2)||
			(a1 < b2 && a2 > b2) ) return true;

		return false;
	}



	int[] GenerateRoom(List<Int2> originPoints, List<Zone.Side> originSides, List<int[]> allBounds, int index, bool large = false)
	{
		int[] bounds = zone.bounds.Clone() as int[];
		int side = (int)originSides[index];
		bounds[side] = side < 2 ? originPoints[index].x : originPoints[index].z;

		int farthestValue = 0;
		int farthestSide = 0;
		for(int s = 0; s < 4; s++)
		{
			int min = s < 2 ? originPoints[index].x : originPoints[index].z;
			
			if(side != s)	// swap this for readability
			{
				int max = zone.bounds[s];
				int distance = min < max ? max - min : min - max;

				if(distance > farthestValue)
				{
					farthestValue = distance;
					farthestSide = s;
				}
				bounds[s] = RandomRange(min, max, large);
			}
		}

		CorrectBounds(bounds, originSides[index], originPoints[index], allBounds, index);

		foreach(int b in bounds)
		{
			Debug.Log(b);
		}
		
		
		if(farthestSide < 2)
			originPoints.Add(new Int2(bounds[farthestSide], RandomRange(bounds[2], bounds[3])));
		else
			originPoints.Add(new Int2(RandomRange(bounds[0], bounds[1]), bounds[farthestSide]));

		originSides.Add(Zone.Opposite((Zone.Side)farthestSide));
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
