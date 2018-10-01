using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;

public class ChunkManager
{
    static EntityManager manager;
    static MeshInstanceRenderer renderer;
    static EntityArchetype chunkArchetype;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        manager = World.Active.GetOrCreateManager<EntityManager>();
        chunkArchetype = manager.CreateArchetype(   typeof(Position),
                                                    typeof(TransformMatrix));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitWithScene()
    {
        renderer = GameObject.FindObjectOfType<MeshInstanceRendererComponent>().Value;
        for(int i = 0; i < 10; i++)
        {
            //  create chunks
        }
    }
}
