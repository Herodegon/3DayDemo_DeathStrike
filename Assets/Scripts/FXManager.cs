using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VFX;

public interface IFXPayload { }

public struct FXSpawnContext
{
    public Vector3 position;
    public Quaternion rotation;
}

public struct BleedPayload : IFXPayload
{
    public float angleDeg;
}

public struct AudioPayload : IFXPayload
{
    public AudioClip clip;
    public float volume;
    public float pitch;
}

[System.Serializable]
public class FXEntry
{
    public string id;
    public GameObject prefab;
    [Min(0)] public int prewarmCount = 5;
}

public class FXManager : MonoBehaviour
{
    public static FXManager Instance {get; private set;}

    [SerializeField] private FXEntry[] fxEntries;
    [SerializeField] private Transform fxContainer;
    [SerializeField] private bool allowPoolGrowth = true;

    private readonly Dictionary<string, GameObject> fxPrefabs = new();
    private readonly Dictionary<string, List<GameObject>> fxPool = new();

    protected virtual void Awake()
    {
        Instance = this;
        foreach (var entry in fxEntries)
        {
            if (string.IsNullOrEmpty(entry.id) || entry.prefab == null) continue;
            fxPrefabs[entry.id] = entry.prefab;
            fxPool[entry.id] = new List<GameObject>();
            for (int i = 0; i < entry.prewarmCount; i++)
            {
                fxPool[entry.id].Add(CreateInstance(entry.id, entry.prefab));
            }
        }
    }

    public GameObject PlayFX(string id, FXSpawnContext spawn, IFXPayload payload = null)
    {
        if (!fxPrefabs.TryGetValue(id, out var prefab)) return null;

        var instance = GetFromPool(id, prefab);
        instance.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        instance.SetActive(true);
        ApplyPayload(instance, payload);
        return instance;
    }

    GameObject GetFromPool(string id, GameObject prefab)
    {
        var pool = fxPool[id];
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeInHierarchy) return pool[i];
        }
        if (!allowPoolGrowth) return pool[0];
        var created = CreateInstance(id, prefab);
        pool.Add(created);
        return created;
    }

    GameObject CreateInstance(string id, GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, fxContainer);
        instance.name = $"{id}_pooled";
        instance.SetActive(false);

        var pooledFX = instance.GetComponent<PooledFX>();
        if (pooledFX != null) pooledFX.Init(id);
        return instance;
    }

    private void ApplyPayload(GameObject instance, IFXPayload payload)
    {
        if (payload == null) return;
        if (payload is BleedPayload bleedPayload)
        {
            var visualEffect = instance.GetComponent<VisualEffect>();
            if (visualEffect != null)
            {
                visualEffect.SetFloat("SliceAngle", bleedPayload.angleDeg);
            }
        }
    }

    public void ReturnToPool(string id, GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(fxContainer, false);
    }
}
