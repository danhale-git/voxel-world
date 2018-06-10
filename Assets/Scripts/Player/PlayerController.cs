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
	}
	
	//	Raycase down to find current chunk
	void GetCurrentChunk()
	{
		//	Only do this once per second
		if(Time.fixedTime - updateTimer < 1)
		{
			return;
		}
		updateTimer = Time.fixedTime;

		//	Raycast to chunk below player
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, 1000f, chunkLayerMask))
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
				Debug.Log("chunk at "+hit.transform.position+" not found! ("+hit.transform.gameObject.name+")");
			}
        }
	}

	//	Simple flying first person movement
	void Movement()
	{
		if(Input.GetKey(KeyCode.W))
		{
			transform.Translate((Vector3.forward * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.S))
		{
			transform.Translate((Vector3.back * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.A))
		{
			transform.Translate((Vector3.left * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.D))
		{
			transform.Translate((Vector3.right * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.LeftControl))
		{
			transform.Translate((Vector3.down * speed) * Time.deltaTime);
		}
		if(Input.GetKey(KeyCode.Space))
		{
			transform.Translate((Vector3.up * speed) * Time.deltaTime);
		}
		
		float h = sensitivity * Input.GetAxis("Mouse X");
        float v = -(sensitivity * Input.GetAxis("Mouse Y"));
        transform.Rotate(0, h, 0);
		mycam.gameObject.transform.Rotate(v, 0, 0);
	}
}
