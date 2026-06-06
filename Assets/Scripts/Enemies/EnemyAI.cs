using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using PrimeTween;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public Transform target;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public bool ReturnToPool { get; set; } = false;
    private NavMeshAgent agent;
    private bool isDead = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null || isDead) return;
        agent.SetDestination(target.position);
    }

    public void TakeDamage(Vector2 attackDirection)
    {
        if (isDead) return;
        float angleDeg = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        StartCoroutine(DestroyObject(angleDeg));
        isDead = true;
        agent.isStopped = true;
    }

    private IEnumerator DestroyObject(float angleDeg)
    {
        DeathAnimation(1.0f);
        GameObject vfx = null;
        if (FXManager.Instance != null)
        {
            vfx = FXManager.Instance.PlayFX("BloodVFX", new FXSpawnContext {
                position = transform.position,
                rotation = transform.rotation,
            }, new BleedPayload {
                angleDeg = angleDeg
            });
        }
        Debug.Log($"vfx: {vfx}");
        yield return new WaitWhile(() => vfx != null && vfx.activeInHierarchy);
        if (ReturnToPool)
        {
            EnemyManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DeathAnimation(float duration)
    {
        Tween.ShakeLocalPosition(transform, new Vector3(0.1f, 0.1f, 0.0f), duration, 15);
        Tween.Alpha(spriteRenderer, 1.0f, 0.0f, duration, Ease.InOutSine);
    }
}
