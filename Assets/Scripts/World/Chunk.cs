using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Chunk
{
	//	Chunk local space and game components
	public GameObject gameObject;

	//	block data
	public BlockUtils.Types[,,] blockTypes;

	//	Chunk status
	public enum Status {GENERATED, DRAWN}
	public Status status;

	List<Vector3> vertices = new List<Vector3>();
	List<Vector3> normals = new List<Vector3>();
	List<int> triangles = new List<int>();
	List<Color> colors = new List<Color>();

	int vertsGenerated = 0;

	MeshFilter filter;
	MeshRenderer renderer;
	MeshCollider collider;

	//	World controller Monobehaviour
	World world;

	//	Chunk size and position in world
	int size;
	public Vector3 position;

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
		blockTypes = new BlockUtils.Types[size,size,size];

		//	Set transform
		gameObject.transform.parent = world.gameObject.transform;
		gameObject.transform.position = position;
		
		//	Always generate blocks when a new chunk is created
		GenerateBlocks();		
	}

	//	Choose types of all blocks in the chunk based on Perlin noise
	void GenerateBlocks()
	{
		//	Iterate over all blocks in chunk
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
			{
				// TODO generate height map first and find non-exposed air and ground chunks to omit from generation
				//	Get height of ground in this column
				int groundHeight = NoiseUtils.GroundHeight( x + (int)position.x,
															z + (int)position.z,
															World.maxGroundHeight);
				//	Generate column
				for(int y = 0; y < size; y++)
				{
					BlockUtils.Types type;

					//	Set block type
					if (y + this.position.y > groundHeight)
					{
						type = BlockUtils.Types.AIR;
					}
					else
					{
						type = BlockUtils.Types.DIRT;
					}

					//	Store new block in 3D array
					blockTypes[x,y,z] = type;
				}
			}
	}

	//	Create a mesh representing all blocks in the chunk
	public void DrawBlocks()
	{
		//	Attributes for the final chunk mesh
		vertices = new List<Vector3>();
		normals = new List<Vector3>();
		triangles = new List<int>();
		colors = new List<Color>();

		//	keep block reference to increment indices
		vertsGenerated = 0;

		//	Iterate over all block locations in chunk		
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					BlockUtils.Types bType = blockTypes[x,y,z];
					if(bType == BlockUtils.Types.AIR) { continue; }

					Vector3 blockPosition = new Vector3(x,y,z);
					
					bool[] exposedFaces = new bool[6];
					bool blockExposed = false;

					//	Check exposed faces
					for(int e = 0; e < 6; e++)
					{
						BlockUtils.CubeFace face = (BlockUtils.CubeFace)e;

						exposedFaces[e] = FaceExposed(face, blockPosition);
						
						if(exposedFaces[e] && !blockExposed)
						{
							blockExposed = true;
						}
					}

					if(!blockExposed) { continue; }

					//	Get bitmask
					Vector3 voxel = this.position + blockPosition;
					byte bitMask = World.GetBitMask(voxel);

					//	Iterate over all six faces

					switch(bitMask)
					{
						case 16:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.FRONT);
							break;

						case 32:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.RIGHT);
							break;

						case 68:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.LEFT);
							break;

						case 53:
						case 21:
						case 85:
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.FRONT);
							break;

						case 57:
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.RIGHT);
							break;

						case 86:
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.LEFT);
							break;

						case 84:
						case 20:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.FRONT);
							break;

						case 49:
						case 17:
						case 33:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.RIGHT);
							break;

						case 168:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.BACK);
							break;

						case 194:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.LEFT);
							break;

						default:
							DrawCube(exposedFaces, blockPosition, bType);
							break;

					}
					
				}
					
		CreateMesh(vertices, normals, triangles, colors, gameObject);
	}

	//	Cube
	void DrawCube(bool[] exposedFaces, Vector3 cubePosition, BlockUtils.Types type)
	{
		for(int i = 0; i < exposedFaces.Length; i++)
		{
			if(exposedFaces[i])
			{
				BlockUtils.CubeFace face = (BlockUtils.CubeFace)i;

				//	Offset vertex positoins with block position in chunk
				Vector3[] faceVerts = BlockUtils.CubeVertices(face, cubePosition);
				vertices.AddRange(faceVerts);

				//	Add normals in same order as vertices
				normals.AddRange(BlockUtils.CubeNormals(face));

				//	Offset triangle indices with number of vertices covered so far
				triangles.AddRange(BlockUtils.CubeTriangles(face, vertsGenerated));

				//	Get color using Types index
				colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

				vertsGenerated += faceVerts.Length;
			}
		}
	}

	//	Wedge
	void DrawWedge(Vector3 wedgePosition, BlockUtils.Types type, BlockUtils.Rotate rotation)
	{
		BlockUtils.WedgeFace face = BlockUtils.WedgeFace.SLOPE;

		//	Verts can be rotated as object is not symmetrical
		Vector3[] faceVerts = BlockUtils.WedgeVertices(face, wedgePosition, rotation);
		vertices.AddRange(faceVerts);

		//	Add normals in same order as vertices
		normals.AddRange(BlockUtils.WedgeNormals(face));

		//	Offset triangle indices with number of vertices covered so far
		triangles.AddRange(BlockUtils.WedgeTriangles(face, vertsGenerated).Reverse());

		//	Get color using Types index
		colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

		vertsGenerated += faceVerts.Length;
	}

	//	Corner out
	void DrawCornerOut(Vector3 wedgePosition, BlockUtils.Types type, BlockUtils.Rotate rotation)
	{
		for(int i = 0; i < 3; i++)
		{
			BlockUtils.CornerOutFace face = (BlockUtils.CornerOutFace)i;

			//	Verts can be rotated as object is not symmetrical
			Vector3[] faceVerts = BlockUtils.CornerOutVertices(face, wedgePosition, rotation);
			vertices.AddRange(faceVerts);

			//	Add normals in same order as vertices
			normals.AddRange(BlockUtils.CornerOutNormals(face));

			//	Offset triangle indices with number of vertices covered so far
			triangles.AddRange(BlockUtils.CornerOutTriangles(face, vertsGenerated));

			//	Get color using Types index
			colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

			vertsGenerated += faceVerts.Length;
		}
	}

	void DrawCornerIn(Vector3 wedgePosition, BlockUtils.Types type, BlockUtils.Rotate rotation)
	{
		for(int i = 0; i < 2; i++)
		{
			BlockUtils.CornerInFace face = (BlockUtils.CornerInFace)i;

			//	Verts can be rotated as object is not symmetrical
			Vector3[] faceVerts = BlockUtils.CornerInVertices(face, wedgePosition, rotation);
			vertices.AddRange(faceVerts);

			//	Add normals in same order as vertices
			normals.AddRange(BlockUtils.CornerInNormals(face));

			//	Offset triangle indices with number of vertices covered so far
			triangles.AddRange(BlockUtils.CornerInTriangles(face, vertsGenerated).Reverse());

			//	Get color using Types index
			colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

			vertsGenerated += faceVerts.Length;
		}
	}

	public void Redraw()
	{
		Object.DestroyImmediate(filter);
		Object.DestroyImmediate(renderer);
		Object.DestroyImmediate(collider);
		DrawBlocks();
	}

	//	create a mesh with given attributes
	void CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, List<Color> colors, GameObject gObject)
	{
		Mesh mesh = new Mesh();

		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTriangles(triangles, 0);
		mesh.SetColors(colors);

		mesh.RecalculateNormals();

		filter = gObject.AddComponent<MeshFilter>();
		filter.mesh = mesh;

		renderer = gObject.AddComponent<MeshRenderer>();		
		renderer.sharedMaterial = world.defaultMaterial;

		collider = gameObject.AddComponent<MeshCollider>();
		collider.sharedMesh = filter.mesh;
	}

	//	Block face is on map edge or player can see through adjacent block
	bool FaceExposed(BlockUtils.CubeFace face, Vector3 voxel)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = BlockUtils.GetDirection(face);	
		//	Neighbour position
		Vector3 neighbour = voxel + faceDirection;
		
		Chunk neighbourOwner;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= World.chunkSize || 
		   neighbour.y < 0 || neighbour.y >= World.chunkSize ||
		   neighbour.z < 0 || neighbour.z >= World.chunkSize)
		{
			//	Next chunk in direction of neighbour
			Vector3 neighbourChunkPos = this.position + (faceDirection * World.chunkSize);
			
			//Debug.Log(neighbourChunkPos);
			
			//	Neighbouring chunk does not exist (map edge)
			if(!World.chunks.TryGetValue(neighbourChunkPos, out neighbourOwner))
			{
				return false;
			}			
			//	Convert local index to neighbouring chunk
			neighbour = BlockUtils.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in this chunk		
		else
		{
			neighbourOwner = this;
		}
		
		//Debug.Log((int)neighbour.x+" "+(int)neighbour.y+" "+(int)neighbour.z);
		
		//	Check seeThrough in neighbour
		BlockUtils.Types type = neighbourOwner.blockTypes[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z];

		return BlockUtils.seeThrough[(int)type];
	}


}
