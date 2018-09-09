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
	Dictionary<int, List<int>> layers = new Dictionary<int, List<int>>();

	int right = 0;
	int left = 1;
	int front = 2;
	int back = 3;

	Int2 forward = new Int2(0,1);

	Zone.Side currentBack = Zone.Side.BOTTOM;

	int width;
	int height;

	
	public LSystem(PointOfInterest POI, Zone zone)
	{
		this.POI = POI;
		this.zone = zone;

		width = zone.size * World.chunkSize;
		height = zone.size * World.chunkSize;
		

		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);
		//noiseGen.SetSeed(7425356); works
		//noiseGen.SetSeed(85646465);
		//noiseGen.SetSeed(9434);
		//noiseGen.SetSeed(1114);
		//noiseGen.SetSeed(5281);	
		//noiseGen.SetSeed(6999);	
		//noiseGen.SetSeed(4727);	
		//noiseGen.SetSeed(7071);		
		//noiseGen.SetSeed(1658);	
		//noiseGen.SetSeed(1435);		
		
		int seed = Random.Range(0,10000);
		Debug.Log("SEED: "+ seed);
		noiseGen.SetSeed(seed);

		this.noise = noiseGen.GetNoise01(POI.position.x, POI.position.z);

		//DrawBlockMatrix();
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

	int AddTo(int add, int to, int side)
	{
		if(side == 0 || side == 2)
			return to + add;
		else return to - add;
	}
	int SubtractFrom(int subtract, int from, int side)
	{
		if(side == 0 || side == 2)
			return Mathf.Max(subtract, from) - Mathf.Min(subtract, from);
		else return from + subtract;
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

	int ZoneToCurrent(Zone.Side side)
	{
		switch(side)
		{	
			case Zone.Side.RIGHT:
				return right;
			case Zone.Side.LEFT:
				return left;
			case Zone.Side.TOP:
				return front;
			case Zone.Side.BOTTOM:
				return back;
			default:
				return 0;
		}
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


	public void DrawBlockMatrix()
	{
		


		zone.blockMatrix = new int[width,height];

		//GenerateRandomBuilding(startPoint, zone.back, 10, 5);

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

	/*public void GenerateRandomBuilding(Int2 startPoint, Zone.Side startSide, int iterations, int minSize)
	{
		

		for(int i = 0; i < iterations; i++)
		{
			if(i == originSides.Count)
			{
				Debug.Log("Room generation out of sync");
				break;
			}


			int[] newBounds = CreateRoom(i);

			int boundsWidth = newBounds[0] - newBounds[1];
			int boundsHeight = newBounds[2] - newBounds[3];

			bool[] eligibleSides = EligibleSides(back, newBounds);

			if(boundsWidth < minSize || boundsHeight < minSize)
			{
				break;
			}
			else
			{
				allBounds.Add(newBounds);
				int chosenSide = MostOpenSide(newBounds, eligibleSides);
				//originPoints.Add(RandomPointOnSide(chosenSide, newBounds));
				originSides.Add(Zone.Opposite((Zone.Side)chosenSide));
			}
		}
	}*/

	public void FirstRoom(bool startAtFront = false, float positionOnStartSide = 0, int minWidth = 0, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		Zone.Side startSide = startAtFront ? zone.front : zone.back;

		//	Position specified from 0 - 1 on start side or random position
		if(positionOnStartSide != 0)
			originPoints.Add(PositionOnSide((int)startSide, zone.bounds, positionOnStartSide));
		else
			originPoints.Add(RandomPointOnSide((int)startSide, zone.bounds, noise));

		originSides.Add(startSide);

		CreateRoom(0, 0, minWidth, maxWidth, minLength, maxLength);
	}

	public void GenerateRooms(int parentLayerIndex, int layerIndex, bool symmetrical = false, Zone.Side parentSide = 0, bool bestSide = true, bool randomSide = false, int minWidth = 2, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		//	Run creation for all squares in layer
		for(int p = 0; p < layers[parentLayerIndex].Count; p++)
		{
			//	Get bounds and check which sides are unoccupied
			int[] parent = allBounds[layers[parentLayerIndex][p]];
			bool[] eligibleSides = EligibleSides((int)originSides[p], parent);

			if(bestSide)
			{
				parentSide = (Zone.Side)MostOpenSide(parent, eligibleSides);
			}
			else if(randomSide)
			{
				Int2 pOrigin = originPoints[p];
				parentSide = (Zone.Side)RandomRange(0, 3, noiseGen.GetNoise01(pOrigin.x+p, pOrigin.z+p));
			}
			else
			{
				//	Adjust given side to square rotation
				parentSide = (Zone.Side)ZoneToCurrent(parentSide);
			}

			//	Chosen side is occupied, creating a square might cause overlaps
			if(!eligibleSides[(int)parentSide]) return;

			originPoints.Add(RandomPointOnSide((int)parentSide, parent, noise));
			originSides.Add(Zone.Opposite(parentSide));
			
			CreateRoom(allBounds.Count, layerIndex, minWidth, maxWidth, minLength, maxLength);
		}

		//	add things to lists
		//	increment number of rooms
	}

	public void CreateRoom(int index, int layer, int minWidth = 2, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		Vector3 global = MatrixToGlobal(originPoints[index]);
		noise = noiseGen.GetNoise01(global.x, global.z);
		Debug.Log("noise: "+noise);
		//	Rotate script values to face the same way as this square
		Rotate(originSides[index]);

		int rightAdjacent;
		int leftAdjacent;
		int[] bounds = new int[4];

		//	Get position of closest adjacent squares
		bool adjacent = CheckAdjacent(originPoints[index], index, out rightAdjacent, out leftAdjacent);

		//	Get perpendicular axis
		int axisSides = SideAxis(originPoints[index]);

		//	Assign zone bounds as max width if not set
		int maxRight = maxWidth == 0 ? zone.bounds[right] : AddTo(maxWidth / 2, axisSides, right);
		int maxLeft = maxWidth == 0 ? zone.bounds[left] : AddTo(maxWidth / 2, axisSides, left);

		//	Assign center point as min width if not set
		int minRight = minWidth == 0 ? axisSides : AddTo(minWidth  / 2, axisSides, right);
		int minLeft = minWidth == 0 ? axisSides : AddTo(minWidth  / 2, axisSides, left);

		//	Randomly generate right and left measurments if no adjacent squares exist
		bounds[right] = rightAdjacent == 0 ? RandomRange(minRight, maxRight, noise) : rightAdjacent;
		bounds[left] = leftAdjacent == 0 ? RandomRange(minLeft, maxLeft, noise) : leftAdjacent;

		//	Clamp bounds if adjacent squares exist
		if(adjacent)
		{
			bounds[right] = Mathf.Clamp(bounds[right], Mathf.Min(minRight, maxRight), Mathf.Max(minRight, maxRight));
			bounds[left] = Mathf.Clamp(bounds[left], Mathf.Min(minLeft, maxLeft), Mathf.Max(minLeft, maxLeft));
		}

		int closestInFront;

		//	Get forward axis
		int axisFront = ForwardAxis(originPoints[index]);

		//	Assign zone bounds as max legth if not set
		int maxFront = maxLength == 0 ? zone.bounds[front] : AddTo(maxLength, axisFront, front);
		int minFront = minLength == 0 ? axisFront : AddTo(minLength, axisFront, front);

		//	Randomly generate forward measurement if no square in front
		if(CheckForward(originPoints[index], bounds, index, out closestInFront))
			bounds[front] = Mathf.Clamp(closestInFront, Mathf.Min(minFront, maxFront), Mathf.Max(minFront, maxFront));
		else
			bounds[front] = RandomRange(minFront, maxFront, noise);

		Debug.Log(axisFront+" "+maxFront);

		//	Back is always the origin
		bounds[back] = ForwardAxis(originPoints[index]);

		//	Clamp all bounds to within zone
		for(int s = 0; s < 4; s++)
		{
			if(s < 2)
				bounds[s] = Mathf.Clamp(bounds[s], 0, width-1);
			else
				bounds[s] = Mathf.Clamp(bounds[s], 0, height-1);
		}

		int squareWidth = Distance(bounds[left], bounds[right]);
		int squareLength = Distance(bounds[front], bounds[back]);

		Debug.Log("w/l: "+squareWidth+" "+squareLength);

		//	Add bounds index to layer dict
		if(!layers.ContainsKey(layer)) layers[layer] = new List<int>();
		layers[layer].Add(index);

		//	Add bounds to list
		allBounds.Add(bounds);		
	}

	int[] MirrorRoom(int[] originalBounds, Zone.Side originSide, Int2 originPoint, int[] parentBounds, int index)
	{
		int[] bounds = originalBounds.Clone() as int[];

		Zone.Side side = Zone.Opposite(originSide);

		originPoint = OppositePoint(originPoint, (int)side, parentBounds);

		Rotate(side);

		int closestInFront;

		if(CheckForward(originPoint, bounds, index, out closestInFront))
			bounds[front] = closestInFront;
		else
			bounds[front] = RandomRange(ForwardAxis(originPoint), zone.bounds[front]);

		bounds[back] = ForwardAxis(originPoint);

		return bounds;
	}

	//	Get bounds of squares to the right and left of originPoint
	bool CheckAdjacent(Int2 originPoint, int index, out int rightBound, out int leftBound)
	{
		rightBound = 0;
		leftBound = 0;

		for(int b = 0; b < index; b++)
		{
			//	Other square's top and bottom bounds overlap
			if(Behind(allBounds[b][back], ForwardAxis(originPoint)) && InFront(allBounds[b][front], ForwardAxis(originPoint)))	
			{
				//	Square is to the left, get it's right bounds
				if(ToLeft(allBounds[b][right], SideAxis(originPoint)))
				{
					if(leftBound == 0 || ToRight(allBounds[b][right], leftBound))
					{
						leftBound = allBounds[b][right];
					}
				}
				//	Square is to the right, get it's left bounds
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

	//	Get closest square in front of bounds with width
	bool CheckForward(Int2 originPoint, int[] bounds, int index, out int closest)
	{
		closest = 0;
		for(int b = 0; b < index; b++)
		{
			//	Other bounds left or right is within width, or left and right are either side
			if(	ToRight(allBounds[b][right], bounds[left]) 	&& ToLeft(allBounds[b][right], bounds[right]) ||
				ToRight(allBounds[b][left], bounds[left]) 	&& ToLeft(allBounds[b][left], bounds[right])  ||
				ToLeft(allBounds[b][left], bounds[left]) 	&& ToRight(allBounds[b][right], bounds[right]))
			{
				//	Store if closer than stored
				if(InFront(allBounds[b][back], ForwardAxis(originPoint)) && (closest == 0 || Behind(allBounds[b][back], closest)))
					closest = allBounds[b][back];
			}	
		}
		if(closest == 0) return false;
		else return true;
	}

	//	Find sides of bounds that are not adjacent to another square
	bool[] EligibleSides(int backSide, int[] bounds)
	{
		bool[] eligibleSides = new bool[4];
		for(int i = 0; i < 4; i++)
		{
			if(i == backSide || SideBlocked(bounds[i], i)) continue;
			else eligibleSides[i] = true;
		}
		return eligibleSides;
	}
	bool SideBlocked(int bound, int side)
	{
		int otherSide = (int)Zone.Opposite(side);

		foreach(int[] bounds in allBounds)
		{
			if(bounds[otherSide] == bound) return true;
		}
		return false;
	}

	//	Find eligible side that is farthest from zone bounds
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

	//	Get opposite point from point on side in bounds
	Int2 OppositePoint(Int2 point, int side, int[] bounds)
	{
		if(side == 0 || side == 2)
			return new Int2(point.x, bounds[Zone.Opposite(side)]);
		else
			return new Int2(bounds[Zone.Opposite(side)], point.z);
	}

	//	Get pseudo random number in range using coherent noise
	int RandomRange(int a, int b, float noise = 0, bool large = false, bool debug = false)
	{
		if(noise == 0) noise = noiseGen.GetNoise01(a, b);

		if(debug) Debug.Log("noise: "+noise);

		if(large) noise = Mathf.Lerp(0.5f, 1f, noise);

		if(a < b)
			return Mathf.RoundToInt(a + ((b - a) * noise));
		else
			return Mathf.RoundToInt(b + ((a - b) * (1 - noise)));
	}

	//	Get pseudo random point on side of bounds using coherent noise
	Int2 RandomPointOnSide(int side, int[] bounds, float noise)
	{
		if(side < 2)
			return new Int2(bounds[side], RandomRange(bounds[2], bounds[3]));
		else
			return new Int2(RandomRange(bounds[0], bounds[1]), bounds[side]);
	}

	//	Get pseudo random point on side of bounds using coherent noise
	Int2 PositionOnSide(int side, int[] bounds, float position)
	{
		if(side < 2)
			return new Int2(bounds[side], Mathf.Clamp(Mathf.RoundToInt(height * position), 0, height));
		else
			return new Int2(Mathf.Clamp(Mathf.RoundToInt(height * position), 0, height), bounds[side]);
	}

	int Distance(int a, int b)
	{
		return a > b ? a - b : b - a;
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
