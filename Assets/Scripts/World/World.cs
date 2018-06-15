using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	//	Number of chunks that are generated around the player
	public static int viewDistance = 3;
	//	Size of all chunks
	public static int chunkSize = 5;
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

	//	Temporary for testing and optimisation
	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void DrawSurroundingChunks(Vector3 centerChunk)
	{
		//	Generate chunks in view distance + 1
		for(int x = -viewDistance; x < viewDistance+1; x++)
			for(int z = -viewDistance; z < viewDistance+1; z++)
				for(int y = -viewDistance; y < viewDistance+1; y++)
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

