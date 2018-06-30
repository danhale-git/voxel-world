using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class World : MonoBehaviour
{
	//	DEBUG
	public static bool markChunks = true;
	public Material defaultMaterial;
	public bool disableChunkGeneration = false;
	DebugWrapper debug;
	//	DEBUG

	//	Number of chunks that are generated around the player
	public static int viewDistance = 10;
	//	Size of all chunks
	public static int chunkSize = 16;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 50;
	//	Draw edges where no more chunks exist
	public static bool drawEdges = true;

	//	Chunk and terrain data
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
	public static Dictionary<Vector3, Column> columns = new Dictionary<Vector3, Column>();

	Coroutine chunkDrawCoroutine;

	void Start()
	{
		debug = gameObject.AddComponent<DebugWrapper>();

		//	Create initial chunks
		LoadChunks(Vector3.zero, viewDistance);
	}

	#region World Generation

	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void LoadChunks(Vector3 centerChunk, int radius)
	{
		if(disableChunkGeneration)
		{ Debug.Log("Chunk generation disabled!"); return; }

		//	Prevent multiple instances of chunk drawing coroutines
		centerChunk = new Vector3(centerChunk.x, 0, centerChunk.z);

		//	Generate terrain in raduis +2
		for(int x = -radius-1; x < radius+2; x++)
			for(int z = -radius-1; z < radius+2; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = centerChunk + offset;

				GenerateColumnData(position);
			}

		//	Get column size in radius +1
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = centerChunk + offset;

				GetColumnSize(position);
			}

		//	Create chunks in radius + 1
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;

				CreateColumn((int)position.x,
						  	 (int)position.z);
			}

		//	Generate block data in radius +1
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;
				
				GenerateColumn((int)position.x,
								(int)position.z);
			}

		//	Draw chunk spiralling out from player in radius
		if(chunkDrawCoroutine != null) StopCoroutine(chunkDrawCoroutine);
		chunkDrawCoroutine = StartCoroutine(DrawChunksInSpiral(centerChunk, radius));
	}

	#endregion

	#region Topology

	//	Terrain data
	public class Column
	{
		public Vector3 position;
		public Chunk.Status spawnStatus;
		public bool sizeCalculated = false;
		public int[,] heightMap =  new int[chunkSize,chunkSize];

		public int highestPoint;
		public int topChunkGenerate;
		public int topChunkDraw;	

		public int lowestPoint;
		public int bottomChunkGenerate;
		public int bottomChunkDraw;
		
		public Column(Vector3 _position, int[,] _heightMap, int _highestPoint, int _lowestPoint)
		{
			position = _position;
			heightMap = _heightMap;
			highestPoint = _highestPoint;
			lowestPoint = _lowestPoint;
		}

		public static Column Get(Vector3 position, World DEBUGworld)
		{
			Column column = columns[new Vector3(position.x, 0, position.z)];
			return column;
		}
	}

	//	Generate terrain and store highest/lowest points
	bool GenerateColumnData(Vector3 position)
	{
		Column column;
		if(columns.TryGetValue(position, out column)) return false;

		//	initalise lowest as high
		int lowest = 10000;
		//	initialise heighest low
		int highest  = 0;

		int[,] map = new int[chunkSize,chunkSize];
		for(int _x = 0; _x < chunkSize; _x++)
			for(int _z = 0; _z < chunkSize; _z++)
			{
				map[_x,_z] = NoiseUtils.GroundHeight(_x + (int)position.x,
													 _z + (int)position.z,
													 World.maxGroundHeight);
				//	Lowest point
				if(map[_x,_z] < lowest)
					lowest = map[_x,_z];

				//	Highest point
				if(map[_x,_z] > highest)
					highest = map[_x,_z];
			}

		column = new Column(position, map, highest, lowest);
		columns[position] = column;

		return true;
	}

	//	Determine which chunks should generated and drawn
	void GetColumnSize(Vector3 position)
	{
		Column column = Column.Get(position, this);
		if(column.sizeCalculated) return;

		Vector3[] adjacent = Util.HorizontalChunkNeighbours(position, chunkSize);

		int highestVoxel = column.highestPoint;
		int lowestVoxel = column.lowestPoint;

		//	Set top and bottom chunks to draw
		column.topChunkDraw = Mathf.FloorToInt((highestVoxel + 1) / chunkSize) * chunkSize;
		column.bottomChunkDraw = Mathf.FloorToInt((lowestVoxel - 1) / chunkSize) * chunkSize;

		//	Find highest and lowest in 3x3 columns around chunk
		for(int i = 0; i < adjacent.Length; i++)
		{
			Column adjacentColumn = Column.Get(adjacent[i], this);	//	DEBUG
			int adjacentHighestVoxel = adjacentColumn.highestPoint;
			int adjacentLowestVoxel = adjacentColumn.lowestPoint;

			if(adjacentHighestVoxel > highestVoxel) highestVoxel = adjacentHighestVoxel;
			if(adjacentLowestVoxel < lowestVoxel) lowestVoxel = adjacentLowestVoxel;		
		}

		//	Set top and bottom chunks to generate
		column.topChunkGenerate = Mathf.FloorToInt((highestVoxel + 1) / chunkSize) * chunkSize;
		column.bottomChunkGenerate = Mathf.FloorToInt((lowestVoxel - 1) / chunkSize) * chunkSize;
		
		//debug.OutlineChunk(new Vector3(position.x, column.topChunk, position.z), Color.black, removePrevious: false, sizeDivision: 2.5f);
		//debug.OutlineChunk(new Vector3(position.x, column.bottomChunk, position.z), Color.blue, removePrevious: false, sizeDivision: 2.5f);

		column.sizeCalculated = true;
	}

	#endregion

	#region Chunk Generation


	//	Create Chunk class instance
	public bool CreateChunk(Vector3 position, bool skipDictCheck = false)
	{
		if(!skipDictCheck && chunks.ContainsKey(position)) return false;

		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);
		chunk.status = Chunk.Status.CREATED;

		return true;
	}
	bool CreateColumn(int x, int z)
	{
		Column topol = columns[new Vector3(x, 0, z)];
		if(topol.spawnStatus != Chunk.Status.NONE) return false;

		//	Create a column of Chunk class instances covering visible terrain + 1
		bool aChunkWasCreated = false;
		for(int y = topol.bottomChunkGenerate - chunkSize; y <= topol.topChunkGenerate + chunkSize; y+=chunkSize)
		{
			bool drawn = CreateChunk(new Vector3(x, y, z), skipDictCheck: true);
			if(!aChunkWasCreated && drawn) aChunkWasCreated = true;
		}
		topol.spawnStatus = Chunk.Status.CREATED;
		return aChunkWasCreated;
	}

	//	Generate blocks in chunk
	public bool GenerateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		if(chunk.status == Chunk.Status.GENERATED) return false;
		chunk.GenerateBlocks();
		return true;
	}
	bool GenerateColumn(int x, int z)
	{
		Column topol = columns[new Vector3(x, 0, z)];
		if((int)topol.spawnStatus > 1) return false;

		//	Generate blocks in chunks covering visible terrain + 1
		bool aChunkWasGenerated = false;
		for(int y = topol.bottomChunkGenerate - chunkSize; y <= topol.topChunkGenerate + chunkSize; y+=chunkSize)
		{
			bool drawn = GenerateChunk(new Vector3(x, y, z));
			if(!aChunkWasGenerated && drawn) aChunkWasGenerated = true;
		}
		topol.spawnStatus = Chunk.Status.GENERATED;
		return aChunkWasGenerated;
	}


	//	Draw chunk meshe
	bool DrawChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.DRAWN) { return false; }
		chunk.Draw();
		return true;
	}
	bool DrawColumn(Vector3 position)
	{
		Column topol = columns[new Vector3(position.x, 0, position.z)];
		if(topol.spawnStatus != Chunk.Status.GENERATED) return false;

		//debug.OutlineChunk(new Vector3(position.x, 100, position.z), Color.white, removePrevious: false, sizeDivision: 3.5f);

		//	Draw chunk meshes covering visible chunks
		bool aChunkWasDrawn = false;
		for(int y = topol.bottomChunkDraw; y <= topol.topChunkDraw; y+=chunkSize)
		{
			bool drawn = DrawChunk(new Vector3(position.x, y, position.z));
			if(!aChunkWasDrawn && drawn) aChunkWasDrawn = true;
		}
		topol.spawnStatus = Chunk.Status.DRAWN;
		return aChunkWasDrawn;
	}


	//	Make a horizontal grid of chunks moving in a spiral out from the center
	IEnumerator DrawChunksInSpiral(Vector3 center, int radius)
	{
		Vector3 position = center;
		//	Trim radius to allow buffer of generated chunks
		radius = radius - 2;
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
	}

	#endregion

	#region Update Chunk

	//	Change type of block at voxel and reload chunk(s)
	public bool ChangeBlock(Vector3 voxel, Blocks.Types type, Shapes.Types shape = Shapes.Types.CUBE)
	{
		//	Find owner chunk
		Chunk chunk;
		if(!chunks.TryGetValue(VoxelOwner(voxel), out chunk))
		{
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

	//	Update/create individual chunks
	void UpdateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		debug.OutlineChunk(position, Color.green, removePrevious: false, sizeDivision: 3);

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

		Debug.Log(chunk.status);

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
	//	Get type of block at voxel
	public static Blocks.Types GetType(Vector3 voxel)
	{
		Chunk chunk = chunks[VoxelOwner(voxel)];
		Vector3 local = voxel - chunk.position;
		return chunk.blockTypes[(int)local.x, (int)local.y, (int)local.z];
	}
	//	Get byte representing arrangement of solid blocks around voxel
	public static byte GetBitMask(Vector3 voxel)
	{
		Vector3[] neighbours = Util.HorizontalBlockNeighbours(voxel);
			int value = 1;
			int total = 0;
			for(int i = 0; i < neighbours.Length; i++)
			{
				if(Blocks.seeThrough[(int)GetType(neighbours[i])])
				{
					total += value;
				}
				value *= 2;
			}
		return (byte)total;
	}

	#endregion

}

