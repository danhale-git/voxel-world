using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSystem
{
	FastNoise noiseGen = new FastNoise();

	//	Zone represented by matrix
	Zone zone;
	//	Noise used for coherent randomisation
	float noise;
	int noiseX;
	int noiseY;

	//	Bounds, origin points and origin sides currently being generated
	public List<Int2> originPoints = new List<Int2>();
	public List<Zone.Side> originSides = new List<Zone.Side>();
	public List<int[]> currentBounds = new List<int[]>();

	//	Bounds defining base areas
	public List<int[]> areaBounds = new List<int[]>();

	//	Directions from perspective of current square orientation
	int right = 0;
	int left = 1;
	int front = 2;
	int back = 3;

	//	Back side per current square orientation
	Zone.Side currentBack = Zone.Side.BOTTOM;

	//	Dimentions of matrix
	int width;
	int height;
	
	public LSystem(Zone zone)
	{
		this.zone = zone;

		width = zone.size * World.chunkSize;
		height = zone.size * World.chunkSize;

		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);	

		//	Randomise seed for debugging
		/*int seed = Random.Range(0,10000);
		Debug.Log("SEED: "+ seed);
		noiseGen.SetSeed(seed);*/


		//	Base noise generated from POI position
		noiseX = (int)zone.POI.position.x;
		noiseY = (int)zone.POI.position.z;
		ResetNoise();
	}

	//	Generate new deterministic noise value
	void ResetNoise()
	{
		noise = noiseGen.GetNoise01(noiseX, noiseY);
		int increment = Mathf.RoundToInt(noise * 10);
		noiseX += increment;
		noiseY += increment;
	}

# region Rotation

	//	Int.x or .z depending on side
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

	//	Add/subtract distance from center depending on side
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

	//	Compare distance from value depending on current orientation
	bool ToLeft(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.RIGHT)
			return (compare <= to);
		else return (compare >= to);
	}
	bool ToRight(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.RIGHT)
			return (compare >= to);
		else return (compare <= to);
	}
	bool InFront(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.LEFT)
			return (compare >= to);
		else return (compare <= to);
	}
	bool Behind(int compare, int to)
	{
		if(currentBack == Zone.Side.BOTTOM || currentBack == Zone.Side.LEFT)
			return (compare <= to);
		else return (compare >= to);
	}

	//	Rotate perspective
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
				break;
			case Zone.Side.LEFT:
				right = 3;
				left = 2;
				front = 0;
				back = 1;
				break;
			case Zone.Side.TOP:
				right = 1;
				left = 0;
				front = 3;
				back = 2;
				break;
			case Zone.Side.BOTTOM:
				right = 0;
				left = 1;
				front = 2;
				back = 3;
				break;

		}
	}

# endregion

