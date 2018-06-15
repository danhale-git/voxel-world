using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Block
{
	public enum BlockType {GROUND, AIR};
	
	public BlockType type;
	public Vector3 position;
	public Chunk owner;

	public List<Vector3> vertices = new List<Vector3>();
	public List<Vector3> normals = new List<Vector3>();
	public List<int> triangles = new List<int>();
	
	//	seeThrough used for quad culling
	public bool seeThrough;

	public Block(BlockType _type, Vector3 _position, Chunk _owner)
	{
		type = _type;
		position = _position;
		owner = _owner;

		//	Set seeThrough
		switch(type)
		{
			case BlockType.AIR:
			seeThrough = true;
			break;
			case BlockType.GROUND:
			seeThrough = false;
			break;
		}
	}

	//	Generate meshes for exposed block faces
	public int GetFaces(int offset)
	{
		if(type == BlockType.AIR) { return 0; }

		//	Iterate over all six faces
		for(int i = 0; i < 6; i++)
		{
			BlockUtils.CubeFace face = (BlockUtils.CubeFace)i;

			//	Add mesh attributes to lists if face exposed
			if(FaceExposed( face ))
			{
				//	offset vertex positoins with block position in chunk
				Vector3[] faceVerts = BlockUtils.GetVertices(face, position);
				vertices.AddRange(faceVerts);
				normals.AddRange(BlockUtils.GetNormals(face));
				triangles.AddRange(BlockUtils.GetTriangles(face, offset));

				offset += faceVerts.Length;
			}
		}

		return vertices.Count;
	}

	//	Block face is on map edge or player can see through adjacent block
	bool FaceExposed(BlockUtils.CubeFace face)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = BlockUtils.GetDirection(face);	
		//	Neighbour position
		Vector3 neighbour = faceDirection + position;

		
		
		Chunk neighbourOwner;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= World.chunkSize || 
		   neighbour.y < 0 || neighbour.y >= World.chunkSize ||
		   neighbour.z < 0 || neighbour.z >= World.chunkSize)
		{
			//	Next chunk in direction of neighbour
			Vector3 neighbourChunkPos = owner.position + (faceDirection * World.chunkSize);

			//	Neighbouring chunk does not exist (map edge)
			if(!World.chunks.TryGetValue(neighbourChunkPos, out neighbourOwner))
			{
				return false;
			}			
			//	Convert local index to neighbouring chunk
			neighbour = BlockUtils.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in this chunk		
		else
		{
			neighbourOwner = owner;
		}
		
		//	Check seeThrough in neighbour
		return neighbourOwner.blocks[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z].seeThrough;
	}
}
