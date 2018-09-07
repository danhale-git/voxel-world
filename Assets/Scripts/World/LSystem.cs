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

	int right = 0;
	int left = 1;
	int front = 2;
	int back = 3;

	Int2 forward = new Int2(0,1);

	Zone.Side currentBack = Zone.Side.BOTTOM;



	

	
	public LSystem(PointOfInterest POI, Zone zone, float noise)
	{
		this.POI = POI;
		this.zone = zone;
		this.noise = noise;

		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);
		//noiseGen.SetSeed(7425356); works
		//noiseGen.SetSeed(85646465);
		//noiseGen.SetSeed(9434);
		//noiseGen.SetSeed(1114);
		//noiseGen.SetSeed(5281);	
		//noiseGen.SetSeed(6999);	
		noiseGen.SetSeed(4727);		

		
		//int seed = Random.Range(0,10000);
		//Debug.Log("SEED: "+ seed);
		//noiseGen.SetSeed(seed);

		DrawBlockMatrix();
	}

# region Rotation

	int ForwardAxis(Int2 point)
	{
		if(right == 0 || right == 1) return point.z;
		else return point.x;
	}
	int SideAxis(Int2 point)
	{
		if(right == 0 || right == 1) return point.x;
		else return point.z;
	}

	bool ToLeft(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.RIGHT)
			return (compare < to);
		else return (compare > to);
	}
	bool ToRight(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.RIGHT)
			return (compare > to);
		else return (compare < to);
	}
	bool InFront(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.LEFT)
			return (compare > to);
		else return (compare < to);
	}
	bool Behind(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.LEFT)
			return (compare < to);
		else return (compare > to);
	}

	void Rotate(Zone.Side backSide)
	{
		currentBack = backSide;
		switch(backSide)
		{	
			case Zone.Side.RIGHT:
				right = 2;
				left = 3;
				front = 1;
				back = 0;
				forward = new Int2(-1, 0);
				break;
			case Zone.Side.LEFT:
				right = 3;
				left = 2;
				front = 0;
				back = 1;
				forward = new Int2(1, 0);
				break;
			case Zone.Side.TOP:
				right = 1;
				left = 0;
				front = 3;
				back = 2;
				forward = new Int2(0, -1);
				break;
			case Zone.Side.BOTTOM:
				right = 0;
				left = 1;
				front = 2;
				back = 3;
				forward = new Int2(0, 1);
				break;

		}
	}