# region Basic Bounds

	//	Rect inside perimeter bounds
	public bool SquareInBounds(int[] perimeterBounds, Zone.Side perimeterSide, float positionOnSide = 0, int minWidth = 0, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		Int2 originPoint;
		if(positionOnSide != 0)
			originPoint = PositionOnSide((int)perimeterSide, perimeterBounds, positionOnSide);
		else
			originPoint = RandomPointOnSide((int)perimeterSide, perimeterBounds);

		return GenerateRect(0, originPoint, perimeterSide, perimeterBounds, false, minWidth, maxWidth, minLength, maxLength);
	}

	//	Rect from point on side of bounds
	public bool SquareFromPoint(int[] perimeterBounds, Int2 originPoint, int[] parentBounds, int minWidth = 0, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		int index = currentBounds.Count;
		Zone.Side originSide = 0;

		for(int i = 0; i < 4; i++)
		{
			if(originPoint.x == parentBounds[i] || originPoint.z == parentBounds[i])
				originSide = Zone.Opposite((Zone.Side)i);

		}
		Debug.Log(originSide);

		return GenerateRect(index, originPoint, originSide, perimeterBounds, false, minWidth, maxWidth, minLength, maxLength);
	}

	bool GenerateRect(int index, Int2 originPoint, Zone.Side originSide, int[] perimeterBounds, bool adjacentOverride, int minWidth, int maxWidth, int minLength, int maxLength)
	{
		ResetNoise();
		//	Rotate script values to face the same way as this square
		Rotate(originSide);

		int rightAdjacent;
		int leftAdjacent;
		int[] bounds = new int[4];

		//	Get position of closest adjacent squares
		bool adjacent = CheckAdjacent(originPoint, index, out rightAdjacent, out leftAdjacent);

		//	Get perpendicular axis
		int axisSides = SideAxis(originPoint);

		//	Assign zone bounds as max width if not set
		int maxRight = maxWidth == 0 ? perimeterBounds[right] : AddTo(maxWidth / 2, axisSides, right);
		int maxLeft = maxWidth == 0 ? perimeterBounds[left] : AddTo(maxWidth / 2, axisSides, left);

		//	Assign center point as min width if not set
		int minRight = minWidth == 0 ? axisSides : AddTo(minWidth  / 2, axisSides, right);
		int minLeft = minWidth == 0 ? axisSides : AddTo(minWidth  / 2, axisSides, left);

		//	Randomly generate right and left measurments if no adjacent squares exist
		bounds[right] = rightAdjacent == 0 ? RandomRange(minRight, maxRight) : rightAdjacent;
		bounds[left] = leftAdjacent == 0 ? RandomRange(minLeft, maxLeft) : leftAdjacent;

		//	Clamp bounds if adjacent squares exist
		if(adjacent && !adjacentOverride)
		{
			bounds[right] = Mathf.Clamp(bounds[right], Mathf.Min(minRight, maxRight), Mathf.Max(minRight, maxRight));
			bounds[left] = Mathf.Clamp(bounds[left], Mathf.Min(minLeft, maxLeft), Mathf.Max(minLeft, maxLeft));
		}

		int closestInFront;

		//	Get forward axis
		int axisFront = ForwardAxis(originPoint);

		//	Assign zone bounds as max legth if not set
		int maxFront = maxLength == 0 ? perimeterBounds[front] : AddTo(maxLength, axisFront, front);
		int minFront = minLength == 0 ? axisFront : AddTo(minLength, axisFront, front);

		//	Randomly generate forward measurement if no square in front
		if(CheckForward(originPoint, bounds, index, out closestInFront))
		{	
			if(Behind(closestInFront, maxFront)) maxFront = closestInFront;
			bounds[front] = Mathf.Clamp(closestInFront, Mathf.Min(minFront, maxFront), Mathf.Max(minFront, maxFront));
		}
		else
			bounds[front] = RandomRange(minFront, maxFront);

		//	Back is always the origin
		bounds[back] = ForwardAxis(originPoint);

		//	Clamp all bounds to within zone
		for(int s = 0; s < 4; s++)
		{
			if(s < 2)
				bounds[s] = Mathf.Clamp(bounds[s], perimeterBounds[1], perimeterBounds[0]);
			else
				bounds[s] = Mathf.Clamp(bounds[s], perimeterBounds[3], perimeterBounds[2]);
		}

		int squareWidth = Distance(bounds[left], bounds[right]);
		int squareLength = Distance(bounds[front], bounds[back]);

		if(squareWidth < minWidth || squareLength < minLength)
		{
			Debug.Log("square too large: "+squareWidth+" x "+squareLength);
			return false;
		}
		else
		{
			AddNewSquare(bounds, originPoint, originSide);
			return true;
		}
	}

	//	Add bounds to list
	void AddNewSquare(int[] bounds, Int2 originPoint, Zone.Side originSide)
	{
		int index = currentBounds.Count;
		//	Add bounds to list
		currentBounds.Add(bounds);
		originPoints.Add(originPoint);
		originSides.Add(originSide);
	}

	//	Get bounds of squares to the right and left of originPoint
	bool CheckAdjacent(Int2 originPoint, int index, out int rightBound, out int leftBound)
	{
		rightBound = 0;
		leftBound = 0;

		for(int b = 0; b < index; b++)
		{
			//	Other square's top and bottom bounds overlap
			if(Behind(currentBounds[b][back], ForwardAxis(originPoint)) && InFront(currentBounds[b][front], ForwardAxis(originPoint)))	
			{
				//	Square is to the left, get it's right bounds
				if(ToLeft(currentBounds[b][right], SideAxis(originPoint)))
				{
					if(leftBound == 0 || ToRight(currentBounds[b][right], leftBound))
					{
						leftBound = currentBounds[b][right];
					}
				}
				//	Square is to the right, get it's left bounds
				else if(ToRight(currentBounds[b][left], SideAxis(originPoint)))
				{
					if(rightBound == 0 || ToLeft(currentBounds[b][left], rightBound))
					{
						rightBound = currentBounds[b][left];
					}
				}
			}
		}

		if(leftBound != 0 || rightBound != 0) return true;
		else return false;
	}

	//	Get closest square in front of bounds with width
	bool CheckForward(Int2 originPoint, int[] bounds, int index, out int closest)
	{
		closest = 0;
		for(int b = 0; b < index; b++)
		{
			//	Other bounds left or right is within width, or left and right are either side
			if(	ToRight(currentBounds[b][right], bounds[left]) 	&& ToLeft(currentBounds[b][right], bounds[right]) ||
				ToRight(currentBounds[b][left], bounds[left]) 	&& ToLeft(currentBounds[b][left], bounds[right])  ||
				ToLeft(currentBounds[b][left], bounds[left]) 	&& ToRight(currentBounds[b][right], bounds[right]))
			{
				//	Store if closer than stored
				if(InFront(currentBounds[b][back], ForwardAxis(originPoint)) && (closest == 0 || Behind(currentBounds[b][back], closest)))
				{
					closest = currentBounds[b][back];
				}
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

		foreach(int[] bounds in currentBounds)
		{
			if(bounds[otherSide] == bound)
			{	
				return true;
			}
		}
		return false;
	}

	//	Find eligible side that is farthest from zone bounds
	Zone.Side MostOpenSide(int[] bounds, bool[] eligibleSides, int[] perimeterBounds)
	{
		int farthestSide = 0;
		int farthestDistance = 0;
		for(int i = 0; i < 4; i++)
		{
			if(!eligibleSides[i]) continue;
			int distance = Mathf.Max(bounds[i], perimeterBounds[i]) - Mathf.Min(bounds[i], perimeterBounds[i]);
			if(distance > farthestDistance)
			{
				farthestDistance = distance;
				farthestSide = i;
			}
		}
		return (Zone.Side)farthestSide;
	}


# endregion

# region Room Generation

	int corridorWidth = 5;
	int roomWidth = 10;

	enum WallType { NONE, EXIT, OUTSIDE, INSIDE }

	struct Line
	{
		public readonly Int2 start, end;
		public Line(Int2 start, Int2 end)
		{
			this.start = start;
			this.end = end;
		}
	}

	struct Room
	{
		public readonly List<Line> edges;
		public readonly List<WallType> wallTypes;
		public readonly int[] bounds;
		public Int2 door;
		public Room(List<Line> edges, List<WallType> wallTypes, int[] bounds, Int2 door)
		{
			this.edges = edges;
			this.wallTypes = wallTypes;
			this.bounds = bounds;
			this.door = door;
		}
	}

	struct Wing
	{
		public int[] bounds;
		public List<Room> rooms;
		public List<Int2> entrances;
		public List<int> entranceSizes;
		public int minRoomSize;
		public int maxCorridorSize;
		public float doorNoise;

		public Wing(int[] bounds, int minRoomSize, int maxCorridorSize, float doorNoise)
		{
			this.bounds = bounds;
			this.rooms = new List<Room>();
			this.entrances = new List<Int2>();
			this.entranceSizes = new List<int>();
			this.minRoomSize = minRoomSize;
			this.maxCorridorSize = maxCorridorSize;
			this.doorNoise = doorNoise;
		}

		public void AddEntrance(Int2 point, int size)
		{
			entrances.Add(point);
			entranceSizes.Add(size);
		}
	}

	public void GenerateBuilding()
	{
		int minRoomSize = 7;
		int maxCorridorSize = 10;
		List<Wing> wings = new List<Wing>();

		//	Corridor size should be odd number
		if(maxCorridorSize % 2 != 0) maxCorridorSize -= 1;

		//	Main wing
		if(SquareInBounds(zone.bufferedBounds, zone.back, positionOnSide: 0.5f, minWidth:50, maxWidth:70, minLength:50, maxLength:70))
		{
			int[] newBounds = currentBounds[0];

			Wing mainWing = new Wing(newBounds, minRoomSize, maxCorridorSize, noise);
			GenerateRooms(mainWing);
			wings.Add(mainWing);

			//	Track entrances used as connectors
			List<int> connectedEntrances = new List<int>();

			//	Add more wings wherever possible
			for(int i = 0; i < mainWing.entrances.Count; i++)
			{
				Int2 point = mainWing.entrances[i];

				if(SquareFromPoint(zone.bufferedBounds, point, mainWing.bounds, 10, 50, 10, 50))
				{
					int[] newBounds2 = currentBounds[currentBounds.Count - 1];

					Wing subWing = new Wing(newBounds2, minRoomSize, maxCorridorSize, noise);
					GenerateRooms(subWing, mainWing, i);
					wings.Add(subWing);

					connectedEntrances.Add(i);
				}
			}		
			
			//	Draw wings
			foreach(Wing wing in wings)
			{
				DrawBoundsBorder(wing.bounds, zone.wallMatrix, 1);
				DrawRooms(zone.wallMatrix, wing);
			}
			//	Remove walls at connectors
			foreach(int i in connectedEntrances)
			{
				DrawConnector(zone.wallMatrix, 0, mainWing.entrances[i], PointSide(mainWing.entrances[i], mainWing.bounds), mainWing.entranceSizes[i]);
			}
		}
	}

	void GenerateRooms(Wing wing, Wing? connectedWing = null, int connectionIndex = 0)
	{
		int width = wing.bounds[0] - wing.bounds[1];
		int height = wing.bounds[2] - wing.bounds[3];

		//	Generate entire bounds as room, all other rooms are split from this
		wing.rooms.Add(new Room(BoundsToEdges(wing.bounds),
								new List<WallType> { WallType.OUTSIDE, WallType.OUTSIDE, WallType.OUTSIDE, WallType.OUTSIDE },
								wing.bounds,
								new Int2(0,0)));

		//	Number and size of corridors based on wing size
		int corridorIterations = Mathf.Max(width, height) / (wing.minRoomSize*3);
		int corridorWidth = Mathf.Max(width, height)/10;

		//	First split, connects with connecting wing
		SplitRoom(wing.rooms[0], wing, connectedWing, connectionIndex, corridorWidth < 5 ? 5 : corridorWidth);

		//	Used to iterate while altering rooms list
		List<Room> roomsCopy;

		//	Split with corridors of decreasing size
		for(int i = 0; i < corridorIterations-1; i++)
		{
			corridorWidth -= 2;
			int cWidth = corridorWidth < 5 ? 5 : corridorWidth;
			roomsCopy = new List<Room>(wing.rooms);
		
			foreach(Room room in roomsCopy)
			{
				SplitRoom(room, wing, corridorWidth:cWidth);
			}
		}

		//	Split without corridoors until no more rooms of acceptable size can be created
		bool roomsCreated = true;
		int iterationCount = 0;
		while(roomsCreated && iterationCount < 5000)
		{
			//	Safety
			iterationCount++;
			if(iterationCount > 4999) Debug.Log("Too many iterations");

			//	Noise used for door position is changed less often to create more artificial looking layout
			ResetNoise();
			wing.doorNoise = noise;

			roomsCopy = new List<Room>(wing.rooms);

			roomsCreated = false;
			foreach(Room room in roomsCopy)
			{
				if(SplitRoom(room, wing) && !roomsCreated)
					if(!roomsCreated) roomsCreated = true;
			}

		}
	}

	List<Line> BoundsToEdges(int[] bounds)
	{
		List<Line> edges = new List<Line>();

		edges.Add(new Line(	new Int2(bounds[1], bounds[3]),		// right
							new Int2(bounds[0], bounds[2])));

		edges.Add(new Line(	new Int2(bounds[1], bounds[2]),		//	left
							new Int2(bounds[1], bounds[3])));

		edges.Add(new Line(	new Int2(bounds[0], bounds[2]),		//	top
							new Int2(bounds[1], bounds[2])));	

		edges.Add(new Line(	new Int2(bounds[1], bounds[3]),		//	bottom
							new Int2(bounds[0], bounds[3])));	
			
		return edges;
	}

	bool SplitRoom(Room room, Wing wing, Wing? connectedWing = null, int connectionIndex = 0, int corridorWidth = 0)
	{
		if(connectedWing == null)
		{
			ResetNoise();
		}

		if(room.edges.Count != 4)
		{
			Debug.Log("Can only split a rectangular room");
			return false;
		}

		int width = room.bounds[0] - room.bounds[1];
		int height = room.bounds[2] - room.bounds[3];

		WallType wallTypeA = corridorWidth > 0 ? WallType.EXIT : WallType.INSIDE;
		WallType wallTypeB = wallTypeA;

		Int2 doorA = new Int2(0,0);
		Int2 doorB = new Int2(0,0);

		List<WallType> wallsA;
		List<WallType> wallsB;

		int[] boundsA;
		int[] boundsB;

		int splitPoint = 0;
		bool splitX = false;

		//	Connected wing defines corridor position and axis
		if(connectedWing != null)
		{
			Wing cWing = (Wing)connectedWing;
			Int2 startPoint = cWing.entrances[connectionIndex];
			corridorWidth = cWing.entranceSizes[connectionIndex];

			//	Add connector to current wing as entrance
			wing.AddEntrance(startPoint, cWing.entranceSizes[connectionIndex]);

			if((int)PointSide(startPoint, cWing.bounds) < 2)
			{
				splitX = false;
				splitPoint = startPoint.z;
			}
			else
			{
				splitX = true;
				splitPoint = startPoint.x;
			}
		}
		//	Room split along smallest axis to help squarify
		else if(width > height)
		{
			splitX = true;
			int splitValue = (int)(width * noise);

			//	If split results in room that's to small adjust or abandon split
			if(Mathf.Min(splitValue, width - splitValue) < wing.minRoomSize)
			{
				if(width >= wing.minRoomSize*2)
					splitValue = wing.minRoomSize;
				else
					return false;
			}

			splitPoint = (int)(room.bounds[1] + splitValue);

			//	If corridor reaches edge of wing create entrance
			if(corridorWidth > 0)
			{
				if(2 != (int)zone.back && room.bounds[2] == wing.bounds[2])
					wing.AddEntrance(new Int2(splitPoint, room.bounds[2]), corridorWidth);
				else if(3 != (int)zone.back && room.bounds[3] == wing.bounds[3])
					wing.AddEntrance(new Int2(splitPoint, room.bounds[3]), corridorWidth);
			}

			
		}
		else
		{
			splitX = false;
			int splitValue = (int)(height * noise);

			if(Mathf.Min(splitValue, height - splitValue) < wing.minRoomSize)
			{
				if(height >= wing.minRoomSize*2)
					splitValue = wing.minRoomSize;
				else
					return false;
			}

			splitPoint = (int)(room.bounds[3] + splitValue);

			if(corridorWidth > 0)
			{
				if(0 != (int)zone.back && room.bounds[0] == wing.bounds[0])
					wing.AddEntrance(new Int2(room.bounds[0], splitPoint), corridorWidth);
				else if(1 != (int)zone.back && room.bounds[1] == wing.bounds[1])
					wing.AddEntrance(new Int2(room.bounds[1], splitPoint), corridorWidth);
			}

			
		}

		//	Split X axis
		if(splitX)
		{
			//	Two new bounds
			boundsA = new int[] { splitPoint - (corridorWidth/2), room.bounds[1], room.bounds[2], room.bounds[3] };
			boundsB = new int[] { room.bounds[0], splitPoint + (corridorWidth/2), room.bounds[2], room.bounds[3] };

			wing.rooms.Remove(room);

			//	Split cuts off room from corridor access, assign exit wall for door to be placed
			if(corridorWidth == 0 && room.wallTypes[2] != WallType.EXIT && room.wallTypes[3] != WallType.EXIT)
			{	
				if(room.wallTypes[0] == WallType.EXIT)
					wallTypeA = WallType.EXIT;
				else
					wallTypeB = WallType.EXIT;
			}

			//	Wall types
			wallsA = new List<WallType> { wallTypeA, room.wallTypes[1], room.wallTypes[2], room.wallTypes[3] };
			wallsB = new List<WallType> { room.wallTypes[0], wallTypeB, room.wallTypes[2], room.wallTypes[3] };
		}
		//	Split Z axis
		else
		{
			boundsA = new int[] { room.bounds[0], room.bounds[1], splitPoint - (corridorWidth/2), room.bounds[3] };
			boundsB = new int[] { room.bounds[0], room.bounds[1], room.bounds[2], splitPoint + (corridorWidth/2) };

			wing.rooms.Remove(room);

			if(corridorWidth == 0 && room.wallTypes[0] != WallType.EXIT && room.wallTypes[1] != WallType.EXIT)
			{	
				if(room.wallTypes[2] == WallType.EXIT)
					wallTypeA = WallType.EXIT;
				else
					wallTypeB = WallType.EXIT;
			}

			wallsA = new List<WallType> { room.wallTypes[0], room.wallTypes[1], wallTypeA, room.wallTypes[3] };
			wallsB = new List<WallType> { room.wallTypes[0], room.wallTypes[1], room.wallTypes[2], wallTypeB };
		}

		//	Place doors
		for(int i = 0; i < 4; i++)
		{
			if(wallsA[i] == WallType.EXIT)
			{
				doorA = PositionOnSide(i, boundsA, wing.doorNoise);
				break;
			}
		}
		
		for(int i = 0; i < 4; i++)
		{
			if(wallsB[i] == WallType.EXIT)
			{
				doorB = PositionOnSide(i, boundsB, wing.doorNoise);
				break;
			}
		}

		//	Add rooms to list
		wing.rooms.Add(new Room(BoundsToEdges(boundsA),
								wallsA,
								boundsA,
								doorA));
		wing.rooms.Add(new Room(BoundsToEdges(boundsB),
								wallsB,
								boundsB,
								doorB));

		return true;
	}

#endregion

# region Positions and points

	//	Get pseudo random number in range using coherent noise
	int RandomRange(int a, int b, bool large = false, bool debug = false)
	{
		ResetNoise();
		if(large) noise = Mathf.Lerp(0.5f, 1f, noise);

		if(a < b)
			return Mathf.RoundToInt(a + ((b - a) * noise));
		else
			return Mathf.RoundToInt(b + ((a - b) * (1 - noise)));
	}

	//	Get pseudo random point on side of bounds using coherent noise
	Int2 RandomPointOnSide(int side, int[] bounds, int lowOffset = 1, int highOffset = 1)
	{
		int x;
		int z;

		if(side < 2)
		{
			x = bounds[side];
			z = RandomRange(bounds[2]-highOffset, bounds[3]+lowOffset);
		}
		else
		{
			x =RandomRange(bounds[0]-highOffset, bounds[1]+lowOffset);
			z = bounds[side];
		}
		return new Int2(x, z);

	}

	//	Get position on side from 0-1 float
	Int2 PositionOnSide(int side, int[] bounds, float position)
	{
		int sideSize;
		int boundsOffset;
		int x;
		int z;

		if(side < 2)
		{
			sideSize = Distance(bounds[2], bounds[3]);
			boundsOffset = Mathf.Min(bounds[2], bounds[3]);
			x = bounds[side];
			z = Mathf.Clamp(Mathf.RoundToInt(sideSize * position), 0, sideSize) + boundsOffset;
		}
		else
		{
			sideSize = Distance(bounds[0], bounds[1]);
			boundsOffset = Mathf.Min(bounds[0], bounds[1]);
			x = Mathf.Clamp(Mathf.RoundToInt(sideSize * position), 0, sideSize) + boundsOffset;
			z = bounds[side];
		}
		return new Int2(x, z);
	}

#endregion

# region Drawing

	public void DefineArea()
	{
		areaBounds.AddRange(currentBounds);
		currentBounds.Clear();
		originSides.Clear();
		originPoints.Clear();
	}

	public void ApplyMaps(POILibrary.POI poi)
	{
		foreach(int[] room in areaBounds)
		{
			DrawHeightGradient(room);
		}

		SetColumnMaps(poi);
	}

	public void DrawBoundsBorder(int[] bounds, int[,] matrix, int value)
	{
		for(int x = bounds[1]; x <= bounds[0]; x++)
		{
			matrix[x, bounds[3]] = 1;
			matrix[x, bounds[2]] = 1;
		}

		for(int z = bounds[3]; z <= bounds[2]; z++)
		{
			matrix[bounds[1], z] = 1;
			matrix[bounds[0], z] = 1;
		}
	}

	void DrawRooms(int[,] matrix, Wing wing)
	{
		foreach(Room room in wing.rooms)
		{
			DrawBoundsBorder(room.bounds, matrix, 1);
		}
		foreach(Room room in wing.rooms)
		{
			DrawPoint(room.door, matrix, 2);
		}
	}

	void DrawConnector(int[,] matrix, int value, Int2 point, Zone.Side side, int size)
	{
		int outward = size/2;
		matrix[point.x,point.z] = value;
		for(int i = 1; i < outward; i++)
		{
			if((int)side < 2)
			{
				matrix[point.x,point.z-i] = value;
				matrix[point.x,point.z+i] = value;
			}
			else
			{
				matrix[point.x-i,point.z] = value;
				matrix[point.x+i,point.z] = value;
			}
		}
	}

	public void DrawBoundsFill(int[] bounds, int[,] matrix, int value, bool includeBorder = false)
	{
		if(!includeBorder)
			for(int x = bounds[1]+1; x < bounds[0]; x++)
				for(int z = bounds[3]+1; z < bounds[2]; z++)
					matrix[x,z] = value;
		else
			for(int x = bounds[1]; x <= bounds[0]; x++)
				for(int z = bounds[3]; z <= bounds[2]; z++)
					matrix[x,z] = value;
	}

	public void DrawPoint(Int2 point, int[,] matrix, int value)
	{
		matrix[point.x, point.z] = value;
	}

	void DrawHeightGradient(int[] bounds)
	{
		int spread = World.chunkSize;
		for(int i = spread; i > 0; i--)
		{
			int xLow = bounds[1]-i;
			int xHigh = bounds[0]+i;

			int zLow = bounds[3]-i;
			int zHigh = bounds[2]+i;

			int gradientValue = (spread - i) + 1;

			for(int x = xLow; x <= xHigh; x++)
			{
				if(x < 0 || x >= width) continue;
				if(zLow >= 0 && zone.heightMatrix[x, zLow] <= gradientValue) zone.heightMatrix[x, zLow] = gradientValue;
				if(zHigh < height && zone.heightMatrix[x, zHigh] <= gradientValue) zone.heightMatrix[x, zHigh] = gradientValue;
			}

			for(int z = zLow; z <= zHigh; z++)
			{
				if(z < 0 || z >= height) continue;
				if(xLow >= 0 && zone.heightMatrix[xLow, z] <= gradientValue) zone.heightMatrix[xLow, z] = gradientValue;
				if(xHigh < width && zone.heightMatrix[xHigh, z] <= gradientValue) zone.heightMatrix[xHigh, z] = gradientValue;
			}

			for(int xm = bounds[1]; xm <= bounds[0]; xm++)
				for(int zm = bounds[3]; zm <= bounds[2]; zm++)
				{
					zone.heightMatrix[xm,zm] = spread;
				}
		}
	}

	void SetColumnMaps(POILibrary.POI poi)
	{
		int chunkSize = World.chunkSize;

		for(int x = 0; x < zone.size; x++)
			for(int z = 0; z < zone.size; z++)
			{
				Column column = zone.POI.columnMatrix[x+zone.x,z+zone.z];
				column.POIType = poi;

				column.POIMap = new int[chunkSize,chunkSize];
				column.POIHeightGradient = new int[chunkSize,chunkSize];
				column.POIDebug = new int[chunkSize,chunkSize];
				column.POIWalls = new int[chunkSize,chunkSize];

				for(int cx = 0; cx < chunkSize; cx++)
					for(int cz = 0; cz < chunkSize; cz++)
					{
						int mx = cx + (x*chunkSize);
						int mz = cz + (z*chunkSize);
	
						column.POIMap[cx,cz] = zone.blockMatrix[mx,mz];
						column.POIHeightGradient[cx,cz] = zone.heightMatrix[mx,mz];
						column.POIDebug[cx,cz] = zone.debugMatrix[mx,mz];
						column.POIWalls[cx,cz] = zone.wallMatrix[mx,mz];
					}
			}
	}

# endregion

	public static Int2 BoundsCenter(int[] bounds)
	{
		int middleX = ((bounds[0] - bounds[1]) / 2) + bounds[1];
		int middleZ = ((bounds[2] - bounds[3]) / 2) + bounds[3];

		return new Int2(middleX, middleZ);
	}

	int Distance(int a, int b)
	{
		return a > b ? a - b : b - a;
	}
	
	Vector3 MatrixToGlobal(Int2 local)
	{
		return new Vector3(	(int)zone.POI.position.x + (zone.x*World.chunkSize) + local.x,
							0,
							(int)zone.POI.position.z + (zone.z*World.chunkSize) + local.z);
	}

	Zone.Side PointSide(Int2 point, int[] bounds)
	{
		for(int i = 0; i < 4; i++)
		{
			if(point.x == bounds[i] || point.z == bounds[i])
				return Zone.Opposite((Zone.Side)i);
		}
		
		Debug.Log("No side found for point");
		return 0;
	}
}
