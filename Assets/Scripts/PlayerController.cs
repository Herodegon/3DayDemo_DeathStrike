using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Player Variables")]
    public int health = 3;
    public float moveSpeed = 10f;
    [SerializeField] private LayerMask sliceableMask;
    [SerializeField] private LayerMask nonSliceableMask;
    private Rigidbody2D playerRb2D;

    [Header("Slice")]
    [SerializeField] private Vector2 sliceHalfExtents = new(0.5f, 0.5f);

    #region Input Variables
    private InputActionAsset inputActions;
    private Vector2 mouseWorldPosition;
    #endregion

    #region Coroutines
    private Coroutine moveToTargetCoroutine;
    #endregion

    #region Slice Path Variables
    private readonly HashSet<GameObject> slicePathTargets = new HashSet<GameObject>();
    #endregion

    #region Debug Variables
    [SerializeField] private GameObject debugPointerObject;
    private readonly List<LineRendererData> debugLineRenderers = new List<LineRendererData>();
    private Vector2 debugTargetPosition = Vector2.zero;
    private Vector2 debugSliceAreaStartPosition = Vector2.zero;
    private List<Vector2> debugSliceArea = new List<Vector2> {
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.5f)
    };
    #endregion

    void Awake()
    {
        inputActions = GetComponent<PlayerInput>().actions;
        inputActions.Disable();
        inputActions.FindActionMap("Player").Enable();

        playerRb2D = GetComponent<Rigidbody2D>();

        // Player to Mouse Pointer
        // debugLineRenderers.Add(GlobalHelper.CreateLineRenderer(
        //     gameObject,
        //     () => transform.position,
        //     () => mouseWorldPosition,
        //     Color.red));

        // Player to Target Position
        debugLineRenderers.Add(GlobalHelper.CreateLineRenderer(
            gameObject,
            () => transform.position,
            () => debugTargetPosition,
            Color.blue));

        InitDebugSliceArea();
    }

    void Update()
    {
        mouseWorldPosition = Camera.main.ScreenToWorldPoint(inputActions.FindActionMap("Player").FindAction("CursorPosition").ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        foreach (var lineRenderer in debugLineRenderers)
        {
            lineRenderer.Update();
        }
        if (moveToTargetCoroutine == null)
        {
            debugPointerObject.transform.position = mouseWorldPosition;
        }
    }

    public void OnAttack()
    {
        Debug.Log("Attack");
        if (moveToTargetCoroutine != null) return;

        // Prevent player from moving through non-sliceable objects
        Vector2 targetPosition = mouseWorldPosition;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, mouseWorldPosition - (Vector2)transform.position, Vector2.Distance(transform.position, mouseWorldPosition), nonSliceableMask);
        if (hit.collider != null)
        {
            targetPosition = hit.point;
        }

        if (CheckForSliceableObjects(targetPosition))
        {
            foreach (var target in slicePathTargets)
            {
                var enemyAI = target.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    Vector2 attackDirection = targetPosition - (Vector2)transform.position;
                    enemyAI.TakeDamage(attackDirection);
                }
            }
        }

        moveToTargetCoroutine = StartCoroutine(MoveToTarget(targetPosition));
    }

    private bool CheckForSliceableObjects(Vector2 targetPosition)
    {
        slicePathTargets.Clear();

        Vector2 startPosition = transform.position;
        Vector2 trajectory = targetPosition - startPosition;
        float distance = trajectory.magnitude;
        if (distance <= Mathf.Epsilon)
        {
            debugSliceArea[0] = new Vector2(-sliceHalfExtents.x, -sliceHalfExtents.y);
            debugSliceArea[1] = new Vector2(sliceHalfExtents.x, -sliceHalfExtents.y);
            debugSliceArea[2] = new Vector2(sliceHalfExtents.x, sliceHalfExtents.y);
            debugSliceArea[3] = new Vector2(-sliceHalfExtents.x, sliceHalfExtents.y);
            return false;
        }

        Vector2 direction = trajectory / distance;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        // Draw the swept slice area as a trajectory-aligned quad in local space.
        debugSliceAreaStartPosition = startPosition;
        debugSliceArea[0] = (-perpendicular * sliceHalfExtents.x) + (-direction * sliceHalfExtents.y);
        debugSliceArea[1] = (perpendicular * sliceHalfExtents.x) + (-direction * sliceHalfExtents.y);
        debugSliceArea[2] = trajectory + (perpendicular * sliceHalfExtents.x) + (direction * sliceHalfExtents.y);
        debugSliceArea[3] = trajectory + (-perpendicular * sliceHalfExtents.x) + (direction * sliceHalfExtents.y);

        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            startPosition,
            sliceHalfExtents * 2f,
            0f,
            direction,
            distance,
            sliceableMask
        );

        foreach (RaycastHit2D sliceHit in hits)
        {
            if (sliceHit.collider == null) continue;
            slicePathTargets.Add(sliceHit.collider.gameObject);
        }

        return slicePathTargets.Count > 0;
    }

    private IEnumerator MoveToTarget(Vector3 targetWorldPosition)
    {
        float startTime = Time.time;
        float duration = Vector3.Distance(transform.position, targetWorldPosition) / moveSpeed;
        Debug.Log($"Projected Duration: {duration}");
        debugTargetPosition = targetWorldPosition;
        while (Vector3.Distance(transform.position, targetWorldPosition) > 0.05f)
        {
            playerRb2D.MovePosition(Vector3.Lerp(transform.position, targetWorldPosition, Time.fixedDeltaTime * moveSpeed));
            yield return null;
        }
        moveToTargetCoroutine = null;
        float endTime = Time.time - startTime;
        Debug.Log($"Actual Duration: {endTime}");
    }

    #region Debug Functions
    private void InitDebugSliceArea()
    {
        // Boundary of Slice Area
        debugLineRenderers.Add(GlobalHelper.CreateLineRenderer(
            gameObject,
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[0],
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[1],
            Color.green));
        debugLineRenderers.Add(GlobalHelper.CreateLineRenderer(
            gameObject,
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[1],
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[2],
            Color.green));
        debugLineRenderers.Add(GlobalHelper.CreateLineRenderer(
            gameObject,
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[2],
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[3],
            Color.green));
        debugLineRenderers.Add(GlobalHelper.CreateLineRenderer(
            gameObject,
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[3],
            () => (Vector3)debugSliceAreaStartPosition + (Vector3)debugSliceArea[0],
            Color.green));
    }
    #endregion
}

