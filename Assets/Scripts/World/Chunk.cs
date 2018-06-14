using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
	//	Chunk local space and game components
	public GameObject gameObject;
	//	All blocks in this chunk
	public Block[,,] blocks;

	//	Chunk status
	public enum Status {GENERATED, DRAWN}
	public Status status;

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
		blocks = new Block[size,size,size];

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
				//	Get height of ground in this column
				int groundHeight = NoiseUtils.GroundHeight( x + (int)position.x,
															z + (int)position.z,
															World.maxGroundHeight);
				//	Generate column
				for(int y = 0; y < size; y++)
				{
					//	Position of block in chunk
					Vector3 blockPosition = new Vector3(x, y, z);
					//	Position of block in world
					Vector3 blockPositionInWorld = blockPosition + this.position;

					Block.BlockType type;

					//	Set block type
					if (blockPositionInWorld.y > groundHeight)
					{
						type = Block.BlockType.AIR;
					}
					else
					{
						type = Block.BlockType.GROUND;
					}

					//	Store new block in 3D array
					blocks[x,y,z] = new Block(type, blockPosition, this);
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

		//	Iterate over all block locations in chunk		
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					Block block = blocks[x,y,z];

					//	Load lists of mesh attributes in block
					block.GetFaces();

					//	Add block's mesh attributes to lists in chunk
					vertices.AddRange(block.vertices);
					normals.AddRange(block.normals);
					triangles.AddRange(block.triangles);

					//	Draw individual blocks on top of whole chunk
					//CreateMesh(block.vertices, block.normals, block.triangles);
				}
		
		Mesh mesh = new Mesh();
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTriangles(triangles, 0);
		
		//mesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0); // Still broken

		//mesh.RecalculateBounds();
		//mesh.RecalculateNormals();
		//mesh.RecalculateTangents();	//	Still broken

		MeshFilter filter = gameObject.AddComponent<MeshFilter>();
		filter.mesh = mesh;

		MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();		
		renderer.sharedMaterial = world.defaultMaterial;

		foreach(Vector3 vert in mesh.vertices)									//	DEBUG
		{
			//Debug.Log(vert); 					// 	Logs values from
		}										//	0 to chunk size

		foreach(int tri in mesh.triangles)
		{
			//Debug.Log(tri); 					// 	0 - 150
												//	1 - 300
												//	2 - 150
		}										//	3 - 300

		Debug.Log("vertices "+mesh.vertices.Length);	//	600
		Debug.Log("normals "+mesh.normals.Length);		//	600
		Debug.Log("triangles "+mesh.triangles.Length);	//	900					//	DEBUG
	}

	//	create a mesh with given attributes
	void CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
	{
		Mesh mesh = new Mesh();

		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTriangles(triangles, 0);

		GameObject obj = new GameObject();

		MeshFilter filter = obj.AddComponent<MeshFilter>();
		filter.mesh = mesh;

		MeshRenderer renderer = obj.AddComponent<MeshRenderer>();		
		renderer.sharedMaterial = world.defaultMaterial;						//	DEBUG
	}
}
