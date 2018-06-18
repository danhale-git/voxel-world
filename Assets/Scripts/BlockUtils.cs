using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BlockUtils
{
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

	//	Return 8 adjacent positions
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
}
