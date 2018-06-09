using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	//	Size of all chunks
	public static int chunkSize = 8;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 20;
	//	Height of world in chunks
	public static int worldHeight = 2;
	//	All chunks in the world
	public static Dictionary<string, Chunk> chunks = new Dictionary<string, Chunk>();

	//	Generate chunk name string based on position in world space
	public static string ChunkName(Vector3 position)
	{
		return 	(int)position.x + "_" + 
				(int)position.y + "_" + 
			    (int)position.z;
	}

	//		//		//	Temporary for testing generation		//		//		//
																				
	public static int worldSize = 6;
	public Material defaultMaterial;											//

	//	Iterate over all chunk locations in world, generate then draw chunks
	void Start()																//
	{
		//	Create chunks
		for(int x = 0; x < worldSize; x++)										//
			for(int y = 0; y < worldHeight; y++)
				for(int z = 0; z < worldSize; z++)
				{																//
					//	Position of chunk in world space
					Vector3 chunkPosition = new Vector3(x * chunkSize,
														y * chunkSize,
														z * chunkSize);

					//	Create chunk, set parent and position
					Chunk chunk = new Chunk(chunkPosition, this);
					
					//	Store reference to chunk by position/name
					chunks.Add(ChunkName(chunkPosition), chunk);
				}

		//	Create, merge and draw meshes
		for(int x = 0; x < worldSize; x++)
			for(int y = 0; y < worldHeight; y++)
				for(int z = 0; z < worldSize; z++)
				{
					Vector3 chunkPosition = new Vector3(x * chunkSize,			//
														y * chunkSize,
														z * chunkSize);
					chunks[ChunkName(chunkPosition)].DrawBlocks();				//
				}
	}
															//			//		//
}

