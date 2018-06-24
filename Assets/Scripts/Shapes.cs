using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Shapes
{
	public enum Types {CUBE, WEDGE, CORNERIN, CORNEROUT, OUTCROP, STRIP, STRIPEND}
	public enum Rotate { FRONT = 0, RIGHT = 90, BACK = 180, LEFT = 270 }
	
	public enum CubeFace {TOP, BOTTOM, RIGHT, LEFT, FRONT, BACK}
	public enum Faces {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM, LSLOPE, RSLOPE}

	//	For specifying which face of a shape is being worked on
	/* public enum WedgeFace {SLOPE, BOTTOM, LEFT, RIGHT, BACK}	//
	public enum CornerOutFace {SLOPE, BOTTOM, LEFT}
	public enum CornerInFace {SLOPE, TOP}
	public enum OutcropFace {RSLOPE, LSLOPE, RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
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
	
	public class Shape
	{
		public virtual List<Faces> GetFaces(bool[] exposedFaces, Quaternion rotation)
		{ return new List<Faces>(); }

		public int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			Quaternion rotation,	bool[] exposedFaces, 	int vertCount)
		{
			List<Faces> faces = GetFaces(exposedFaces, rotation);
			int localVertCount = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				Vector3[] verts = Vertices(faces[i], position);

				vertices.AddRange(	RotateVectors(	verts,
													position,
													rotation));

				normals.AddRange(	RotateNormals(	Normals(faces[i]),
													rotation));

				triangles.AddRange(					Triangles(faces[i],
													vertCount + localVertCount));

				localVertCount += verts.Length;
			}
			return localVertCount;
		}

		public virtual Vector3[] Vertices(Faces face, Vector3 offset) {	return new Vector3[0]; }
		public virtual int[] Triangles(Faces face, int offset) { return new int[0]; }
		public virtual Vector3[] Normals(Faces face) {	return new Vector3[0]; }
	}

	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//	//

	public class Cube : Shape
	{
		public override List<Faces> GetFaces(bool[] exposedFaces, Quaternion rotation)
		{
			List<Faces> faces = new List<Faces>();
			if(exposedFaces[(int)Shapes.CubeFace.TOP])		faces.Add(Faces.TOP);
			if(exposedFaces[(int)Shapes.CubeFace.BOTTOM])	faces.Add(Faces.BOTTOM);
			if(exposedFaces[(int)Shapes.CubeFace.FRONT])	faces.Add(Faces.FRONT);
			if(exposedFaces[(int)Shapes.CubeFace.BACK])		faces.Add(Faces.BACK);
			if(exposedFaces[(int)Shapes.CubeFace.RIGHT])	faces.Add(Faces.RIGHT);
			if(exposedFaces[(int)Shapes.CubeFace.LEFT])		faces.Add(Faces.LEFT);
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
		public override List<Faces> GetFaces(bool[] exposedFaces, Quaternion rotation)
		{
			List<Faces> faces = new List<Faces>();
			if(TopFront(exposedFaces, rotation))	faces.Add(Faces.FRONT);
			if(Left(exposedFaces, rotation))		faces.Add(Faces.RIGHT);
			if(Right(exposedFaces, rotation))		faces.Add(Faces.LEFT);
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
				case Faces.FRONT: 	return Enumerable.Repeat(Vector3.up, 4).ToArray();
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

	//	TODO: Update rest of shapes to new system, finish shapes
	/*public  class CornerOut
	{
		 int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount, byte belowBlock)
		{
			List<CornerOutFace> faces = new List<CornerOutFace>();

			if(	TopFrontRight(exposedFaces, rotation))
				faces.Add(CornerOutFace.SLOPE);
			if( belowBlock == 0)
				faces.Add(CornerOutFace.BOTTOM);

			int localVertCount = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				Vector3[] faceVerts = CornerOut.Vertices(faces[i], position);

				vertices.AddRange(	RotateVectors(	faceVerts,
													position,
													rotation));

				normals.AddRange(	RotateNormals(	CornerOut.Normals(faces[i]),
													rotation));

				triangles.AddRange(	CornerOut.Triangles(faces[i],
									vertCount + localVertCount));

				localVertCount += faceVerts.Length;
			}
			return localVertCount;			
		}

		public  Vector3[] Vertices(CornerOutFace face, Vector3 offset)
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

		public  int[] Triangles(CornerOutFace face, int offset)
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

		public  Vector3[] Normals(CornerOutFace face)
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

	public  class CornerIn
	{
		 int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
		{
			List<CornerInFace> faces = new List<CornerInFace>();

			faces.Add(CornerInFace.SLOPE);

			if(Top(exposedFaces, rotation))
				faces.Add(CornerInFace.TOP);

			int localVertCount = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				Vector3[] faceVerts = CornerIn.Vertices(faces[i], position);

				vertices.AddRange(	RotateVectors(	faceVerts,
													position,
													rotation));

				normals.AddRange(	RotateNormals(	CornerIn.Normals(faces[i]),
													rotation));

				triangles.AddRange(	CornerIn.Triangles(faces[i],
									vertCount + localVertCount));

				localVertCount += faceVerts.Length;	
			}
			return localVertCount;			
		}
		public  Vector3[] Vertices(CornerInFace face, Vector3 offset)
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

		public  int[] Triangles(CornerInFace face, int offset)
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

		public  Vector3[] Normals(CornerInFace face)
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

	public  class Outcrop
	{
		 int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
		{
			List<OutcropFace> faces = new List<OutcropFace>();

			if(TopFront(exposedFaces, rotation))
				faces.Add(OutcropFace.RSLOPE);
				faces.Add(OutcropFace.LSLOPE);
				faces.Add(OutcropFace.TOP);

			if(Right(exposedFaces, rotation))
				faces.Add(OutcropFace.RIGHT);
			if(Left(exposedFaces, rotation))
				faces.Add(OutcropFace.LEFT);
			if(Front(exposedFaces, rotation))
				faces.Add(OutcropFace.FRONT);
			if(Back(exposedFaces, rotation))
				faces.Add(OutcropFace.BACK);
			if(Bottom(exposedFaces, rotation))
				faces.Add(OutcropFace.BOTTOM);

			int localVertCount = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				Vector3[] faceVerts = Outcrop.Vertices(faces[i], position);

				vertices.AddRange(	RotateVectors(	faceVerts,
													position,
													rotation));

				normals.AddRange(	RotateNormals(	Outcrop.Normals(faces[i]),
													rotation));

				triangles.AddRange(	Outcrop.Triangles(faces[i],
									vertCount + localVertCount));

				localVertCount += faceVerts.Length;	
			}
			return localVertCount;			
		}

		public  Vector3[] Vertices(OutcropFace face, Vector3 offset)
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

		public  int[] Triangles(OutcropFace face, int offset)
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

		public  Vector3[] Normals(OutcropFace face)
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

	public  class Strip
	{
		 int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
		{
			List<StripFace> faces = new List<StripFace>();

			faces.Add(StripFace.RIGHT);
			faces.Add(StripFace.LEFT);

			if(Back(exposedFaces, rotation))
				faces.Add(StripFace.BACK);
			if(Front(exposedFaces, rotation))
				faces.Add(StripFace.FRONT);
			if(Bottom(exposedFaces, rotation))
				faces.Add(StripFace.BOTTOM);

			int localVertCount = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				Vector3[] faceVerts = Strip.Vertices(faces[i], position);

				vertices.AddRange(	RotateVectors(	faceVerts,
													position,
													rotation));

				normals.AddRange(	RotateNormals(	Strip.Normals(faces[i]),
													rotation));

				triangles.AddRange(	Strip.Triangles(faces[i],
									vertCount + localVertCount));

				localVertCount += faceVerts.Length;	
			}
			return localVertCount;			
		}

		public  Vector3[] Vertices(StripFace face, Vector3 offset)
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

		public  int[] Triangles(StripFace face, int offset)
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

		public  Vector3[] Normals(StripFace face)
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

	public  class StripEnd
	{
		 int Draw(List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
						Vector3 position, 			Quaternion rotation, 	bool[] exposedFaces, int vertCount)
		{
			List<StripEndFace> faces = new List<StripEndFace>();

			faces.Add(StripEndFace.RIGHT);
			faces.Add(StripEndFace.LEFT);
			faces.Add(StripEndFace.FRONT);

			if(Back(exposedFaces, rotation))
				faces.Add(StripEndFace.BACK);
			if(Bottom(exposedFaces, rotation))
				faces.Add(StripEndFace.BOTTOM);

			int localVertCount = 0;
			for(int i = 0; i < faces.Count; i++)
			{
				Vector3[] faceVerts = StripEnd.Vertices(faces[i], position);

				vertices.AddRange(	RotateVectors(	faceVerts,
													position,
													rotation));

				normals.AddRange(	RotateNormals(	StripEnd.Normals(faces[i]),
													rotation));

				triangles.AddRange(	StripEnd.Triangles(faces[i],
									vertCount + localVertCount));

				localVertCount += faceVerts.Length;	
			}
			return localVertCount;			
		}
		public  Vector3[] Vertices(StripEndFace face, Vector3 offset)
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

		public  int[] Triangles(StripEndFace face, int offset)
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

		public  Vector3[] Normals(StripEndFace face)
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
	}*/
	
	//	Choose which shape a block has based on which adjacent blocks are see through
	public static void SetSlopes(Chunk chunk, Vector3 voxel)
	{
		int x = (int)voxel.x;
		int y = (int)voxel.y;
		int z = (int)voxel.z;

		if(chunk.blockTypes[x,y,z] != Blocks.Types.DIRT) return;

		switch(chunk.blockBytes[x,y,z])
		{
			/*//	CORNER OUT

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
				break;*/

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
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotation)] ||
				exposedFaces[(int)RotateFace(CubeFace.FRONT, rotation)]);
	}

	static bool TopFrontRight(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotation)]   ||
				exposedFaces[(int)RotateFace(CubeFace.FRONT, rotation)] ||
				exposedFaces[(int)RotateFace(CubeFace.RIGHT, rotation)]);
	}

	static bool TopFrontLeft(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotation)]   ||
				exposedFaces[(int)RotateFace(CubeFace.FRONT, rotation)] ||
				exposedFaces[(int)RotateFace(CubeFace.LEFT, rotation)]);
	}

	static bool Right(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.RIGHT, rotation)]);
	}

	static bool Left(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.LEFT, rotation)]);
	}

	static bool Front(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.FRONT, rotation)]);
	}

	static bool Back(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.BACK, rotation)]);
	}

	static bool Top(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.TOP, rotation)]);
	}

	static bool Bottom(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(CubeFace.BOTTOM, rotation)]);
	}
	
	

	//	Return vector matching cube face normal
	public static Vector3 FaceToDirection(CubeFace face)
	{
		Vector3 direction;
		
		switch(face)
		{
			case CubeFace.TOP: direction = Vector3.up; break;
			case CubeFace.BOTTOM: direction = Vector3.down; break;
			case CubeFace.RIGHT: direction = Vector3.right; break;
			case CubeFace.LEFT: direction = Vector3.left; break;
			case CubeFace.FRONT: direction = Vector3.forward; break;
			case CubeFace.BACK: direction = Vector3.back; break;
			default: direction = Vector3.zero; break;
		}

		return direction;
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
	static CubeFace RotateFace(CubeFace face, Quaternion rotation)
	{
		//	Convert to Vector3 direction, rotate then convert back to face
		return DirectionToFace(RotateVector(FaceToDirection(face), Vector3.zero, rotation));
	}

	#endregion
}
