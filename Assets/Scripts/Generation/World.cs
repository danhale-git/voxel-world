using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.UI;


public class World : MonoBehaviour
{
	//	DEBUG
	public Material defaultMaterial;
	public bool disableChunkGeneration;
	public static DebugWrapper debug;
	public Text debugText;

	int chunksCreated = 0;
	public int chunksGenerated = 0;
	public int chunksDrawn = 0;
	//	DEBUG

	//	Number of chunks that are generated around the player
	public static int viewDistance = 6;
	//	Size of all chunks
	public static int chunkSize = 16;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 50;
	//	Draw edges where no more chunks exist
	public static bool drawEdges = true;

	TerrainGenerator terrain;
	public static Shapes.Meshes shapeMeshes;

	//	All block data for a chunkSize/chunkSize area
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();

	//	2D terrain data such as heightmaps
	public static Dictionary<Vector3, Column> columns = new Dictionary<Vector3, Column>();

	//	Collections of columns containing post processed terrain or other elements
	public static List<PointOfInterest> POIs = new List<PointOfInterest>();

	Coroutine currentCoroutine;
	List<IEnumerator> coroutines = new List<IEnumerator>();
	bool coroutineRunning = false;

	public PlayerController player;

	void Awake()
	{
		shapeMeshes = new Shapes.Meshes();
	}

	void Start()
	{
		debug = gameObject.AddComponent<DebugWrapper>();
		debug.world = this;

		terrain = new TerrainGenerator();

		//	Create initial chunks
		//	Must always be multiple of ChunkSize
		LoadChunks(VoxelOwner(player.transform.position), viewDistance);
	}

	#region World Generation

	//	Generate and draw chunks in a of veiwDistance around player
	//	Called in PlayerController and World.Start()
	public void LoadChunks(Vector3 centerChunk, int radius)
	{
		if(disableChunkGeneration)
		{ Debug.Log("Chunk generation disabled!"); return; }

		//	Clear queue
		//if(coroutineRunning) ClearCoroutines();

		// Zero out Y
		centerChunk = new Vector3(centerChunk.x, 0, centerChunk.z);

		//	Queue up chunk generation processes

		//	Generate column class instances (heightmaps and other 2D terrain data)
		//	+1 buffer allows adjacent column lookups without TryGetValue
		AddCoroutine(CreateColumnsInSquare(centerChunk, radius+1, 20));
		//	Generate cellular data and check POI eligibility in Column constructor
		//	If a point of interest (POI) is detected, generate all chunks in the point of interest before continuing
		//	Generate height maps

		//	Determine the highest and lowest chunk in each column that must be generated/drawn
		AddCoroutine(ChunksInSquare(centerChunk, radius, GetColumnSize, 20));

		//	Create chunk instances for all chunks that will be generated
		AddCoroutine(ChunksInSquare(centerChunk, radius, CreateColumnChunks, 20));

		//	Generate block data for all chunks to be generated
		AddCoroutine(ChunksInSquare(centerChunk, radius, GenerateColumnChunks, 20));

		/*//	Generate structure block data for column
		AddCoroutine(ChunksInSquare(centerChunk, radius, GenerateColumnStructures, 10));*/

		//	Process surface blocks for smoothed block types and apply shape types and rotation
		AddCoroutine(ChunksInSquare(centerChunk, radius-1, SmoothColumnChunks, 20));

		//	Collect mesh data and generate chunk meshes in a spiral starting at the player
		AddCoroutine(ChunksInSpiral(centerChunk, radius-2, DrawColumnChunks, 2));
	}

	//	Add coroutine to list, start if necessary
	void AddCoroutine(IEnumerator coroutine)
	{
		coroutines.Add(coroutine);
		if(coroutines.Count == 1)
		{
			coroutineRunning = true;
			currentCoroutine = StartCoroutine(coroutines[0]);
		}
	}
	//	Coroutine has finished running
	void CoroutineComplete()
	{
		if(currentCoroutine != null) StopCoroutine(currentCoroutine);
		coroutines.RemoveAt(0);
		if(coroutines.Count == 0)
		{
			coroutineRunning = false;
		}
		else
		{
			currentCoroutine = StartCoroutine(coroutines[0]);
		}
		debug.Output("Queued coroutines", coroutines.Count.ToString());
	}
	//	Clear all coroutines
	void ClearCoroutines()
	{
		coroutines.Clear();
		if(currentCoroutine != null) StopCoroutine(currentCoroutine);
	}

	#endregion

	#region IEnumerators

	delegate bool ChunkOperation(Vector3 position);

