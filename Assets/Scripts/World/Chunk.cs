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

	public int[,] heightmap; 

	//	block data
	public BlockUtils.Types[,,] blockTypes;
	public byte[,,] blockBytes;
	public Shapes.Shape[,,] blockShapes;
	public Shapes.Rotate[,,] blockYRotation;

	void InitialiseArrays()
	{
		heightmap = new int[size,size];

		blockTypes = new BlockUtils.Types[size,size,size];
		blockBytes = new byte[size,size,size];
		blockShapes = new Shapes.Shape[size,size,size];
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
				// TODO generate height map first and find non-exposed air and ground chunks to omit from generation
				//	Get height of ground in this column
				heightmap[x,z] = NoiseUtils.GroundHeight( x + (int)position.x,
															z + (int)position.z,
															World.maxGroundHeight);
			}

		//	Iterate over all blocks in chunk
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
			{
				//	Get height of ground in this column
				int groundHeight = heightmap[x,z];
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

		/*for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					Vector3 blockPosition = new Vector3(x,y,z);

					Vector3 voxel = this.position + blockPosition;
					blockBytes[x,y,z] = World.GetBitMask(voxel);

					SetSlopes(blockPosition);
				}*/
	}

	//	Create a mesh representing all blocks in the chunk
	public void DrawBlocks()
	{
		for(int x = 0; x < size; x++)
			for(int z = 0; z < size; z++)
				for(int y = 0; y < size; y++)
				{
					Vector3 blockPosition = new Vector3(x,y,z);

					blockBytes[x,y,z] = World.GetBitMask(blockPosition + this.position);
					SetSlopes(blockPosition);
				}

		ChunkMesh.Draw(this);
		return;
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


					//	Get bitmask
					Vector3 voxel = this.position + blockPosition;
					blockBytes[x,y,z] = World.GetBitMask(voxel);

					if(!blockExposed && blockBytes[x,y,z] == 0) { continue; }

					/*if(!exposedFaces[(int)BlockUtils.CubeFace.TOP] && !exposedFaces[(int)BlockUtils.CubeFace.BOTTOM])
					{
						blockBytes[x,y,z] = 0;
					}*/

					//blockBytes[x,y,z] = 0;

					switch(blockBytes[x,y,z])
					{
						//	CORNER OUT

						case 53:
						case 21:
						case 85:
						case 117://
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.CornerOutFace.SLOPE);
							if(World.GetBitMask(voxel + Vector3.down) == 0)
								DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.CornerOutFace.BOTTOM);
							break;

						case 57:
						case 41:
						case 169:
						case 185://
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.CornerOutFace.SLOPE);
							if(World.GetBitMask(voxel + Vector3.down) == 0)
								DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.CornerOutFace.BOTTOM);
							break;

						case 202:
						case 138:
						case 170:
						case 234://
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.CornerOutFace.SLOPE);
							if(World.GetBitMask(voxel + Vector3.down) == 0)
								DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.CornerOutFace.BOTTOM);
							break;

						case 70:
						case 86://
						case 214://
						case 82:
						case 198:
							DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.CornerOutFace.SLOPE);
							if(World.GetBitMask(voxel + Vector3.down) == 0)
								DrawCornerOut(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.CornerOutFace.BOTTOM);
							break;

						//	CORNER IN

						case 16:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.CornerInFace.SLOPE);
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.CornerInFace.TOP);
							break;

						case 32:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.CornerInFace.SLOPE);
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.CornerInFace.TOP);
							break;

						case 128:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.CornerInFace.SLOPE);
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.CornerInFace.TOP);
							break;

						case 64:
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.CornerInFace.SLOPE);
							DrawCornerIn(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.CornerInFace.TOP);
							break;

						//	WEDGE

						case 20:
						case 84:
						case 68:
						case 4:
							//vertsGenerated = Shapes.Wedge.Draw(	vertices, normals, triangles, blockPosition, Quaternion.identity,
							//					new Shapes.WedgeFace[1] {Shapes.WedgeFace.SLOPE}, vertsGenerated);
							//DrawWedge(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.WedgeFace.SLOPE);
							break;

						case 49:
						case 17:
						case 33:
						case 1:
							//vertsGenerated = Shapes.Wedge.Draw(	vertices, normals, triangles, blockPosition, Quaternion.Euler(0,90,0),
							//					new Shapes.WedgeFace[1] {Shapes.WedgeFace.SLOPE}, vertsGenerated);
							break;

						//case 48:
						case 168:
						case 40:
						case 8:
						case 136:
							//vertsGenerated = Shapes.Wedge.Draw(	vertices, normals, triangles, blockPosition, Quaternion.Euler(0,180,0),
							//					new Shapes.WedgeFace[1] {Shapes.WedgeFace.SLOPE}, vertsGenerated);
							break;

						case 194:
						case 2:
						case 130:
						case 66:
							//vertsGenerated = Shapes.Wedge.Draw(	vertices, normals, triangles, blockPosition, Quaternion.Euler(0,270,0),
							//					new Shapes.WedgeFace[1] {Shapes.WedgeFace.SLOPE}, vertsGenerated);
							break;

						case 87:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.WedgeFace.SLOPE);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.WedgeFace.RIGHT);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.FRONT, BlockUtils.WedgeFace.LEFT);
							break;

						case 61:
						case 254:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.WedgeFace.SLOPE);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.WedgeFace.RIGHT);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.RIGHT, BlockUtils.WedgeFace.LEFT);
							break;

						case 171:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.WedgeFace.SLOPE);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.WedgeFace.RIGHT);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.BACK, BlockUtils.WedgeFace.LEFT);
							break;

						case 206:
						case 253:
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.WedgeFace.SLOPE);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.WedgeFace.RIGHT);
							DrawWedge(blockPosition, bType, BlockUtils.Rotate.LEFT, BlockUtils.WedgeFace.LEFT);
							break;

						//	CUBE

						case 0:
							DrawCube(exposedFaces, blockPosition, bType);
							//DrawCube(new bool[6] { true,true,true,true,true,true }, blockPosition, bType);
							break;

						default:
							DrawCube(new bool[6] { true,true,true,true,true,true }, blockPosition, bType);
							break;
					}
					
				}

		CreateMesh(vertices, normals, triangles, colors);
	}


	public void SetSlopes(Vector3 voxel)
	{
		int x = (int)voxel.x;
		int y = (int)voxel.y;
		int z = (int)voxel.z;

		switch(blockBytes[x,y,z])
		{
			//	CORNER OUT

			case 53:
			case 21:
			case 85:
			case 117:
				blockShapes[x,y,z] = Shapes.Shape.CORNEROUT;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				break;

			case 57:
			case 41:
			case 169:
			case 185:
				blockShapes[x,y,z] = Shapes.Shape.CORNEROUT;
				blockYRotation[x,y,z] = Shapes.Rotate.RIGHT;
				break;

			case 202:
			case 138:
			case 170:
			case 234:
				blockShapes[x,y,z] = Shapes.Shape.CORNEROUT;
				blockYRotation[x,y,z] = Shapes.Rotate.BACK;
				break;

			case 70:
			case 86:
			case 214:
			case 82:
			case 198:
				blockShapes[x,y,z] = Shapes.Shape.CORNEROUT;
				blockYRotation[x,y,z] = Shapes.Rotate.LEFT;
				break;

			//	CORNER IN

			case 16:
				blockShapes[x,y,z] = Shapes.Shape.CORNERIN;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				break;

			case 32:
				blockShapes[x,y,z] = Shapes.Shape.CORNERIN;
				blockYRotation[x,y,z] = Shapes.Rotate.RIGHT;
				break;

			case 128:
				blockShapes[x,y,z] = Shapes.Shape.CORNERIN;
				blockYRotation[x,y,z] = Shapes.Rotate.BACK;
				break;

			case 64:
				blockShapes[x,y,z] = Shapes.Shape.CORNERIN;
				blockYRotation[x,y,z] = Shapes.Rotate.LEFT;
				break;

			//	WEDGE

			case 20:
			case 84:
			case 68:
			case 4:
				//blockShapes[x,y,z] = Shapes.Shape.CUBE;
				//blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				blockShapes[x,y,z] = Shapes.Shape.WEDGE;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				break;

			case 49:
			case 17:
			case 33:
			case 1:
				blockShapes[x,y,z] = Shapes.Shape.CUBE;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				//blockShapes[x,y,z] = Shapes.Shape.WEDGE;
				//blockYRotation[x,y,z] = Shapes.Rotate.RIGHT;
				break;

			//case 48:
			case 168:
			case 40:
			case 8:
			case 136:
				blockShapes[x,y,z] = Shapes.Shape.CUBE;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				//blockShapes[x,y,z] = Shapes.Shape.WEDGE;
				//blockYRotation[x,y,z] = Shapes.Rotate.BACK;
				break;

			case 194:
			case 2:
			case 130:
			case 66:
				blockShapes[x,y,z] = Shapes.Shape.CUBE;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				//blockShapes[x,y,z] = Shapes.Shape.WEDGE;
				//blockYRotation[x,y,z] = Shapes.Rotate.LEFT;
				break;

			//	CUBE

			case 0:
				blockShapes[x,y,z] = Shapes.Shape.CUBE;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				break;

			default:
				blockShapes[x,y,z] = Shapes.Shape.CUBE;
				blockYRotation[x,y,z] = Shapes.Rotate.FRONT;
				break;
		}
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
				//colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

				vertsGenerated += faceVerts.Length;
			}
		}
	}

	//	Wedge
	void DrawWedge(Vector3 position, BlockUtils.Types type, BlockUtils.Rotate rotation, BlockUtils.WedgeFace face)
	{
		//	Verts can be rotated as object is not symmetrical
		Vector3[] faceVerts = BlockUtils.WedgeVertices(face, position, rotation);
		vertices.AddRange(faceVerts);

		//	Add normals in same order as vertices
		normals.AddRange(BlockUtils.RotateNormals(BlockUtils.WedgeNormals(face), rotation));

		//	Offset triangle indices with number of vertices covered so far
		triangles.AddRange(BlockUtils.WedgeTriangles(face, vertsGenerated));

		//	Get color using Types index
		//colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

		vertsGenerated += faceVerts.Length;
	}

	//	Corner out
	void DrawCornerOut(Vector3 position, BlockUtils.Types type, BlockUtils.Rotate rotation, BlockUtils.CornerOutFace face)
	{
		//	Verts can be rotated as object is not symmetrical
		Vector3[] faceVerts = BlockUtils.CornerOutVertices(face, position, rotation);
		vertices.AddRange(faceVerts);

		//	Add normals in same order as vertices
		normals.AddRange(BlockUtils.RotateNormals(BlockUtils.CornerOutNormals(face), rotation));

		//	Offset triangle indices with number of vertices covered so far
		triangles.AddRange(BlockUtils.CornerOutTriangles(face, vertsGenerated));

		//	Get color using Types index
		//colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

		vertsGenerated += faceVerts.Length;	
	}

	void DrawCornerIn(Vector3 position, BlockUtils.Types type, BlockUtils.Rotate rotation, BlockUtils.CornerInFace face)
	{
		//	Verts can be rotated as object is not symmetrical
		Vector3[] faceVerts = BlockUtils.CornerInVertices(face, position, rotation);
		vertices.AddRange(faceVerts);

		//	Add normals in same order as vertices
		normals.AddRange(BlockUtils.RotateNormals(BlockUtils.CornerInNormals(face), rotation));

		//	Offset triangle indices with number of vertices covered so far
		triangles.AddRange(BlockUtils.CornerInTriangles(face, vertsGenerated));

		//	Get color using Types index
		//colors.AddRange(Enumerable.Repeat( (Color)BlockUtils.colors[(int)type], faceVerts.Length ));

		vertsGenerated += faceVerts.Length;
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

	//	Block face is on map edge or player can see through adjacent block
	public bool FaceExposed(BlockUtils.CubeFace face, Vector3 blockPosition)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = BlockUtils.GetDirection(face);	
		//	Neighbour position
		Vector3 neighbour = blockPosition + faceDirection;
		
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

		return (BlockUtils.seeThrough[(int)type]);// || slopedBlocks[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z]);
	}


}
