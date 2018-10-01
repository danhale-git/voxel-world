using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGenerator
{
	List<Wing> wings = new List<Wing>();

	int right = 0, left = 1, top = 2, bottom = 3;

	Zone zone;

	//	Noise used for coherent randomisation
	FastNoise noiseGen = new FastNoise();
	float noise;
	int noiseX;
	int noiseY;

	//	Dimentions of matrix
	int width;
	int height;

	struct Wing
	{
		public int[] bounds;
		public float noise;
		public int minRoomSize;
		
		public List<Room> rooms;
		public List<Int2> entrances;
		public List<int> entranceSizes;
		public List<int> connectedEntrances;
		

		public Wing(int[] bounds, int minRoomSize, int maxCorridorSize, float doorNoise)
		{
			this.bounds = bounds;
			this.noise = doorNoise;
			this.minRoomSize = minRoomSize;

			this.rooms = new List<Room>();
			this.entrances = new List<Int2>();
			this.entranceSizes = new List<int>();
			this.connectedEntrances = new List<int>();
		}

		public void AddEntrance(Int2 point, int size)
		{
			entrances.Add(point);
			entranceSizes.Add(size);
		}

		void NewRoom(WallType[] walls, int[] bounds, Int2 door)
		{
			rooms.Add(new Room(walls, bounds, door));
		}
	}

	public BuildingGenerator(Zone zone)
	{
		//	Zone and size in blocks
		this.zone = zone;
		width = zone.size * WorldManager.chunkSize;
		height = zone.size * WorldManager.chunkSize;

		//	Leightweight noise algorithm
		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);	

		//	Randomise seed for debugging
		//int seed = Random.Range(0,10000);
		//Debug.Log("SEED: "+ seed);
		noiseGen.SetSeed(9770);


		//	Base noise generated from POI position
		noiseX = (int)zone.POI.position.x;
		noiseY = (int)zone.POI.position.z;
		ResetNoise();
	}

	//	TODO: update values before assigning new noise because it's more readable
	//	Generate new deterministic noise value
	float ResetNoise()
	{
		//	Generate and store noise
		noise = noiseGen.GetNoise01(noiseX, noiseY);
		int increment = Mathf.RoundToInt(noise * 10);
		//	Update values used next time
		noiseX += increment;
		noiseY += increment;
		return noise;
	}

	public void Generate()
	{
		GenerateMainWing(0, minWidth:30, maxWidth:40, minHeight:60, maxHeight:60);
		GenerateRooms(wings[0]);

		int mainBest = BestEntrance(wings[0]);

		GenerateWing(wings[0].entrances[mainBest], wings[0], minWidth:80, maxWidth:100, minHeight:40, maxHeight:50);
		//GenerateRooms(wings[1], wings[0], mainBest);

		//int oneBest = BestEntrance(wings[1]);

		//GenerateWing(wings[1].entrances[oneBest], wings[1], minWidth:20, maxWidth:30, minHeight:90, maxHeight:120);
		//GenerateRooms(wings[2], wings[1], oneBest);
	}

	//	TODO: 	if max/min width/height == 0 generate random values
	//			
	void GenerateMainWing(int startPoint, int minWidth = 0, int maxWidth = 0, int minHeight = 0, int maxHeight = 0)
	{
		int[] bounds = new int[4];

		//	Random size
		int width = RandomRange(minWidth, maxWidth);
		int height = RandomRange(minHeight, maxHeight);

		//	Left, right or middle
		switch(startPoint)
		{
			case -1:
				bounds[left] = zone.bufferedBounds[left];
				bounds[right] = zone.bufferedBounds[left] + width;
				break;
		
			case 1:
				
				bounds[right] = zone.bufferedBounds[right];
				bounds[left] = zone.bufferedBounds[right] - width;
				break;
		
			case 0:
			default:
				int middle = zone.bufferedBounds[right]/2;
				bounds[left] = middle - (width / 2);
				bounds[right] = middle + (width / 2);
				break;
		}
		bounds[bottom] = zone.bufferedBounds[bottom];
		bounds[top] = zone.bufferedBounds[bottom] + height;

		wings.Add(new Wing(bounds, 10, 10, noise));
	}

	void GenerateWing(Int2 point, Wing parent, int minWidth = 0, int maxWidth = 0, int minHeight = 0, int maxHeight = 0)
	{
		//	Ideal size of wing
		Int2 dimentions = new Int2(RandomRange(minWidth, maxWidth), RandomRange(minHeight, maxHeight));

		//	Side of parent object wing originates from
		int parentSide = (int)Side(point, parent.bounds);

		//	Start of bounds object originates from
		int startSide = Zone.Opposite(parentSide);

		//	Start with 1x1 bounds at point
		int[] bounds = new int[] { point.x, point.x, point.z, point.z };

		//	Available space outward from sides
		int[] space = new int[4];
		space[startSide] = 0;

		//	adjacent, adjacent, outward
		int[] sideIndices = startSide < 2 ? new int[] { 2, 3, parentSide } : new int[] { 0, 1, parentSide };

		//	Iterate over 3 sides, checking available space
		for(int i = 0; i < 3; i++)
		{
			//	Follow specific order of sides
			int side = sideIndices[i];

			//	Start with distance from bounds
			space[side] = CheckSpace(point, side, bounds, parent);

			//	Size of extension
			int size = i < 2 ? PointAxis(dimentions, side)/2 : PointAxis(dimentions, side);

			bounds[side] = ExtendBound(
				Mathf.Min(space[side], size),
				bounds[side],
				side);
		}

		wings.Add(new Wing(bounds, 10, 10, noise));
	}

	//	Get amount of space out from point on side of parent
	int CheckSpace(Int2 point, int side, int[] bounds, Wing parent)
	{
		int space = Distance(PointAxis(point, side), zone.bufferedBounds[side]);

		//	Check all other wings
		for(int w = 0; w < wings.Count; w++)
		{
			//	Ignore parent
			if(wings[w].bounds == parent.bounds)
				continue;

			//	Other wing is overlapping
			if(InOrEitherSide(bounds, wings[w].bounds, side))
			{
				//	Opposide side on overlapping wing
				int oppositeBounds = wings[w].bounds[Zone.Opposite(side)];

				//	Overlapping wing is outwards from this side (not on the other side)
				if( OutwardFrom(bounds[side], oppositeBounds, side) )
				{
					//	If closer than current closest distance, store
					int distanceToWing = Distance(bounds[side], oppositeBounds);
					if(distanceToWing < space) space = distanceToWing;
				}
			}
		}

		return space;
	}

	//	Is B outwards from A per side
	bool OutwardFrom(int a, int b, int side)
	{
		if(side == 0 || side == 2)
			return (b >= a);
		else
			return (b <= a);
	}

	//	Are B values overlapping A values
	bool InOrEitherSide(int[] boundsA, int[] boundsB, int side)
	{	
		int lowA = side < 2 ? boundsA[bottom] : boundsA[left];
		int highA = side < 2 ? boundsA[top] : boundsA[right];

		int lowB = side < 2 ? boundsB[bottom] : boundsB[left];
		int highB = side < 2 ? boundsB[top] : boundsB[right];

		return(	(lowB <= lowA 	&& highB >= highA)	||	//	B either side of A
				(lowB > lowA 	&& highB < highA)	||	//	A either side of B
				(lowB <= highA 	&& lowB >= lowA)	||	//	A either side of lowB
				(highB <= highA && highB >= lowA));		//	A either side of highB
	}

	//	Extend outward from bound center
	int ExtendBound(int add, int to, int side, bool clamp = true)
	{
		int value;
		if(side == 0 || side == 2)
			value = to + add;
		else
			value = to - add;

		if(clamp)
			return Mathf.Clamp(value, zone.bufferedBounds[bottom], zone.bufferedBounds[top]);
		else
			return value;
	}

	//	Axis of int 2 aligned with side
	int PointAxis(Int2 point, int side)
	{
		if(side < 2)
			return point.x;
		else
			return point.z;
	}

	//	Entrance with the most space outward from it
	int BestEntrance(Wing checkWing)
	{
		int entranceIndex = 0;
		int largestDistance = 0;
		for(int i = 0; i < checkWing.entrances.Count; i++)
		{
			Int2 point = checkWing.entrances[i];
			int side = (int)Side(checkWing.entrances[i], checkWing.bounds);

			int[] bounds = new int[] { point.x, point.x, point.z, point.z };

			foreach(Wing wing in wings)
			{
				if(wing.bounds == checkWing.bounds) continue;

				int newDistance = CheckSpace(point, side, bounds, checkWing);
				if(newDistance > largestDistance)
				{
					entranceIndex = i;
					largestDistance = newDistance;
				}
			}
		}

		return entranceIndex;
	} 

	# region Room generation

	enum WallType { NONE, EXIT, OUTSIDE, INSIDE }
	enum SplitAxis { NONE, HORIZONTAL, VERTICAL }

	struct Room
	{
		public readonly WallType[] wallTypes;
		public readonly int[] bounds;
		public readonly Int2 door;
		public Room(WallType[] wallTypes, int[] bounds, Int2 door)
		{
			this.wallTypes = wallTypes;
			this.bounds = bounds;
			this.door = door;
		}
	}

	void GenerateRooms(Wing wing, Wing? connectedWing = null, int connectionIndex = 0)
	{
		int width = wing.bounds[0] - wing.bounds[1];
		int height = wing.bounds[2] - wing.bounds[3];

		//	Number and size of corridors based on wing size
		int corridorIterations = Mathf.Max( ((width + height) /2) / (wing.minRoomSize*3), 1 );
		int corridorWidth = Mathf.Max(Mathf.Max(width, height)/10, 5);

		List<Room> roomsCopy;

		//	Generate entire bounds as room, all other rooms are split from this
		wing.rooms.Add(NewRoom(
			new WallType[] { WallType.OUTSIDE, WallType.OUTSIDE, WallType.OUTSIDE, WallType.OUTSIDE },
			wing.bounds,
			wing));

		bool badFirstSplit = false;
		bool horizontalSplit = false;

		//	First split, connects with connecting wing
		if(connectedWing != null)
		{
			Wing cWing = (Wing)connectedWing;
			cWing.connectedEntrances.Add(connectionIndex);
			SplitRoom(wing.rooms[0], wing, cWing, connectionIndex, corridorWidth, true);

			//	First split bisects longest sides, bad first split
			int side = (int)Side(cWing.entrances[connectionIndex], cWing.bounds);
			horizontalSplit = (side < 2);
			if(horizontalSplit != (width > height)) badFirstSplit = true;
		}
		else
			SplitRoom(wing.rooms[0], wing, corridorWidth, true);

		//	Split with corridors of decreasing size
		for(int i = 0; i < corridorIterations; i++)
		{
			corridorWidth -= 2;
			corridorWidth = corridorWidth < 5 ? 5 : corridorWidth;
			roomsCopy = new List<Room>(wing.rooms);
		
			if(connectedWing == null || !badFirstSplit || i != 0)
				foreach(Room room in roomsCopy)
					SplitRoom(
						room, wing,
						corridorWidth);
			//	Correct bad first split by making second set of splits perpendicular to longest side			
			else
				foreach(Room room in roomsCopy)
					SplitRoom(
						room, wing,
						corridorWidth,
						split:(horizontalSplit ? SplitAxis.VERTICAL : SplitAxis.HORIZONTAL));
		}

		wing.noise = ResetNoise();

		//	Split without corridoors until no more rooms of minimum size can be created
		bool roomsCreated = true;
		int iterationCount = 0;
		while(roomsCreated)
		{
			//	Safety
			iterationCount++;
			if(iterationCount > 100)
				break;

			roomsCopy = new List<Room>(wing.rooms);

			//	Split all rooms in wing
			roomsCreated = false;
			foreach(Room room in roomsCopy)
				if(SplitRoom(room, wing, random:false) && !roomsCreated)
					if(!roomsCreated) roomsCreated = true;

		}
	}

	//	TODO: Second corridor split is always perpendicular to first

	//	Split room at connection
	bool SplitRoom(Room room, Wing wing, Wing connectedWing, int connectionIndex = 0, int corridorWidth = 0, bool firstSplit = false)
	{
		int splitPoint = 0;
		bool verticalSplit = false;

		//	Wing connection defines corridor position and axis
		Wing cWing = (Wing)connectedWing;
		Int2 startPoint = cWing.entrances[connectionIndex];
		corridorWidth = cWing.entranceSizes[connectionIndex];

		if((int)Zone.Opposite(Side(startPoint, cWing.bounds)) < 2)
		{
			verticalSplit = false;
			splitPoint = startPoint.z;
		}
		else
		{
			verticalSplit = true;
			splitPoint = startPoint.x;
		}

		Split(room, wing, verticalSplit, splitPoint, corridorWidth);	

		return true;
	}

	//	Split room
	bool SplitRoom(Room room, Wing wing, int corridorWidth = 0, bool firstSplit = false, bool random = true, SplitAxis split = 0)
	{
		int width = room.bounds[0] - room.bounds[1];
		int height = room.bounds[2] - room.bounds[3];

		int splitPoint = 0;
		bool verticalSplit = false;

		switch(split)
		{
			case SplitAxis.HORIZONTAL:
				verticalSplit = false;
				break;

			case SplitAxis.VERTICAL:
				verticalSplit = true;
				break;

			default:
				verticalSplit = firstSplit ? (height > width) : (width > height);
			break;
		}

		//	Values depending on angle of split
		int bisectBreadth = verticalSplit? width : height;
		int parallelLow = verticalSplit ? left : bottom;

		int splitValue = (int)(bisectBreadth * (random ? ResetNoise() : wing.noise));

		//	Room is too small to split, split more evenly or return false
		if(Mathf.Min(splitValue, bisectBreadth - splitValue) < wing.minRoomSize)
			if(bisectBreadth >= wing.minRoomSize*2)
				splitValue = wing.minRoomSize;
			else
				return false;

		splitPoint = (int)(room.bounds[parallelLow] + splitValue);	

		Split(room, wing, verticalSplit, splitPoint, corridorWidth);	

		return true;
	}

	void Split(Room room, Wing wing, bool verticalSplit, int splitPoint, int corridorWidth)
	{
		WallType wallTypeA = corridorWidth > 0 ? WallType.EXIT : WallType.INSIDE;
		WallType wallTypeB = wallTypeA;

		WallType[] wallsA, wallsB;
		int[] boundsA, boundsB;

		//	Values depending on angle of split
		int bisectHigh = verticalSplit ? top : right;
		int bisectLow = verticalSplit ?  bottom : left;
		int parallelHigh = verticalSplit ? right : top;

		if(corridorWidth > 0)
		{
			//	Corridor reaches edge of wing, create entrance
			if(room.bounds[bisectHigh] == wing.bounds[bisectHigh] && room.bounds[bisectHigh] != zone.bufferedBounds[bisectHigh])
				wing.AddEntrance(SetInt2(splitPoint, room.bounds[bisectHigh], verticalSplit), corridorWidth);
			if(room.bounds[bisectLow] == wing.bounds[bisectLow] && room.bounds[bisectLow] != zone.bufferedBounds[bisectLow])
				wing.AddEntrance(SetInt2(splitPoint, room.bounds[bisectLow], verticalSplit), corridorWidth);
		}
		else if(room.wallTypes[bisectHigh] != WallType.EXIT && room.wallTypes[bisectLow] != WallType.EXIT)
		{	
			//	One of rooms has no access to corridor, place connecting door
			if(room.wallTypes[parallelHigh] == WallType.EXIT)
				wallTypeA = WallType.EXIT;
			else
				wallTypeB = WallType.EXIT;
		}

		//	Arrange arrays depending on angle of split
		if(verticalSplit)
		{
			boundsA = new int[] { splitPoint - (corridorWidth/2), room.bounds[left], room.bounds[top], room.bounds[bottom] };
			boundsB = new int[] { room.bounds[right], splitPoint + (corridorWidth/2), room.bounds[top], room.bounds[bottom] };

			wallsA = new WallType[] { wallTypeA, room.wallTypes[left], room.wallTypes[top], room.wallTypes[bottom] };
			wallsB = new WallType[] { room.wallTypes[right], wallTypeB, room.wallTypes[top], room.wallTypes[bottom] };
		}
		else
		{
			boundsA = new int[] { room.bounds[right], room.bounds[left], splitPoint - (corridorWidth/2), room.bounds[bottom] };
			boundsB = new int[] { room.bounds[right], room.bounds[left], room.bounds[top], splitPoint + (corridorWidth/2) };

			wallsA = new WallType[] { room.wallTypes[right], room.wallTypes[left], wallTypeA, room.wallTypes[bottom] };
			wallsB = new WallType[] { room.wallTypes[right], room.wallTypes[left], room.wallTypes[top], wallTypeB };
		}

		//	Add rooms to list
		wing.rooms.Add(NewRoom(wallsA, boundsA, wing));
		wing.rooms.Add(NewRoom(wallsB, boundsB, wing));

		wing.rooms.Remove(room);	
	}

	Room NewRoom(WallType[] walls, int[] bounds, Wing wing)
	{
		Int2 door = new Int2(0,0);
		for(int i = 0; i < 4; i++)
			if(walls[i] == WallType.EXIT)
			{
				door = PositionOnSide(i, bounds, wing.noise);
				break;
			}
		return new Room(walls, bounds, door);
	}

	#endregion

	# region Positions and points

	Int2 SetInt2(int set, int other, bool setX)
	{
		return setX? new Int2(set, other) : new Int2(other, set);
	}

	//	Get pseudo random number in range using coherent noise
	int RandomRange(int a, int b, bool debug = false)
	{
		ResetNoise();

		if(a < b)
			return Mathf.RoundToInt(a + ((b - a) * noise));
		else
			return Mathf.RoundToInt(b + ((a - b) * (1 - noise)));
	}

	//	Get pseudo random point on side of bounds using coherent noise
	Int2 RandomPointOnSide(int side, int[] bounds, int lowOffset = 1, int highOffset = 1)
	{
		int x, z;

		if(side < 2)
		{
			x = bounds[side];
			z = RandomRange(bounds[top]-highOffset, bounds[bottom]+lowOffset);
		}
		else
		{
			x =RandomRange(bounds[right]-highOffset, bounds[left]+lowOffset);
			z = bounds[side];
		}
		return new Int2(x, z);
	}

	//TODO: boundOffset should always be bottom/left?
	//	Get position on side from 0-1 float
	Int2 PositionOnSide(int side, int[] bounds, float position)
	{
		int sideSize;
		int boundsOffset;
		int x;
		int z;

		if(side < 2)
		{
			sideSize = Distance(bounds[top], bounds[bottom]);
			boundsOffset = bounds[bottom];
			x = bounds[side];
			z = Mathf.Clamp(Mathf.RoundToInt(sideSize * position), 0, sideSize) + boundsOffset;
		}
		else
		{
			sideSize = Distance(bounds[right], bounds[left]);
			boundsOffset = bounds[left];
			x = Mathf.Clamp(Mathf.RoundToInt(sideSize * position), 0, sideSize) + boundsOffset;
			z = bounds[side];
		}
		return new Int2(x, z);
	}

	int Distance(int a, int b)
	{
		return a > b ? a - b : b - a;
	}

	Zone.Side Side(Int2 point, int[] bounds)
	{
		for(int i = 0; i < 4; i++)
		{
			if(point.x == bounds[i] || point.z == bounds[i])
			{
				return (Zone.Side)i;
			}
		}
		
		Debug.Log("No side found for point");
		return 0;
	}



