using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Shapes
{
	
	public enum Rotate { FRONT = 0, RIGHT = 90, BACK = 180, LEFT = 270 }
	public enum Types {CUBE, WEDGE, CORNERIN, CORNEROUT, OUTCROP, STRIP, STRIPEND}

	//	For specifying which face of a shape is being worked on
	public enum CubeFace {TOP, BOTTOM, RIGHT, LEFT, FRONT, BACK}
	public enum WedgeFace {SLOPE, BOTTOM, LEFT, RIGHT, BACK}
	public enum CornerOutFace {SLOPE, BOTTOM, LEFT}
	public enum CornerInFace {SLOPE, TOP}
	public enum OutcropFace {RSLOPE, LSLOPE, RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum StripFace {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum StripEndFace {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum LumpFace {RIGHT, LEFT, FRONT, BACK, BOTTOM}


	// Coordinates for 1x1 cube vertices relative to center	
	readonly static Vector3 v0 = new Vector3( 	-0.5f, -0.5f,	 0.5f );	//	left bottom front
	readonly static Vector3 v2 = new Vector3( 	 0.5f, -0.5f,	-0.5f );	//	right bottom back
	readonly static Vector3 v3 = new Vector3( 	-0.5f, -0.5f,	-0.5f ); 	//	left bottom back
	readonly static Vector3 v1 = new Vector3( 	 0.5f, -0.5f,	 0.5f );	//	right bottom front
	readonly static Vector3 v4 = new Vector3( 	-0.5f,  0.5f,	 0.5f );	//	left top front
	readonly static Vector3 v5 = new Vector3( 	 0.5f,  0.5f,	 0.5f );	//	right top front
	readonly static Vector3 v6 = new Vector3( 	 0.5f,  0.5f,	-0.5f );	//	right top back
	readonly static Vector3 v7 = new Vector3( 	-0.5f,  0.5f,	-0.5f );	//	left top back
	
	readonly static Vector3 v8 = new Vector3(  	0f, 	0f,		-0.5f);		//	middle back
	readonly static Vector3 v9 = new Vector3(  	0f, 	0f,		 0.5f);		//	middle front
	readonly static Vector3 v10 = new Vector3( 	0.5f,	0f,		 0f);		//	middle right
	readonly static Vector3 v11 = new Vector3( -0.5f,	0f,		 0f);		//	middle left
	readonly static Vector3 v12 = new Vector3( 	0f, 	0f,		 0f);		//	middle
	
	public class Cube
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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

		public static Vector3[] Vertices(CubeFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case CubeFace.TOP: vertices = new Vector3[] {v7+offset, v6+offset, v5+offset, v4+offset};
					break;

				case CubeFace.BOTTOM: vertices = new Vector3[] {v0+offset, v1+offset, v2+offset, v3+offset};
					break;

				case CubeFace.RIGHT: vertices = new Vector3[] {v5+offset, v6+offset, v2+offset, v1+offset};
					break;

				case CubeFace.LEFT: vertices = new Vector3[] {v7+offset, v4+offset, v0+offset, v3+offset};
					break;

				case CubeFace.FRONT: vertices = new Vector3[] {v4+offset, v5+offset, v1+offset, v0+offset};
					break;
				
				case CubeFace.BACK: vertices = new Vector3[] {v6+offset, v7+offset, v3+offset, v2+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;
		}

		public static int[] Triangles(CubeFace face, int offset)
		{
			return new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
		}

		public static Vector3[] Normals(CubeFace face)
		{
			Vector3[] normals;
			
			switch(face)
			{
				case CubeFace.TOP: normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
					break;

				case CubeFace.BOTTOM: normals = Enumerable.Repeat(Vector3.down, 4).ToArray();
					break;

				case CubeFace.RIGHT: normals = Enumerable.Repeat(Vector3.right, 4).ToArray();
					break;

				case CubeFace.LEFT: normals = Enumerable.Repeat(Vector3.left, 4).ToArray();
					break;

				case CubeFace.FRONT: normals = Enumerable.Repeat(Vector3.forward, 4).ToArray();
					break;

				case CubeFace.BACK: normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
					break;

				default: normals = null;
					break;
			}

			return normals;
		}
	}

	public static class Wedge
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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
		
		public static Vector3[] Vertices(WedgeFace face, Vector3 offset)
		{ 

			Vector3[] vertices;
	
			switch(face)
			{
				case WedgeFace.SLOPE: vertices = new Vector3[] {v7+offset, v6+offset, v1+offset, v0+offset};
					break;

				case WedgeFace.RIGHT: vertices = new Vector3[] {v6+offset, v2+offset, v1+offset};
					break;

				case WedgeFace.LEFT: vertices = new Vector3[] {v7+offset, v0+offset, v3+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;

			}

		public static Vector3[] Normals(WedgeFace face)
		{
			Vector3[] normals;
	
			switch(face)
			{
				case WedgeFace.SLOPE: normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
					break;

				case WedgeFace.RIGHT: normals = Enumerable.Repeat(Vector3.right, 3).ToArray();
					break;

				case WedgeFace.LEFT: normals = Enumerable.Repeat(Vector3.left, 3).ToArray();
					break;

				default: normals = null;
					break;
			}
					
			return normals;
		}

		public static int[] Triangles(WedgeFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case WedgeFace.SLOPE: triangles = new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
					break;

				case WedgeFace.RIGHT: triangles = new int[] {2+offset, 1+offset, 0+offset};
					break;

				case WedgeFace.LEFT: triangles = new int[] {2+offset, 1+offset, 0+offset};
					break;

				default: triangles = null;
					break;
			}
					
			return triangles;
		}
	}

	public static class CornerOut
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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

		public static Vector3[] Vertices(CornerOutFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case CornerOutFace.SLOPE: vertices = new Vector3[] {v7+offset, v2+offset, v0+offset};
					break;

				case CornerOutFace.BOTTOM: vertices = new Vector3[] {v0+offset, v1+offset, v2+offset};
					break;

				case CornerOutFace.LEFT: vertices = new Vector3[] {v7+offset, v0+offset, v4+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;
		}

		public static int[] Triangles(CornerOutFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case CornerOutFace.SLOPE: triangles = new int[] {2+offset, 1+offset, 0+offset};
					break;

				case CornerOutFace.BOTTOM: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case CornerOutFace.LEFT: triangles = new int[] {2+offset, 1+offset, 0+offset};
					break;
				
				default: triangles = null;
					break;
			}
			return triangles;
		}

		public static Vector3[] Normals(CornerOutFace face)
		{
			Vector3[] normals;
			
			switch(face)
			{
				case CornerOutFace.SLOPE: normals = Enumerable.Repeat(Vector3.up + Vector3.forward + Vector3.right, 3).ToArray();
					break;

				case CornerOutFace.BOTTOM: normals = Enumerable.Repeat(Vector3.up, 3).ToArray();
					break;

				case CornerOutFace.LEFT: normals = Enumerable.Repeat(Vector3.right, 3).ToArray();
					break;

				default: normals = null;
					break;
			}

			return normals;
		}
	}

	public static class CornerIn
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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
		public static Vector3[] Vertices(CornerInFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case CornerInFace.SLOPE: vertices = new Vector3[] {v4+offset, v6+offset, v1+offset};
					break;

				case CornerInFace.TOP: vertices = new Vector3[] {v7+offset, v6+offset, v4+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;
		}

		public static int[] Triangles(CornerInFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case CornerInFace.SLOPE: triangles = new int[] {2+offset, 1+offset, 0+offset};
					break;

				case CornerInFace.TOP: triangles = new int[] {2+offset, 1+offset, 0+offset};
					break;
				
				default: triangles = null;
					break;
			}
			return triangles;
		}

		public static Vector3[] Normals(CornerInFace face)
		{
			Vector3[] normals;
			
			switch(face)
			{
				case CornerInFace.SLOPE: normals = Enumerable.Repeat(Vector3.up + Vector3.forward + Vector3.right, 3).ToArray();
					break;

				case CornerInFace.TOP: normals = Enumerable.Repeat(Vector3.up, 3).ToArray();
					break;

				default: normals = null;
					break;
			}

			return normals;
		}
	}

	public static class Outcrop
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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

		public static Vector3[] Vertices(OutcropFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case OutcropFace.RSLOPE: vertices = new Vector3[] {v1+offset, v6+offset, v9+offset};
					break;

				case OutcropFace.LSLOPE: vertices = new Vector3[] {v9+offset, v7+offset, v0+offset};
					break; 

				case OutcropFace.RIGHT: vertices = new Vector3[] {v2+offset, v6+offset, v1+offset};
					break; 

				case OutcropFace.LEFT: vertices = new Vector3[] {v0+offset, v7+offset, v3+offset};
					break;

				case OutcropFace.FRONT: vertices = new Vector3[] {v0+offset, v1+offset, v9+offset};
					break; 
				
				case OutcropFace.BACK: vertices = new Vector3[] {v6+offset, v7+offset, v3+offset, v2+offset};
					break;
				
				case OutcropFace.TOP: vertices = new Vector3[] {v7+offset, v9+offset, v6+offset};
					break;
				
				case OutcropFace.BOTTOM: vertices = new Vector3[] {v0+offset, v1+offset, v2+offset, v3+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;
		}

		public static int[] Triangles(OutcropFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case OutcropFace.RSLOPE: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case OutcropFace.LSLOPE: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case OutcropFace.RIGHT: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case OutcropFace.LEFT: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case OutcropFace.FRONT: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case OutcropFace.BACK: triangles = new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
					break;

				case OutcropFace.TOP: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case OutcropFace.BOTTOM: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;


				default: triangles = null;
					break;
			}
			return triangles;
		}

		public static Vector3[] Normals(OutcropFace face)
		{
			Vector3[] normals;
			
			switch(face)
			{
				case OutcropFace.RSLOPE: normals = Enumerable.Repeat(Vector3.up + (Vector3.forward / 2) + Vector3.right, 3).ToArray();
					break;

				case OutcropFace.LSLOPE: normals = Enumerable.Repeat(Vector3.up + (Vector3.forward / 2) + Vector3.left, 3).ToArray();
					break;

				case OutcropFace.RIGHT: normals = Enumerable.Repeat(Vector3.right, 3).ToArray();
					break;

				case OutcropFace.LEFT: normals = Enumerable.Repeat(Vector3.left, 3).ToArray();
					break;

				case OutcropFace.FRONT: normals = Enumerable.Repeat(Vector3.forward, 3).ToArray();
					break;

				case OutcropFace.BACK: normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
					break;

				case OutcropFace.TOP: normals = Enumerable.Repeat(Vector3.up + (Vector3.forward / 2), 3).ToArray();
					break;

				case OutcropFace.BOTTOM: normals = Enumerable.Repeat(Vector3.down, 4).ToArray();
					break;

				

				default: normals = null;
					break;
			}

			return normals;
		}
	}

	public static class Strip
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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

		public static Vector3[] Vertices(StripFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case StripFace.RIGHT: vertices = new Vector3[] {v0+offset, v9+offset, v8+offset, v3+offset};
					break;

				case StripFace.LEFT: vertices = new Vector3[] {v9+offset, v1+offset, v2+offset, v8+offset};
					break;
				
				case StripFace.FRONT: vertices = new Vector3[] {v0+offset, v1+offset, v9+offset};
					break;

				case StripFace.BACK: vertices = new Vector3[] {v8+offset, v2+offset, v3+offset};
					break;

				case StripFace.BOTTOM: vertices = new Vector3[] {v3+offset, v2+offset, v1+offset, v0+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;
		}

		public static int[] Triangles(StripFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case StripFace.RIGHT: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;

				case StripFace.LEFT: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;

				case StripFace.FRONT: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;
				
				case StripFace.BACK: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case StripFace.BOTTOM: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;
				
				default: triangles = null;
					break;
			}
			return triangles;
		}

		public static Vector3[] Normals(StripFace face)
		{
			Vector3[] normals;
			
			switch(face)
			{
				case StripFace.RIGHT: normals = Enumerable.Repeat(Vector3.right + Vector3.up, 4).ToArray();
					break;
				
				case StripFace.LEFT: normals = Enumerable.Repeat(Vector3.left + Vector3.up, 4).ToArray();
					break;

				case StripFace.FRONT: normals = Enumerable.Repeat(Vector3.forward, 3).ToArray();
					break;
				
				case StripFace.BACK: normals = Enumerable.Repeat(Vector3.back, 3).ToArray();
					break;

				case StripFace.BOTTOM: normals = Enumerable.Repeat(Vector3.down, 4).ToArray();
					break;

				default: normals = null;
					break;
			}

			return normals;
		}
	}

	public static class StripEnd
	{
		static int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
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
		public static Vector3[] Vertices(StripEndFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case StripEndFace.RIGHT: vertices = new Vector3[] {v0+offset, v12+offset, v8+offset, v3+offset};
					break;

				case StripEndFace.LEFT: vertices = new Vector3[] {v12+offset, v1+offset, v2+offset, v8+offset};
					break;
				
				case StripEndFace.FRONT: vertices = new Vector3[] {v0+offset, v1+offset, v12+offset};
					break;

				case StripEndFace.BACK: vertices = new Vector3[] {v8+offset, v2+offset, v3+offset};
					break;

				case StripEndFace.BOTTOM: vertices = new Vector3[] {v3+offset, v2+offset, v1+offset, v0+offset};
					break;

				default: vertices = null;
					break;
			}
					
			return vertices;
		}

		public static int[] Triangles(StripEndFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case StripEndFace.RIGHT: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;

				case StripEndFace.LEFT: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;

				case StripEndFace.FRONT: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;
				
				case StripEndFace.BACK: triangles = new int[] {0+offset, 1+offset, 2+offset};
					break;

				case StripEndFace.BOTTOM: triangles = new int[] {0+offset, 1+offset, 2+offset, 0+offset, 2+offset, 3+offset};
					break;
				
				default: triangles = null;
					break;
			}
			return triangles;
		}

		public static Vector3[] Normals(StripEndFace face)
		{
			Vector3[] normals;
			
			switch(face)
			{
				case StripEndFace.RIGHT: normals = Enumerable.Repeat(Vector3.right + Vector3.up, 4).ToArray();
					break;
				
				case StripEndFace.LEFT: normals = Enumerable.Repeat(Vector3.left + Vector3.up, 4).ToArray();
					break;

				case StripEndFace.FRONT: normals = Enumerable.Repeat(Vector3.forward, 3).ToArray();
					break;
				
				case StripEndFace.BACK: normals = Enumerable.Repeat(Vector3.back, 3).ToArray();
					break;

				case StripEndFace.BOTTOM: normals = Enumerable.Repeat(Vector3.down, 4).ToArray();
					break;

				default: normals = null;
					break;
			}

			return normals;
		}
	}
	
	//	Choose which shape a block has based on which adjacent blocks are see through
	public static void SetSlopes(Chunk chunk, Vector3 voxel)
	{
		int x = (int)voxel.x;
		int y = (int)voxel.y;
		int z = (int)voxel.z;

		if(chunk.blockTypes[x,y,z] != Blocks.Types.DIRT) return;

		switch(chunk.blockBytes[x,y,z])
		{
			//	CORNER OUT

			case 53:
			case 21:
			case 85:
			case 117:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 57:
			case 41:
			case 169:
			case 185:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 202:
			case 138:
			case 170:
			case 234:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 70:
			case 86:
			case 214:
			case 82:
			case 198:
				chunk.blockShapes[x,y,z] = Types.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	CORNER IN

			case 16:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 32:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 128:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 64:
				chunk.blockShapes[x,y,z] = Types.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	OUTCROP

			case 80:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 48:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 160:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 192:
				chunk.blockShapes[x,y,z] = Types.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//  STRIP
			case 243:
			case 163:
			case 83:
				chunk.blockShapes[x,y,z] = Types.STRIP;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 252:
			case 60:
			case 204:
				chunk.blockShapes[x,y,z] = Types.STRIP;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			//	STRIPEND
			
			case 247:
			case 87:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;
			
			case 61:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 251:
			case 171:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 254:
			case 206:
				chunk.blockShapes[x,y,z] = Types.STRIPEND;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	WEDGE

			case 20:
			case 84:
			case 68:
			case 4:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 49:
			case 17:
			case 33:
			case 1:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 168:
			case 40:
			case 8:
			case 136:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 194:
			case 2:
			case 130:
			case 66:
				chunk.blockShapes[x,y,z] = Types.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	CUBE

			case 0:
				chunk.blockShapes[x,y,z] = Types.CUBE;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			default:
				chunk.blockShapes[x,y,z] = Types.CUBE;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;
		}
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