	//	Special IEnumerator for handling structure post processing during column creation
	IEnumerator CreateColumnsInSquare(Vector3 center, int radius, int iterationsPerFrame)
	{
		int iterationCount = 0;
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = center + offset;

				Column newColumn;

				if(CreateColumn(position, out newColumn))
				{
					//	If column is eligible for point of interest, discover all adjacent aligible columns before continuing
					if(newColumn.IsPOI) POIs.Add(new PointOfInterest(newColumn, this));
					//	If it's a POI columns will be generated after POI processing in PointOfInterest
					else GenerateColumnTopology(newColumn);

					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
			}
		CoroutineComplete();		
	}

	IEnumerator ChunksInSquare(Vector3 center, int radius, ChunkOperation delegateOperation, int iterationsPerFrame)
	{
		int iterationCount = 0;
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = center + offset;

				if(delegateOperation(position))
				{
					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
			}
		CoroutineComplete();		
	}

	IEnumerator ChunksInSpiral(Vector3 center, int radius, ChunkOperation delegateOperation, int iterationsPerFrame)
	{
		Vector3 position = center;
		//	Trim radius to allow buffer of generated chunks
		delegateOperation(position);

		int iterationCount = 0;

		int increment = 1;
		for(int i = 0; i < radius; i++)
		{
			//	right then back
			for(int r = 0; r < increment; r++)
			{
				position += Vector3.right * chunkSize;
				if(delegateOperation(position))
				{
					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
			}
			for(int b = 0; b < increment; b++)
			{
				position += Vector3.back * chunkSize;
				if(delegateOperation(position))
				{
					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
			}

			increment++;

			//	left then forward
			for(int l = 0; l < increment; l++)
			{
				position += Vector3.left * chunkSize;
				if(delegateOperation(position))
				{
					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
			}
			for(int f = 0; f < increment; f++)
			{
				position += Vector3.forward * chunkSize;
				if(delegateOperation(position))
				{
					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
			}

			increment++;
		}
		//	Square made by spiral is always missing one corner
		for(int r = 0; r < increment - 1; r++)
		{
			position += Vector3.right * chunkSize;
			if(delegateOperation(position))
				{
					iterationCount++;
					if(iterationCount >= iterationsPerFrame)
					{
						iterationCount = 0;
						yield return null;
					}
				}
		}
		CoroutineComplete();
	}

	#endregion

	#region Column Generation

	//	Generate terrain and store highest/lowest points
	public bool CreateColumn(Vector3 position, out Column thisColumn)
	{
		Column column;
		if(columns.TryGetValue(position, out column))
		{
			thisColumn = null;
			return false;
		}

		column = new Column(position, this);

		if(column.IsPOI)
		{
			debug.OutlineChunk(new Vector3(position.x, column.topChunkDraw+chunkSize, position.z), Color.red, sizeDivision: 1f);	//	//	//
		}
		else
			debug.OutlineChunk(new Vector3(position.x, 100, position.z), Color.grey, sizeDivision: 3.5f);	//	//	//

		columns[position] = column;
		thisColumn = column;

		return true;
	}

	public void GetColumnCellularData(Column column)
	{
		terrain.GetCellData(column);
	}

	public void GenerateColumnTopology(Column column)
	{
		terrain.GetTopologyData(column);
	}

	//	Determine which chunks in the column should generated and drawn
	bool GetColumnSize(Vector3 position)
	{
		Column column = Column.Get(position);
		if(column.sizeCalculated) return false;

		Vector3[] adjacent = Util.HorizontalChunkNeighbours(position);

		int highestVoxel = column.highestPoint;
		int lowestVoxel = column.lowestPoint;

		//	Set top and bottom chunks to draw
		column.topChunkDraw = Mathf.FloorToInt((highestVoxel + 1) / chunkSize) * chunkSize;
		column.bottomChunkDraw = Mathf.FloorToInt((lowestVoxel - 1) / chunkSize) * chunkSize;

		//column.topChunkDraw += chunkSize;//	DEBUGGING STRUCTURES

		//	Find highest and lowest in 3x3 columns around chunk
		for(int i = 0; i < adjacent.Length; i++)
		{
			Column adjacentColumn = Column.Get(adjacent[i]);	//	DEBUG
			int adjacentHighestVoxel = adjacentColumn.highestPoint;
			int adjacentLowestVoxel = adjacentColumn.lowestPoint;

			if(adjacentHighestVoxel > highestVoxel) highestVoxel = adjacentHighestVoxel;
			if(adjacentLowestVoxel < lowestVoxel) lowestVoxel = adjacentLowestVoxel;		
		}

		//	Set top and bottom chunks to generate
		column.topChunkGenerate = (Mathf.FloorToInt((highestVoxel + 1) / chunkSize) * chunkSize) + chunkSize;
		column.bottomChunkGenerate = (Mathf.FloorToInt((lowestVoxel - 1) / chunkSize) * chunkSize) - chunkSize;

		//debug.OutlineChunk(new Vector3(position.x, column.topChunkGenerate, position.z), Color.black, removePrevious: false, sizeDivision: 2f);
		//debug.OutlineChunk(new Vector3(position.x, column.bottomChunkGenerate, position.z), Color.blue, removePrevious: false, sizeDivision: 2f);

		//debug.OutlineChunk(new Vector3(position.x, column.topChunkDraw, position.z), Color.white, removePrevious: false, sizeDivision: 3f);
		//debug.OutlineChunk(new Vector3(position.x, column.bottomChunkDraw, position.z), Color.red, removePrevious: false, sizeDivision: 3f);

		column.sizeCalculated = true;
		return true;
	}

	#endregion

	#region Chunk Generation

	//	Create column of Chunk class instances
	bool CreateColumnChunks(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];

		//	Skip if already created
		if(topol.spawnStatus != Chunk.Status.NONE) return false;

		//	Iterate over chunks defined by column's top and bottom visible blocks
		for(int y = topol.bottomChunkGenerate; y <= topol.topChunkGenerate; y+=chunkSize)
		{
			CreateChunk(new Vector3(position.x, y, position.z), skipDictCheck: true);
		}

		//	Update column status to created
		topol.spawnStatus = Chunk.Status.CREATED;
		return true;
	}
	void CreateChunk(Vector3 position, bool skipDictCheck = false)
	{
		if(!skipDictCheck && chunks.ContainsKey(position)) return;

		//	Track chunk metrics
		chunksCreated++;
		debug.Output("Chunks created", chunksCreated.ToString());

		//	Create chunk
		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);

		//	Update chunk status to created
		chunk.status = Chunk.Status.CREATED;
	}
	
	//	Generate blocks in column of chunks
	bool GenerateColumnChunks(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if((int)topol.spawnStatus > 1) return false;

		for(int y = topol.bottomChunkGenerate; y <= topol.topChunkGenerate; y+=chunkSize)
		{
			GenerateChunk(new Vector3(position.x, y, position.z));
		}

		topol.GeneratePOIBlocks();

		topol.spawnStatus = Chunk.Status.GENERATED;

		//debug.OutlineChunk(new Vector3(position.x, 100, position.z), Color.white, sizeDivision: 2.5f);	//	//	//

		return true;
	}
	void GenerateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		if(chunk.status == Chunk.Status.GENERATED) return;
		//debug.OutlineChunk(new Vector3(position.x, position.y, position.z), Color.white, sizeDivision: 3.5f);	//	//	//

		chunk.GenerateBlocks();
	}


	//	Smooth terrain in column of chunks
	bool SmoothColumnChunks(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if(topol.spawnStatus != Chunk.Status.GENERATED) return false;

		for(int y = topol.bottomChunkDraw; y <= topol.topChunkDraw; y+=chunkSize)
		{
			SmoothChunk(new Vector3(position.x, y, position.z));
		}
		return true;
	}
	void SmoothChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status != Chunk.Status.GENERATED) return;

