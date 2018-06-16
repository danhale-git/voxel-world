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
		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<int> triangles = new List<int>();
		List<Color> colors = new List<Color>();

		//	keep block reference to increment indices
		int vertsGenerated = 0;

		//	Iterate over all block locations in chunk		
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					if(blockTypes[x,y,z] == BlockUtils.Types.AIR) { continue; }

					Vector3 blockPosition = new Vector3(x,y,z);

					//	Iterate over all six faces
					for(int i = 0; i < 6; i++)
					{
						BlockUtils.CubeFace face = (BlockUtils.CubeFace)i;

						//	Add mesh attributes to lists if face exposed
						bool exposed = FaceExposed(face, blockPosition);

						if(exposed)
						{
							//	offset vertex positoins with block position in chunk
							Vector3[] faceVerts = BlockUtils.GetVertices(face, blockPosition);
							vertices.AddRange(faceVerts);

							normals.AddRange(BlockUtils.GetNormals(face));

							//	offset triangle indices with number of vertices covered so far
							triangles.AddRange(BlockUtils.GetTriangles(face, vertsGenerated));

							//	TODO associate color with block in BlockUtils.blockColors[]
							colors.AddRange(Enumerable.Repeat( 	(Color) new Color32((byte)Random.Range(9, 11),
																					(byte)Random.Range(95, 110),
																					(byte)Random.Range(30, 40),
																					255),
																faceVerts.Length ));
							vertsGenerated += faceVerts.Length;

						
						}
					
					}
				}
					
		CreateMesh(vertices, normals, triangles, colors, gameObject);
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
