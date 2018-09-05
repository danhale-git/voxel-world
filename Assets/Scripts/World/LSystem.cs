using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSystem
{
	FastNoise noiseGen = new FastNoise();

	PointOfInterest POI;
	Zone zone;
	float noise;

	List<Int2> originPoints = new List<Int2>();
	List<Zone.Side> originSides = new List<Zone.Side>();
	List<int[]> allBounds = new List<int[]>();

	public LSystem(PointOfInterest POI, Zone zone, float noise)
	{
		this.POI = POI;
		this.zone = zone;
		this.noise = noise;

		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);
		//noiseGen.SetSeed(7425356); works
		//noiseGen.SetSeed(85646465); works
		noiseGen.SetSeed(Random.Range(0,10000));

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


		originPoints.Add(startPoint);
		originSides.Add(zone.back);

		for(int i = 0; i < 8; i++)
		{
			if(i == originSides.Count) break;

			allBounds.Add(Generate(originSides[i], originPoints[i], i));
		}

		foreach(int[] room in allBounds)
		{
			DrawRoom(room);
		}

		

		SetColumnMaps();
	}

	int[] Generate(Zone.Side originSide, Int2 originPoint, int index)
	{
		int rightMax;
		int leftMax;
		int topMax;
		int bottomMax;

		int[] bounds = new int[4];

		if((int)originSide < 2)
		{
			Debug.Log("vertical overlap");
			VerticalOverlap(originPoint, out topMax, out bottomMax, index);

			if(originSide == Zone.Side.LEFT)
			{
				leftMax = originPoint.x;
				rightMax = HorizontalBlocker(originSide, originPoint, bounds, index);
			}
			else
			{
				rightMax = originPoint.x;
				leftMax = HorizontalBlocker(originSide, originPoint, bounds, index);
			}

		}
		else
		{
			Debug.Log("horizontal overlap");
			HorizontalOverlap(originPoint, out rightMax, out leftMax, index);

			if(originSide == Zone.Side.BOTTOM)
			{
				bottomMax = originPoint.z;
				topMax = VerticalBlocker(originSide, originPoint, bounds, index);
			}
			else
			{
				topMax = originPoint.z;
				bottomMax = VerticalBlocker(originSide, originPoint, bounds, index);
			}
		}

		bounds[0] = rightMax == 0 ? RandomRange(originPoint.x, zone.bounds[0]) : rightMax;
		bounds[1] = leftMax == 0 ? RandomRange(originPoint.x, zone.bounds[1]) : leftMax;
		bounds[2] = topMax == 0 ? RandomRange(originPoint.z, zone.bounds[2]) : topMax;
		bounds[3] = bottomMax == 0 ? RandomRange(originPoint.z, zone.bounds[3]) : bottomMax;

		/*for(int i = 0; i < 2; i++)
		{
			Debug.Log((Zone.Side)i+" "+ bounds[i]);
		}
		Debug.Log("--");*/

		bounds[(int)originSide] = (int)originSide < 2 ? originPoint.x : originPoint.z;

		/*for(int i = 0; i < 2; i++)
		{
			Debug.Log((Zone.Side)i+" "+ bounds[i]);
		}
		Debug.Log("--__--__--");*/

		int farthestSide = 0;
		int farthestDistance = 0;
		for(int i = 0; i < 4; i++)
		{
			if(i == (int)originSide || SideBlocked(bounds[i], i)) continue;
			int distance = Mathf.Max(bounds[i], zone.bounds[i]) - Mathf.Min(bounds[i], zone.bounds[i]);
			if(distance > farthestDistance)
			{
				farthestDistance = distance;
				farthestSide = i;
			}
		}

		if(farthestSide < 2)
			originPoints.Add(new Int2(bounds[farthestSide], RandomRange(bounds[2], bounds[3])));
		else
			originPoints.Add(new Int2(RandomRange(bounds[0], bounds[1]), bounds[farthestSide]));

		originSides.Add(Zone.Opposite((Zone.Side)farthestSide));

		Debug.Log(Zone.Opposite((Zone.Side)farthestSide)); 

		return bounds;
	}

	void VerticalOverlap(Int2 originPoint, out int topClosest, out int bottomClosest, int index)
	{
		topClosest = 0;
		bottomClosest = 0;

		for(int b = 0; b < index; b++)
		{		
			if(allBounds[b][1] <= originPoint.x && allBounds[b][0] >= originPoint.x)		//	Point between left + right bounds
			{
				if(allBounds[b][3] > originPoint.z)										//	Point below bottom bounds
				{
					if(topClosest == 0 || allBounds[b][3] < topClosest)				//	topClosest unassigned or new value is closer
						topClosest = allBounds[b][3];
				}
				else if(allBounds[b][2] < originPoint.z)								//	Point above top bounds
				{
					if(bottomClosest == 0 || allBounds[b][2] > bottomClosest)		//	bottomClosest unassigned or new value is closer
						bottomClosest = allBounds[b][2];
				}
			}
		}
	}

	void HorizontalOverlap(Int2 originPoint, out int rightClosest, out int leftClosest, int index)
	{
		rightClosest = 0;
		leftClosest = 0;

		for(int b = 0; b < index; b++)
		{
			if(allBounds[b][3] <= originPoint.z && allBounds[b][2] >= originPoint.z)		//	Point between top + bottom bounds
			{
				if(allBounds[b][1] > originPoint.x)										//	Point to the left of left bounds
				{
					if(rightClosest == 0 || allBounds[b][1] < rightClosest)			//	rightClosest unassigned or new value is closer
						rightClosest = allBounds[b][1];
				}
				else if(allBounds[b][0] < originPoint.x)								//	Point to the right of right bounds
				{
					if(leftClosest == 0 || allBounds[b][0] > leftClosest)			//	leftClosest unassigned or new value is closer
						leftClosest = allBounds[b][0];
				}
			}
		}
	}

	int VerticalBlocker(Zone.Side originSide, Int2 originPoint, int[] bounds, int index)
	{
		int closest = 0;
		
		for(int b = 0; b < index; b++)
		{
			if(BoundsBlockingBounds(allBounds[b][1], allBounds[b][0], bounds[1], bounds[0]))	//	Check other left and right against current left and right bounds
			{
				switch(originSide)
				{
					case Zone.Side.TOP:
						if(closest == 0 || allBounds[b][2] < closest)						//	closest unassigned or new value is closer
							closest = allBounds[b][2];
						break;
					case Zone.Side.BOTTOM:
						if(closest == 0 || allBounds[b][3] < closest)						//	closest unassigned or new value is closer
							closest = allBounds[b][3];
						break;
					case Zone.Side.RIGHT:
					case Zone.Side.LEFT:

						Debug.Log("USE OTHER FUNCTION, WRONG ORIGIN SIDE");
						break;

				}
			}
		}

		return closest;
	}

	int HorizontalBlocker(Zone.Side originSide, Int2 originPoint, int[] bounds, int index)
	{
		int closest = 0;
		
		for(int b = 0; b < index; b++)
		{
			//Debug.Log(BlockingBounds(allBounds[b][3], allBounds[b][2], bounds[3], bounds[2]));
			if(BoundsBlockingBounds(allBounds[b][3], allBounds[b][2], bounds[3], bounds[2]))	//	Check other bottom and top against current bottom and top bounds
			{
				switch(originSide)
				{
					case Zone.Side.RIGHT:
						Debug.Log("left side");
						//if(allBounds[b][0] >= originPoint.x) continue;
						if(closest == 0 || allBounds[b][0] < closest)						//	closest unassigned or new value is closer
							closest = allBounds[b][0];
						break;
					case Zone.Side.LEFT:
						Debug.Log("right side");
						//if(allBounds[b][1] <= originPoint.x) continue;
						if(closest == 0 || allBounds[b][1] < closest)						//	closest unassigned or new value is closer
							closest = allBounds[b][1];
						break;
					case Zone.Side.TOP:
					case Zone.Side.BOTTOM:
						Debug.Log("USE OTHER FUNCTION, WRONG ORIGIN SIDE");
						break;

				}
			}
		}

		return closest;
	}

	//	Check if a is overlapping b
	bool BoundsBlockingBounds(int a1, int a2, int b1, int b2)
	{
		if(	(a1 > b1 && a1 < b2)||
			(a2 > b1 && a2 < b2)||
			(a1 < b1 && a2 > b2) ) return true;

		return false;
	}

	//	Check if point is in bounds
	bool SideBlocked(int bound, int side)
	{
		int otherSide = (int)Zone.Opposite(side);

		foreach(int[] bounds in allBounds)
		{
			if(bounds[otherSide] == bound) return true;
		}
		return false;
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
