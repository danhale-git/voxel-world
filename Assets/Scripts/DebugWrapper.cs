using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugWrapper : MonoBehaviour
{
	List<Vector3> lineStart = new List<Vector3>();
	List<Vector3> lineEnd = new List<Vector3>();
	List<Color> lineColor = new List<Color>();

	Shapes.Cube cube = new Shapes.Cube();

	void OnDrawGizmos()
    {
		for(int i = 0; i < lineStart.Count; i++)
		{
			Gizmos.color = lineColor[i];
			Gizmos.DrawLine(lineStart[i], lineEnd[i]);
		}
	}

	void Line(Vector3 start, Vector3 end, Color color)
	{
		lineStart.Add(start);
		lineEnd.Add(end);
		lineColor.Add(color);
	}

	public void OutlineChunk(Vector3 position, Color color, bool removePrevious = true, float sizeDivision = 2)
	{
		if(!World.markChunks) return;
		color.a = 0.3f;
		if(removePrevious)
		{
			lineStart = new List<Vector3>();
			lineEnd = new List<Vector3>();
			lineColor = new List<Color>();
		}

		Vector3 offset = position + ((Vector3.up + Vector3.forward + Vector3.right) *  (World.chunkSize / 2));

		for(int i = 0; i < 4; i++)
		{
			Shapes.Faces face = (Shapes.Faces)i;
			Vector3[] vertices = cube.Vertices(face, Vector3.zero);

			for(int e = 0; e < vertices.Length; e++)
			{
				vertices[e] = (vertices[e] * (World.chunkSize / sizeDivision)) + offset;
			}

			Line(vertices[0], vertices[1], color);
			Line(vertices[1], vertices[2], color);
			Line(vertices[2], vertices[3], color);
			Line(vertices[3], vertices[0], color);
		}
	}
}
