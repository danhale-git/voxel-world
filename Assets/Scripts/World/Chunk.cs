using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Chunk
{
	//	Chunk local space and game components
	public GameObject gameObject;

	//	Chunk status
	public enum Status {GENERATED, DRAWN}
	public Status status;

	MeshFilter filter;
	MeshRenderer renderer;
	MeshCollider collider;

	//	World controller Monobehaviour
	World world;

	//	Chunk size and position in world
	int size;
	public Vector3 position;

	public int[,] heightmap; 

	//	block data
	public Blocks.Types[,,] blockTypes;
	public byte[,,] blockBytes;
	public Shapes.Types[,,] blockShapes;
	public Shapes.Rotate[,,] blockYRotation;

	void InitialiseArrays()
	{
		heightmap = new int[size,size];

		blockTypes = new Blocks.Types[size,size,size];
		blockBytes = new byte[size,size,size];
		blockShapes = new Shapes.Types[size,size,size];
		blockYRotation = new Shapes.Rotate[size,size,size];
	}

	public Chunk(Vector3 _position, World _world)
	{
		//	Create GameObject
		gameObject = new GameObject(_position.ToString());
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

		//	Always generate blocks when a new chunk is created
		GenerateBlocks();		
	}

	//	Choose types of all blocks in the chunk based on Perlin noise
	void GenerateBlocks()
	{
		//	Heightmap

		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
			{
				heightmap[x,z] = NoiseUtils.GroundHeight( x + (int)position.x,
															z + (int)position.z,
															World.maxGroundHeight);
			}

		//	Iterate over all blocks in chunk
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
			{
				int groundHeight = heightmap[x,z];
				//	Generate column
				for(int y = 0; y < size; y++)
				{
					Blocks.Types type;

					//	Set block type
					if (y + this.position.y > groundHeight)
					{
						type = Blocks.Types.AIR;
					}
					else
					{
						type = Blocks.Types.DIRT;
					}

					//	Store new block in 3D array
					blockTypes[x,y,z] = type;
				}
			}
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

	//	Create a mesh representing all blocks in the chunk
	public void DrawBlocks()
	{	
		ChunkMesh.Draw(this);
		return;
	}

	public void Redraw()
	{
		Object.DestroyImmediate(filter);
		Object.DestroyImmediate(renderer);
		Object.DestroyImmediate(collider);
		DrawBlocks();
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
}
