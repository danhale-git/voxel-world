using UnityEngine;

//	Square area for an L system to work on
public class Zone
{
	//	Sides of the square
	public enum Side { RIGHT, LEFT, TOP, BOTTOM };

	//	Positions
	public int x, z, size, bottom, top, left, right;
	//	Square as a matrix
	public int[,] matrix;

	//	Matrixes defining various elements of a POI
	public int[,] blockMatrix;
	public int[,] debugMatrix;
	public int[,] heightMatrix;

	public int[,] wallMatrix;

	//	Full zone bounds
	int[] bounds;
	//	Bounds in which objects can be generated
	public int[] bufferedBounds;

	//	Most and least exposed sides of the square
	public Side front, back;
	
	public Zone(int x, int z, int size, int[,] matrix)
	{
		int chunkSize = World.chunkSize;
		this.x = x;
		this.z = z;
		this.size = size;
		this.matrix = matrix;

		this.bottom = z;
		this.top = z+size-1;
		this.left = x;
		this.right = x+size-1;

		int blockSize = size * chunkSize;

		blockMatrix = new int[blockSize,blockSize];
		heightMatrix = new int[blockSize,blockSize];
		debugMatrix = new int[blockSize,blockSize];
		wallMatrix = new int[blockSize,blockSize];

		bounds = new int[] { blockSize - 1, 0, blockSize - 1, 0 };

		bufferedBounds = new int [] { bounds[0]-chunkSize, bounds[1]+chunkSize, bounds[2]-chunkSize, bounds[3]+chunkSize };
	}

	public static Side Opposite(Side side)
	{
		switch(side)
		{
			case Side.RIGHT:
				return Side.LEFT;
			case Side.LEFT:
				return Side.RIGHT;
			case Side.TOP:
				return Side.BOTTOM;
			case Side.BOTTOM:
				return Side.TOP;
			default:
				return Side.RIGHT;
		}
	}
	public static int Opposite(int side)
	{
		switch(side)
		{
			case 0:
				return 1;
			case 1:
				return 0;
			case 2:
				return 3;
			case 3:
				return 2;
			default:
				return 0;
		}
	}
}