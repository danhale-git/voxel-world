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

	public List<Shapes.Shape> shapes = new List<Shapes.Shape>()
	{
		new Shapes.Cube(),
		new Shapes.Wedge()
	};

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
		Draw();
		return;
	}

	public void Redraw()
	{
		Object.DestroyImmediate(filter);
		Object.DestroyImmediate(renderer);
		Object.DestroyImmediate(collider);
		DrawBlocks();
	}

	public void Draw()
	{
		List<Vector3> verts = new List<Vector3>();
		List<Vector3> norms = new List<Vector3>();
		List<int> tris = new List<int>();
		List<Color> cols = new List<Color>();

		//	Vertex count for offsetting triangle indices
		int vertexCount = 0;

		//	Generate mesh data
		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
				for(int y = 0; y < World.chunkSize; y++)
				{
					//	Check block type, skip drawing if air
					Blocks.Types type = blockTypes[x,y,z];
					if(type == Blocks.Types.AIR) { continue; }

					Vector3 blockPosition = new Vector3(x,y,z);
					Shapes.Types shape = blockShapes[x,y,z];
				
					//	Check if adjacent blocks are exposed
					bool[] exposedFaces = new bool[6];
					bool blockExposed = false;
					for(int e = 0; e < 6; e++)
					{
						exposedFaces[e] = FaceExposed((Shapes.CubeFace)e, blockPosition);
						
						if(exposedFaces[e] && !blockExposed) { blockExposed = true; }
					}

					//	Block is not visible so nothing to draw
					if(!blockExposed && blockBytes[x,y,z] == 0) { continue; }

					//	Check block shapes and generate mesh data
					int localVertCount = 0;
					Quaternion rotation = Quaternion.Euler(0, (int)blockYRotation[x,y,z], 0);

					localVertCount = shapes[(int)blockShapes[x,y,z]].Draw(verts, norms, tris, blockPosition,
														 rotation, exposedFaces, vertexCount);

					//	Keep count of vertices to offset triangles
					vertexCount += localVertCount;
					cols.AddRange(	Enumerable.Repeat(	(Color)Blocks.colors[(int)blockTypes[x,y,z]],
														localVertCount));
				}
		CreateMesh(verts, norms, tris, cols);
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
	bool FaceExposed(Shapes.CubeFace face, Vector3 blockPosition)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = Shapes.FaceToDirection(face);	
		//	Neighbour position
		Vector3 neighbour = blockPosition + faceDirection;
		
		Chunk neighbourOwner;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= World.chunkSize || 
		   neighbour.y < 0 || neighbour.y >= World.chunkSize ||
		   neighbour.z < 0 || neighbour.z >= World.chunkSize)
		{
			//	Next chunk in direction of neighbour
			Vector3 neighbourChunkPos = position + (faceDirection * World.chunkSize);
			
			//Debug.Log(neighbourChunkPos);
			
			//	Neighbouring chunk does not exist (map edge)
			if(!World.chunks.TryGetValue(neighbourChunkPos, out neighbourOwner))
			{
				return false;
			}			
			//	Convert local index to neighbouring chunk
			neighbour = BlockUtils.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in chunk being drawn		
		else
		{
			neighbourOwner = this;
		}
		
		//	Check seeThrough in neighbour
		Blocks.Types type = neighbourOwner.blockTypes[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z];

		return (Blocks.seeThrough[(int)type]);
	}
}
