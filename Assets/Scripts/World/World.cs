using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	//	Number of chunks that are generated around the player
	public static int viewDistance = 4;
	//	Size of all chunks
	public static int chunkSize = 3;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 20;
	//	Height of world in chunks
	public static int worldHeight = 2;
	//	All chunks in the world
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
																				
	public static int worldSize = 2;
	public Material defaultMaterial;

	void Start()
	{
		//	Create initial chunks
		GenerateChunk(Vector3.zero);
		DrawChunk(Vector3.zero);
		DrawSurroundingChunks(Vector3.zero);
	}

	//	Change type of block at voxel
	public static bool ChangeBlock(Vector3 voxel, BlockUtils.Types type)
	{
		//	Find owner chunk
		Chunk chunk;
		if(!chunks.TryGetValue(BlockOwner(voxel), out chunk))
		{
			return false;
		}

		//	Change block type
		Vector3 local = voxel - chunk.position;
		chunk.blockTypes[(int)local.x, (int)local.y, (int)local.z] = type;

		List<Vector3> redraw = new List<Vector3>() { chunk.position };

		//	add adjacent chunks to be redrawn if block is at the edge
		if(local.x == 0) 
			redraw.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y,					chunk.position.z)));
		if(local.x == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y,					chunk.position.z)));
		if(local.y == 0) 
			redraw.Add((new Vector3(chunk.position.x,					chunk.position.y-chunkSize,	chunk.position.z)));
		if(local.y == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x,					chunk.position.y+chunkSize,	chunk.position.z)));
		if(local.z == 0) 
			redraw.Add((new Vector3(chunk.position.x,					chunk.position.y,					chunk.position.z-chunkSize)));
		if(local.z == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x,					chunk.position.y,					chunk.position.z+chunkSize)));

		//	redraw chunks
		foreach(Vector3 chunkPosition in redraw)
		{
			World.chunks[chunkPosition].Redraw();
		}
		return true;
	}

	//	Find position of chunk that owns block
	public static Vector3 BlockOwner(Vector3 voxel)
	{
		int x = Mathf.FloorToInt(voxel.x / chunkSize);
		int y = Mathf.FloorToInt(voxel.y / chunkSize);
		int z = Mathf.FloorToInt(voxel.z / chunkSize);
		return new Vector3(x*chunkSize,y*chunkSize,z*chunkSize);
	}

	//	Get type of block at voxel
	public static BlockUtils.Types GetType(Vector3 voxel)
	{
		Chunk chunk;
		if(!chunks.TryGetValue(BlockOwner(voxel), out chunk))
		{
			return 0;
		}
		Vector3 local = voxel - chunk.position;
		return chunk.blockTypes[(int)local.x, (int)local.y, (int)local.z];
	}

	public static byte GetBitMask(Vector3 voxel)
	{
		Vector3[] neighbours = BlockUtils.HorizontalNeighbours(voxel);
			int value = 1;
			int total = 0;
			for(int i = 0; i < neighbours.Length; i++)
			{
				if(BlockUtils.seeThrough[(int)GetType(neighbours[i])])
				{
					total += value;
				}
				value *= 2;
			}
		return (byte)total;
	}

	//	Temporary for testing and optimisation
	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void DrawSurroundingChunks(Vector3 centerChunk)
	{
		//	Generate chunks in view distance + 1
		for(int x = -viewDistance-1; x < viewDistance+1; x++)
			for(int z = -viewDistance-1; z < viewDistance+1; z++)
				for(int y = -viewDistance-1; y < viewDistance+1; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 location = centerChunk + offset;
					GenerateChunk(location);
				}

		//	Graw chunks in view distance
		for(int x = -viewDistance; x < viewDistance; x++)
			for(int z = -viewDistance; z < viewDistance; z++)
				for(int y = -viewDistance; y < viewDistance; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 location = centerChunk + offset;
					DrawChunk(location);
				}
	}

	//	Generate chunk at position in world
	void GenerateChunk(Vector3 position)
	{
		if(chunks.ContainsKey(position)) { return; }
		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);
		chunk.status = Chunk.Status.GENERATED;		
	}

	//	Draw chunk at position key in dictionary
	void DrawChunk(Vector3 position)
	{		
		Chunk chunk = chunks[position];
		if(chunk.status == Chunk.Status.DRAWN) { return; }
		chunk.DrawBlocks();
		chunk.status = Chunk.Status.DRAWN;
	}
}

