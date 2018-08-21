using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Util
{
	public static double EpochMilliseconds()
	{
		DateTime dt1970 = new DateTime(1970, 1, 1);
    	DateTime current = DateTime.UtcNow;
    	TimeSpan span = current - dt1970;
    	return span.TotalMilliseconds;
	}

	public static Vector3[] CubeFaceDirections()
	{
		return new Vector3[]
		{
			Vector3.up,			//	0
			Vector3.down,		//	1
			Vector3.right,		//	2
			Vector3.left,		//	3
			Vector3.forward,	//	4
			Vector3.back		//	5
		};
	}

	//	Round Vector values to nearest ints
	public static Vector3 RoundVector3(Vector3 toRound)
	{
		Vector3 rounded = new Vector3(	Mathf.Round(toRound.x),
										Mathf.Round(toRound.y),
										Mathf.Round(toRound.z));
		return rounded;
	}

	public static bool InChunk(float value, float offsetIn)
	{
		if(value < 0 + offsetIn || value >= World.chunkSize - offsetIn) return false;
		return true;
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

	//	Return 8 adjacent positions
	public static Vector3[] HorizontalBlockNeighbours(Vector3 voxel)
	{
		return new Vector3[] { 	Vector3.right + voxel,
								Vector3.left + voxel,
								Vector3.forward + voxel,
								Vector3.back + voxel,
								Vector3.right + Vector3.forward + voxel,
								Vector3.right + Vector3.back + voxel,
								Vector3.left + Vector3.forward + voxel,
								Vector3.left + Vector3.back + voxel	};
	}

	public static Vector3[] HorizontalChunkNeighbours(Vector3 position, int chunkSize)
	{
		return new Vector3[] { 	(Vector3.right * chunkSize) + position,
								(Vector3.left * chunkSize) + position,
								(Vector3.forward * chunkSize) + position,
								(Vector3.back * chunkSize) + position,
								((Vector3.right + Vector3.forward) * chunkSize) + position,
								((Vector3.right + Vector3.back) * chunkSize) + position,
								((Vector3.left + Vector3.forward) * chunkSize) + position,
								((Vector3.left + Vector3.back) * chunkSize) + position	};
	}

	public static double RoundToDP(float value, int decimalPlaces)
	{
		return System.Math.Round(value, decimalPlaces);
	}
}