#endregion

	# region Drawing

	public void ApplyMaps(POILibrary.POI poi)
	{
		foreach(Wing wing in wings)
		{
			DrawHeightGradient(wing.bounds);
			DrawBoundsBorder(wing.bounds, zone.wallMatrix, 1);
			DrawRooms(zone.wallMatrix, wing);
		}
		foreach(Wing wing in wings)
		{
			foreach(int i in wing.connectedEntrances)
			{
				DrawConnector(zone.wallMatrix, 0, wing.entrances[i], Side(wing.entrances[i], wing.bounds), wing.entranceSizes[i]);
			}
		}
		foreach(Wing wing in wings)
		{
			foreach(Int2 e in wing.entrances)
			{
				DrawPoint(e, zone.wallMatrix, 3);
			}
		}

		SetColumnMaps(poi);
	}

	public void DrawBoundsBorder(int[] bounds, int[,] matrix, int value)
	{
		for(int x = bounds[left]; x <= bounds[right]; x++)
		{
			matrix[x, bounds[bottom]] = 1;
			matrix[x, bounds[top]] = 1;
		}

		for(int z = bounds[bottom]; z <= bounds[top]; z++)
		{
			matrix[bounds[left], z] = 1;
			matrix[bounds[right], z] = 1;
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
			for(int x = bounds[left]+1; x < bounds[right]; x++)
				for(int z = bounds[bottom]+1; z < bounds[top]; z++)
					matrix[x,z] = value;
		else
			for(int x = bounds[left]; x <= bounds[right]; x++)
				for(int z = bounds[bottom]; z <= bounds[top]; z++)
					matrix[x,z] = value;
	}

	public void DrawPoint(Int2 point, int[,] matrix, int value)
	{
		matrix[point.x, point.z] = value;
	}

	void DrawHeightGradient(int[] bounds)
	{
		int spread = WorldManager.chunkSize;
		for(int i = spread; i > 0; i--)
		{
			int xLow = bounds[left]-i;
			int xHigh = bounds[right]+i;

			int zLow = bounds[bottom]-i;
			int zHigh = bounds[top]+i;

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

			for(int xm = bounds[left]; xm <= bounds[right]; xm++)
				for(int zm = bounds[bottom]; zm <= bounds[top]; zm++)
				{
					zone.heightMatrix[xm,zm] = spread;
				}
		}
	}

	void SetColumnMaps(POILibrary.POI poi)
	{
		int chunkSize = WorldManager.chunkSize;

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
}
