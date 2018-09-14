using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest
{
	World world;

	//	Bottom left column position
	public Vector3 position;
	//	Coherent noise value
	float noise;

	public int width = 0;
	public int height = 0;

	//	Columns that are part of the POI
	int[,] baseMatrix;
	//	Edge columns that are adjacent to the same biome
	int[,] exposedEdgeMatrix;
	//	Edge columns that are adjacent to a different biome
	int[,] boundaryEdgeMatrix;
	//	All edge columns
	int[,] edgeMatrix;
	//	Columns where a zone has been generated
	int[,] occupiedMatrix;
	//	Matrix to Column class instance reference
	public Column[,] columnMatrix;

	List<Zone> zones = new List<Zone>();

	List<Column> allColumns = new List<Column>();
	List<Column> exposedEdge = new List<Column>();
	List<Column> boundaryEdge = new List<Column>();

	float right = 0, left = 0, top = 0, bottom = 0;

	public PointOfInterest(Column initialColumn, World world)
	{
		this.world = world;

		InitialiseBoundaries(initialColumn.position);

		//	Load all connected POI eligible columns
		List<Column> allCreated = DiscoverCells(initialColumn);

		//	Create integer matrix showing eligible columns as 1
		MapMatrix();

		//	Generate noise to be used in pseudo random decision making
		noise = Mathf.PerlinNoise(position.x, position.y);

		//	Find largest square
		zones.Add(LargestSquare(occupiedMatrix));
		//	Set above square's columns as occupied in matrix
		UpdateOccupied(zones[0].matrix);
		//	Process new zone
		ProcessZone(zones[0]);

		//	System for creating random building arrangements
		LSystem lSystem = new LSystem(zones[0]);
		//	Generate building from POI library
		TerrainGenerator.worldBiomes.POIs.GenerateMatrixes(lSystem, zones[0]);

		//	Generate column topologies
		foreach(Column column in allCreated)
		{
			world.GenerateColumnTopology(column);
		}

		DebugMatrix(zones[0].matrix, Color.yellow, 2f);
		DebugMatrix(exposedEdgeMatrix, Color.green, 2.5f);
		DebugMatrix(boundaryEdgeMatrix, Color.white, 2.5f);
	}

	#region POI Processing

	//	TODO: Are we sure this is deterministic - currently ChooseEntrance() is not
	List<Column> DiscoverCells(Column initialColumn)
	{
		List<Column> allCreatedColumns = new List<Column>();
		allCreatedColumns.Add(initialColumn);

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
				Vector3[] neighbourPos = {	new Vector3(column.position.x + World.chunkSize, column.position.y, column.position.z),
											new Vector3(column.position.x - World.chunkSize, column.position.y, column.position.z),
											new Vector3(column.position.x, column.position.y, column.position.z + World.chunkSize),
											new Vector3(column.position.x, column.position.y, column.position.z - World.chunkSize)	};

				//	Check adjacent columns
				for(int i = 0; i < neighbourPos.Length; i++)
				{
					Column newColumn;
					if(world.CreateColumn(neighbourPos[i], out newColumn))
					{
						//	If adjacent column needed spawning and is eligible for POI
						if(newColumn.IsPOI)
						{
							newColumns.Add(newColumn);

							//	Track outwardmost columns for matrix size
							CheckBoundaries(newColumn.position);
						}
						allCreatedColumns.Add(newColumn);
					}
					else
					{
						newColumn = World.columns[neighbourPos[i]];
					}

					if(!isEdge && !newColumn.IsPOI)
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

		return allCreatedColumns;
	}

	//	Initialise boundary values make sure Checkboundaries works
	void InitialiseBoundaries(Vector3 columnPosition)
	{
		right = columnPosition.x;
		left = columnPosition.x;
		top = columnPosition.z;
		bottom = columnPosition.z;
	}
	//	Update right, top , left and bottom most column positions
	void CheckBoundaries(Vector3 columnPosition)
	{
		if(columnPosition.x > right) right = columnPosition.x;
		else if(columnPosition.x < left) left = columnPosition.x;
		if(columnPosition.z > top) top = columnPosition.z;
		else if(columnPosition.z < bottom) bottom = columnPosition.z;
	}

	//	Map matrix of 0 for non POI column and 1 for POI column
	//	Store corresponding matrix with column intances
	void MapMatrix()
	{
		//	Difference divided by chunk size + 1
		width = (int)(((Mathf.Max(right, left) - Mathf.Min(right, left)) / World.chunkSize) + 1);
		height = (int)(((Mathf.Max(top, bottom) - Mathf.Min(top, bottom)) / World.chunkSize) + 1);

        //	World position of POI grid 0,0
        position = new Vector3(this.left, 0, this.bottom);

		columnMatrix = new Column[width,height];

		//	Map all eligible columns in matrix
		foreach(Column column in allColumns)
		{
			Vector3 matrixLocalPos = LocalPosition(column.position);

			int x = (int)matrixLocalPos.x;
			int z = (int)matrixLocalPos.z;

			columnMatrix[x,z] = column;
		}
		//	Map other matrixes
		baseMatrix = ColumnsToMatrix(allColumns);
		exposedEdgeMatrix = ColumnsToMatrix(exposedEdge);
		boundaryEdgeMatrix = ColumnsToMatrix(boundaryEdge);
		edgeMatrix = MergeMatrixes(exposedEdgeMatrix, boundaryEdgeMatrix);
		occupiedMatrix = baseMatrix.Clone() as int[,];
	}

	//	Find the largest square of 1s in an int matrix
	Zone LargestSquare(int[,] baseMatrix, int minX = 0, int minZ = 0, int maxX = 0, int maxZ = 0)
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

		for(int x = maxX; x >= minX; x--)
			for(int z = maxZ; z >= minZ; z--)
			{
				//	At edge, max square size is 1 so default to original matrix
				if(x == maxX || z == maxZ) continue;

				//	Square is 1, value is equal to 1 + lowed of the three adjacent squares
				if(baseMatrix[x,z] > 0) cacheMatrix[x,z] = 1 + Util.MinInt(new int[3] {cacheMatrix[x,z+1], cacheMatrix[x+1,z], cacheMatrix[x+1,z+1]});

				//	Largest square so far, store values
				if(cacheMatrix[x,z] > resultSize)
				{
					resultX = x;
					resultZ = z;
					resultSize = cacheMatrix[x,z];
				}
			}

		//	Draw resulting matrix
		for(int x = resultX; x < resultX + resultSize; x++)
			for(int z = resultZ; z < resultZ + resultSize; z++)
			{
				resultMatrix[x,z] = 1;
			}
		return new Zone(this, resultX, resultZ, resultSize, resultMatrix);
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

	#endregion

	#region Zone Processing

	//	Determine which sides of zone are back/front i.e. least/most exposed
	void ProcessZone(Zone e)
	{
		int[,] testMatrix = new int[width,height];

		int rightScore = 0;
		int leftScore = 0;
		int topScore = 0;
		int bottomScore = 0;

		//	Iterate over squares on each side, scoring for openness
		for(int z = e.bottom; z <= e.top; z++)
			{
				int x = e.right;
				if(exposedEdgeMatrix[x,z] == 1) rightScore -= 1;				//	Exposed edge
				else if(boundaryEdgeMatrix[x,z] == 1) rightScore -= 2;			//	Boundary edge
				else if(x+1 < width && edgeMatrix[x+1,z] == 0) rightScore += 2;	//	No edge or adjacent edge
				else rightScore += 1;											//	No edge
				
				x = e.left;
				if(exposedEdgeMatrix[x,z] == 1) leftScore -= 1;
				else if(boundaryEdgeMatrix[x,z] == 1) leftScore -= 2;
				else if(x-1 >= 0 && edgeMatrix[x-1,z] == 0) leftScore += 2;
				else leftScore += 1;
			}

		for(int x = e.left; x <= e.right; x++)
			{
				int z = e.top;
				if(exposedEdgeMatrix[x,z] == 1) topScore -= 1;
				else if(boundaryEdgeMatrix[x,z] == 1) topScore -= 2;
				else if(z+1 < height && edgeMatrix[x,z+1] == 0) topScore += 2;
				else topScore += 1;

				z = e.bottom;
				if(exposedEdgeMatrix[x,z] == 1) bottomScore -= 1;
				else if(boundaryEdgeMatrix[x,z] == 1) bottomScore -= 2;
				else if(z-1 >= 0 && edgeMatrix[x,z-1] == 0) bottomScore += 2;	
				else bottomScore += 1;	
			}

		int[] scores = new int[] { rightScore, leftScore, topScore, bottomScore };

		//	Check scores
		e.back = (Zone.Side)Util.MinIntIndex(scores);
		e.front = (Zone.Side)Util.MaxIntIndex(scores);

		switch(e.front)
		{
			case Zone.Side.BOTTOM:
				for(int x = e.left; x <= e.right; x++) testMatrix[x,e.bottom] = 1;
				break;
			case Zone.Side.TOP:
				for(int x = e.left; x <= e.right; x++) testMatrix[x,e.top] = 1;
				break;
			case Zone.Side.LEFT:
				for(int z = e.bottom; z <= e.top; z++) testMatrix[e.left,z] = 1;
				break;
			case Zone.Side.RIGHT:
				for(int z = e.bottom; z <= e.top; z++) testMatrix[e.right,z] = 1;
				break;
		}

		//DebugMatrix(testMatrix, Color.cyan, 1.2f);
	}

	#endregion


	#region Utility

	int[,] MergeMatrixes(int[,] matrixA, int[,] matrixB)
	{
		int[,] matrix = new int[width, height];
		for(int x = 0; x < width; x++)
			for(int z = 0; z < height; z++)
			{
				if(matrixA[x,z] == 1 || matrixB[x,z] == 1) matrix[x,z] = 1;
			}
		return matrix;
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

	Vector3 WorldPosition(int x, int z)
	{
		return (new Vector3(x, 0, z) * World.chunkSize) + this.position;
	}

	Vector3 LocalPosition(Vector3 worldPosition)
	{
		return (worldPosition - this.position) / World.chunkSize;
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

	#endregion
}
