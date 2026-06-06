using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class PooledVisualFX : PooledFX
{
    private VisualEffect visualEffect;
    private bool useDuration = true;

    private void Awake()
    {
        visualEffect = GetComponent<VisualEffect>();
    }

    public override void Init(string id)
    {
        base.Init(id);
        if (visualEffect == null)
        {
            visualEffect = GetComponent<VisualEffect>();
        }
    }

    public override void OnEnable()
    {
        if (visualEffect == null)
        {
            visualEffect = GetComponent<VisualEffect>();
            if (visualEffect == null) return;
        }

        visualEffect.SendEvent("OnPlay");
        if (!visualEffect.HasFloat("Duration"))
        {
            useDuration = false;
        }
        else
        {
            returnAt = Time.time + visualEffect.GetFloat("Duration");
        }
    }

    public override void Update()
    {
        if ((useDuration && Time.time >= returnAt) 
            || (!useDuration && visualEffect.aliveParticleCount == 0))
        {
            if (FXManager.Instance != null)
            {
                FXManager.Instance.ReturnToPool(poolId, gameObject);
            }
        }
    }
}