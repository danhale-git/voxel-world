using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class NoiseTesting : MonoBehaviour
{
	private Texture2D texture;
    private SpriteRenderer spriteRenderer;
	private RectTransform rectTrans;

	int size = 512;

    void Start()
	{
        spriteRenderer = GetComponent<SpriteRenderer>();
		rectTrans = GetComponent<RectTransform>();
		//size = (int)rectTrans.rect.height;
		texture = new Texture2D(size, size);
		Noise();
    }
    
	void Noise()
	{
		Debug.Log("Generating noise");
		for(int x = 0; x < size; x++)
			for(int y = 0; y < size; y++)
			{
				float factor = 0.01f;
				float perlin = NoiseUtils.BrownianMotion(x*factor, y*factor, 1, 0.3f);

				int _x = x;// - x/2;
				int _y = y;// - y/2;

				texture.SetPixel(_x, _y, new Color(perlin, perlin, perlin, 1));
			}
		
		Debug.Log(rectTrans.pivot);

		texture.Apply();
		Sprite mySprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector3(0.5f, 0.5f));
		mySprite.name = "noiseImage";
		spriteRenderer.sprite = mySprite;
	}
  
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Space)) Noise();
	}
}