using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class Shapes
{
	
	public enum Rotate { FRONT = 0, RIGHT = 90, BACK = 180, LEFT = 270 }
	public enum Shape {CUBE, WEDGE, CORNERIN, CORNEROUT, OUTCROP, STRIP}

	//	For specifying which face of a shape is being worked on
	public enum CubeFace {TOP, BOTTOM, RIGHT, LEFT, FRONT, BACK}
	public enum WedgeFace {SLOPE, BOTTOM, LEFT, RIGHT, BACK}
	public enum CornerOutFace {SLOPE, BOTTOM, LEFT}
	public enum CornerInFace {SLOPE, TOP}
	public enum OutcropFace {RSLOPE, LSLOPE, RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}
	public enum StripFace {RIGHT, LEFT, FRONT, BACK, TOP, BOTTOM}

	// Coordinates for 1x1 cube vertices relative to center	
	public static Vector3 v0 = new Vector3( -0.5f,  -0.5f,  0.5f );	//	left bottom front
	public static Vector3 v2 = new Vector3(  0.5f,  -0.5f, -0.5f );	//	right bottom back
	public static Vector3 v3 = new Vector3( -0.5f,  -0.5f, -0.5f ); //	left bottom back
	public static Vector3 v1 = new Vector3(  0.5f,  -0.5f,  0.5f );	//	right bottom front
	public static Vector3 v4 = new Vector3( -0.5f,   0.5f,  0.5f );	//	left top front
	public static Vector3 v5 = new Vector3(  0.5f,   0.5f,  0.5f );	//	right top front
	public static Vector3 v6 = new Vector3(  0.5f,   0.5f, -0.5f );	//	right top back
	public static Vector3 v7 = new Vector3( -0.5f,   0.5f, -0.5f );	//	left top back
	
	public static Vector3 v8 = new Vector3(	 0f, 	 0f,   -0.5f);	//	middle back
	public static Vector3 v9 = new Vector3(  0f, 	 0f,	0.5f);	//	middle front
	public static Vector3 v10 = new Vector3( 0.5f, 	 0f,    0f);	//	middle right
	public static Vector3 v11 = new Vector3(-0.5f, 	 0f, 	0f);	//	middle left
	public static Vector3 v12 = new Vector3( 0f, 	 0f,    0f);	//	middle
	

	public static class Cube
	{
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

		 

		public static Vector3[] Vertices(StripFace face, Vector3 offset)
		{
			Vector3[] vertices;
		
			switch(face)
			{
				case StripFace.RIGHT: vertices = new Vector3[] {v0+offset, v9+offset, v8+offset, v3+offset};
					break;

				case StripFace.LEFT: vertices = new Vector3[] {v8+offset, v2+offset, v1+offset, v9+offset};
					break;
				
				case StripFace.FRONT: vertices = new Vector3[] {v0+offset, v1+offset, v9+offset};
					break;

				case StripFace.BACK: vertices = new Vector3[] {v3+offset, v2+offset, v1+offset};
					break;

				case StripFace.BOTTOM: vertices = new Vector3[] {v0+offset, v1+offset, v2+offset, v3+offset};
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
				chunk.blockShapes[x,y,z] = Shape.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 57:
			case 41:
			case 169:
			case 185:
				chunk.blockShapes[x,y,z] = Shape.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 202:
			case 138:
			case 170:
			case 234:
				chunk.blockShapes[x,y,z] = Shape.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 70:
			case 86:
			case 214:
			case 82:
			case 198:
				chunk.blockShapes[x,y,z] = Shape.CORNEROUT;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	CORNER IN

			case 16:
				chunk.blockShapes[x,y,z] = Shape.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 32:
				chunk.blockShapes[x,y,z] = Shape.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 128:
				chunk.blockShapes[x,y,z] = Shape.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 64:
				chunk.blockShapes[x,y,z] = Shape.CORNERIN;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	OUTCROP

			case 80:
				chunk.blockShapes[x,y,z] = Shape.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 48:
				chunk.blockShapes[x,y,z] = Shape.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 160:
				chunk.blockShapes[x,y,z] = Shape.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 192:
				chunk.blockShapes[x,y,z] = Shape.OUTCROP;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	WEDGE

			case 20:
			case 84:
			case 68:
			case 4:
				chunk.blockShapes[x,y,z] = Shape.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			case 49:
			case 17:
			case 33:
			case 1:
				chunk.blockShapes[x,y,z] = Shape.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.RIGHT;
				break;

			case 168:
			case 40:
			case 8:
			case 136:
				chunk.blockShapes[x,y,z] = Shape.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.BACK;
				break;

			case 194:
			case 2:
			case 130:
			case 66:
				chunk.blockShapes[x,y,z] = Shape.WEDGE;
				chunk.blockYRotation[x,y,z] = Rotate.LEFT;
				break;

			//	CUBE

			case 0:
				chunk.blockShapes[x,y,z] = Shape.CUBE;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;

			default:
				chunk.blockShapes[x,y,z] = Shape.CUBE;
				chunk.blockYRotation[x,y,z] = Rotate.FRONT;
				break;
		}
	}	
}
