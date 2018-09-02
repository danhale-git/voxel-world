using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest
{
	World world;

	//	Bottom left column position
	Vector3 position;
	//	Coherent noise value
	float noise;

	int width = 0;
	int height = 0;

	int[,] baseMatrix;
	int[,] exposedEdgeMatrix;
	int[,] boundaryEdgeMatrix;
	int[,] occupiedMatrix;
	Column[,] columnMatrix;

	Int2 entrance;

	List<POIElement> elements = new List<POIElement>();

	List<Column> allColumns = new List<Column>();
	List<Column> exposedEdge = new List<Column>();
	List<Column> boundaryEdge = new List<Column>();

	float rightMost = 0, leftMost = 0, topMost = 0, bottomMost = 0;

	public PointOfInterest(Column initialColumn, World world)
	{
		this.world = world;

		InitialiseBoundaries(initialColumn.position);

		//	Load all connected POI eligible columns
		DiscoverCells(initialColumn);

		Debug.Log("POI created with "+allColumns.Count+" columns");
		Debug.Log("From X: "+leftMost+" to "+rightMost+"\nFrom: Z "+bottomMost+" to "+topMost);

		//	Create integer matrix showing eligible columns as 1
		MapMatrix();

		//	Generate noise to be used in pseudo random decision making
		noise = Mathf.PerlinNoise(position.x, position.y);

		entrance = ChooseEntrance();

		elements.Add(LargestSquare(occupiedMatrix));
		UpdateOccupied(elements[0].matrix);

		ProcessMainElement(elements[0]);

		DebugMatrix(elements[0].matrix, Color.yellow, 2f);
		DebugMatrix(exposedEdgeMatrix, Color.green, 2.5f);
		DebugMatrix(boundaryEdgeMatrix, Color.white, 2.5f);

	}

	//	TODO: Are we sure this is coherent/deterministic
	void DiscoverCells(Column initialColumn)
	{
		allColumns.Add(initialColumn);

		//	Columns currently being checked
		List<Column> columnsToCheck = new List<Column>();
		columnsToCheck.Add(initialColumn);

		//	Continue until no more chunks need to be checked
		//  TODO: Revisit the 500 iteration limit
		int iterationCount = 0;
		while(iterationCount < 500)	//	recursive
		{
			List<Column> newColumns = new List<Column>();

			//	Process current column list
			foreach(Column column in columnsToCheck)
			{
				bool isEdge = false;
				bool isBoundary = false;
				Vector3[] neighbourPos = Util.HorizontalChunkNeighbours(column.position);

				//	Check adjacent columns
				for(int i = 0; i < neighbourPos.Length; i++)
				{
					Column newColumn;
					if(world.GenerateColumnData(neighbourPos[i], out newColumn))
					{
						//	If adjacent column needed spawning and is eligible for POI
						if(newColumn.IsPOI)
						{
							newColumns.Add(newColumn);

							//	Track outwardmost columns for matrix size
							CheckBoundaries(newColumn.position);
						}
					}
					else
					{
						newColumn = World.columns[neighbourPos[i]];
					}

					if(!isEdge && !newColumn.IsPOI && i < 4)
					{
						//	Track POI edges that do no cross into other biomes
						isEdge = true;
						isBoundary = newColumn.biomeBoundary;
					}
				}

				if(isEdge)
				{
					if(!isBoundary)
						exposedEdge.Add(column);
					else
						boundaryEdge.Add(column);
				}
			}
			if(newColumns.Count == 0) break;

			//	Check newly created eligible columns next
			columnsToCheck = newColumns;

			//	Store new columns
			allColumns.AddRange(newColumns);

			//	Safety, maybe remove this later
			iterationCount++;
			if(iterationCount > 498) Debug.Log("Too many structure processing iterations!\nAbandoned while loop early");
		}
	}

	//	Initialise boundary values make sure Checkboundaries works
	void InitialiseBoundaries(Vector3 columnPosition)
	{
		rightMost = columnPosition.x;
		leftMost = columnPosition.x;
		topMost = columnPosition.z;
		bottomMost = columnPosition.z;
	}
	//	Update right, top , left and bottom most column positions
	void CheckBoundaries(Vector3 columnPosition)
	{
		if(columnPosition.x > rightMost) rightMost = columnPosition.x;
		else if(columnPosition.x < leftMost) leftMost = columnPosition.x;
		if(columnPosition.z > topMost) topMost = columnPosition.z;
		else if(columnPosition.z < bottomMost) bottomMost = columnPosition.z;
	}

	//	Map matrix of 0 for non POI column and 1 for POI column
	//	Store corresponding matrix with column intances
	void MapMatrix()
	{
		float r = Mathf.Abs(rightMost);
		float l = Mathf.Abs(leftMost);
		float t = Mathf.Abs(topMost);
		float b = Mathf.Abs(bottomMost);

		//	Difference divided by chunk size + 1
		width = (int)(((Mathf.Max(r, l) - Mathf.Min(r, l)) / World.chunkSize) + 1);
		height = (int)(((Mathf.Max(t, b) - Mathf.Min(t, b)) / World.chunkSize) + 1);

		//	World position of POI grid 0,0
		position = new Vector3(leftMost, 0, bottomMost);

		columnMatrix = new Column[width,height];

		//	Map all eligible columns in matrix
		foreach(Column column in allColumns)
		{
			Vector3 matrixLocalPos = LocalPosition(column.position);

			int x = (int)matrixLocalPos.x;
			int z = (int)matrixLocalPos.z;

			columnMatrix[x,z] = column;
		}
		baseMatrix = ColumnsToMatrix(allColumns);
		exposedEdgeMatrix = ColumnsToMatrix(exposedEdge);
		boundaryEdgeMatrix = ColumnsToMatrix(boundaryEdge);

		occupiedMatrix = baseMatrix.Clone() as int[,];
	}

	int[,] ColumnsToMatrix(List<Column> columns)
	{
		int[,] matrix = new int[width,height];

		foreach(Column column in columns)
		{
			//	Local position of column
			Vector3 matrixLocalPos = LocalPosition(column.position);

			//	Array index for column
			int x = (int)matrixLocalPos.x;
			int z = (int)matrixLocalPos.z;

			matrix[x,z] = 1;
		}
		return matrix;
	}

	//	Find the largest square of 1s in an int matrix
	POIElement LargestSquare(int[,] baseMatrix, int minX = 0, int minZ = 0, int maxX = 0, int maxZ = 0)
	{
		//	Default or clamp matrix size
		if(maxX == 0 || maxX >= width)
			maxX = width - 1;
		if(maxZ == 0 || maxZ >= width)
			maxZ = height - 1;

		//	Copy original matix to cache so it defaults to original matrix values
		int[,] cacheMatrix = baseMatrix.Clone() as int[,];

		//	Resulting matrix origin and dimensions
		int resultX = 0;
		int resultZ = 0;
		int resultSize = 0;
		int[,] resultMatrix = new int[width,height];

		for(int x = minX; x <= maxX; x++)
			for(int z = minZ; z <= maxZ; z++)
			{
				//	At edge, max square size is 1 so default to original matrix
				if(x == minX || z == minZ) continue;

				//	Square is 1, value is equal to 1 + lowed of the three adjacent squares
				if(baseMatrix[x,z] > 0) cacheMatrix[x,z] = 1 + Util.MinInt(new int[3] {cacheMatrix[x,z-1], cacheMatrix[x-1,z], cacheMatrix[x-1,z-1]});

				//	Larges square so far, store values
				if(cacheMatrix[x,z] > resultSize)
				{
					resultX = x;
					resultZ = z;
					resultSize = cacheMatrix[x,z];
				}
			}

		//	Draw resulting matrix
		for(int x = resultX; x > resultX - resultSize; x--)
			for(int z = resultZ; z > resultZ - resultSize; z--)
			{
				resultMatrix[x,z] = 1;
			}
		return new POIElement(resultX, resultZ, resultSize, resultMatrix);
	}

	// Track occupied columns
	void UpdateOccupied(int[,] occupiedColumns)
	{
		for(int x = 0; x < width; x++)
			for(int z = 0; z < height; z++)
			{
				if(occupiedColumns[x,z] == 1)
				{
					this.occupiedMatrix[x,z] = 0;
				}
			}
	}

	Int2 ChooseEntrance()
	{
		int index = Mathf.FloorToInt(exposedEdge.Count * noise);
		Vector3 localPosition = LocalPosition(exposedEdge[index].position);

		return new Int2((int)localPosition.x, (int)localPosition.z);
	}

	void DebugMatrix(int[,] matrix, Color color, float divisor)
	{
		for(int z = 0; z < height; z++)
			for(int x = 0; x < width; x++)
			{
				Vector3 worldPos = WorldPosition(x, z) + (Vector3.up * 100);

				if(matrix[x,z] == 1)
					World.debug.OutlineChunk(worldPos, color, sizeDivision: divisor);
			}
	}

	Vector3 WorldPosition(int x, int z)
	{
		return (new Vector3(x, 0, z) * World.chunkSize) + this.position;
	}

	Vector3 LocalPosition(Vector3 worldPosition)
	{
		return (worldPosition - this.position) / World.chunkSize;
	}

	struct POIElement
	{
		public int x, z, size;
		public int[,] matrix;
		public POIElement(int x, int z, int size, int[,] matrix)
		{
			this.x = z;
			this.z = z;
			this.size = size;
			this.matrix = matrix;
		}
	}

	void ProcessMainElement(POIElement element)
	{
		int[,] testMatrix = new int[width,height];
		for(int x = element.x; x < element.x + element.size; x++)
			{
				Debug.Log(element.x);
				Debug.Log(element.z);
				//testMatrix[x,0] = 1;
			}

		DebugMatrix(testMatrix, Color.cyan, 1.5f);
	}
}
