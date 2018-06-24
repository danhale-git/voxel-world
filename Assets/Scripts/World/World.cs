using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	//	DEBUG
	public GameObject chunkMarker;
	//	DEBUG

	//	Number of chunks that are generated around the player
	public static int viewDistance = 5;
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

	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void LoadChunks(Vector3 centerChunk, int radius)
	{
		//	DEBUG
		//Debug.Log("Generating "+Mathf.Pow((radius*2+1), 3)+" chunks: "+(radius*2+1)+"x"+(radius*2+1)+"x"+(radius*2+1));
		double epoch = Util.EpochMilliseconds();
		//	DEBUG

		//	Generate terrain in view distance + 1
		int topologyCount = 0;
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = new Vector3(centerChunk.x, 0, centerChunk.z) + offset;
				
				if(GetTopology(position)) topologyCount++;
			}

		//	DEBUG
		double currentEpoch = Util.EpochMilliseconds();
		//Debug.Log(topologyCount+" columns topology generated in "+Mathf.Round((float)(currentEpoch - epoch))+" milliseconds");
		//	DEBUG

		//	Create chunks in view distance + 1 and set hidden
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Topology columnTopology = topology[new Vector3(x*chunkSize,0,z*chunkSize)];

				for(int y = -radius-1; y < radius+1; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					
					CreateChunk(position, columnTopology);
				}
			}

		//	Generate block data in view distance +1
		int chunkCount = 0;
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
				for(int y = -radius-1; y < radius+1; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					
					if(GenerateChunk(position)) chunkCount++;
				}

		//	DEBUG
		currentEpoch = Util.EpochMilliseconds();
		Debug.Log(chunkCount+" chunks GEN - "+Mathf.Round((float)(currentEpoch - epoch))+" ms");
		//	DEBUG

		//	Draw chunks in view distance
		int drawnChunkCount = 0;
		for(int x = -radius; x < radius; x++)
			for(int z = -radius; z < radius; z++)
				for(int y = -radius; y < radius; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					
					if(DrawChunk(position)) drawnChunkCount++;
				}
		
		//	DEBUG
		currentEpoch = Util.EpochMilliseconds();
		Debug.Log(drawnChunkCount+" chunks DRAWN - "+Mathf.Round((float)(currentEpoch - epoch))+" ms");
		//	DEBUG
	}

	void UpdateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];

		if(chunk.hidden) chunk.hidden = false;

		for(int i = 0; i < 6; i++)
		{
			Vector3 neighbourChunkPos = (Shapes.FaceToDirection((Shapes.CubeFace)i) * chunkSize) + position;
			Chunk neighbourChunk = chunks[neighbourChunkPos];
			if(neighbourChunk.hidden)
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

	bool CreateChunk(Vector3 position, Topology columnTopology)
	{
		if(chunks.ContainsKey(position)) { return false; }

		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);

		if( chunk.position.y > columnTopology.highestPoint + (chunkSize * 2) ||
			chunk.position.y < columnTopology.lowestPoint - (chunkSize * 2))
		{
			chunk.hidden = true;
			
		}
		return true;
	}

	bool GenerateChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.GENERATED || chunk.hidden) return false;
		chunk.GenerateBlocks();
		return true;
	}

	bool DrawChunk(Vector3 position)
	{
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.DRAWN || chunk.hidden) { return false; }
		chunk.Draw();
		return true;
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

	#endregion

}

