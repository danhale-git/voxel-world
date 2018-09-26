using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POILibrary
{
	//public enum Tiles {NONE, WALL, PATH};

	public class POI
	{
		public int wallHeight = 10;

		public void GenerateMatrixes(BuildingGenerator buildingGenerator, Zone zone)
		{
			buildingGenerator.Generate();

			buildingGenerator.ApplyMaps(this);
		}


	}

}
