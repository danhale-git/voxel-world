using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class World : MonoBehaviour
{
	//	DEBUG
	public GameObject chunkMarkerWhite;
	public GameObject chunkMarkerRed;
	public GameObject chunkMarkerYellow;
	public bool markChunks;
	public Material defaultMaterial;
	public bool disableChunkGeneration = false;
	DebugWrapper debug;
	//	DEBUG

	//	Number of chunks that are generated around the player
	public static int viewDistance = 8;
	//	Size of all chunks
	public static int chunkSize = 16;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 60;
	//	Draw edges where no more chunks exist
	public static bool drawEdges = true;

	//	Chunk and terrain data
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
	public static Dictionary<Vector3, Topology> topology = new Dictionary<Vector3, Topology>();

	//	Terrain data
	public class Topology
	{
		public bool columnSizeCalculated = false;

		public static Topology Get(Vector3 position, World DEBUGworld)
		{
			Topology top;
			if(!topology.TryGetValue(new Vector3(position.x, 0, position.z), out top))
			{
				for(int i = 0; i < 10; i++)
				{
					Instantiate(DEBUGworld.chunkMarkerRed, new Vector3(position.x, i * World.chunkSize, position.y), Quaternion.identity);
					DEBUGworld.disableChunkGeneration = true;
					DEBUGworld.StopAllCoroutines();
				}
				Debug.Log(position);
				return null;
			}
			return top;
			
			
			//return topology[new Vector3(position.x, 0, position.y)];
		}
		public int[,] heightMap =  new int[chunkSize,chunkSize];

		public int highestPoint = 0;
		public int highestPointOnGenerate;
		public int topChunkInColumn;

		public int lowestPoint = chunkSize;
		public int lowestPointOnGenerate;
		public int bottomChunkInColumn;

		public Chunk.Status spawnStatus;
	}

	void Start()
	{
		debug = gameObject.AddComponent<DebugWrapper>();

		//	Create initial chunks
		LoadChunks(Vector3.zero, viewDistance);
	}

	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void LoadChunks(Vector3 centerChunk, int radius)
	{
		if(disableChunkGeneration) return;
		centerChunk = new Vector3(centerChunk.x, 0, centerChunk.z);

		//	Generate terrain in view distance + 1
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = centerChunk + offset;

				GetTopology(position);
			}

		for(int x = -radius; x < radius; x++)
			for(int z = -radius; z < radius; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = centerChunk + offset;

				GetColumnSize(position);
			}

		//	Create chunks in view distance + 1
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;

					CreateChunkColumn((int)position.x,
								  	  (int)position.z);
				
			}

		//	Generate block data in view distance +1
		for(int x = -radius; x < radius+1; x++)
			for(int z = -radius; z < radius+1; z++)
				{
					Vector3 offset = new Vector3(x, 0, z) * chunkSize;
					Vector3 position = centerChunk + offset;

					GenerateChunkColumn((int)position.x,
									 	(int)position.z);
				}

		StartCoroutine(DrawChunksInSpiral(centerChunk, radius - 1));
	}


	//	Generate terrain and store highest/lowest points
	bool GetTopology(Vector3 position)
	{
		if(topology.ContainsKey(position)) return false;
		//	initalise lowest as high
		int lowestPoint = 10000;
		//	initialise heighest low
		int highestPoint  = 0;

		int[,] map = new int[chunkSize,chunkSize];
		for(int _x = 0; _x < chunkSize; _x++)
			for(int _z = 0; _z < chunkSize; _z++)
			{
				map[_x,_z] = NoiseUtils.GroundHeight(	_x + (int)position.x,
														_z + (int)position.z,
														World.maxGroundHeight);
				//	Lower than lowest
				if(map[_x,_z] < lowestPoint)
					lowestPoint = map[_x,_z];
				//	Higher than highest
				if(map[_x,_z] > highestPoint)
					highestPoint = map[_x,_z];
			}
		Topology top = new Topology();
		top.heightMap = map;
		top.highestPoint = highestPoint;
		top.lowestPoint = lowestPoint;

		top.highestPointOnGenerate = highestPoint;
		top.lowestPointOnGenerate = lowestPoint;

		topology[position] = top;

		return true;
	}

	//	Calculate minimum set of chunks
	//	needed for a column's and it's
	//	neighbouring column's blocks
	void GetColumnSize(Vector3 position)
	{
		Topology top = Topology.Get(position, this);	//	DEBUG
		if(top.columnSizeCalculated) return;


		Vector3[] adjacent = Util.HorizontalChunkNeighbours(position, chunkSize);

		int highestVoxel = top.highestPoint;
		int lowestVoxel = top.lowestPoint;

		for(int i = 0; i < adjacent.Length; i++)
		{
			Topology adjacentTopology = Topology.Get(adjacent[i], this);	//	DEBUG
			int adjacentHighestVoxel = adjacentTopology.highestPoint;
			int adjacentLowestVoxel = adjacentTopology.highestPoint;

			if(adjacentHighestVoxel > highestVoxel) highestVoxel = adjacentHighestVoxel;
			if(adjacentLowestVoxel > lowestVoxel) lowestVoxel = adjacentLowestVoxel;
		}

		//	Get owner chunk
		top.topChunkInColumn = Mathf.FloorToInt((highestVoxel + 1) / chunkSize);
		top.bottomChunkInColumn = Mathf.FloorToInt((lowestVoxel - 1) / chunkSize);

		debug.OutlineChunk(new Vector3(position.x, top.topChunkInColumn * chunkSize, position.z), Color.grey, false);
		//debug.OutlineChunk(new Vector3(position.x, top.bottomChunkInColumn * chunkSize, position.z), Color.white, false);

		top.columnSizeCalculated = true;
	}


	//	Create chunk class instances
	public bool CreateChunk(Vector3 position)
	{
		if(chunks.ContainsKey(position)) return false;

		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);

		return true;
	}
	bool CreateChunkColumn(int x, int z)
	{
		Topology topol = topology[new Vector3(x, 0, z)];
		if((int)topol.spawnStatus > 0) return false;

		bool aChunkWasCreated = false;
		//	Create a +1 radius outside the bounds that will be drawn
		for(int y = -chunkSize; y < topol.highestPointOnGenerate + (chunkSize*2); y+=chunkSize)
		{
			bool drawn = CreateChunk(new Vector3(x, y, z));
			if(!aChunkWasCreated && drawn) aChunkWasCreated = true;
		}
		topol.spawnStatus = Chunk.Status.CREATED;
		return aChunkWasCreated;
	}


	//	Generate blocks in chunks
	public bool GenerateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.GENERATED) return false;
		chunk.GenerateBlocks();
		return true;
	}
	bool GenerateChunkColumn(int x, int z)
	{
		Topology top = topology[new Vector3(x, 0, z)];
		if((int)top.spawnStatus > 1) return false;

		bool aChunkWasGenerated = false;
		//	Generate a +1 radius outside the bounds that will be drawn
		for(int y = -chunkSize; y < top.highestPointOnGenerate + (chunkSize*2); y+=chunkSize)
		{
			bool drawn = GenerateChunk(new Vector3(x, y, z));
			if(!aChunkWasGenerated && drawn) aChunkWasGenerated = true;
		}
		top.spawnStatus = Chunk.Status.GENERATED;
		return aChunkWasGenerated;
	}


	//	Draw chunk meshes
	bool DrawChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.DRAWN) { return false; }
		chunk.Draw();
		return true;
	}
	bool DrawChunkColumn(Vector3 position)
	{
		Topology top = topology[new Vector3(position.x, 0, position.z)];
		if((int)top.spawnStatus > 2) return false;

		bool aChunkWasDrawn = false;
		for(int y = 0; y < top.highestPointOnGenerate + chunkSize; y+=chunkSize)
		{
			bool drawn = DrawChunk(new Vector3(position.x, y, position.z));
			if(!aChunkWasDrawn && drawn) aChunkWasDrawn = true;
		}
		top.spawnStatus = Chunk.Status.DRAWN;
		return aChunkWasDrawn;
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


	//	Change type of block at voxel and reload chunk(s)
	public bool ChangeBlock(Vector3 voxel, Blocks.Types type, Shapes.Types shape = Shapes.Types.CUBE)
	{
		//	Find owner chunk
		Chunk chunk;
		if(!chunks.TryGetValue(VoxelOwner(voxel), out chunk))
		{
			return false;
		}

		Topology columnTopology = topology[new Vector3(chunk.position.x, 0, chunk.position.z)];
		
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

	//	Make a horizontal grid spiral of chunks
	IEnumerator DrawChunksInSpiral(Vector3 center, int radius)
	{
		DrawChunkColumn(center);
		Vector3 position = center;
		int increment = 1;
		for(int i = 0; i < radius; i++)
		{
			//	right then back
			for(int r = 0; r < increment; r++)
			{
				position += Vector3.right * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}
			for(int b = 0; b < increment; b++)
			{
				position += Vector3.back * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}

			increment++;

			//	left then forward
			for(int l = 0; l < increment; l++)
			{
				position += Vector3.left * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}
			for(int f = 0; f < increment; f++)
			{
				position += Vector3.forward * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}

			increment++;
		}
		//	Square made by spiral is always missing one chunk
		for(int r = 0; r < increment - 1; r++)
		{
			position += Vector3.right * chunkSize;
			if(DrawChunkColumn(position)) yield return null;
		}
	}

	#endregion

}

