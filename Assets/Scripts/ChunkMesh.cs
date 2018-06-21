using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ChunkMesh
{
	public static void Draw(Chunk chunk)
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
					Blocks.Types blockType = chunk.blockTypes[x,y,z];
					if(blockType == Blocks.Types.AIR) { continue; }

					Vector3 blockPosition = new Vector3(x,y,z);
					Shapes.Types blockShape = chunk.blockShapes[x,y,z];
				
					//	Check if adjacent blocks are exposed
					bool[] exposedFaces = new bool[6];
					bool blockExposed = false;
					for(int e = 0; e < 6; e++)
					{
						exposedFaces[e] = FaceExposed((Shapes.CubeFace)e, blockPosition, chunk);
						
						if(exposedFaces[e] && !blockExposed) { blockExposed = true; }
					}

					//	Block is not visible so nothing to draw
					if(!blockExposed && chunk.blockBytes[x,y,z] == 0) { continue; }

					//	Check block shapes and generate mesh data
					int localVertCount = 0;
					Quaternion shapeRotation = Quaternion.Euler(0, (int)chunk.blockYRotation[x,y,z], 0);
					switch(blockShape)
					{
						case Shapes.Types.WEDGE:
							localVertCount = DrawWedge(		verts, norms, tris, blockPosition, shapeRotation,
															exposedFaces, vertexCount);
							break;

						case Shapes.Types.CORNEROUT:	

							byte belowBlock;	//	Handle case where there is a slope under this slope			//			//
							if(y == 0)			//	Uses bitmask to check diagonally adjacent blocks
							{					//	Only creates bottom triangle if below/adjacent corner is not see through
								Chunk belowChunk = World.BlockOwnerChunk(chunk.position + (blockPosition + Vector3.down)); 
								belowBlock = belowChunk.blockBytes[x, World.chunkSize - 1, z];
							}																	//TODO: Make this go away	//
							else
							{
								belowBlock = chunk.blockBytes[x, y-1, z];
							}			//			//			//			//			//			//			//			//

							localVertCount = DrawCornerOut(	verts, norms, tris, blockPosition, shapeRotation,
															exposedFaces, vertexCount, belowBlock);							
							break;

						case Shapes.Types.CORNERIN:
							localVertCount = DrawCornerIn(	verts, norms, tris, blockPosition, shapeRotation,
															exposedFaces, vertexCount);
							break;

						case Shapes.Types.OUTCROP:
							localVertCount = DrawOutcrop(	verts, norms, tris, blockPosition, shapeRotation,
															exposedFaces, vertexCount);
							break;

						case Shapes.Types.STRIP:
							localVertCount = DrawStrip(		verts, norms, tris, blockPosition, shapeRotation,
															exposedFaces, vertexCount);
							break;

						case Shapes.Types.STRIPEND:
							localVertCount = DrawStripEnd(		verts, norms, tris, blockPosition, shapeRotation,
															exposedFaces, vertexCount);
							break;

						default:
							localVertCount = DrawCube(		verts, norms, tris, blockPosition,
															exposedFaces, vertexCount);
							break;
					}
					//	Keep count of vertices to offset triangles
					vertexCount += localVertCount;
					cols.AddRange(	Enumerable.Repeat(	(Color)Blocks.colors[(int)chunk.blockTypes[x,y,z]],
														localVertCount));
				}

		chunk.CreateMesh(verts, norms, tris, cols);
	}

	//	Cube
	static int DrawCube(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
							Vector3 position, 			bool[] exposedFaces, 	int vertCount)
	{
		int localVertCount = 0;
		for(int i = 0; i < exposedFaces.Length; i++)
		{
			if(exposedFaces[i])
			{
				Shapes.CubeFace face = (Shapes.CubeFace)i;

				//	Vertices are offset by their position in the chunk
				Vector3[] faceVerts = Shapes.Cube.Vertices(face, position);

				vertices.AddRange(	faceVerts);

				//	Add normals in same order as vertices
				normals.AddRange(	Shapes.Cube.Normals(face));

				//	Offset triangle indices with number of vertices covered so far
				triangles.AddRange(	Shapes.Cube.Triangles(	face,
															vertCount + localVertCount));

				//	Count vertices in shape
				localVertCount += faceVerts.Length;
			}
		}
		//	Count vertices in chunk
		return localVertCount;
	}


	//	Wedge
	static int DrawWedge(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
							Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
	{
		List<Shapes.WedgeFace> faces = new List<Shapes.WedgeFace>();

		//	Check exposed faces taking into account shape rotation
		if(TopFront(exposedFaces, rotation))
			faces.Add(Shapes.WedgeFace.SLOPE);
		if(Left(exposedFaces, rotation))
			faces.Add(Shapes.WedgeFace.RIGHT);
		if(Right(exposedFaces, rotation))
			faces.Add(Shapes.WedgeFace.LEFT);

		
		int localVertCount = 0;
		//	Iterate over necessary faces
		for(int i = 0; i < faces.Count; i++)
		{
			Vector3[] verts = Shapes.Wedge.Vertices(faces[i], position);

			vertices.AddRange(	RotateVectors(	verts,
												position,
												rotation));

			normals.AddRange(	RotateNormals(	Shapes.Wedge.Normals(faces[i]),
												rotation));

			triangles.AddRange(	Shapes.Wedge.Triangles(faces[i],
								vertCount + localVertCount));

			localVertCount += verts.Length;
		}
		return localVertCount;
	}

	//	Corner out
	static int DrawCornerOut(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
								Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount, byte belowBlock)
	{
		List<Shapes.CornerOutFace> faces = new List<Shapes.CornerOutFace>();

		if(	TopFrontRight(exposedFaces, rotation))
			faces.Add(Shapes.CornerOutFace.SLOPE);
		if( belowBlock == 0)
			faces.Add(Shapes.CornerOutFace.BOTTOM);

		int localVertCount = 0;
		for(int i = 0; i < faces.Count; i++)
		{
			Vector3[] faceVerts = Shapes.CornerOut.Vertices(faces[i], position);

			vertices.AddRange(	RotateVectors(	faceVerts,
												position,
												rotation));

			normals.AddRange(	RotateNormals(	Shapes.CornerOut.Normals(faces[i]),
												rotation));

			triangles.AddRange(	Shapes.CornerOut.Triangles(faces[i],
								vertCount + localVertCount));

			localVertCount += faceVerts.Length;
		}
		return localVertCount;			
	}

	//	Corner in
	static int DrawCornerIn(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
								Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
	{
		List<Shapes.CornerInFace> faces = new List<Shapes.CornerInFace>();

		faces.Add(Shapes.CornerInFace.SLOPE);

		if(Top(exposedFaces, rotation))
			faces.Add(Shapes.CornerInFace.TOP);

		int localVertCount = 0;
		for(int i = 0; i < faces.Count; i++)
		{
			Vector3[] faceVerts = Shapes.CornerIn.Vertices(faces[i], position);

			vertices.AddRange(	RotateVectors(	faceVerts,
												position,
												rotation));

			normals.AddRange(	RotateNormals(	Shapes.CornerIn.Normals(faces[i]),
												rotation));

			triangles.AddRange(	Shapes.CornerIn.Triangles(faces[i],
								vertCount + localVertCount));

			localVertCount += faceVerts.Length;	
		}
		return localVertCount;			
	}

	//	Outcrop
	static int DrawOutcrop(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
							Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
	{
		List<Shapes.OutcropFace> faces = new List<Shapes.OutcropFace>();

		if(TopFront(exposedFaces, rotation))
			faces.Add(Shapes.OutcropFace.RSLOPE);
			faces.Add(Shapes.OutcropFace.LSLOPE);
			faces.Add(Shapes.OutcropFace.TOP);

		if(Right(exposedFaces, rotation))
			faces.Add(Shapes.OutcropFace.RIGHT);
		if(Left(exposedFaces, rotation))
			faces.Add(Shapes.OutcropFace.LEFT);
		if(Front(exposedFaces, rotation))
			faces.Add(Shapes.OutcropFace.FRONT);
		if(Back(exposedFaces, rotation))
			faces.Add(Shapes.OutcropFace.BACK);
		if(Bottom(exposedFaces, rotation))
			faces.Add(Shapes.OutcropFace.BOTTOM);

		int localVertCount = 0;
		for(int i = 0; i < faces.Count; i++)
		{
			Vector3[] faceVerts = Shapes.Outcrop.Vertices(faces[i], position);

			vertices.AddRange(	RotateVectors(	faceVerts,
												position,
												rotation));

			normals.AddRange(	RotateNormals(	Shapes.Outcrop.Normals(faces[i]),
												rotation));

			triangles.AddRange(	Shapes.Outcrop.Triangles(faces[i],
								vertCount + localVertCount));

			localVertCount += faceVerts.Length;	
		}
		return localVertCount;			
	}

	//	Strip
	static int DrawStrip(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
							Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
	{
		List<Shapes.StripFace> faces = new List<Shapes.StripFace>();

		faces.Add(Shapes.StripFace.RIGHT);
		faces.Add(Shapes.StripFace.LEFT);

		if(Back(exposedFaces, rotation))
			faces.Add(Shapes.StripFace.BACK);
		if(Front(exposedFaces, rotation))
			faces.Add(Shapes.StripFace.FRONT);
		if(Bottom(exposedFaces, rotation))
			faces.Add(Shapes.StripFace.BOTTOM);

		int localVertCount = 0;
		for(int i = 0; i < faces.Count; i++)
		{
			Vector3[] faceVerts = Shapes.Strip.Vertices(faces[i], position);

			vertices.AddRange(	RotateVectors(	faceVerts,
												position,
												rotation));

			normals.AddRange(	RotateNormals(	Shapes.Strip.Normals(faces[i]),
												rotation));

			triangles.AddRange(	Shapes.Strip.Triangles(faces[i],
								vertCount + localVertCount));

			localVertCount += faceVerts.Length;	
		}
		return localVertCount;			
	}

	//	Strip end
	static int DrawStripEnd(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
							Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
	{
		List<Shapes.StripEndFace> faces = new List<Shapes.StripEndFace>();

		faces.Add(Shapes.StripEndFace.RIGHT);
		faces.Add(Shapes.StripEndFace.LEFT);
		faces.Add(Shapes.StripEndFace.FRONT);

		if(Back(exposedFaces, rotation))
			faces.Add(Shapes.StripEndFace.BACK);
		if(Bottom(exposedFaces, rotation))
			faces.Add(Shapes.StripEndFace.BOTTOM);

		int localVertCount = 0;
		for(int i = 0; i < faces.Count; i++)
		{
			Vector3[] faceVerts = Shapes.StripEnd.Vertices(faces[i], position);

			vertices.AddRange(	RotateVectors(	faceVerts,
												position,
												rotation));

			normals.AddRange(	RotateNormals(	Shapes.StripEnd.Normals(faces[i]),
												rotation));

			triangles.AddRange(	Shapes.StripEnd.Triangles(faces[i],
								vertCount + localVertCount));

			localVertCount += faceVerts.Length;	
		}
		return localVertCount;			
	}





    #region Misc

	//	Check which faces of a shape are exposed, adjusting for the shape's rotation

	static bool TopFront(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)] ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)]);
	}

	static bool TopFrontRight(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)]   ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)] ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.RIGHT, rotation)]);
	}

	static bool TopFrontLeft(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)]   ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)] ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.LEFT, rotation)]);
	}

	static bool Right(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.RIGHT, rotation)]);
	}

	static bool Left(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.LEFT, rotation)]);
	}

	static bool Front(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)]);
	}

	static bool Back(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.BACK, rotation)]);
	}

	static bool Top(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)]);
	}

	static bool Bottom(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.BOTTOM, rotation)]);
	}
	
	

	//	Return vector matching cube face normal
	static Vector3 FaceToDirection(Shapes.CubeFace face)
	{
		Vector3 direction;
		
		switch(face)
		{
			case Shapes.CubeFace.TOP: direction = Vector3.up; break;
			case Shapes.CubeFace.BOTTOM: direction = Vector3.down; break;
			case Shapes.CubeFace.RIGHT: direction = Vector3.right; break;
			case Shapes.CubeFace.LEFT: direction = Vector3.left; break;
			case Shapes.CubeFace.FRONT: direction = Vector3.forward; break;
			case Shapes.CubeFace.BACK: direction = Vector3.back; break;
			default: direction = Vector3.zero; break;
		}

		return direction;
	}

	//	Return cube face facing direction
	static Shapes.CubeFace DirectionToFace(Vector3 direction)
	{
		if(direction == Vector3.up) return Shapes.CubeFace.TOP;
		if(direction == Vector3.down) return Shapes.CubeFace.BOTTOM;
		if(direction == Vector3.right) return Shapes.CubeFace.RIGHT;
		if(direction == Vector3.left) return Shapes.CubeFace.LEFT;
		if(direction == Vector3.forward) return Shapes.CubeFace.FRONT;
		if(direction == Vector3.back) return Shapes.CubeFace.BACK;
		else Debug.Log("BAD FACE"); return Shapes.CubeFace.TOP;
	}

	//	Player can see through adjacent block
	static bool FaceExposed(Shapes.CubeFace face, Vector3 blockPosition, Chunk chunk)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = FaceToDirection(face);	
		//	Neighbour position
		Vector3 neighbour = blockPosition + faceDirection;
		
		Chunk neighbourOwner;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= World.chunkSize || 
		   neighbour.y < 0 || neighbour.y >= World.chunkSize ||
		   neighbour.z < 0 || neighbour.z >= World.chunkSize)
		{
			//	Next chunk in direction of neighbour
			Vector3 neighbourChunkPos = chunk.position + (faceDirection * World.chunkSize);
			
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
			neighbourOwner = chunk;
		}
		
		//	Check seeThrough in neighbour
		Blocks.Types type = neighbourOwner.blockTypes[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z];

		return (Blocks.seeThrough[(int)type]);
	}

	#endregion

    #region Rotation

	//	Rotate vertices around centre by yRotation on Y axis
	static Vector3[] RotateVectors(Vector3[] vectors, Vector3 centre, Quaternion rotation)
	{		
		Vector3[] rotatedVertices = new Vector3[vectors.Length];
		for(int i = 0; i < vectors.Length; i++)
		{
			//	rotate vertex position around centre
			rotatedVertices[i] = rotation * (vectors[i] - centre) + centre;
		}
		
		return rotatedVertices;
	}

	//	Rotate vertex around centre by yRotation on Y axis
	static Vector3 RotateVector(Vector3 vector, Vector3 centre, Quaternion rotation)
	{				
		return rotation * (vector - centre) + centre;
	}

	//	Rotate normal directions
	static Vector3[] RotateNormals(Vector3[] normals, Quaternion rotation)
	{
		Vector3[] rotatedNormals = new Vector3[normals.Length];
		for(int i = 0; i < normals.Length; i++)
		{
			//	rotate normal direction
			rotatedNormals[i] = rotation * normals[i];
		}
		return rotatedNormals;
	}

	//	Adjust face enum by direction
	static Shapes.CubeFace RotateFace(Shapes.CubeFace face, Quaternion rotation)
	{
		//	Convert to Vector3 direction, rotate then convert back to face
		return DirectionToFace(RotateVector(FaceToDirection(face), Vector3.zero, rotation));
	}

	#endregion
	
}
