using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class World : MonoBehaviour
{
	//	DEBUG
	public GameObject chunkMarker;
	//	DEBUG

	//	Number of chunks that are generated around the player
	public static int viewDistance = 10;
	//	Size of all chunks
	public static int chunkSize = 16;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 20;
	//	Draw edges where no more chunks exist
	public static bool drawEdges = true;
	//	All chunks in the world
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
	public static Dictionary<Vector3, Topology> topology = new Dictionary<Vector3, Topology>();
																				
	public Material defaultMaterial;

	//	Terrain data
	public class Topology
	{
		public int[,] heightMap =  new int[chunkSize,chunkSize];
		public int highestPoint = 0;
		public int lowestPoint = chunkSize;
	}

	void Start()
	{
		//	Create initial chunks
		LoadChunks(Vector3.zero, viewDistance);
	}

	void GetChunk(Vector3 position, bool generate)
	{
		Chunk chunk;
		if(!chunks.TryGetValue(position, out chunk))
		{
			//CreateChunk(position);
		}
	}

	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void LoadChunks(Vector3 centerChunk, int radius)
	{
		//	Generate terrain in view distance + 1
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = new Vector3(centerChunk.x, 0, centerChunk.z) + offset;

				GetTopology(position);
			}

		//	Create chunks in view distance + 1 and set hidden
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x, 0, z) * chunkSize;
				Vector3 position = centerChunk + offset;

				CreateChunkColumn((int)position.x,
								  (int)position.z);
				
			}

		//	Generate block data in view distance +1
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
				{
					Vector3 offset = new Vector3(x, 0, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					GenerateChunkColumn((int)position.x,
									 	(int)position.z);
				}

		StartCoroutine(DrawChunksInSpiral(centerChunk, viewDistance - 1));
	}

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
		topology[position] = new Topology();
		topology[position].heightMap = map;
		topology[position].highestPoint = highestPoint;
		topology[position].lowestPoint = lowestPoint;

		return true;
	}

	bool CreateChunk(Vector3 position, Topology columnTopology)	//	TODO remove columnTopology?
	{
		if(chunks.ContainsKey(position)) { return false; }

		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);

		return true;
	}

	bool CreateChunkColumn(int x, int z)
	{
		Topology top = topology[new Vector3(x, 0, z)];

		bool aChunkWasCreated = false;
		//	Generate a +1 radius outside the bounds that will be drawn
		for(int y = -chunkSize; y < top.highestPoint + (chunkSize*2); y+=chunkSize)
		{
			bool drawn = CreateChunk(new Vector3(x, y, z), top);
			if(!aChunkWasCreated && drawn) aChunkWasCreated = true;
		}
		return aChunkWasCreated;
	}

	bool GenerateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.GENERATED) return false;
		chunk.GenerateBlocks();
		return true;
	}

	//	TODO make this work
	bool GenerateChunkColumn(int x, int z)
	{
		Topology top = topology[new Vector3(x, 0, z)];

		bool aChunkWasGenerated = false;
		//	Generate a +1 radius outside the bounds that will be drawn
		for(int y = -chunkSize; y < top.highestPoint + (chunkSize*2); y+=chunkSize)
		{
			bool drawn = GenerateChunk(new Vector3(x, y, z));
			if(!aChunkWasGenerated && drawn) aChunkWasGenerated = true;
		}
		return aChunkWasGenerated;
	}

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

		bool aChunkWasDrawn = false;
		for(int y = 0; y < top.highestPoint + chunkSize; y+=chunkSize)
		{
			bool drawn = DrawChunk(new Vector3(position.x, y, position.z));
			if(!aChunkWasDrawn && drawn) aChunkWasDrawn = true;
		}
		return aChunkWasDrawn;
	}

	void UpdateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		//	Make sure
		for(int i = 0; i < 6; i++)
		{
			Vector3 neighbourChunkPos = (Shapes.FaceToDirection((Shapes.CubeFace)i) * chunkSize) + position;

			//	Create chunk if not already created
			if(CreateChunk(neighbourChunkPos, topology[new Vector3(position.x, 0, position.z)])) Debug.Log("created "+chunk);//DEBUG
			//	Generate blocks
			Chunk neighbourChunk = chunks[neighbourChunkPos];
			if(neighbourChunk.status == Chunk.Status.CREATED)
			{
				neighbourChunk.GenerateBlocks();
			}
		}

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


	//	Change type of block at voxel
	public bool ChangeBlock(Vector3 voxel, Blocks.Types type, Shapes.Types shape = Shapes.Types.CUBE)
	{
		//	Find owner chunk
		Chunk chunk;
		if(!chunks.TryGetValue(BlockOwner(voxel), out chunk))
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

		//	add adjacent chunks to be adjacentn if block is at the edge
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

		//	adjacent chunks
		foreach(Vector3 chunkPosition in adjacent)
		{
			Chunk updateChunk = World.chunks[chunkPosition];
			//updateChunk.SmoothBlocks();
			//updateChunk.Redraw();
			UpdateChunk(chunkPosition);
		}
		UpdateChunk(chunk.position);

		return true;
	}

	#region Utility

	//	Find position of chunk that owns block
	public static Vector3 BlockOwner(Vector3 voxel)
	{
		int x = Mathf.FloorToInt(voxel.x / chunkSize);
		int y = Mathf.FloorToInt(voxel.y / chunkSize);
		int z = Mathf.FloorToInt(voxel.z / chunkSize);
		return new Vector3(x*chunkSize,y*chunkSize,z*chunkSize);
	}

	//	Get type of block at voxel
	public static Blocks.Types GetType(Vector3 voxel)
	{
		Chunk chunk = chunks[BlockOwner(voxel)];
		Vector3 local = voxel - chunk.position;
		return chunk.blockTypes[(int)local.x, (int)local.y, (int)local.z];
	}

	//	Get byte representing arrangement of blocks around voxel
	public static byte GetBitMask(Vector3 voxel)
	{
		Vector3[] neighbours = BlockUtils.HorizontalNeighbours(voxel);
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

	IEnumerator DrawChunksInSpiral(Vector3 center, int radius)
	{
		DrawChunk(center);
		Vector3 position = center;
		int increment = 1;
		for(int i = 0; i < radius; i++)
		{
			for(int r = 0; r < increment; r++)
			{
				position += Vector3.right * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}
			for(int d = 0; d < increment; d++)
			{
				position += Vector3.back * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}

			increment++;

			for(int l = 0; l < increment; l++)
			{
				position += Vector3.left * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}
			for(int u = 0; u < increment; u++)
			{
				position += Vector3.forward * chunkSize;
				if(DrawChunkColumn(position)) yield return null;
			}

			increment++;
		}
		for(int u = 0; u < increment - 1; u++)
		{
			position += Vector3.right * chunkSize;
			if(DrawChunkColumn(position)) yield return null;
		}
	}

	#endregion

}

