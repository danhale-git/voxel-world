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
	public enum Shape {CUBE, WEDGE, CORNERIN, CORNEROUT}

	//	For specifying which face of a shape is being worked on
	public enum CubeFace {TOP, BOTTOM, RIGHT, LEFT, FRONT, BACK}
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
	
	public static class Cube
	{
		//	Vertices for a cube
		public static Vector3[] Vertices(CubeFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case CubeFace.TOP:
					vertices = new Vector3[] {v7+offset, v6+offset, v5+offset, v4+offset};
				break;

				case CubeFace.BOTTOM:
					vertices = new Vector3[] {v0+offset, v1+offset, v2+offset, v3+offset};
				break;

				case CubeFace.RIGHT:
					vertices = new Vector3[] {v5+offset, v6+offset, v2+offset, v1+offset};
				break;

				case CubeFace.LEFT:
					vertices = new Vector3[] {v7+offset, v4+offset, v0+offset, v3+offset};
				break;

				case CubeFace.FRONT:
					vertices = new Vector3[] {v4+offset, v5+offset, v1+offset, v0+offset};
				break;
				
				case CubeFace.BACK:
					vertices = new Vector3[] {v6+offset, v7+offset, v3+offset, v2+offset};
				break;

				default:
					vertices = null;
					break;
			}
					
			return vertices;
		}

		//	Triangles for a cube
		public static int[] Triangles(CubeFace face, int offset)
		{
			return new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
		}

		//	Normals for a cube
		public static Vector3[] Normals(CubeFace face)
		{
			Vector3[] normals;
			
			//	TODO:
			//	Enumerable.Repeat(Vector3.down,4).ToList();
			switch(face)
			{
				case CubeFace.TOP:
					normals = new Vector3[] {	Vector3.up,
												Vector3.up, 
												Vector3.up,
												Vector3.up};
				break;

				case CubeFace.BOTTOM:
					normals = new Vector3[] {	Vector3.down,
												Vector3.down, 
												Vector3.down,
												Vector3.down};
				break;
				
				case CubeFace.RIGHT:
					normals = new Vector3[] {	Vector3.right,
												Vector3.right, 
												Vector3.right,
												Vector3.right};
				break;

				case CubeFace.LEFT:
					normals = new Vector3[] {	Vector3.left,
												Vector3.left, 
												Vector3.left,
												Vector3.left};
				break;

				case CubeFace.FRONT:
					normals = new Vector3[] {	Vector3.forward,
												Vector3.forward, 
												Vector3.forward,
												Vector3.forward};
				break;

				case CubeFace.BACK:
					normals = new Vector3[] {	Vector3.back,
												Vector3.back, 
												Vector3.back,
												Vector3.back};
				break;

				default:
					normals = null;
				break;
			}

			return normals;
		}
	}

	public static class Wedge
	{
		
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

	
}
