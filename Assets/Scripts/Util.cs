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

	public static void SpiralTest()
	{
		Vector3 position = new Vector3(0,50,0);
		int increment = 1;
		for(int i = 0; i < 5; i++)
		{
			for(int r = 0; r < increment; r++)
			{
				position += Vector3.right;
				// do stuff
			}
			for(int d = 0; d < increment; d++)
			{
				position += Vector3.back;
				// do stuff
			}

			increment++;

			for(int l = 0; l < increment; l++)
			{
				position += Vector3.left;
				// do stuff
			}
			for(int u = 0; u < increment; u++)
			{
				position += Vector3.forward;
				// do stuff
			}

			increment++;
		}
		for(int u = 0; u < increment - 1; u++)
		{
			position += Vector3.right;
			// do stuff
		}
	}

	public static Vector3[] CubeFaceDirections()
	{
		return new Vector3[]
		{
			Vector3.up,
			Vector3.down,
			Vector3.right,
			Vector3.left,
			Vector3.forward,
			Vector3.back
		};
	}

	public static bool InChunk(float value, float offsetIn)
	{
		if(value < 0 + offsetIn || value >= World.chunkSize - offsetIn) return false;
		return true;
	}
}
