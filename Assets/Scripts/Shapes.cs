using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Shapes
{
	public enum Types {CUBE, WEDGE, CORNERIN, CORNEROUT, OUTCROP, STRIP, STRIPEND, WEDGECORNER, WEDGECORNERINVERSE, NONE}
	public enum Rotate { FRONT, RIGHT, BACK, LEFT }
	static int[] rotations = { 0, 90, 180, 270 };
	
	//public enum CubeFace {TOP, BOTTOM, RIGHT, LEFT, FRONT, BACK}
	public enum CubeFace {FRONT, RIGHT, BACK, LEFT, TOP, BOTTOM}

	public enum Faces {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM, LSLOPE, RSLOPE}
	const int numberOfFaces = 8;

	//	For specifying which face of a shape is being worked on
	/* public enum WedgeFace {SLOPE, BOTTOM, LEFT, RIGHT, BACK}	//
	public enum CornerOutFace {SLOPE, BOTTOM, LEFT}
	public enum CornerInFace {SLOPE, TOP}
	public enum OutCropFace {RSLOPE, LSLOPE, RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum StripFace {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum StripEndFace {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum LumpFace {RIGHT, LEFT, FRONT, BACK, BOTTOM}	//*/

	//	Cube corners
	readonly static Vector3 v0 = new Vector3( 	-0.5f, -0.5f,	 0.5f );	//	left bottom front
	readonly static Vector3 v2 = new Vector3( 	 0.5f, -0.5f,	-0.5f );	//	right bottom back
	readonly static Vector3 v3 = new Vector3( 	-0.5f, -0.5f,	-0.5f ); 	//	left bottom back
	readonly static Vector3 v1 = new Vector3( 	 0.5f, -0.5f,	 0.5f );	//	right bottom front
	readonly static Vector3 v4 = new Vector3( 	-0.5f,  0.5f,	 0.5f );	//	left top front
	readonly static Vector3 v5 = new Vector3( 	 0.5f,  0.5f,	 0.5f );	//	right top front
	readonly static Vector3 v6 = new Vector3( 	 0.5f,  0.5f,	-0.5f );	//	right top back
	readonly static Vector3 v7 = new Vector3( 	-0.5f,  0.5f,	-0.5f );	//	left top back
	
	//	Cube mid points
	readonly static Vector3 v8 = new Vector3(  	0f, 	0f,		-0.5f);		//	middle back
	readonly static Vector3 v9 = new Vector3(  	0f, 	0f,		 0.5f);		//	middle front
	readonly static Vector3 v10 = new Vector3( 	0.5f,	0f,		 0f);		//	middle right
	readonly static Vector3 v11 = new Vector3( -0.5f,	0f,		 0f);		//	middle left
	readonly static Vector3 v12 = new Vector3( 	0f, 	0f,		 0f);		//	middle

	public class Meshes
	{
		public List<Shape> shapes = new List<Shape>();

		public Meshes()
		{
			shapes.Add(new Cube());
			shapes.Add(new Wedge());
			shapes.Add(new CornerIn());
			shapes.Add(new CornerOut());
			shapes.Add(new Outcrop());
			shapes.Add(new Strip());
			shapes.Add(new StripEnd());
			shapes.Add(new WedgeCorner());
			shapes.Add(new WedgeCornerInverse());

			foreach(Shape shape in shapes)
			{
				shape.GenerateMeshes();
			}
		}
	}

	public class Shape
	{
		Vector3[][][] shapeVertices = new Vector3[rotations.Length][][];
		Vector3[][][] shapeNormals = new Vector3[rotations.Length][][];
		int[][][] shapeTriangles = new int[rotations.Length][][];

		public void GenerateMeshes()
		{
			for(int r = 0; r < rotations.Length; r++)
			{
				Quaternion rotation = Quaternion.Euler(0, rotations[r], 0);
				List<Faces> faces = GetFaces(new bool[6], 0, true);

				shapeVertices[r] = new Vector3[numberOfFaces][];
				shapeNormals[r] = new Vector3[numberOfFaces][];
				shapeTriangles[r] = new int[numberOfFaces][];

				for(int f = 0; f < faces.Count; f++)
				{
					shapeVertices[r][(int)faces[f]] = RotateVectors(	Vertices(faces[f], Vector3.zero),
																		Vector3.zero,
																		rotation);

					shapeNormals[r][(int)faces[f]] = RotateNormals(		Normals(faces[f]),
																		rotation);

					shapeTriangles[r][(int)faces[f]] = Triangles(		faces[f], 0);
				}
			}
		}

		public virtual List<Faces> GetFaces(bool[] exposedFaces, int rotationIndex, bool getAll = false)
		{ return new List<Faces>(); }

		public int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			int rotationIndex,	bool[] exposedFaces, 	int vertCount)
		{
			//Quaternion qRotation = Quaternion.Euler(0, rotations[rotationIndex], 0);

			//	rotationIndex is equal to Y rotation divided by 90
			List<Faces> faces = GetFaces(exposedFaces, rotationIndex);
			int localVertCount = 0;
			int localTriCount = 0;
			
			Vector3[][] vertArrays = new Vector3[faces.Count][];
			Vector3[][] normArrays = new Vector3[faces.Count][];
			int[][] triArrays = new int[faces.Count][];

			for(int i = 0; i < faces.Count; i++)
			{
				vertArrays[i] =	OffsetVectors(		shapeVertices[rotationIndex][(int)faces[i]],
													position);

				normArrays[i] = shapeNormals[rotationIndex][(int)faces[i]];

				triArrays[i] = Triangles(faces[i], vertCount + localVertCount);
				
				localTriCount += triArrays[i].Length;
				localVertCount += vertArrays[i].Length;
			}

			Vector3[] allVerts = new Vector3[localVertCount];
			Vector3[] allNorms = new Vector3[localVertCount];
			int[] allTris = new int[localTriCount];

			int vertIndexOffset = 0;
			int triIndexOffset = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				vertArrays[i].CopyTo(allVerts, vertIndexOffset);
				normArrays[i].CopyTo(allNorms, vertIndexOffset);

				triArrays[i].CopyTo( allTris,  triIndexOffset);

				vertIndexOffset += vertArrays[i].Length;
				triIndexOffset += triArrays[i].Length;
			}
			vertices.AddRange(allVerts);
			normals.AddRange(allNorms);
			triangles.AddRange(allTris);

			return localVertCount;
		}

		public virtual Vector3[] Vertices(Faces face, Vector3 offset) {	return new Vector3[0]; }
		public virtual int[] Triangles(Faces face, int offset) { return new int[0]; }
		public virtual Vector3[] Normals(Faces face) {	return new Vector3[0]; }
	}

	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//

	public class Cube : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)
		{
			List<Faces> faces = new List<Faces>();
			if(getAll || exposedFaces[(int)Shapes.CubeFace.TOP])		faces.Add(Faces.TOP);
			if(getAll || exposedFaces[(int)Shapes.CubeFace.BOTTOM])		faces.Add(Faces.BOTTOM);
			if(getAll || exposedFaces[(int)Shapes.CubeFace.FRONT])		faces.Add(Faces.FRONT);
			if(getAll || exposedFaces[(int)Shapes.CubeFace.BACK])		faces.Add(Faces.BACK);
			if(getAll || exposedFaces[(int)Shapes.CubeFace.RIGHT])		faces.Add(Faces.RIGHT);
			if(getAll || exposedFaces[(int)Shapes.CubeFace.LEFT])		faces.Add(Faces.LEFT);
			return faces;	
		}

		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{	
			switch(face)
			{
				case Faces.TOP: 	return new Vector3[] {v7+offset, v6+offset, v5+offset, v4+offset};
				case Faces.BOTTOM: 	return new Vector3[] {v0+offset, v1+offset, v2+offset, v3+offset};
				case Faces.RIGHT: 	return new Vector3[] {v5+offset, v6+offset, v2+offset, v1+offset};
				case Faces.LEFT: 	return new Vector3[] {v7+offset, v4+offset, v0+offset, v3+offset};
				case Faces.FRONT: 	return new Vector3[] {v4+offset, v5+offset, v1+offset, v0+offset};
				case Faces.BACK: 	return new Vector3[] {v6+offset, v7+offset, v3+offset, v2+offset};
				default: 			return null;
			}		
		}

		public override int[] Triangles(Faces face, int offset)
		{
			return new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.TOP: return new Vector3[] {Vector3.up,Vector3.up,Vector3.up,Vector3.up};
				case Faces.BOTTOM: return new Vector3[] {Vector3.down,Vector3.down,Vector3.down,Vector3.down};				
				case Faces.RIGHT: return new Vector3[] {Vector3.right,Vector3.right,Vector3.right,Vector3.right};
				case Faces.LEFT: return new Vector3[] {Vector3.left,Vector3.left,Vector3.left,Vector3.left};
				case Faces.FRONT: return new Vector3[] {Vector3.forward,Vector3.forward,Vector3.forward,Vector3.forward};
				case Faces.BACK: return new Vector3[] {Vector3.back,Vector3.back,Vector3.back,Vector3.back};
				default: return null;
			}

		}
	}

	public class Wedge : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)
		{
			List<Faces> faces = new List<Faces>();
			if(getAll || TopFront(exposedFaces, rotation))	faces.Add(Faces.FRONT);
			if(getAll || Left(exposedFaces, rotation))		faces.Add(Faces.RIGHT);
			if(getAll || Right(exposedFaces, rotation))		faces.Add(Faces.LEFT);
			return faces;	
		}
				
		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{ 
			switch(face)
			{
				case Faces.FRONT: 	return new Vector3[] {v7+offset, v6+offset, v1+offset, v0+offset};
				case Faces.RIGHT: 	return new Vector3[] {v6+offset, v2+offset, v1+offset};
				case Faces.LEFT: 	return new Vector3[] {v7+offset, v0+offset, v3+offset};
				default: 			return null;
			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.FRONT: 	return Enumerable.Repeat(Vector3.up + Vector3.forward, 4).ToArray();
				case Faces.RIGHT: 	return Enumerable.Repeat(Vector3.right, 3).ToArray();
				case Faces.LEFT: 	return Enumerable.Repeat(Vector3.left, 3).ToArray();
				default: 			return null;

			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.FRONT: 	return new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
				case Faces.RIGHT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.LEFT: 	return new int[] {2+offset, 1+offset, 0+offset};
				default: 			return null;
			}
		}
	}

	public  class CornerIn : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)
		{		
			List<Faces> faces = new List<Faces>();
			faces.Add(Faces.FRONT);
			if(getAll || Top(exposedFaces, rotation))	faces.Add(Faces.TOP);
			return faces;
		}

		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{
			switch(face)
			{
				case Faces.FRONT: return new Vector3[] {v4+offset, v6+offset, v1+offset};
				case Faces.TOP: return new Vector3[] {v7+offset, v6+offset, v4+offset};
				default: return null;
			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.FRONT: return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.TOP: return new int[] {2+offset, 1+offset, 0+offset};				
				default: return null;
			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.FRONT: return Enumerable.Repeat(Vector3.up + Vector3.forward + Vector3.right, 3).ToArray();
				case Faces.TOP: return Enumerable.Repeat(Vector3.up, 3).ToArray();
				default: return null;
			}
		}
	}

	public  class CornerOut : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)
		{
			List<Faces> faces = new List<Faces>();
			if(getAll || TopFrontRight(exposedFaces, rotation))	faces.Add(Faces.FRONT);
			if(getAll || !Bottom(exposedFaces, rotation))			faces.Add(Faces.BOTTOM);
			return faces;
		}	

		public override  Vector3[] Vertices(Faces face, Vector3 offset)
		{
			switch(face)
			{
				case Faces.FRONT: return new Vector3[] {v7+offset, v2+offset, v0+offset};
				case Faces.BOTTOM: return new Vector3[] {v0+offset, v1+offset, v2+offset};
				case Faces.LEFT: return new Vector3[] {v7+offset, v0+offset, v4+offset};
				default: return null;
			}
		}

		public override  int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.FRONT: return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.BOTTOM: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.LEFT: return new int[] {2+offset, 1+offset, 0+offset};				
				default: return null;
			}
		}

		public override  Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.FRONT: return Enumerable.Repeat(Vector3.up + Vector3.forward + Vector3.right, 3).ToArray();
				case Faces.BOTTOM: return Enumerable.Repeat(Vector3.up, 3).ToArray();
				case Faces.LEFT: return Enumerable.Repeat(Vector3.right, 3).ToArray();
				default: return null;
			}
		}
	}

	public  class Outcrop : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)		 
		{
			List<Faces> faces = new List<Faces>();

			if(getAll || TopFront(exposedFaces, rotation))
			{
												faces.Add(Faces.RSLOPE);
												faces.Add(Faces.LSLOPE);
												faces.Add(Faces.TOP);
			}
			if(getAll || Right(exposedFaces, rotation))	faces.Add(Faces.RIGHT);
			if(getAll || Left(exposedFaces, rotation))	faces.Add(Faces.LEFT);
			if(getAll || Front(exposedFaces, rotation))	faces.Add(Faces.FRONT);
			if(getAll || Back(exposedFaces, rotation))	faces.Add(Faces.BACK);
			if(getAll || Bottom(exposedFaces, rotation))	faces.Add(Faces.BOTTOM);
			return faces;
		}

		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{
			switch(face)
			{
				case Faces.RSLOPE: return new Vector3[] {v1+offset, v6+offset, v9+offset};
				case Faces.LSLOPE: return new Vector3[] {v9+offset, v7+offset, v0+offset};
				case Faces.RIGHT: return new Vector3[] {v2+offset, v6+offset, v1+offset};
				case Faces.LEFT: return new Vector3[] {v0+offset, v7+offset, v3+offset};
				case Faces.FRONT: return new Vector3[] {v0+offset, v1+offset, v9+offset};				
				case Faces.BACK: return new Vector3[] {v6+offset, v7+offset, v3+offset, v2+offset};				
				case Faces.TOP: return new Vector3[] {v7+offset, v9+offset, v6+offset};				
				case Faces.BOTTOM: return new Vector3[] {v0+offset, v1+offset, v2+offset, v3+offset};
				default: return null;			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.RSLOPE: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.LSLOPE: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.RIGHT: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.LEFT: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.FRONT: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.BACK: return new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
				case Faces.TOP: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.BOTTOM: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
				default: return null;			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.RSLOPE: return Enumerable.Repeat(Vector3.up + (Vector3.forward / 2) + Vector3.right, 3).ToArray();
				case Faces.LSLOPE: return Enumerable.Repeat(Vector3.up + (Vector3.forward / 2) + Vector3.left, 3).ToArray();
				case Faces.RIGHT: return Enumerable.Repeat(Vector3.right, 3).ToArray();
				case Faces.LEFT: return Enumerable.Repeat(Vector3.left, 3).ToArray();
				case Faces.FRONT: return Enumerable.Repeat(Vector3.forward, 3).ToArray();
				case Faces.BACK: return Enumerable.Repeat(Vector3.back, 4).ToArray();
				case Faces.TOP: return Enumerable.Repeat(Vector3.up + (Vector3.forward / 2), 3).ToArray();
				case Faces.BOTTOM: return Enumerable.Repeat(Vector3.down, 4).ToArray();
				default: return null;
			}
		}
	}

	public class Strip : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)		 		
		{
			List<Faces> faces = new List<Faces>();
			faces.Add(Faces.RIGHT);
			faces.Add(Faces.LEFT);
			if(getAll || Back(exposedFaces, rotation))		faces.Add(Faces.BACK);
			if(getAll || Front(exposedFaces, rotation))		faces.Add(Faces.FRONT);
			if(getAll || Bottom(exposedFaces, rotation))	faces.Add(Faces.BOTTOM);
			return faces;			
		}

		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{
			switch(face)
			{
				case Faces.RIGHT: return new Vector3[] {v0+offset, v9+offset, v8+offset, v3+offset};
				case Faces.LEFT: return new Vector3[] {v9+offset, v1+offset, v2+offset, v8+offset};				
				case Faces.FRONT: return new Vector3[] {v0+offset, v1+offset, v9+offset};
				case Faces.BACK: return new Vector3[] {v8+offset, v2+offset, v3+offset};
				case Faces.BOTTOM: return new Vector3[] {v3+offset, v2+offset, v1+offset, v0+offset};
				default: return null;
			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.RIGHT: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
				case Faces.LEFT: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
				case Faces.FRONT: return new int[] {0+offset, 1+offset, 2+offset};				
				case Faces.BACK: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.BOTTOM: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};				
				default: return null;
			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.RIGHT: return Enumerable.Repeat(Vector3.right + Vector3.up, 4).ToArray();				
				case Faces.LEFT: return Enumerable.Repeat(Vector3.left + Vector3.up, 4).ToArray();
				case Faces.FRONT: return Enumerable.Repeat(Vector3.forward, 3).ToArray();				
				case Faces.BACK: return Enumerable.Repeat(Vector3.back, 3).ToArray();
				case Faces.BOTTOM: return Enumerable.Repeat(Vector3.down, 4).ToArray();
				default: return null;
			}
		}
	}

	public class StripEnd : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)		 				
		{
			List<Faces> faces = new List<Faces>();
			faces.Add(Faces.RIGHT);
			faces.Add(Faces.LEFT);
			faces.Add(Faces.FRONT);
			if(getAll || Back(exposedFaces, rotation))		faces.Add(Faces.BACK);
			if(getAll || Bottom(exposedFaces, rotation))	faces.Add(Faces.BOTTOM);
			return faces;		
		}
		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{
			switch(face)
			{
				case Faces.RIGHT: return new Vector3[] {v0+offset, v12+offset, v8+offset, v3+offset};
				case Faces.LEFT: return new Vector3[] {v12+offset, v1+offset, v2+offset, v8+offset};				
				case Faces.FRONT: return new Vector3[] {v0+offset, v1+offset, v12+offset};
				case Faces.BACK: return new Vector3[] {v8+offset, v2+offset, v3+offset};
				case Faces.BOTTOM: return new Vector3[] {v3+offset, v2+offset, v1+offset, v0+offset};
				default: return null;
			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.RIGHT: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
				case Faces.LEFT: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
				case Faces.FRONT: return new int[] {0+offset, 1+offset, 2+offset};				
				case Faces.BACK: return new int[] {0+offset, 1+offset, 2+offset};
				case Faces.BOTTOM: return new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};				
				default: return null;
			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.RIGHT: return Enumerable.Repeat(Vector3.right + Vector3.up, 4).ToArray();				
				case Faces.LEFT: return Enumerable.Repeat(Vector3.left + Vector3.up, 4).ToArray();
				case Faces.FRONT: return Enumerable.Repeat(Vector3.forward, 3).ToArray();				
				case Faces.BACK: return Enumerable.Repeat(Vector3.back, 3).ToArray();
				case Faces.BOTTOM: return Enumerable.Repeat(Vector3.down, 4).ToArray();
				default: return null;
			}
		}
	}

	public class WedgeCorner : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)
		{
			List<Faces> faces = new List<Faces>();
			faces.Add(Faces.TOP);
			if(getAll || TopFront(exposedFaces, rotation))	faces.Add(Faces.FRONT);
			if(getAll || Left(exposedFaces, rotation))		faces.Add(Faces.RIGHT);
			if(getAll || Right(exposedFaces, rotation))		faces.Add(Faces.LEFT);
			return faces;	
		}
				
		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{ 
			switch(face)
			{
				case Faces.FRONT: 	return new Vector3[] {v6+offset, v1+offset, v0+offset};
				case Faces.RIGHT: 	return new Vector3[] {v6+offset, v2+offset, v1+offset};
				case Faces.LEFT: 	return new Vector3[] {v7+offset, v0+offset, v3+offset};
				case Faces.TOP: 	return new Vector3[] {v6+offset, v0+offset, v4+offset};
				default: 			return null;
			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.FRONT: 	return Enumerable.Repeat(Vector3.up + Vector3.forward, 3).ToArray();
				case Faces.RIGHT: 	return Enumerable.Repeat(Vector3.right, 3).ToArray();
				case Faces.LEFT: 	return Enumerable.Repeat(Vector3.left, 3).ToArray();
				case Faces.TOP: 	return Enumerable.Repeat(Vector3.forward + Vector3.right, 3).ToArray();
				default: 			return null;

			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.FRONT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.RIGHT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.LEFT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.TOP: 	return new int[] {2+offset, 1+offset, 0+offset};
				default: 			return null;
			}
		}
	}

	public class WedgeCornerInverse : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, int rotation, bool getAll = false)
		{
			List<Faces> faces = new List<Faces>();
			faces.Add(Faces.TOP);
			if(getAll || TopFront(exposedFaces, rotation))	faces.Add(Faces.FRONT);
			if(getAll || Left(exposedFaces, rotation))		faces.Add(Faces.RIGHT);
			if(getAll || Right(exposedFaces, rotation))		faces.Add(Faces.LEFT);
			return faces;	
		}
				
		public override Vector3[] Vertices(Faces face, Vector3 offset)
		{ 
			switch(face)
			{
				case Faces.FRONT: 	return new Vector3[] {v1+offset, v0+offset, v7+offset};
				case Faces.RIGHT: 	return new Vector3[] {v6+offset, v2+offset, v1+offset};
				case Faces.LEFT: 	return new Vector3[] {v7+offset, v0+offset, v3+offset};
				case Faces.TOP: 	return new Vector3[] {v5+offset, v1+offset, v7+offset};
				default: 			return null;
			}
		}

		public override Vector3[] Normals(Faces face)
		{
			switch(face)
			{
				case Faces.FRONT: 	return Enumerable.Repeat(Vector3.up + Vector3.forward, 3).ToArray();
				case Faces.RIGHT: 	return Enumerable.Repeat(Vector3.right, 3).ToArray();
				case Faces.LEFT: 	return Enumerable.Repeat(Vector3.left, 3).ToArray();
				case Faces.TOP: 	return Enumerable.Repeat(Vector3.forward + Vector3.left, 3).ToArray();
				default: 			return null;

			}
		}

		public override int[] Triangles(Faces face, int offset)
		{
			switch(face)
			{
				case Faces.FRONT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.RIGHT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.LEFT: 	return new int[] {2+offset, 1+offset, 0+offset};
				case Faces.TOP: 	return new int[] {2+offset, 1+offset, 0+offset};
				default: 			return null;
			}
		}
	}
	
	//	Choose which shape a block has based on which adjacent blocks are see through
	public static void SetSlopes(Chunk chunk, Vector3 voxel)
	{
		int x = (int)voxel.x;
		int y = (int)voxel.y;
		int z = (int)voxel.z;

		//if(chunk.blockTypes[x,y,z] != Blocks.Types.LIGHTGRASS) return;

		switch(chunk.blockBytes[x,y,z])
		{
			//	CORNER OUT

			case 53:
			case 21:
			case 85:
			case 117:
			case 100:
			case 101:
			case 97:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;

			case 57:
			case 41:
			case 169:
			case 185:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = (int)Rotate.RIGHT;
				break;

			case 202:
			case 138:
			case 170:
			case 234:
			case 104:
			case 106:
			case 177:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = (int)Rotate.BACK;
				break;

			case 70:
			case 86:
			case 214:
			case 82:
			case 198:
			case 98:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = (int)Rotate.LEFT;
				break;

			//	CORNER IN
			
			case 16:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;

			case 32:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = (int)Rotate.RIGHT;
				break;

			case 128:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = (int)Rotate.BACK;
				break;

			case 64:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = (int)Rotate.LEFT;
				break;

			//	OUTCROP
			
			case 80:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;

			case 48:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = (int)Rotate.RIGHT;
				break;

			case 160:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = (int)Rotate.BACK;
				break;

			case 192:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = (int)Rotate.LEFT;
				break;

			//  STRIP
			
			case 243:
			case 163:
			case 83:
				chunk.blockShapes[x,y,z] = Types.STRIP;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;

			case 252:
			case 60:
			case 204:
				chunk.blockShapes[x,y,z] = Types.STRIP;
				chunk.blockYRotation[x,y,z] = (int)Rotate.RIGHT;
				break;

			//	STRIPEND
			
			case 247:
			case 87:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;
			
			case 61:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = (int)Rotate.RIGHT;
				break;

			case 251:
			case 171:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = (int)Rotate.BACK;
				break;

			case 254:
			case 206:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = (int)Rotate.LEFT;
				break;

			//	WEDGE

			case 20:
			case 84:
			case 68:
			case 4:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;

			case 49:
			case 17:
			case 33:
			case 1:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = (int)Rotate.RIGHT;
				break;

			case 168:
			case 40:
			case 8:
			case 136:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = (int)Rotate.BACK;
				break;

			case 194:
			case 2:
			case 130:
			case 66:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = (int)Rotate.LEFT;
				break;

			//	CUBE

			case 0:
				chunk.blockShapes[x,y,z] = Types.CUBE;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;

			//	DELETE
			case 127:
			case 111:
			case 239:
			case 78:
			case 255:
			case 187:
				chunk.blockTypes[x,y,z] = Blocks.Types.AIR;
				break;

			default:
				chunk.blockShapes[x,y,z] = Types.CUBE;
				chunk.blockYRotation[x,y,z] = (int)Rotate.FRONT;
				break;
		}
	}

	#region Misc

	//	Check which faces of a shape are exposed
	//  adjusting for the shape's rotation by applying rotation index to the CubeFace enum

	static bool TopFront(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotationIndex)] ||
				exposedFaces[(int)RotateFace(CubeFace.FRONT, rotationIndex)]);
	}

	static bool TopFrontRight(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotationIndex)]   ||
				exposedFaces[(int)RotateFace(CubeFace.FRONT, rotationIndex)] ||
				exposedFaces[(int)RotateFace(CubeFace.RIGHT, rotationIndex)]);
	}

	static bool TopFrontLeft(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotationIndex)]   ||
				exposedFaces[(int)RotateFace(CubeFace.FRONT, rotationIndex)] ||
				exposedFaces[(int)RotateFace(CubeFace.LEFT, rotationIndex)]);
	}

	static bool Right(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.RIGHT, rotationIndex)]);
	}

	static bool Left(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.LEFT, rotationIndex)]);
	}

	static bool Front(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.FRONT, rotationIndex)]);
	}

	static bool Back(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.BACK, rotationIndex)]);
	}

	static bool Top(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)CubeFace.TOP]);
	}

	static bool Bottom(bool[] exposedFaces, int rotationIndex)
	{
		return (exposedFaces[(int)CubeFace.BOTTOM]);
	}

	//	Return cube face facing direction
	public static CubeFace DirectionToFace(Vector3 direction)
	{
		if(direction == Vector3.up) return CubeFace.TOP;
		if(direction == Vector3.down) return CubeFace.BOTTOM;
		if(direction == Vector3.right) return CubeFace.RIGHT;
		if(direction == Vector3.left) return CubeFace.LEFT;
		if(direction == Vector3.forward) return CubeFace.FRONT;
		if(direction == Vector3.back) return CubeFace.BACK;
		else Debug.Log("BAD FACE"); return CubeFace.TOP;
	}

	#endregion

    #region Rotation and offset

	static Vector3[] OffsetVectors(Vector3[] vectors, Vector3 offset)
	{
		//	Apply adjusted values to new array to avoid editing the original shape
		Vector3[] adjustedVectors = new Vector3[vectors.Length];
		for(int i = 0; i < vectors.Length; i++)
		{
			adjustedVectors[i] = offset + vectors[i];
		}
		return adjustedVectors;
	}

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

	//	Adjust face enum using rotation index
	//	enum order is effectively clockwise in 90 degree increments
	static CubeFace RotateFace(CubeFace face, int rotationIndex)
	{
		//	Convert to Vector3 direction, rotate then convert back to face
		//return DirectionToFace(RotateVector(FaceToDirection(face), Vector3.zero, rotation));

		float rotationIncrement = rotationIndex + (int)face;

		float finaRotation = rotationIncrement > 4 ? rotationIncrement - 4 : rotationIncrement;

		return (CubeFace)finaRotation;
	}

	#endregion
}
