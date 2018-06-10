using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	public static int viewDistance = 2;
	//	Size of all chunks
	public static int chunkSize = 16;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 20;
	//	Height of world in chunks
	public static int worldHeight = 2;
	//	All chunks in the world
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
																				
	public static int worldSize = 2;
	public Material defaultMaterial;

	//	Iterate over all chunk locations in world, generate then draw chunks
	void Start()
	{
		//	Create initial chunks
		GenerateChunk(Vector3.zero);
		DrawChunk(Vector3.zero);
		DrawSurroundingChunks(Vector3.zero);
	}

	//	Generate and draw chunks in a cube radius of veiwDistance around player
	public void DrawSurroundingChunks(Vector3 centerChunk)
	{
		//	List the names of chunks within range
		Dictionary<Vector3, Vector3> chunksInRange = new Dictionary<Vector3, Vector3>();
		for(int x = -viewDistance; x < viewDistance; x++)
			for(int z = -viewDistance; z < viewDistance; z++)
				for(int y = -viewDistance; y < viewDistance; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 location = centerChunk + offset;
					chunksInRange.Add(location, location);
				}

		//	Check chunk status, draw or generate accordingly
		foreach(Vector3 chunkName in chunksInRange.Keys)
		{
			Chunk chunk;
			if(!chunks.TryGetValue(chunkName, out chunk))
			{
				//	Generate then draw
				GenerateChunk(chunksInRange[chunkName]);
				DrawChunk(chunkName);
			}
		}
	}

	void GenerateChunk(Vector3 position)
	{
		Chunk chunk = new Chunk(position, this);
		chunks.Add(position, chunk);
	}

	void DrawChunk(Vector3 name)
	{
		Chunk chunk = chunks[name];
		chunk.DrawBlocks();
	}
}

