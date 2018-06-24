using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
	//	DEBUG
	public GameObject chunkMarker;
	//	DEBUG

	//	Number of chunks that are generated around the player
	public static int viewDistance = 6;
	//	Size of all chunks
	public static int chunkSize = 8;
	//	Maximum height of non-air blocks
	public static int maxGroundHeight = 20;
	//	Draw edges where no more chunks exist
	public static bool drawEdges = true;
	//	All chunks in the world
	public static Dictionary<Vector3, Chunk> chunks = new Dictionary<Vector3, Chunk>();
	public static Dictionary<Vector3, Topology> topology = new Dictionary<Vector3, Topology>();
																				
	public Material defaultMaterial;

	public class Topology
	{
		public int[,] heightMap =  new int[chunkSize,chunkSize];
		public int highestPoint = 0;
		public int lowestPoint = chunkSize;
	}

	void Start()
	{
		//	Create initial chunks
		UpdateChunks(Vector3.zero, viewDistance);
	}

	//	Temporary for testing and optimisation
	//	Generate and draw chunks in a cube radius of veiwDistance around player
	//	Called in PlayerController
	public void UpdateChunks(Vector3 centerChunk, int radius)
	{
		//	DEBUG
		Debug.Log("Generating "+Mathf.Pow((radius*2+1), 3)+" chunks: "+(radius*2+1)+"x"+(radius*2+1)+"x"+(radius*2+1));
		int epoch = (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
		//	DEBUG

		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Vector3 offset = new Vector3(x,0,z) * chunkSize;
				Vector3 position = new Vector3(centerChunk.x, 0, centerChunk.z) + offset;
				
				if(topology.ContainsKey(position)) continue;	

				//	initalise lowest as heigest
				int lowestPoint = (int)position.y + chunkSize;
				//	initialise heighest as lowest
				int highestPoint  = (int)position.y;

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
			}

		int currentEpoch = (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
		Debug.Log(topology.Count+" columns topology generated in "+(currentEpoch - epoch)+" seconds");

		//	Generate chunks in view distance + 1
		for(int x = -radius-1; x < radius+1; x++)
			for(int z = -radius-1; z < radius+1; z++)
			{
				Topology columnTopology = topology[new Vector3(x*chunkSize,0,z*chunkSize)];

				for(int y = -radius-1; y < radius+1; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					
					if(chunks.ContainsKey(position)) { continue; }

					Chunk chunk = new Chunk(position, this);
					chunks.Add(position, chunk);


					chunk.GenerateBlocks();
				}
			}

		currentEpoch = (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
		Debug.Log(chunks.Count+" chunks GENERATED in "+(currentEpoch - epoch)+" seconds");

		//	Find hidden chunks
		/*for(int x = -radius + 1; x < radius - 1; x++)
			for(int z = -radius + 1; z < radius - 1; z++)
				for(int y = -radius + 1; y < radius - 1; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					Chunk chunk = chunks[position];

					bool hasSurface = false;
					bool hasSolid = false;
					bool hasEmpty = false;

					for(int _x = -1; _x < 2; _x++)
						for(int _y = -1; _y < 2; _y++)
							for(int _z = -1; _z < 2; _z++)
							{
								Chunk neighbourChunk;
								if(!chunks.TryGetValue(position + new Vector3(x,y,z), out neighbourChunk))
									continue;

								switch(neighbourChunk.composition)
								{
									case Chunk.Composition.SURFACE: if(!hasSurface) hasSurface = true; break;
									case Chunk.Composition.SOLID: if(!hasSolid) hasSolid = true; break;
									case Chunk.Composition.EMPTY: if(!hasEmpty) hasEmpty = true; break;
								}
							}
					if( (hasEmpty && !hasSolid && !hasSurface) || (!hasEmpty && hasSolid && !hasSurface) )
					{
						chunk.hidden = true;
						chunk.DebugMarkerColor(Color.red);
					}
				}*/

		/*//	Smooth chunks in view distance
		for(int x = -radius; x < radius; x++)
			for(int z = -radius; z < radius; z++)
				for(int y = -radius; y < radius; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 location = centerChunk + offset;
					chunks[location].SmoothBlocks();
				}*/

		//	Draw chunks in view distance
		int drawnChunkCount = 0;
		for(int x = -radius; x < radius; x++)
			for(int z = -radius; z < radius; z++)
				for(int y = -radius; y < radius; y++)
				{
					Vector3 offset = new Vector3(x, y, z) * chunkSize;
					Vector3 position = centerChunk + offset;
					
					Chunk chunk = chunks[position];
					if(chunk.status == Chunk.Status.DRAWN || chunk.hidden) { continue; }

					chunk.Draw();
					drawnChunkCount++;
				}
		
		currentEpoch = (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds;
		Debug.Log(drawnChunkCount+" chunks DRAWN in "+(currentEpoch - epoch)+" seconds");
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

		List<Vector3> redraw = new List<Vector3>() { chunk.position };

		//	add adjacent chunks to be redrawn if block is at the edge
		if(local.x == 0) 
			redraw.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y,			chunk.position.z)));
		if(local.x == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y,			chunk.position.z)));
		if(local.y == 0) 
			redraw.Add((new Vector3(chunk.position.x,			chunk.position.y-chunkSize,	chunk.position.z)));
		if(local.y == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x,			chunk.position.y+chunkSize,	chunk.position.z)));
		if(local.z == 0) 
			redraw.Add((new Vector3(chunk.position.x,			chunk.position.y,			chunk.position.z-chunkSize)));
		if(local.z == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x,			chunk.position.y,			chunk.position.z+chunkSize)));

		if(local.x == 0 && local.z == 0) 
			redraw.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y, chunk.position.z-chunkSize)));
		if(local.x == chunkSize - 1 && local.z == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y, chunk.position.z+chunkSize)));

		if(local.x == 0 && local.z == chunkSize - 1) 
			redraw.Add((new Vector3(chunk.position.x-chunkSize,	chunk.position.y, chunk.position.z+chunkSize)));
		if(local.x == chunkSize - 1 && local.z == 0) 
			redraw.Add((new Vector3(chunk.position.x+chunkSize,	chunk.position.y, chunk.position.z-chunkSize)));

		//	redraw chunks
		foreach(Vector3 chunkPosition in redraw)
		{
			Chunk updateChunk = World.chunks[chunkPosition];
			//updateChunk.SmoothBlocks();
			updateChunk.Redraw();

			/*if(updateChunk.hidden)
			{
				updateChunk.hidden = false;
				UpdateChunks(updateChunk.position, 1);
			}
			else
			{
				updateChunk.Redraw();
			}*/
		}
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

