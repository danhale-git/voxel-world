using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POILibrary
{
	//public enum Tiles {NONE, WALL, PATH};

	public class POI
	{
		public int wallHeight = 5;

		public void GenerateMatrixes(LSystem lSystem, Zone zone)
		{
			lSystem.GenerateBuilding();

			lSystem.DefineArea();

			lSystem.ApplyMaps(this);
		}


	}

}
