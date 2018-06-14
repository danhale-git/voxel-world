using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockUtils
{
	//	For specifying which face of a cube we are working on
	public enum CubeFace {TOP, BOTTOM, LEFT, RIGHT, FRONT, BACK}

	// Coordinates for 1x1 cube vertices relative to center	
	public static Vector3 v0 = new Vector3( -0.5f,  -0.5f,  0.5f );
	public static Vector3 v1 = new Vector3(  0.5f,  -0.5f,  0.5f );
	public static Vector3 v2 = new Vector3(  0.5f,  -0.5f, -0.5f );
	public static Vector3 v3 = new Vector3( -0.5f,  -0.5f, -0.5f ); 
	public static Vector3 v4 = new Vector3( -0.5f,   0.5f,  0.5f );
	public static Vector3 v5 = new Vector3(  0.5f,   0.5f,  0.5f );
	public static Vector3 v6 = new Vector3(  0.5f,   0.5f, -0.5f );
	public static Vector3 v7 = new Vector3( -0.5f,   0.5f, -0.5f );

	//	Vertices for a cube
	public static Vector3[] GetVertices(CubeFace face, Vector3 offset)
	{
		Vector3[] vertices = new Vector3[4];
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
		}
		return vertices;
	}

	//	Triangles for a cube
	public static int[] GetTriangles(CubeFace face)
	{
		int[] triangles = new int[6];
		
		switch(face)
		{
			case CubeFace.TOP:
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;

			case CubeFace.BOTTOM:
				triangles = new int[] { 3, 1, 0, 3, 2, 1};
			break;

			case CubeFace.RIGHT:
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;

			case CubeFace.LEFT:
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;

			case CubeFace.FRONT:
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
			
			case CubeFace.BACK:
				triangles = new int[] {3, 1, 0, 3, 2, 1};
			break;
		}

		return triangles;
	}

	//	Normals for a cube
	public static Vector3[] GetNormals(CubeFace face)
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
}
