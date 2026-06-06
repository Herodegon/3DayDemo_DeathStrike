using UnityEngine;

public abstract class PooledFX : MonoBehaviour
{
    public string poolId;
    protected float returnAt;

    public virtual void Init(string id) {poolId = id;}

    public virtual void OnEnable()
    {
        returnAt = Time.time;
    }

    public virtual void Update()
    {
        if (Time.time >= returnAt)
        {
            if (FXManager.Instance != null)
            {
                FXManager.Instance.ReturnToPool(poolId, gameObject);
            }
        }
    }
}
