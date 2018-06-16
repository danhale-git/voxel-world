using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

	public Chunk currentChunk;
	public World world;

	//	First person controls
	int speed = 25;
	int sensitivity = 1;
	Rigidbody rigidBody;
	Camera mycam;

	//	Chunk checks
	float updateTimer;
	int chunkLayerMask;
	

	void Start ()
	{
		mycam = GetComponentInChildren<Camera>();
		rigidBody = GetComponent<Rigidbody>();

		updateTimer = Time.fixedTime;
		chunkLayerMask = LayerMask.GetMask("Chunk");
	}
	
	void Update ()
	{
		Movement();
		
		GetCurrentChunk();

		//	Break block
		if(Input.GetButtonDown("Fire1"))
		{
			DebugBitMask(Camera.main.ScreenPointToRay(Input.mousePosition));
		}

		//	Break block
		if(Input.GetButtonDown("Fire2"))
		{
			AddBlock(Camera.main.ScreenPointToRay(Input.mousePosition));
		}
		
	}
	
	//	Raycast down to find current chunk
	//	Generate more chunks if player has moved to a new chunk
	void GetCurrentChunk()
	{
		//	Only do this twice per second
		if(Time.fixedTime - updateTimer < 0.5f)
		{
			return;
		}
		updateTimer = Time.fixedTime;

		//	Raycast to chunk below player
        RaycastHit hit;
        if (Physics.Raycast(transform.position,
							transform.TransformDirection(Vector3.down),
							out hit, 1000f,
							chunkLayerMask))
        {
			Chunk chunk;			
			//	Check if chunk exists
            if(World.chunks.TryGetValue(hit.transform.position, out chunk))
			{
				//	Initialise currentChunk
				if(currentChunk == null){ currentChunk = chunk; }

				//	Check if player has moved to a different chunk
				else if(chunk.position != currentChunk.position)
				{
					currentChunk = chunk;

					//	Generate chunks around player location
					world.DrawSurroundingChunks(currentChunk.gameObject.transform.position);
				}
			}
			else
			{
				//	Something strange happened
				Debug.Log("chunk at "+hit.transform.position+" not found! ("+hit.transform.gameObject.name+")");
			}
        }
	}

	//	Simple flying first person movement
	//	Probably temporary
	void Movement()
	{
		if(Input.GetKey(KeyCode.W))	//	forward
		{
			transform.Translate((Vector3.forward * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.S))	//	back
		{
			transform.Translate((Vector3.back * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.A))	//	left
		{
			transform.Translate((Vector3.left * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.D))	//	right
		{
			transform.Translate((Vector3.right * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.LeftControl))	//	down
		{
			transform.Translate((Vector3.down * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.Space))	//	up
		{
			transform.Translate((Vector3.up * speed) * Time.deltaTime);
		}
		
		float horizontal = sensitivity * Input.GetAxis("Mouse X");
        float vertical = -(sensitivity * Input.GetAxis("Mouse Y"));
        transform.Rotate(0, horizontal, 0);
		mycam.gameObject.transform.Rotate(vertical, 0, 0);		
	}

	void RemoveBlock(Ray ray)
	{
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit))
		{
			//	get voxel position
			Vector3 pointInCube = hit.point - (hit.normal * 0.5f);
			Vector3 voxel = BlockUtils.RoundVector3(pointInCube);

			World.ChangeBlock(voxel, BlockUtils.Types.AIR);

		}
	}

	void AddBlock(Ray ray)
	{
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit))
		{
			//	get voxel position
			Vector3 pointInCube = hit.point + (hit.normal * 0.5f);
			Vector3 voxel = BlockUtils.RoundVector3(pointInCube);

			World.ChangeBlock(voxel, BlockUtils.Types.DIRT);
		}
	}

	void DebugBitMask(Ray ray)
	{
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit))
		{
			//	get voxel position
			Vector3 pointInCube = hit.point - (hit.normal * 0.5f);
			Vector3 voxel = BlockUtils.RoundVector3(pointInCube);
		}
	}
}
