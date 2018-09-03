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
	//	Matrix of voxel coordinates within the square
	public int[,] blockMatrix;

	public int[] bounds;

	//	Most and least exposed sides of the square
	public Side front, back;
	
	public Zone(int x, int z, int size, int[,] matrix)
	{
		this.x = x;
		this.z = z;
		this.size = size;
		this.matrix = matrix;

		this.bottom = z;
		this.top = z+size-1;
		this.left = x;
		this.right = x+size-1;

		bounds = new int[] { (size * World.chunkSize) - 1, 0, (size * World.chunkSize) - 1, 0 };
	}
}