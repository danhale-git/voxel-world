using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Chunk
{
	//	Scene object with mesh/collider etc
	public GameObject gameObject;

	//	Chunk status
	public enum Status {NONE, CREATED, GENERATED, DRAWN}
	public Status status;
	public enum Composition {EMPTY, MIX, SOLID}
	public Composition composition;

	MeshFilter filter;
	MeshRenderer renderer;
	MeshCollider collider;

	//	World controller Monobehaviour
	World world;

	//	Chunk size and position in world
	int size;
	public Vector3 position;

	//	block data
	public Blocks.Types[,,] blockTypes;
	public byte[,,] blockBytes;
	public Shapes.Types[,,] blockShapes;
	public Shapes.Rotate[,,] blockYRotation;

	List<Shapes.Shape> shapes = new List<Shapes.Shape>()
	{
		new Shapes.Cube(),
		new Shapes.Wedge()
	};

	void InitialiseArrays()
	{
		blockTypes = new Blocks.Types[size,size,size];
		blockBytes = new byte[size,size,size];
		blockShapes = new Shapes.Types[size,size,size];
		blockYRotation = new Shapes.Rotate[size,size,size];
	}

	public Chunk(Vector3 _position, World _world)
	{
		//	Create GameObject
		gameObject = new GameObject("chunk");
		gameObject.layer = 9;
		
		world = _world;
		position = _position;
		
		//	Apply chunk size
		size = World.chunkSize;

		//	initialise arrays
		InitialiseArrays();

		//	Set transform
		gameObject.transform.parent = world.gameObject.transform;
		gameObject.transform.position = position;
	}

	//	Choose types of all blocks in the chunk based on Perlin noise
	public void GenerateBlocks()
	{
		if(status == Chunk.Status.GENERATED || status == Chunk.Status.DRAWN) return;

		world.chunksGenerated++;
		World.debug.Output("Chunks generated", world.chunksGenerated.ToString());

		World.Column column = World.Column.Get(position);
		int[,] heightMap = column.heightMap;

		bool hasAir = false;
		bool hasBlocks = false;

		//	Iterate over all blocks in chunk
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
			{				
				//	Generate column
				for(int y = 0; y < size; y++)
				{
					int voxel = (int) (y + this.position.y);
					//	Set block type
					if (voxel <= heightMap[x,z])
					{
						blockTypes[x,y,z] = TerrainGenerator.defaultBiome.GetLayer(TerrainGenerator.defaultBiome.BaseNoise(x+(int)position.x,z+(int)position.z)).surfaceBlock;
						if(!hasBlocks)
							hasBlocks = true;	
					}
					else if(voxel > heightMap[x,z])
					{
						blockTypes[x,y,z] = Blocks.Types.AIR;
						if(!hasAir)
							hasAir = true;
					}
					
				}
			}
	
		//	Record the composition of the chunk
		if(hasAir && !hasBlocks)
			composition = Composition.EMPTY;
		else if(!hasAir && hasBlocks)
			composition = Composition.SOLID;
		else if(hasAir && hasBlocks)
			composition = Composition.MIX;

		status = Status.GENERATED;
	}

	public void ClearAllBlocks()
	{
		//	Iterate over all blocks in chunk
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					blockTypes[x,y,z] = Blocks.Types.AIR;
				}
		composition = Composition.EMPTY;
	}

	//	Generate bitmask representing surrounding blocks and chose slope type
	public void SmoothBlocks()
	{
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					Vector3 blockPosition = new Vector3(x,y,z);

					blockBytes[x,y,z] = World.GetBitMask(blockPosition + this.position);
					Shapes.SetSlopes(this, blockPosition);
				}
	}

	public void Redraw()
	{
		Object.DestroyImmediate(filter);
		Object.DestroyImmediate(renderer);
		Object.DestroyImmediate(collider);
		Draw(redraw: true);
	}

	public void Draw(bool redraw = false)
	{	
		//bool debugging = false;

		if(status == Status.DRAWN && !redraw ||
		   composition == Composition.EMPTY)
		{
			return;
		}
		
		Chunk[] adjacentChunks = new Chunk[6];
		Vector3[] offsets = Util.CubeFaceDirections();
		int solidAdjacentChunkCount = 0;
		for(int i = 0; i < 6; i++)
		{
			Vector3 adjacentPosition = this.position + (offsets[i] * this.size);
			/*if(!World.chunks.TryGetValue(adjacentPosition, out adjacentChunks[i]))
			{
				debugging = true;
				debug.OutlineChunk(adjacentPosition, Color.red, removePrevious: false);
				debug.OutlineChunk(position, Color.green, removePrevious: false);
				world.disableChunkGeneration = true;

				Debug.Log(adjacentPosition);

				//world.CreateChunk(adjacentPosition);
				//world.GenerateChunk(adjacentPosition);
			}*/

			adjacentChunks[i] = World.chunks[adjacentPosition];
			if(adjacentChunks[i].composition == Chunk.Composition.SOLID)
			{
				solidAdjacentChunkCount++;
			}
			if(solidAdjacentChunkCount == 6) return;
		}

		world.chunksDrawn++;
		World.debug.Output("Chunks drawn", world.chunksDrawn.ToString());

		List<Vector3> verts = new List<Vector3>();
		List<Vector3> norms = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Color> cols = new List<Color>();

		//	Vertex count for offsetting triangle indices
		int vertexCount = 0;
		int exposedBlockCount = 0;

		//	Generate mesh data
		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
				for(int y = 0; y < World.chunkSize; y++)
				{
					//	Check block type, skip drawing if air
					Blocks.Types type = blockTypes[x,y,z];
					if(type == Blocks.Types.AIR)
					{ continue; }
					if(	composition == Composition.SOLID && (Util.InChunk(x, 1) && Util.InChunk(y, 1) && Util.InChunk(z, 1)) )
					{ continue; }

					Vector3 blockPosition = new Vector3(x,y,z);
					Shapes.Types shape = blockShapes[x,y,z];
				
					//	Check if adjacent blocks are exposed
					bool[] exposedFaces = new bool[6];
					bool blockExposed = false;
					for(int e = 0; e < 6; e++)
					{
						exposedFaces[e] = FaceExposed((Shapes.CubeFace)e, blockPosition, adjacentChunks);
						
						if(exposedFaces[e] && !blockExposed) { blockExposed = true; }
					}

					if(blockExposed) exposedBlockCount++;

					//	Block is not visible so nothing to draw
					if(!blockExposed && blockBytes[x,y,z] == 0) { continue; }

					//	Check block shapes and generate mesh data
					int localVertCount = 0;
					Quaternion rotation = Quaternion.Euler(0, (int)blockYRotation[x,y,z], 0);

					localVertCount = shapes[(int)blockShapes[x,y,z]].Draw(	verts, norms, tris,
																			blockPosition,
														 					rotation, 
																			exposedFaces,
																			vertexCount);

					//	Keep count of vertices to offset triangles
					vertexCount += localVertCount;
					cols.AddRange(	Enumerable.Repeat(	(Color)Blocks.colors[(int)blockTypes[x,y,z]],
														localVertCount));
				}
		CreateMesh(verts, norms, tris, cols);
		status = Status.DRAWN;
	}

	//	create a mesh with given attributes
	public void CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, List<Color> colors)
	{
		Mesh mesh = new Mesh();

		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTriangles(triangles, 0);
		mesh.SetColors(colors);

		mesh.RecalculateNormals();

		filter = gameObject.AddComponent<MeshFilter>();
		filter.mesh = mesh;

		renderer = gameObject.AddComponent<MeshRenderer>();		
		renderer.sharedMaterial = world.defaultMaterial;

		collider = gameObject.AddComponent<MeshCollider>();
		collider.sharedMesh = filter.mesh;
	}

	//	Player can see through adjacent block
	bool FaceExposed(Shapes.CubeFace face, Vector3 blockPosition, Chunk[] adjacent)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = Shapes.FaceToDirection(face);	
		//	Neighbour position
		Vector3 neighbour = blockPosition + faceDirection;
		
		Chunk neighbourOwner = null;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= size || 
		   neighbour.y < 0 || neighbour.y >= size ||
		   neighbour.z < 0 || neighbour.z >= size)
		{
			//	Get adjacent chunk on that side
			if(neighbour.x < 0)	neighbourOwner = adjacent[(int)Shapes.CubeFace.LEFT];
			if(neighbour.y < 0)	neighbourOwner = adjacent[(int)Shapes.CubeFace.BOTTOM];
			if(neighbour.z < 0) neighbourOwner = adjacent[(int)Shapes.CubeFace.BACK];


			if(neighbour.x >= size) neighbourOwner = adjacent[(int)Shapes.CubeFace.RIGHT];
			if(neighbour.y >= size) neighbourOwner = adjacent[(int)Shapes.CubeFace.TOP];
			if(neighbour.z >= size)	neighbourOwner = adjacent[(int)Shapes.CubeFace.FRONT];

			//	Convert local index to neighbouring chunk
			neighbour = BlockUtils.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in chunk being drawn		
		else
		{
			neighbourOwner = this;

			//	Block not at edge and chunk is solid
			if(composition == Composition.SOLID) return false;
		}

		//	Neighbour has no blocks generated so this area is not exposed
		if(neighbourOwner.status == Chunk.Status.CREATED)
		{
			return false;
		}
		else
		{
			//	Check if block type is see through
			Blocks.Types type = neighbourOwner.blockTypes[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z];

			return (Blocks.seeThrough[(int)type]);
		}
	}
}