# endregion


	void DrawBlockMatrix()
	{
		int width = zone.size * World.chunkSize;
		int height = zone.size * World.chunkSize;

		Int2 startPoint = RandomPointOnSide((int)zone.back, zone.bounds);
		noise = noiseGen.GetNoise01(startPoint.x, startPoint.z);


		
		//	Create and build block matrix
		zone.blockMatrix = new int[width,height];
		zone.blockMatrix[startPoint.x,startPoint.z] = 1;

		GenerateBuilding(startPoint, zone.back, 10, 5);

		foreach(int[] room in allBounds)
		{
			DrawRoom(room);
		}

		for(int i = 0; i < allBounds.Count; i++)
		{
			DebugRooms(i);
		}	

		SetColumnMaps();
	}

	void GenerateBuilding(Int2 startPoint, Zone.Side startSide, int iterations, int minSize)
	{
		originPoints.Add(startPoint);
		originSides.Add(startSide);

		for(int i = 0; i < iterations; i++)
		{
			if(i == originSides.Count)
			{
				Debug.Log("Room generation out of sync");
				break;
			}

			bool[] eligibleSides;

			int[] newBounds = Room(originSides[i], originPoints[i], i, out eligibleSides);

			int boundsWidth = newBounds[0] - newBounds[1];
			int boundsHeight = newBounds[2] - newBounds[3];


			if(boundsWidth < minSize || boundsHeight < minSize)
			{
				break;
			}
			else
			{
				allBounds.Add(newBounds);
				int chosenSide = MostOpenSide(newBounds, eligibleSides);
				originPoints.Add(RandomPointOnSide(chosenSide, newBounds));
				originSides.Add(Zone.Opposite((Zone.Side)chosenSide));
			}
		}
	}

	int[] Room(Zone.Side backSide, Int2 originPoint, int index, out bool[] eligibleSides)
	{
		Rotate(backSide);

		int rightAdjacent;
		int leftAdjacent;

		int[] bounds = new int[4];

		bool adjacent = CheckAdjacent(originPoint, index, out rightAdjacent, out leftAdjacent);

		bounds[right] = rightAdjacent == 0 ? RandomRange(SideAxis(originPoint), zone.bounds[right]) : rightAdjacent;
		bounds[left] = leftAdjacent == 0 ? RandomRange(SideAxis(originPoint), zone.bounds[left]) : leftAdjacent;

		int closestInFront;

		if(CheckForward(originPoint, bounds, index, out closestInFront))
			bounds[front] = closestInFront;
		else
			bounds[front] = RandomRange(ForwardAxis(originPoint), zone.bounds[front]);

		bounds[back] = ForwardAxis(originPoint);

		eligibleSides = new bool[4];
		for(int i = 0; i < 4; i++)
		{
			if(i == (int)backSide || SideBlocked(bounds[i], i)) continue;
			else eligibleSides[i] = true;
		}
		return bounds;
	}


	bool CheckAdjacent(Int2 originPoint, int index, out int rightBound, out int leftBound)
	{
		rightBound = 0;
		leftBound = 0;

		for(int b = 0; b < index; b++)
		{
			if(Behind(allBounds[b][back], ForwardAxis(originPoint)) && InFront(allBounds[b][front], ForwardAxis(originPoint)))	
			{
				if(ToLeft(allBounds[b][right], SideAxis(originPoint)))
				{
					if(leftBound == 0 || ToRight(allBounds[b][right], leftBound))
					{
						leftBound = allBounds[b][right];
					}
				}
				else if(ToRight(allBounds[b][left], SideAxis(originPoint)))
				{
					if(rightBound == 0 || ToLeft(allBounds[b][left], rightBound))
					{
						rightBound = allBounds[b][left];
					}
				}
			}
		}

		if(leftBound != 0 || rightBound != 0) return false;
		else return true;
	}

	bool CheckForward(Int2 originPoint, int[] bounds, int index, out int closest)
	{
		closest = 0;
		for(int b = 0; b < index; b++)
		{
			if(	ToRight(allBounds[b][right], bounds[left]) 	&& ToLeft(allBounds[b][right], bounds[right]) ||
				ToRight(allBounds[b][left], bounds[left]) 	&& ToLeft(allBounds[b][left], bounds[right])  ||
				ToLeft(allBounds[b][left], bounds[left]) 	&& ToRight(allBounds[b][right], bounds[right]))
			{
				if(InFront(allBounds[b][back], ForwardAxis(originPoint)) && (closest == 0 || Behind(allBounds[b][back], closest)))
					closest = allBounds[b][back];
			}	
		}
		if(closest == 0) return false;
		else return true;
	}


	//	Check if side is up against another side
	bool SideBlocked(int bound, int side)
	{
		int otherSide = (int)Zone.Opposite(side);

		foreach(int[] bounds in allBounds)
		{
			if(bounds[otherSide] == bound) return true;
		}
		return false;
	}
	int MostOpenSide(int[] bounds, bool[] eligibleSides)
	{
		int farthestSide = 0;
		int farthestDistance = 0;
		for(int i = 0; i < 4; i++)
		{
			if(!eligibleSides[i]) continue;
			int distance = Mathf.Max(bounds[i], zone.bounds[i]) - Mathf.Min(bounds[i], zone.bounds[i]);
			if(distance > farthestDistance)
			{
				farthestDistance = distance;
				farthestSide = i;
			}
		}
		return farthestSide;
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

	Int2 RandomPointOnSide(int side, int[] bounds)
	{
		if(side < 2)
			return new Int2(bounds[side], RandomRange(bounds[2], bounds[3]));
		else
			return new Int2(RandomRange(bounds[0], bounds[1]), bounds[side]);
	}
	
	Vector3 MatrixToGlobal(Int2 local)
	{
		return new Vector3(	(int)POI.position.x + (zone.x*World.chunkSize) + local.x,
							0,
							(int)POI.position.z + (zone.z*World.chunkSize) + local.z);
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

	void DebugRooms(int index)
	{
		int[] bounds = allBounds[index];
		int middleX = ((bounds[0] - bounds[1]) / 2) + bounds[1];
		int middleZ = ((bounds[2] - bounds[3]) / 2) + bounds[3];

		zone.blockMatrix[middleX, middleZ] = 2;

		zone.blockMatrix[originPoints[index].x, originPoints[index].z] = 3;
	}
}
