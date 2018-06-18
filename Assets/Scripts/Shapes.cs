using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Shapes
{
	/*public static class TEMPLATE
	{
		public static Vector3[] Vertices(TEMPface face, Vector3 offset) { }

		public static Vector3[] TrisIndices() { }

		public static int[] Normals() { }

		
	}*/
	public enum Rotate { FRONT = 0, RIGHT = 90, BACK = 180, LEFT = 270 }
	public enum Shape {CUBE, WEDGE}

	//	For specifying which face of a shape is being worked on
	public enum CubeFace {TOP, BOTTOM, LEFT, RIGHT, FRONT, BACK}
	public enum WedgeFace {SLOPE, BOTTOM, LEFT, RIGHT, BACK}
	public enum CornerOutFace {SLOPE, BOTTOM, LEFT}
	public enum CornerInFace {SLOPE, TOP}

	// Coordinates for 1x1 cube vertices relative to center	
	public static Vector3 v0 = new Vector3( -0.5f,  -0.5f,  0.5f );
	public static Vector3 v1 = new Vector3(  0.5f,  -0.5f,  0.5f );
	public static Vector3 v2 = new Vector3(  0.5f,  -0.5f, -0.5f );
	public static Vector3 v3 = new Vector3( -0.5f,  -0.5f, -0.5f ); 
	public static Vector3 v4 = new Vector3( -0.5f,   0.5f,  0.5f );
	public static Vector3 v5 = new Vector3(  0.5f,   0.5f,  0.5f );
	public static Vector3 v6 = new Vector3(  0.5f,   0.5f, -0.5f );
	public static Vector3 v7 = new Vector3( -0.5f,   0.5f, -0.5f );
	
	public static class Wedge
	{
		public static int Draw(	List<Vector3> vertices, 	List<Vector3> normals, 	List<int> triangles,
								Vector3 position, 			Quaternion rotation, 	WedgeFace[] faces, int vertCount)
		{
			for(int i = 0; i < faces.Length; i++)
			{
				Vector3[] verts = RotateVertices(	Vertices(faces[i], position),
													position,
													rotation );
				vertices.AddRange(verts);

				normals.AddRange(	RotateNormals(	Normals(faces[i]),
													rotation )	);

				triangles.AddRange(	Triangles(faces[i], vertCount));

				vertCount += verts.Length;
			}

			return vertCount;
		}
		public static Vector3[] Vertices(WedgeFace face, Vector3 offset)
		{ 

			Vector3[] vertices;
	
			switch(face)
			{
				case WedgeFace.SLOPE:
					vertices = new Vector3[] {v7+offset, v6+offset, v1+offset, v0+offset};
				break;

				case WedgeFace.RIGHT:
					vertices = new Vector3[] {v6+offset, v2+offset, v1+offset};
				break;

				case WedgeFace.LEFT:
					vertices = new Vector3[] {v7+offset, v0+offset, v3+offset};
				break;

				default:
					vertices = null;
					break;
			}
					
			return vertices;

			}

		public static Vector3[] Normals(WedgeFace face)
		{
			Vector3[] normals;
	
			switch(face)
			{
				case WedgeFace.SLOPE:
					normals = new Vector3[] {	Vector3.up + Vector3.forward,
												Vector3.up + Vector3.forward, 
												Vector3.up + Vector3.forward,
												Vector3.up + Vector3.forward};
				break;

				case WedgeFace.RIGHT:
					normals = new Vector3[] {	Vector3.right + Vector3.forward,
												Vector3.right + Vector3.forward, 
												Vector3.right + Vector3.forward};
				break;

				case WedgeFace.LEFT:
					normals = new Vector3[] {	Vector3.left + Vector3.forward,
												Vector3.left + Vector3.forward, 
												Vector3.left + Vector3.forward};
				break;

				default:
					normals = null;
					break;
			}
					
			return normals;
		}

		public static int[] Triangles(WedgeFace face, int offset)
		{
			int[] triangles;
			switch(face)
			{
				case WedgeFace.SLOPE:
					triangles = new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
				break;

				case WedgeFace.RIGHT:
					triangles = new int[] {2+offset, 1+offset, 0+offset};
				break;

				case WedgeFace.LEFT:
					triangles = new int[] {2+offset, 1+offset, 0+offset};
				break;

				default:
					triangles = null;
					break;
			}
					
			return triangles;
		}
	}

	//	Rotate given vertex around centre by yRotation on Y axis
	static Vector3[] RotateVertices(Vector3[] vertices, Vector3 centre, Quaternion rotation)
	{		
		Vector3[] rotatedVertices = new Vector3[vertices.Length];
		for(int i = 0; i < vertices.Length; i++)
		{
			//	rotate vertex position around centre
			rotatedVertices[i] = rotation * (vertices[i] - centre) + centre;
		}
		
		return rotatedVertices;
	}

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
}