		//debug.OutlineChunk(position, Color.cyan, sizeDivision: 4f);

		chunk.SmoothBlocks();
	}
	
	//	Draw chunk meshes in column of chunks
	bool DrawColumnChunks(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if(topol.spawnStatus != Chunk.Status.GENERATED) return false;
		
		for(int y = topol.bottomChunkDraw; y <= topol.topChunkDraw; y+=chunkSize)
		{
			DrawChunk(new Vector3(position.x, y, position.z));
		}
		topol.spawnStatus = Chunk.Status.DRAWN;

		return true;
	}
	void DrawChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.DRAWN) return;
		
		//debug.OutlineChunk(position, Color.red, sizeDivision: 3.5f);

		chunk.Draw();
	}

	#endregion

	#region Update Chunk

	public void RemoveChunk(Vector3 position)
	{
		Chunk chunk;
		if(!chunks.TryGetValue(position, out chunk)) return;
		
		chunk.ClearAllBlocks();
		UpdateChunk(position);
		UpdateAdjacentChunks(position);
	}

	//	Change type of block at voxel and reload chunk(s)
	public bool ChangeBlock(Vector3 voxel, Blocks.Types type, Shapes.Types shape = Shapes.Types.CUBE)
	{
		//	Find owner chunk
		Chunk chunk;
		if(!chunks.TryGetValue(VoxelOwner(voxel), out chunk))
		{
			Debug.Log("can't find chunk at " + VoxelOwner(voxel));
			return false;
		}

		Column columnTopology = columns[new Vector3(chunk.position.x, 0, chunk.position.z)];
		
		//	Check highest/lowest blocks in column
		if(type == Blocks.Types.AIR)
		{
			if(voxel.y < columnTopology.lowestPoint) columnTopology.lowestPoint = (int)voxel.y;
			if(chunk.composition == Chunk.Composition.SOLID) chunk.composition = Chunk.Composition.MIX;
		}
		else
		{
			if(voxel.y > columnTopology.highestPoint) columnTopology.highestPoint = (int)voxel.y;
			if(chunk.composition == Chunk.Composition.EMPTY) chunk.composition = Chunk.Composition.MIX;
		}

		//	Change block type
		Vector3 local = voxel - chunk.position;
		chunk.blockTypes[(int)local.x, (int)local.y, (int)local.z] = type;
		chunk.blockShapes[(int)local.x, (int)local.y, (int)local.z] = shape;

		List<Vector3> adjacent = new List<Vector3>();

		//	If the block is at edge(s) get it's adjacent chunk(s)
		if(local.x == 0) 
			adjacent.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y,			chunk.position.z)));
		if(local.x == chunkSize - 1) 
			adjacent.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y,			chunk.position.z)));
		if(local.y == 0) 
			adjacent.Add((new Vector3(chunk.position.x,			chunk.position.y-chunkSize,	chunk.position.z)));
		if(local.y == chunkSize - 1) 
			adjacent.Add((new Vector3(chunk.position.x,			chunk.position.y+chunkSize,	chunk.position.z)));
		if(local.z == 0) 
			adjacent.Add((new Vector3(chunk.position.x,			chunk.position.y,			chunk.position.z-chunkSize)));
		if(local.z == chunkSize - 1) 
			adjacent.Add((new Vector3(chunk.position.x,			chunk.position.y,			chunk.position.z+chunkSize)));

		if(local.x == 0 && local.z == 0) 
			adjacent.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y, chunk.position.z-chunkSize)));
		if(local.x == chunkSize - 1 && local.z == chunkSize - 1) 
			adjacent.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y, chunk.position.z+chunkSize)));

		if(local.x == 0 && local.z == chunkSize - 1) 
			adjacent.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y, chunk.position.z+chunkSize)));
		if(local.x == chunkSize - 1 && local.z == 0) 
			adjacent.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y, chunk.position.z-chunkSize)));

		//	Update adjacent
		foreach(Vector3 chunkPosition in adjacent)
		{
			Chunk updateChunk = World.chunks[chunkPosition];
			UpdateChunk(chunkPosition);
		}
		//	Update target
		UpdateChunk(chunk.position);

		return true;
	}

	void UpdateAdjacentChunks(Vector3 position)
	{
		Vector3[] adjacent = Util.HorizontalChunkNeighbours(position);
		for(int i = 0; i < adjacent.Length; i++)
		{
			UpdateChunk(adjacent[i]);
		}
		UpdateChunk(position + (Vector3.up * chunkSize));
		UpdateChunk(position + (Vector3.down * chunkSize));
	}

	//	Update/create individual chunks
	void UpdateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		//	Check adjacent chunks on 6 sides
		Vector3[] offsets = Util.CubeFaceDirections();
		for(int i = 0; i < 6; i++)
		{
			Vector3 adjacentChunkPos = position + (offsets[i] * chunkSize);
			//	Create chunk if not already created
			CreateChunk(adjacentChunkPos);

			//	Generate blocks if not already generated
			Chunk adjacentChunk = chunks[adjacentChunkPos];
			if(adjacentChunk.status == Chunk.Status.CREATED)
			{
				adjacentChunk.GenerateBlocks();
			}
		}

		//	Update target chunk
		switch(chunk.status)
		{
			case Chunk.Status.CREATED:
				GenerateChunk(position);
				DrawChunk(position);
				break;

			case Chunk.Status.GENERATED:
				DrawChunk(position);
				break;

			case Chunk.Status.DRAWN:
				chunk.Redraw();
				break;
		}
	}

	#endregion

	#region Utility

	//	Find position of chunk that owns voxel
	public static Vector3 VoxelOwner(Vector3 voxel)
	{
		int x = Mathf.FloorToInt(voxel.x / chunkSize);
		int y = Mathf.FloorToInt(voxel.y / chunkSize);
		int z = Mathf.FloorToInt(voxel.z / chunkSize);
		return new Vector3(x*chunkSize,y*chunkSize,z*chunkSize);
	}

	public static Chunk VoxelOwnerChunk(Vector3 voxel)
	{
		int x = Mathf.FloorToInt(voxel.x / chunkSize);
		int y = Mathf.FloorToInt(voxel.y / chunkSize);
		int z = Mathf.FloorToInt(voxel.z / chunkSize);
		return chunks[new Vector3(x*chunkSize,y*chunkSize,z*chunkSize)];
	}

	#endregion

}

