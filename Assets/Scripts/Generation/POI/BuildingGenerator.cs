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

	public BuildingGenerator(Zone zone)
	{
		//	Zone and size in blocks
		this.zone = zone;
		width = zone.size * World.chunkSize;
		height = zone.size * World.chunkSize;

		//	Leightweight noise algorithm
		noiseGen.SetNoiseType(FastNoise.NoiseType.Simplex);
		noiseGen.SetInterp(FastNoise.Interp.Linear);
		noiseGen.SetFrequency(0.9f);	

		//	Randomise seed for debugging
		//int seed = Random.Range(0,10000);
		//Debug.Log("SEED: "+ seed);
		noiseGen.SetSeed(9099);


		//	Base noise generated from POI position
		noiseX = (int)zone.POI.position.x;
		noiseY = (int)zone.POI.position.z;
		ResetNoise();
	}

	//	TODO: update values before assigning new noise because it's more readable
	//	Generate new deterministic noise value
	void ResetNoise()
	{
		//	Generate and store noise
		noise = noiseGen.GetNoise01(noiseX, noiseY);
		int increment = Mathf.RoundToInt(noise * 10);
		//	Update values used next time
		noiseX += increment;
		noiseY += increment;
	}

	public void Generate()
	{
		GenerateMainWing(0, minWidth:20, maxWidth:40, minHeight:20, maxHeight:40);
		GenerateWing(RandomPointOnSide(0, wings[0].bounds), wings[0], minWidth:20, maxWidth:40, minHeight:20, maxHeight:40);
		GenerateWing(RandomPointOnSide(1, wings[0].bounds), wings[0], minWidth:20, maxWidth:40, minHeight:20, maxHeight:40);
		//GenerateWing(RandomPointOnSide(3, wings[2].bounds), wings[2], minWidth:20, maxWidth:40, minHeight:20, maxHeight:40);
	}

	//	TODO: if width/height == 0 generate random values
	void GenerateMainWing(int startPoint, int minWidth = 0, int maxWidth = 0, int minHeight = 0, int maxHeight = 0)
	{
		int[] bounds = new int[4];

		int width = RandomRange(minWidth, maxWidth);
		int height = RandomRange(minHeight, maxHeight);

		switch(startPoint)
		{
			case -1:
				bounds[right] = zone.bufferedBounds[right];
				bounds[left] = zone.bufferedBounds[right] - width;
				break;
		
			case 1:
				bounds[left] = zone.bufferedBounds[left];
				bounds[right] = zone.bufferedBounds[left] + width;
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

	//	MAYBE ITERATE AROUND SIDES IN AN ARC FROM START SIDE?????
	void GenerateWing(Int2 point, Wing parent, int minWidth = 0, int maxWidth = 0, int minHeight = 0, int maxHeight = 0)
	{
		int width = RandomRange(minWidth, maxWidth);
		int height = RandomRange(minHeight, maxHeight);

		Int2 dimentions = new Int2(width, height);

		int parentSide = (int)Side(point, parent.bounds);

		//	Start one after the origin side
		int startSide = Zone.Opposite(parentSide);

		//	Start with 1x1 bounds at point
		int[] bounds = new int[] { point.x, point.x, point.z, point.z };

		//	Space outward from wing, 2 is always facing out from origin wing
		int[] space = new int[4];
		space[startSide] = 0;

		int[] sideIndices = startSide < 2 ? new int[] { 2, 3, parentSide } : new int[] { 0, 1, parentSide };

		//	Iterate over 3 sides
		for(int i = 0; i < 3; i++)
		{
			int side = sideIndices[i];

			//	Start with distance from bounds
			space[side] = Distance(PointAxis(point, side), zone.bufferedBounds[side]);

			for(int w = 0; w < wings.Count; w++)
			{
				if(wings[w].bounds == parent.bounds)
				{
					continue;
				}
				//	Other wing is overlapping
				if(InOrEitherSide(bounds, wings[w].bounds, side))
				{
					//	Parent side is opposite to start side
					int oppositeBounds = wings[w].bounds[Zone.Opposite(side)];
					//	Other wing is on the out facing side of this one
					if( OutwardFrom(bounds[side], oppositeBounds, side) )
					{
						int distanceToWing = Distance(bounds[side], oppositeBounds);
						if(distanceToWing < space[side]) space[side] = distanceToWing;
					}
				}
			}

			int size = i < 2 ? PointAxis(dimentions, side)/2 : PointAxis(dimentions, side);

			bounds[side] = AddTo( 	Mathf.Min(space[side], size),
										bounds[side],
										side);
		}

		wings.Add(new Wing(bounds, 10, 10, noise));
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

	int AddTo(int add, int to, int side, bool clamp = true)
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

	int Wrap3(int value)
	{
		if(value > 3)
			while(value > 3)
				value -= 4;

		return 0 + value;
	}

	int PointAxis(Int2 point, int side)
	{
		if(side < 2)
			return point.x;
		else
			return point.z;
	}

	# region Room generation

	enum WallType { NONE, EXIT, OUTSIDE, INSIDE }

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

	struct Wing
	{
		public int[] bounds;
		public float doorNoise;
		public int minRoomSize;
		public int maxCorridorSize;
		
		public List<Room> rooms;
		public List<Int2> entrances;
		public List<int> entranceSizes;
		

		public Wing(int[] bounds, int minRoomSize, int maxCorridorSize, float doorNoise)
		{
			this.bounds = bounds;
			this.doorNoise = doorNoise;
			this.minRoomSize = minRoomSize;
			this.maxCorridorSize = maxCorridorSize;

			this.rooms = new List<Room>();
			this.entrances = new List<Int2>();
			this.entranceSizes = new List<int>();
		}

		public void AddEntrance(Int2 point, int size)
		{
			entrances.Add(point);
			entranceSizes.Add(size);
		}
	}

	#endregion

	# region Positions and points

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
			boundsOffset = Mathf.Min(bounds[top], bounds[bottom]);
			x = bounds[side];
			z = Mathf.Clamp(Mathf.RoundToInt(sideSize * position), 0, sideSize) + boundsOffset;
		}
		else
		{
			sideSize = Distance(bounds[right], bounds[left]);
			boundsOffset = Mathf.Min(bounds[right], bounds[left]);
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
		int spread = World.chunkSize;
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
}
