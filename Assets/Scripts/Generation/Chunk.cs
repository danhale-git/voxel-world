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

	Column column;

	//	block data
	public Blocks.Types[,,] blockTypes;
	public byte[,,] blockBytes;
	public Shapes.Types[,,] blockShapes;
	public int[,,] blockYRotation;

	List<Shapes.Shape> shapes = World.shapeMeshes.shapes;

	void InitialiseArrays()
	{
		blockTypes = new Blocks.Types[size,size,size];
		blockBytes = new byte[size,size,size];
		blockShapes = new Shapes.Types[size,size,size];
		blockYRotation = new int[size,size,size];
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

		column = Column.Get(position);
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

					//	Terrain
					if (voxel <= heightMap[x,z])
					{
						blockTypes[x,y,z] = column.biomeLayers[x,z].surfaceBlock;

						if(!hasBlocks) hasBlocks = true;
					}
					//	Air
					else if(voxel > heightMap[x,z])
					{
						blockTypes[x,y,z] = Blocks.Types.AIR;
						if(!hasAir) hasAir = true;
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

	public void SmoothBlocks()
	{
		//	*This is not completely deterministic - the order in which chunks are processed could impact the final terrain in some cases
		if(this.composition != Composition.MIX) return;
		//	Remove unwanted blocks from surface
		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
			{
				int y = column.heightMap[x,z] - (int)this.position.y;

				if(y > World.chunkSize-1 || y < 0) continue;

				Blocks.Types type = blockTypes[x,y,z];

				if(Blocks.smoothSurface[(int)type])
				{
					blockBytes[x,y,z] = GetBitMask(new Vector3(x,y,z));//, true, type);
					Shapes.RemoveBlocks(this, x, y, z);
				}
			}

		//	Assign shapes to smooth terrain
		for(int x = 0; x < World.chunkSize; x++)
			for(int z = 0; z < World.chunkSize; z++)
			{
				int height = column.heightMap[x,z] - (int)this.position.y;
				Shapes.Types previousShape = 0;
				int previousY = 0;

				for(int y = height; y > height - 2; y-- )
				{
					if(y > World.chunkSize-1 || y < 0) continue;

					Blocks.Types type = blockTypes[x,y,z];
					Vector3 blockPosition = new Vector3(x,y,z);
					if(Blocks.smoothSurface[(int)type])
					{
						blockBytes[x,y,z] = GetBitMask(blockPosition);
						Shapes.SetSlopes(this, x, y, z);
					}

					//	Avoid overhangs on steep slopes - does not handle iterating between two chunks
					if(previousShape == Shapes.Types.CORNEROUT && (blockShapes[x,y,z] == Shapes.Types.CORNEROUT || blockShapes[x,y,z] == Shapes.Types.WEDGE))
					{
						blockShapes[x,y+1,z] = Shapes.Types.CORNEROUT2;
						blockShapes[x,y,z] = Shapes.Types.CUBE;
					}
					else if(previousShape == Shapes.Types.WEDGE)
					{
						blockShapes[x,y,z] = Shapes.Types.CUBE;
					}

					previousShape = blockShapes[x,y,z];
					previousY = y;
				}
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

		Vector3[] offsets = Util.CubeFaceDirections();
		int solidAdjacentChunkCount = 0;
		for(int i = 0; i < 6; i++)
		{
			Vector3 adjacentPosition = this.position + (offsets[i] * World.chunkSize);

			//World.debug.OutlineChunk(adjacentPosition, Color.green, sizeDivision: 3.5f);

			Chunk adjacentChunk = World.chunks[adjacentPosition];
			if(adjacentChunk.composition == Chunk.Composition.SOLID)
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
		List<Vector2> UVs = new List<Vector2>();
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
						exposedFaces[e] = FaceExposed(offsets[e], blockPosition);

						if(exposedFaces[e] && !blockExposed) { blockExposed = true; }
					}

					if(blockExposed) exposedBlockCount++;

					//	Block is not visible so nothing to draw
					if(!blockExposed && blockBytes[x,y,z] == 0) { continue; }

					//	Check block shapes and generate mesh data
					int localVertCount = 0;


					localVertCount = shapes[(int)blockShapes[x,y,z]].Draw(	verts, norms, tris, UVs,
																			blockPosition,
														 					blockYRotation[x,y,z],
																			exposedFaces,
																			vertexCount,
																			(int)type);

					//	Keep count of vertices to offset triangles
					vertexCount += localVertCount;

					Color color = (Color)Blocks.colors[(int)blockTypes[x,y,z]];

					/*if(column.POIWalls != null && column.POIWalls[x,z] == 1) color = Color.black;
					else if(column.POIWalls != null && column.POIWalls[x,z] == 2) color = Color.red;
					else if(column.POIWalls != null && column.POIWalls[x,z] == 3) color = Color.green;
					/*else if(column.POIHeightGradient != null)
					{
						float colVal = ((float)column.POIHeightGradient[x,z])/10;
						if(colVal == 0) colVal = 0.05f;
						color = new Color(colVal,colVal,colVal);
					}*/
					

					cols.AddRange(	Enumerable.Repeat(	color,
														localVertCount));
				}

		CreateMesh(verts, norms, tris, UVs, cols);
		status = Status.DRAWN;
	}

	//	create a mesh with given attributes
	public void CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, List<Vector2> UVs, List<Color> colors)
	{
		Mesh mesh = new Mesh();

		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetTriangles(triangles, 0);
		mesh.SetColors(colors);
		mesh.SetUVs(0, UVs);
		//mesh.RecalculateNormals();
		UnityEditor.MeshUtility.Optimize(mesh);

		filter = gameObject.AddComponent<MeshFilter>();
		filter.mesh = mesh;

		renderer = gameObject.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = world.defaultMaterial;
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;



		collider = gameObject.AddComponent<MeshCollider>();
		collider.sharedMesh = filter.mesh;
	}

	//	Player can see through adjacent block
	bool FaceExposed(Vector3 offset, Vector3 blockPosition)
	{
		//	Neighbour position
		Vector3 neighbour = blockPosition + offset;

		Chunk neighbourOwner = BlockOwner(neighbour);

		//	Neighbour is outside this chunk
		if(!neighbourOwner.Equals(this))
		{
			//	Convert local index to neighbouring chunk
			neighbour = Util.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in chunk being drawn
		else
		{
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
			int x = (int)neighbour.x, y = (int)neighbour.y, z = (int)neighbour.z;

			Blocks.Types type = neighbourOwner.blockTypes[x, y, z];


			return (Blocks.seeThrough[(int)type] || (int)neighbourOwner.blockShapes[x, y, z] != 0);
		}
	}

	public byte GetBitMask(Vector3 voxel)
	{
		Vector3[] neighbours = Util.HorizontalBlockNeighbours(voxel);
		int value = 1;
		int total = 0;

		for(int i = 0; i < neighbours.Length; i++)
		{
			Chunk owner;
			Vector3 pos;
			//	TODO: Use out keyword in Blockowner and have it return bool instead of running InChunk for the if statement.
			if(!Util.InChunk(neighbours[i]))
			{
				owner = BlockOwner(neighbours[i]);
				pos = owner != this ? Util.WrapBlockIndex(neighbours[i]) : neighbours[i];
			}
			else
			{
				owner = this;
				pos = neighbours[i];
			}

			int x = (int)pos.x, y = (int)pos.y, z = (int)pos.z;

			Blocks.Types type = owner.blockTypes[x, y, z];

			if(Blocks.seeThrough[(int)type])
			{
				total += value;
			}
			value *= 2;
		}
		return (byte)total;
	}

	Chunk BlockOwner(Vector3 pos)
	{
		//	Get block's edge
		int x = 0, y = 0, z = 0;

		if		(pos.x < 0) 				x = -1;
		else if (pos.x > World.chunkSize-1) 	x = 1;

		if		(pos.y < 0) 				y = -1;
		else if (pos.y > World.chunkSize-1) 	y = 1;

		if		(pos.z < 0) 				z = -1;
		else if (pos.z > World.chunkSize-1) 	z = 1;

		//	The edge
		Vector3 edge = new Vector3(x, y, z);

		//	Voxel is in this chunk
		if(edge == Vector3.zero) return this;

		return World.chunks[this.position + (edge * World.chunkSize)];
	}
	Vector3 BlockPosition(Chunk owner, Vector3 position)
	{
		return owner != this ? Util.WrapBlockIndex(position) : position;
	}
}
