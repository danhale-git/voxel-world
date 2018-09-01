using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest
{
	World world;

	//	Bottom left column position
	Vector3 position;

	int width = 0;
	int height = 0;

	int[,] matrix;
	int[,] edgeMatrix;
	int[,] occupiedMatrix;
	Column[,] columnMatrix;

	List<Column> allColumns = new List<Column>();
	List<Column> exposedEdge = new List<Column>();

	float rightMost = 0, leftMost = 0, topMost = 0, bottomMost = 0;

	public struct POIData
	{
		public bool isEdge;
	}

	public PointOfInterest(Column initialColumn, World world)
	{
		this.world = world;

		InitialiseBoundaries(initialColumn.position);
		DiscoverCells(initialColumn);

		Debug.Log("POI created with "+allColumns.Count+" columns");
		Debug.Log("From X: "+leftMost+" to "+rightMost+"\nFrom: Z "+bottomMost+" to "+topMost);
		MapMatrix();

		DrawDebug();
	}

	//	TODO: process initial column the same as the rest to keep everything coherent
	void DiscoverCells(Column initialColumn)
	{
		allColumns.Add(initialColumn);

		//	Columns currently being checked
		List<Column> columnsToCheck = new List<Column>();
		columnsToCheck.Add(initialColumn);

		int iterationCount = 0;
		//	Continue until no more chunks need to be checked (should never need 500)
		while(iterationCount < 500)	//	recursive
		{
			List<Column> newColumns = new List<Column>();

			//	Process current column list
			foreach(Column column in columnsToCheck)	//	check columns
			{
				bool isEdge = false;

				Vector3[] neighbourPos = Util.HorizontalChunkNeighbours(column.position);

				//	Check adjacent columns
				for(int i = 0; i < neighbourPos.Length; i++)	//	adjacent
				{
					Column newColumn;
					if(world.GenerateColumnData(neighbourPos[i], out newColumn))
					{
						//	If adjacent column needed spawning and is eligible for POI
						if(newColumn.IsPOI)
						{
							newColumns.Add(newColumn);
							CheckBoundaries(newColumn.position);
						}
					}
					else
					{
						newColumn = World.columns[neighbourPos[i]];
					}

					if(!isEdge && !newColumn.IsPOI && !newColumn.biomeBoundary && i < 4)
					{
						isEdge = true;
						exposedEdge.Add(column);
					}
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

		//	World position of POI grid
		position = new Vector3(leftMost, 0, bottomMost);

		matrix = new int[width,height];
		edgeMatrix = new int[width,height];
		columnMatrix = new Column[width,height];

		//	Map all eligible columns in matrix
		foreach(Column column in allColumns)
		{
			//	Local position of column
			Vector3 matrixLocalPos = column.position - this.position;

			// Array index of column
			int x = (int)(matrixLocalPos.x / World.chunkSize);
			int z = (int)(matrixLocalPos.z / World.chunkSize);

			matrix[x,z] = 1;
			columnMatrix[x,z] = column;
		}
		//	Map exposed edges
		foreach(Column column in exposedEdge)
		{
			Vector3 matrixLocalPos = column.position - this.position;

			int x = (int)(matrixLocalPos.x / World.chunkSize);
			int z = (int)(matrixLocalPos.z / World.chunkSize);

			edgeMatrix[x,z] = 1;
		}
		occupiedMatrix = matrix;
	}

	int[,] LargestSquare(int[,] baseMatrix, int minX = 0, int minZ = 0, int maxX = 0, int maxZ = 0)
	{
		if(maxX == 0 || maxX >= width)
			maxX = width - 1;

		if(maxZ == 0 || maxZ >= width)
			maxZ = height - 1;

		int[,] cacheMatrix = baseMatrix.Clone() as int[,];

		int resultX = 0;
		int resultZ = 0;
		int resultSize = 0;
		int[,] resultMatrix = new int[width,height];

		for(int x = minX; x <= maxX; x++)
			for(int z = minZ; z <= maxZ; z++)
			{
				//	Default to newMatrix value at edge
				if(x == minX || z == minZ) continue;
				if(baseMatrix[x,z] > 0) cacheMatrix[x,z] = 1 + Util.MinInt(new int[3] {cacheMatrix[x,z-1], cacheMatrix[x-1,z], cacheMatrix[x-1,z-1]});
				if(cacheMatrix[x,z] > resultSize)
				{
					resultX = x;
					resultZ = z;
					resultSize = cacheMatrix[x,z];
				}
			}

		for(int x = resultX; x > resultX - resultSize; x--)
			for(int z = resultZ; z > resultZ - resultSize; z--)
			{
				resultMatrix[x,z] = 1;
			}
		return resultMatrix;
	}

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

	void DrawDebug()
	{
		int[,] largestSquare = LargestSquare(occupiedMatrix);

		UpdateOccupied(largestSquare);

		int[,] secondLargestSquare = LargestSquare(occupiedMatrix);

		UpdateOccupied(secondLargestSquare);

		int[,] thirdLargestSquare = LargestSquare(occupiedMatrix);

		for(int z = 0; z < height; z++)
			for(int x = 0; x < width; x++)
			{
				Vector3 worldPos = new Vector3((x*World.chunkSize)+this.position.x, 100, (z*World.chunkSize)+this.position.z);
				
				if(largestSquare[x,z] == 1)
					World.debug.OutlineChunk(worldPos, Color.yellow, sizeDivision: 2.5f);

				if(secondLargestSquare[x,z] == 1)
					World.debug.OutlineChunk(worldPos, Color.black, sizeDivision: 2f);
				
				if(thirdLargestSquare[x,z] == 1)
					World.debug.OutlineChunk(worldPos, Color.cyan, sizeDivision: 2f);


				if(edgeMatrix[x,z] == 1)
					World.debug.OutlineChunk(worldPos, Color.green, sizeDivision: 2.5f);

			}
	}
}
