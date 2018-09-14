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
		int seed = Random.Range(0,10000);
		Debug.Log("SEED: "+ seed);
		noiseGen.SetSeed(2202);
		//noiseGen.SetSeed(seed);


		//	Base noise generated from POI position
		noiseX = (int)zone.POI.position.x;
		noiseY = (int)zone.POI.position.z;
	}

	void ResetNoise()
	{
		noise = noiseGen.GetNoise01(noiseX, noiseY);
		int increment = Mathf.RoundToInt(noise * 10);
		noiseX += increment;
		noiseY += increment;
	}

	public struct Line
	{
		public readonly Int2 start, end;
		public Line(Int2 start, Int2 end)
		{
			this.start = start;
			this.end = end;
		}
	}

	/*public struct Building
	{
		public readonly List<int[]> bounds;
		public readonly List<Int2> joints;
		public readonly List<Zone.Side> jointSides;

		public readonly List<int>[] layerIndices;
		
		public List<Line> lines;

		public Building(List<int[]> bounds, List<Int2> joints, List<Zone.Side> jointSides, List<int>[] layerIndices)
		{
			this.bounds = bounds;
			this.joints = joints;
			this.jointSides = jointSides;
			this.layerIndices = layerIndices;

			Int2 centerMain = LSystem.BoundsCenter(bounds[0]);

			lines = new List<Line>();

			//	master line extends all the way
			//	all other lines meet it
			if(layerIndices.Length > 1)
			{
				foreach(int i in layerIndices[1])
				{
					Line line;
					if((int)jointSides[i] < 2)
					{
						line = new Line(joints[i], new Int2(centerMain.x, joints[i].z));
					}
					else
					{
						line = new Line(joints[i], new Int2(joints[i].x, centerMain.z));
					}
					Debug.Log("drew line:"+i+" "+line.start.x+" "+line.end.x);
					lines.Add(line);
				}
			}

			foreach(Line l in lines)
			{

				
			}
		}
	}*/

	//	Operations and comparisons with regards to current square orientaion
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

	public bool SquareInBounds(int[] perimeterBounds, Zone.Side perimeterSide, float positionOnSide = 0, int minWidth = 0, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		Int2 originPoint;
		if(positionOnSide != 0)
			originPoint = PositionOnSide((int)perimeterSide, perimeterBounds, positionOnSide);
		else
			originPoint = RandomPointOnSide((int)perimeterSide, perimeterBounds);

		return GenerateSquare(0, originPoint, perimeterSide, perimeterBounds, false, minWidth, maxWidth, minLength, maxLength);
	}
	public bool ConnectedSquare(int[] perimeterBounds, int parentIndex, Zone.Side parentSide = 0, bool bestSide = false, float positionOnSide = 0, int minWidth = 0, int maxWidth = 0, int minLength = 0, int maxLength = 0)
	{
		int index = currentBounds.Count;
		//int[] newBounds;
		Int2 originPoint;
		Zone.Side originSide;

		int[] parentBounds = currentBounds[parentIndex];

		if(bestSide)
			parentSide = MostOpenSide(parentBounds, EligibleSides((int)originSides[parentIndex], parentBounds), zone.bufferedBounds);

		if(positionOnSide != 0)
			originPoint = PositionOnSide((int)parentSide, parentBounds, positionOnSide);
		else
			originPoint = RandomPointOnSide((int)parentSide, parentBounds);

		originSide = Zone.Opposite(parentSide);

		return GenerateSquare(index, originPoint, originSide, perimeterBounds, false, minWidth, maxWidth, minLength, maxLength);
	}

	bool GenerateSquare(int index, Int2 originPoint, Zone.Side originSide, int[] perimeterBounds, bool adjacentOverride, int minWidth, int maxWidth, int minLength, int maxLength)
	{
		Vector3 global = MatrixToGlobal(originPoint);
		ResetNoise();
		Debug.Log(noise);
		//	Rotate script values to face the same way as this square
		Rotate(originSide);

		Debug.Log(originSide + " "+originPoint.x+" "+originPoint.z);

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

		Debug.Log(squareWidth+" x "+squareLength);

		if(squareWidth < minWidth || squareLength < minLength)
			return false;
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

# region Positions and points

//	Get opposite point from point on side in bounds
	Int2 OppositePoint(Int2 point, int side, int[] bounds)
	{
		if(side > 1)
			return new Int2(point.x, bounds[Zone.Opposite(side)]);
		else
			return new Int2(bounds[Zone.Opposite(side)], point.z);
	}

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
	Int2 RandomPointOnSide(int side, int[] bounds, int lowOffset = 0, int highOffset = 0)
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

	Int2 RandomPointOnLine(Line line)
	{
		int x;
		int z;
		if(line.start.x == line.end.x)
		{
			z = RandomRange(line.start.z, line.end.z);
			x = line.start.x;
		}
		else
		{
			x = RandomRange(line.start.x, line.end.x);
			z = line.start.z;
		}
		return new Int2(x,z);
	}

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

	public void SegmentBounds(int segmentWidth, int segmentLength, int[] bounds, Zone.Side backSide, out int[] horizontalDivides, out int[] verticalDivides)
	{
		Rotate(backSide);
		int width = Distance(bounds[right], bounds[left]);
		int length = Distance(bounds[front], bounds[back]);

		horizontalDivides = new int[(width/segmentWidth) - 1];
		verticalDivides = new int[(length/segmentLength) - 1];

		int[] currentHorizontal;
		int[] currentVertical;

		//	Define length/width of segments relative to back side but return arrays relative to global orientation
		if((int)currentBack > 1)
		{
			currentHorizontal = horizontalDivides;
			currentVertical = verticalDivides;
		}
		else
		{
			currentHorizontal = verticalDivides;
			currentVertical = horizontalDivides;
		}


		for(int i = 1; i <= horizontalDivides.Length; i++)
		{
			currentHorizontal[i-1] = Mathf.Min(bounds[right], bounds[left]) + (i * segmentWidth);
		}
		for(int i = 1; i <= verticalDivides.Length; i++)
		{
			currentVertical[i-1] = Mathf.Min(bounds[front], bounds[back]) + (i * segmentLength);			
		}
	}

# region Drawing

	/*public void DrawBuilding(Building building, int[,] matrix)
	{
		foreach(int[] b in building.bounds)
		{
			DrawBoundsBorder(b, matrix, 1);
		}
		foreach(Line l in building.lines)
		{
			DrawLine(l, matrix, 1);
		}
	}*/

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

	public void DrawLine(Line line, int[,] matrix, int value)
	{
		if(line.start.x == line.end.x)
		{
			int sign = (int)Mathf.Sign(line.end.z - line.start.z);
			for(int z = line.start.z; z != line.end.z; z+=sign)
			{
				matrix[line.start.x,z] = value;
			}
		}
		else
		{
			int sign = (int)Mathf.Sign(line.end.x - line.start.x);
			for(int x = line.start.x; x != line.end.x; x+=sign)
			{
				matrix[x,line.start.z] = value;
			}
		}
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

	public void DrawBoundsBorderWithSegments(int[] bounds, int[] horizontalDivides, int[] verticalDivides, int[,] matrix, int value)
	{
		for(int x = bounds[1]; x <= bounds[0]; x++)
		{
			matrix[x, bounds[3]] = value;
			matrix[x, bounds[2]] = value;

			if(x > bounds[1] && x < bounds[0])
			{
				foreach(int divide in verticalDivides)
				{
					matrix[x, divide] = value;
				}
			}
		}

		for(int z = bounds[3]; z <= bounds[2]; z++)
		{
			matrix[bounds[1], z] = value;
			matrix[bounds[0], z] = value;

			if(z > bounds[3] && z < bounds[2])
			{
				foreach(int divide in horizontalDivides)
				{
					matrix[divide, z] = value;
				}
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

	public int CurrentIndex()
	{
		return currentBounds.Count - 1;
	}

}
