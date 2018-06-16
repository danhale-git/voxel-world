using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockUtils
{
	public enum Rotate { FRONT = 0, RIGHT = 90, BACK = 180, LEFT = 270 }
	public enum Shape {CUBE, WEDGE}

	//	For specifying which face of a shape is being worked on
	public enum CubeFace {TOP, BOTTOM, LEFT, RIGHT, FRONT, BACK}
	public enum WedgeFace {SLOPE, BOTTOM, LEFT, RIGHT, BACK}
	public enum CornerOutFace {SLOPE, BOTTOM, LEFT}
	public enum CornerInFace {SLOPE, TOP}

	//	Enum indices correspond to fixed block attribute arrays below
	public enum Types {		AIR = 0,
							DIRT = 1,
							STONE = 2
							};

	//	Block type is see-through
	public static bool[] seeThrough = new bool[] {	true,		//	0	//	AIR
														false,		//	1	//	DIRT
														false		//	2	//	STONE
														};

	//	Block type color
	public static Color32[] colors = new Color32[]{	Color.white,					//	0	//	AIR
													new Color32(11, 110, 35, 255),	//	1	//	DIRT
													new Color32(200, 200, 200, 255)	//	2	//	STONE
													};											


	// Coordinates for 1x1 cube vertices relative to center	
	public static Vector3 v0 = new Vector3( -0.5f,  -0.5f,  0.5f );
	public static Vector3 v1 = new Vector3(  0.5f,  -0.5f,  0.5f );
	public static Vector3 v2 = new Vector3(  0.5f,  -0.5f, -0.5f );
	public static Vector3 v3 = new Vector3( -0.5f,  -0.5f, -0.5f ); 
	public static Vector3 v4 = new Vector3( -0.5f,   0.5f,  0.5f );
	public static Vector3 v5 = new Vector3(  0.5f,   0.5f,  0.5f );
	public static Vector3 v6 = new Vector3(  0.5f,   0.5f, -0.5f );
	public static Vector3 v7 = new Vector3( -0.5f,   0.5f, -0.5f );

	//  //
    #region Cube
            //  //

	//	Vertices for a cube
	public static Vector3[] CubeVertices(CubeFace face, Vector3 offset)
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
	public static int[] CubeTriangles(CubeFace face, int offset)
	{
		return new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
	}

	//	Normals for a cube
	public static Vector3[] CubeNormals(CubeFace face)
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

	#endregion

	//  //
    #region Wedge
            //  //

	public static Vector3[] WedgeVertices(WedgeFace face, Vector3 offset, Rotate rotation)
	{
		Vector3[] vertices;
	
		switch(face)
		{
			case WedgeFace.SLOPE:
				vertices = new Vector3[] {v7+offset, v6+offset, v1+offset, v0+offset};
			break;

			default:
				vertices = null;
				break;
		}
				
		return RotateVertices(vertices, offset, rotation);
	}

	//	Triangles for a cube
	public static int[] WedgeTriangles(WedgeFace face, int offset)
	{
		int[] triangles;
		switch(face)
		{
			case WedgeFace.SLOPE:
				triangles = new int[] {3+offset, 1+offset, 0+offset, 3+offset, 2+offset, 1+offset};
			break;

			default:
				triangles = null;
				break;
		}
				
		return triangles;
	}

	public static Vector3[] WedgeNormals(WedgeFace face)
	{
		Vector3[] normals = new Vector3[4];
	
		switch(face)
		{
			case WedgeFace.SLOPE:
				normals = new Vector3[] {	Vector3.up + Vector3.forward,
											Vector3.up + Vector3.forward, 
											Vector3.up + Vector3.forward,
											Vector3.up + Vector3.forward};
			break;
		}
				
		return normals;
	}

	#endregion

	//  //
    #region CornerOut
            //  //
	
	//	Vertices for a cube
	public static Vector3[] CornerOutVertices(CornerOutFace face, Vector3 offset, Rotate rotation)
	{
		Vector3[] vertices;
	
		switch(face)
		{
			case CornerOutFace.SLOPE:
				vertices = new Vector3[] {v7+offset, v2+offset, v0+offset};
			break;

			case CornerOutFace.BOTTOM:
				vertices = new Vector3[] {v0+offset, v1+offset, v2+offset};
			break;

			case CornerOutFace.LEFT:
				vertices = new Vector3[] {v7+offset, v0+offset, v4+offset};
			break;

			default:
				vertices = null;
				break;
		}
				
		return RotateVertices(vertices, offset, rotation);
	}

	//	Triangles for a cube
	public static int[] CornerOutTriangles(CornerOutFace face, int offset)
	{
		int[] triangles;
		switch(face)
		{
			case CornerOutFace.SLOPE:
				triangles = new int[] {2+offset, 1+offset, 0+offset};
			break;

			case CornerOutFace.BOTTOM:
				triangles = new int[] {0+offset, 1+offset, 2+offset};
			break;

			case CornerOutFace.LEFT:
				triangles = new int[] {2+offset, 1+offset, 0+offset};
			break;
			
			default:
				triangles = null;
				break;
		}
		return triangles;
	}

	//	Normals for a cube
	public static Vector3[] CornerOutNormals(CornerOutFace face)
	{
		Vector3[] normals;
		
		//	TODO:
		//	Enumerable.Repeat(Vector3.down,4).ToList();
		switch(face)
		{
			case CornerOutFace.SLOPE:
				normals = new Vector3[] {	Vector3.up + Vector3.forward + Vector3.right,
											Vector3.up + Vector3.forward + Vector3.right, 
											Vector3.up + Vector3.forward + Vector3.right};
			break;

			case CornerOutFace.BOTTOM:
				normals = new Vector3[] {	Vector3.up,
											Vector3.up, 
											Vector3.up};
			break;

			case CornerOutFace.LEFT:
				normals = new Vector3[] {	Vector3.right,
											Vector3.right, 
											Vector3.right};
			break;

			default:
				normals = null;
			break;
		}

		return normals;
	}

	#endregion


	//  //
    #region CornerIn
            //  //

	//	Vertices for a cube
	public static Vector3[] CornerInVertices(CornerInFace face, Vector3 offset, Rotate rotation)
	{
		Vector3[] vertices;
	
		switch(face)
		{
			case CornerInFace.SLOPE:
				vertices = new Vector3[] {v4+offset, v6+offset, v1+offset};
			break;

			case CornerInFace.TOP:
				vertices = new Vector3[] {v7+offset, v6+offset, v4+offset};
			break;

			default:
				vertices = null;
				break;
		}
				
		return RotateVertices(vertices, offset, rotation);
	}

	//	Triangles for a cube
	public static int[] CornerInTriangles(CornerInFace face, int offset)
	{
		int[] triangles;
		switch(face)
		{
			case CornerInFace.SLOPE:
				triangles = new int[] {2+offset, 1+offset, 0+offset};
			break;

			case CornerInFace.TOP:
				triangles = new int[] {2+offset, 1+offset, 0+offset};
			break;
			
			default:
				triangles = null;
				break;
		}
		return triangles;
	}

	//	Normals for a cube
	public static Vector3[] CornerInNormals(CornerInFace face)
	{
		Vector3[] normals;
		
		//	TODO:
		//	Enumerable.Repeat(Vector3.down,4).ToList();
		switch(face)
		{
			case CornerInFace.SLOPE:
				normals = new Vector3[] {	Vector3.up + Vector3.forward + Vector3.right,
											Vector3.up + Vector3.forward + Vector3.right, 
											Vector3.up + Vector3.forward + Vector3.right};
			break;

			case CornerInFace.TOP:
				normals = new Vector3[] {	Vector3.up,
											Vector3.up, 
											Vector3.up};
			break;

			default:
				normals = null;
			break;
		}

		return normals;
	}

	#endregion

	//	Direction of face normal
	public static Vector3 GetDirection(CubeFace face)
	{
		Vector3 direction;
		
		switch(face)
		{
			case CubeFace.TOP:
				direction = Vector3.up;
			break;

			case CubeFace.BOTTOM:
				direction = Vector3.down;
			break;
			
			case CubeFace.RIGHT:
				direction = Vector3.right;
			break;

			case CubeFace.LEFT:
				direction = Vector3.left;
			break;

			case CubeFace.FRONT:
				direction = Vector3.forward;
			break;

			case CubeFace.BACK:
				direction = Vector3.back;
			break;

			default:
				direction = Vector3.zero;
			break;
		}

		return direction;
	}

	public static Vector3[] HorizontalNeighbours(Vector3 voxel)
	{
		return new Vector3[] { 	Vector3.right + voxel,
								Vector3.left + voxel,
								Vector3.forward + voxel,
								Vector3.back + voxel,
								Vector3.right + Vector3.forward + voxel,
								Vector3.right + Vector3.back + voxel,
								Vector3.left + Vector3.forward + voxel,
								Vector3.left + Vector3.back + voxel
								};
	}

	//	Wrap local block positions outside max chunk size
	public static Vector3 WrapBlockIndex(Vector3 index)
	{
		float[] vector = new float[3] {	index.x,
									index.y,
									index.z};

		for(int i = 0; i < 3; i++)
		{
			//	if below min then max
			if(vector[i] == -1) 
				vector[i] = World.chunkSize-1; 
			//	if above max then min
			else if(vector[i] == World.chunkSize) 
				vector[i] = 0;
		}

		return new Vector3(vector[0], vector[1], vector[2]);
	}

	//	Round Vector values to nearest ints
	public static Vector3 RoundVector3(Vector3 toRound)
	{
		Vector3 rounded = new Vector3(	Mathf.Round(toRound.x),
										Mathf.Round(toRound.y),
										Mathf.Round(toRound.z));
		return rounded;
	}
	
	//	Rotate given vertex around centre by yRotation on Y axis
	public static Vector3[] RotateVertices(Vector3[] vertices, Vector3 centre, Rotate yRotation)
	{		
		Vector3[] rotatedVertices = new Vector3[vertices.Length];
		Quaternion rotation = Quaternion.Euler(0, (int)yRotation, 0);

		for(int i = 0; i < vertices.Length; i++)
		{
			rotatedVertices[i] = rotation * (vertices[i] - centre) + centre;
		}
		
		return rotatedVertices;
	}

	public static Vector3[] RotateNormals(Vector3[] normals, Rotate yRotation)
	{
		Vector3[] rotatedNormals = new Vector3[normals.Length];
		Quaternion rotation = Quaternion.Euler(0, (int)yRotation, 0);
		for(int i = 0; i < normals.Length; i++)
		{
			rotatedNormals[i] = rotation * normals[i];
		}
		return rotatedNormals;
	}
}
