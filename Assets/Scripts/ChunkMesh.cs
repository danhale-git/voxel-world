using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ChunkMesh
{	/*
    #region Misc

	//	Check which faces of a shape are exposed, adjusting for the shape's rotation

	static bool TopFront(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)] ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)]);
	}

	static bool TopFrontRight(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)]   ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)] ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.RIGHT, rotation)]);
	}

	static bool TopFrontLeft(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)]   ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)] ||
				exposedFaces[(int)RotateFace(Shapes.CubeFace.LEFT, rotation)]);
	}

	static bool Right(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.RIGHT, rotation)]);
	}

	static bool Left(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.LEFT, rotation)]);
	}

	static bool Front(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.FRONT, rotation)]);
	}

	static bool Back(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.BACK, rotation)]);
	}

	static bool Top(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.TOP, rotation)]);
	}

	static bool Bottom(bool[] exposedFaces, Quaternion rotation)
	{
		return (exposedFaces[(int)RotateFace(Shapes.CubeFace.BOTTOM, rotation)]);
	}
	
	

	//	Return vector matching cube face normal
	static Vector3 FaceToDirection(Shapes.CubeFace face)
	{
		Vector3 direction;
		
		switch(face)
		{
			case Shapes.CubeFace.TOP: direction = Vector3.up; break;
			case Shapes.CubeFace.BOTTOM: direction = Vector3.down; break;
			case Shapes.CubeFace.RIGHT: direction = Vector3.right; break;
			case Shapes.CubeFace.LEFT: direction = Vector3.left; break;
			case Shapes.CubeFace.FRONT: direction = Vector3.forward; break;
			case Shapes.CubeFace.BACK: direction = Vector3.back; break;
			default: direction = Vector3.zero; break;
		}

		return direction;
	}

	//	Return cube face facing direction
	static Shapes.CubeFace DirectionToFace(Vector3 direction)
	{
		if(direction == Vector3.up) return Shapes.CubeFace.TOP;
		if(direction == Vector3.down) return Shapes.CubeFace.BOTTOM;
		if(direction == Vector3.right) return Shapes.CubeFace.RIGHT;
		if(direction == Vector3.left) return Shapes.CubeFace.LEFT;
		if(direction == Vector3.forward) return Shapes.CubeFace.FRONT;
		if(direction == Vector3.back) return Shapes.CubeFace.BACK;
		else Debug.Log("BAD FACE"); return Shapes.CubeFace.TOP;
	}

	//	Player can see through adjacent block
	static bool FaceExposed(Shapes.CubeFace face, Vector3 blockPosition, Chunk chunk)
	{	
		//	Direction of neighbour
		Vector3 faceDirection = FaceToDirection(face);	
		//	Neighbour position
		Vector3 neighbour = blockPosition + faceDirection;
		
		Chunk neighbourOwner;

		//	Neighbour is outside this chunk
		if(neighbour.x < 0 || neighbour.x >= World.chunkSize || 
		   neighbour.y < 0 || neighbour.y >= World.chunkSize ||
		   neighbour.z < 0 || neighbour.z >= World.chunkSize)
		{
			//	Next chunk in direction of neighbour
			Vector3 neighbourChunkPos = chunk.position + (faceDirection * World.chunkSize);
			
			//Debug.Log(neighbourChunkPos);
			
			//	Neighbouring chunk does not exist (map edge)
			if(!World.chunks.TryGetValue(neighbourChunkPos, out neighbourOwner))
			{
				return false;
			}			
			//	Convert local index to neighbouring chunk
			neighbour = BlockUtils.WrapBlockIndex(neighbour);
		}
		//	Neighbour is in chunk being drawn		
		else
		{
			neighbourOwner = chunk;
		}
		
		//	Check seeThrough in neighbour
		Blocks.Types type = neighbourOwner.blockTypes[(int)neighbour.x, (int)neighbour.y, (int)neighbour.z];

		return (Blocks.seeThrough[(int)type]);
	}

	#endregion

    #region Rotation

	//	Rotate vertices around centre by yRotation on Y axis
	static Vector3[] RotateVectors(Vector3[] vectors, Vector3 centre, Quaternion rotation)
	{		
		Vector3[] rotatedVertices = new Vector3[vectors.Length];
		for(int i = 0; i < vectors.Length; i++)
		{
			//	rotate vertex position around centre
			rotatedVertices[i] = rotation * (vectors[i] - centre) + centre;
		}
		
		return rotatedVertices;
	}

	//	Rotate vertex around centre by yRotation on Y axis
	static Vector3 RotateVector(Vector3 vector, Vector3 centre, Quaternion rotation)
	{				
		return rotation * (vector - centre) + centre;
	}

	//	Rotate normal directions
	static Vector3[] RotateNormals(Vector3[] normals, Quaternion rotation)
	{
		Vector3[] rotatedNormals = new Vector3[normals.Length];
		for(int i = 0; i < normals.Length; i++)
		{
			//	rotate normal direction
			rotatedNormals[i] = rotation * normals[i];
		}
		return rotatedNormals;
	}

	//	Adjust face enum by direction
	static Shapes.CubeFace RotateFace(Shapes.CubeFace face, Quaternion rotation)
	{
		//	Convert to Vector3 direction, rotate then convert back to face
		return DirectionToFace(RotateVector(FaceToDirection(face), Vector3.zero, rotation));
	}

	#endregion
	*/
}
