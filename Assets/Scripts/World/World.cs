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
	public static int viewDistance = 8;
	//	Size of all chunks
	public static int chunkSize = 16;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 50;
	//	Draw edges where no more chunks exist
	public static bool drawEdges = true;

	TerrainGenerator terrain;
	public static Shapes.Meshes shapeMeshes;

	//	Chunk and terrain data
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
	public static Dictionary<Vector3, Column> columns = new Dictionary<Vector3, Column>();

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

	void AddCoroutine(IEnumerator coroutine)
	{
		coroutines.Add(coroutine);
		if(coroutines.Count == 1)
		{
			coroutineRunning = true;
			currentCoroutine = StartCoroutine(coroutines[0]);
		}
	}
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
	}

	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void LoadChunks(Vector3 centerChunk, int radius)
	{
		if(disableChunkGeneration)
		{ Debug.Log("Chunk generation disabled!"); return; }
		if(coroutineRunning) return;

		centerChunk = new Vector3(centerChunk.x, 0, centerChunk.z);

		//	Generate terrain in raduis +2
		/*for(int x = -radius-1; x < radius+2; x++)
			for(int z = -radius-1; z < radius+2; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = centerChunk + offset;

				GenerateColumnData(position);
			}*/

		//	Get column size in radius +1
		/*for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = centerChunk + offset;

				GetColumnSize(position);
			}*/

		//	Create chunks in radius + 1
		/*for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;

				CreateColumn((int)position.x,
						  	 (int)position.z);
			}*/

		/*//	Generate block data in radius +1
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;
				
				GenerateColumn(	(int)position.x,
								(int)position.z);
			}*/

		/*for(int x = -radius + 1; x < radius; x++)
			for(int z = -radius + 1; z < radius; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;

				SmoothColumn(	(int)position.x,
								(int)position.z);
			}*/

		//	Draw chunks spiralling out from player in radius
		//if(chunkDrawCoroutine != null) StopCoroutine(chunkDrawCoroutine);
		//chunkDrawCoroutine = StartCoroutine(ChunksInSpiral(centerChunk, radius, DrawColumn));

		//chunkDrawCoroutine = StartCoroutine(DrawChunksInSpiral(centerChunk, radius));

		AddCoroutine(ChunksInSquare(centerChunk, radius+1, GenerateColumnData, 20));

		AddCoroutine(ChunksInSquare(centerChunk, radius, GetColumnSize, 20));

		AddCoroutine(ChunksInSquare(centerChunk, radius, CreateColumn, 20));

		AddCoroutine(ChunksInSquare(centerChunk, radius, GenerateColumn, 20));

		AddCoroutine(ChunksInSquare(centerChunk, radius-1, SmoothColumn, 20));

		AddCoroutine(ChunksInSpiral(centerChunk, radius-1, DrawColumn, 2));
	}

	#endregion

	#region Topology

	//	Generate terrain and store highest/lowest points
	bool GenerateColumnData(Vector3 position)
	{
		Column column;
		if(columns.TryGetValue(position, out column)) return false;

		column = new Column(position, terrain, this);
		columns[position] = column;

		return true;
	}

	//	Determine which chunks should generated and drawn
	bool GetColumnSize(Vector3 position)
	{
		Column column = Column.Get(position);
		if(column.sizeCalculated) return false;

		Vector3[] adjacent = Util.HorizontalChunkNeighbours(position, chunkSize);

		int highestVoxel = column.highestPoint;
		int lowestVoxel = column.lowestPoint;

		//	Set top and bottom chunks to draw
		column.topChunkDraw = Mathf.FloorToInt((highestVoxel + 1) / chunkSize) * chunkSize;
		column.bottomChunkDraw = Mathf.FloorToInt((lowestVoxel - 1) / chunkSize) * chunkSize;

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

		//debug.OutlineChunk(new Vector3(position.x, column.topChunkDraw, position.z), Color.red, removePrevious: false, sizeDivision: 3f);
		//debug.OutlineChunk(new Vector3(position.x, column.bottomChunkDraw, position.z), Color.red, removePrevious: false, sizeDivision: 3f);

		column.sizeCalculated = true;
		return true;
	}

	#endregion

	#region Chunk Generation


	//	Create column of Chunk class instances
	bool CreateColumn(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];

		//	Skip if already created
		if(topol.spawnStatus != Chunk.Status.NONE) return false;

		//	Create a column of Chunk class instances covering visible terrain + 1
		bool aChunkWasCreated = false;
		for(int y = topol.bottomChunkGenerate; y <= topol.topChunkGenerate; y+=chunkSize)
		{
			bool drawn = CreateChunk(new Vector3(position.x, y, position.z), skipDictCheck: true);
			if(!aChunkWasCreated && drawn) aChunkWasCreated = true;
		}
		topol.spawnStatus = Chunk.Status.CREATED;
		return aChunkWasCreated;
	}
	public bool CreateChunk(Vector3 position, bool skipDictCheck = false)
	{
		if(!skipDictCheck && chunks.ContainsKey(position)) return false;

		chunksCreated++;
		debug.Output("Chunks created", chunksCreated.ToString());

		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);
		chunk.status = Chunk.Status.CREATED;

		return true;
	}
	
	//	Generate blocks in chunk
	public bool GenerateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		if(chunk.status == Chunk.Status.GENERATED) return false;

		debug.OutlineChunk(position, Color.white, sizeDivision: 2.5f);

		chunk.GenerateBlocks();
		return true;
	}
	bool GenerateColumn(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if((int)topol.spawnStatus > 1) return false;

		//	Generate blocks in chunks covering visible terrain + 1
		bool aChunkWasGenerated = false;
		for(int y = topol.bottomChunkGenerate; y <= topol.topChunkGenerate; y+=chunkSize)
		{
			bool drawn = GenerateChunk(new Vector3(position.x, y, position.z));
			if(!aChunkWasGenerated && drawn) aChunkWasGenerated = true;
		}
		topol.spawnStatus = Chunk.Status.GENERATED;
		return aChunkWasGenerated;
	}

	//	Smooth terrain
	bool SmoothChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status != Chunk.Status.GENERATED) { return false; }

		debug.OutlineChunk(position, Color.cyan, sizeDivision: 4f);

		chunk.SmoothBlocks();
		return true;
	}
	bool SmoothColumn(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if(topol.spawnStatus != Chunk.Status.GENERATED) return false;

		for(int y = topol.bottomChunkDraw; y <= topol.topChunkDraw; y+=chunkSize)
		{
			SmoothChunk(new Vector3(position.x, y, position.z));
		}
		return true;
	}

	//	Draw chunk meshes
	bool DrawChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.DRAWN) { return false; }
		
		debug.OutlineChunk(position, Color.red, sizeDivision: 3.5f);

		chunk.Draw();
		return true;
	}
	bool DrawColumn(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if(topol.spawnStatus != Chunk.Status.GENERATED) return false;
		
		bool aChunkWasDrawn = false;
		for(int y = topol.bottomChunkDraw; y <= topol.topChunkDraw; y+=chunkSize)
		{
			bool drawn = DrawChunk(new Vector3(position.x, y, position.z));
			if(!aChunkWasDrawn && drawn) aChunkWasDrawn = true;
		}
		topol.spawnStatus = Chunk.Status.DRAWN;
		return aChunkWasDrawn;
	}

	delegate bool ChunkOperation(Vector3 position);

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

	//	Make a horizontal grid of chunks moving in a spiral out from the center
	/*IEnumerator DrawChunksInSpiral(Vector3 center, int radius)
	{
		Vector3 position = center;

		DrawColumn(position);
		int increment = 1;
		for(int i = 0; i < radius; i++)
		{
			//	right then back
			for(int r = 0; r < increment; r++)
			{
				position += Vector3.right * chunkSize;
				if(DrawColumn(position)) yield return null;
			}
			for(int b = 0; b < increment; b++)
			{
				position += Vector3.back * chunkSize;
				if(DrawColumn(position)) yield return null;
			}

			increment++;

			//	left then forward
			for(int l = 0; l < increment; l++)
			{
				position += Vector3.left * chunkSize;
				if(DrawColumn(position)) yield return null;
			}
			for(int f = 0; f < increment; f++)
			{
				position += Vector3.forward * chunkSize;
				if(DrawColumn(position)) yield return null;
			}

			increment++;
		}
		//	Square made by spiral is always missing one corner
		for(int r = 0; r < increment - 1; r++)
		{
			position += Vector3.right * chunkSize;
			if(DrawColumn(position)) yield return null;
		}
	}*/

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
		Vector3[] adjacent = Util.HorizontalChunkNeighbours(position, chunkSize);
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

