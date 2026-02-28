using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 史莱姆敌人控制器
/// 动画状态：idle, run, die
/// 行为：巡逻(idle) → 发现玩家 → 追击(run) → 接触伤害
/// 挂载需要：Rigidbody2D / CapsuleCollider2D / SpriteRenderer / Animator
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class SlimeEnemy : MonoBehaviour
{
    [Header("移动参数")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float patrolRange = 4f;

    [Header("检测参数")]
    public float detectionRange = 6f;   // 索敌范围
    public float damageRange = 0.6f; // 接触伤害范围
    public LayerMask groundLayer;

    [Header("战斗参数")]
    public int maxHealth = 40;
    public int contactDamage = 10;   // 接触造成的伤害
    public float damageCooldown = 0.8f; // 接触伤害冷却

    // ── Animator 参数 ────────────────────────────────────────
    private static readonly int ParamSpeed = Animator.StringToHash("speed");
    private static readonly int ParamIsDead = Animator.StringToHash("isDead");

    // ── 组件 ─────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    // ── 玩家 ─────────────────────────────────────────────────
    private List<Transform> players = new List<Transform>();
    private Transform target;

    // ── 状态 ─────────────────────────────────────────────────
    private enum State { Patrol, Chase }
    private State state = State.Patrol;

    private int currentHealth;
    private bool isDead = false;
    private float damageTimer = 0f;

    // ── 巡逻 ─────────────────────────────────────────────────
    private float patrolOriginX;
    private int patrolDir = 1;
    private float dirChangeTimer = 0f;

    // ── 检测点 ───────────────────────────────────────────────
    private Transform groundCheckTr;
    private Transform wallCheckTr;
    private const float CHECK_RADIUS = 0.08f;

    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;
        patrolOriginX = transform.position.x;

        groundCheckTr = CreatePoint("_GroundCheck", new Vector3(0.2f, -0.15f, 0));
        wallCheckTr = CreatePoint("_WallCheck", new Vector3(0.3f, 0.0f, 0));
    }

    Transform CreatePoint(string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    void Start()
    {
        foreach (var o in GameObject.FindGameObjectsWithTag("Player1"))
            players.Add(o.transform);
        foreach (var o in GameObject.FindGameObjectsWithTag("Player2"))
            players.Add(o.transform);
    }

    void Update()
    {
        if (isDead) return;

        if (damageTimer > 0f) damageTimer -= Time.deltaTime;
        if (dirChangeTimer > 0f) dirChangeTimer -= Time.deltaTime;

        DecideState();
        ExecuteState();
        TryContactDamage();
    }

    // ── 状态决策 ─────────────────────────────────────────────
    void DecideState()
    {
        float minDist = detectionRange;
        target = null;

        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector2.Distance(transform.position, p.position);
            if (d < minDist) { minDist = d; target = p; }
        }

        state = (target != null) ? State.Chase : State.Patrol;
    }

    // ── 执行状态 ─────────────────────────────────────────────
    void ExecuteState()
    {
        switch (state)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase: DoChase(); break;
        }
    }

    void DoPatrol()
    {
        // 超出范围强制反向
        float relX = transform.position.x - patrolOriginX;
        if (relX > patrolRange && patrolDir == 1) FlipDir();
        if (relX < -patrolRange && patrolDir == -1) FlipDir();

        // 前方无地 / 碰墙反向
        if (dirChangeTimer <= 0f && IsWallAhead())
            FlipDir();

        rb.velocity = new Vector2(patrolDir * patrolSpeed, rb.velocity.y);
        SetFacing(patrolDir);
        anim.SetFloat(ParamSpeed, 0f); // 巡逻时播 idle
    }

    void DoChase()
    {
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * chaseSpeed, rb.velocity.y);
        SetFacing((int)dir);
        anim.SetFloat(ParamSpeed, chaseSpeed); // 追击时播 run
    }

    // ── 接触伤害 ─────────────────────────────────────────────
    void TryContactDamage()
    {
        if (damageTimer > 0f) return;

        foreach (var p in players)
        {
            if (p == null) continue;
            if (Vector2.Distance(transform.position, p.position) <= damageRange)
            {
                p.SendMessage("TakeDamage", contactDamage, SendMessageOptions.DontRequireReceiver);
                damageTimer = damageCooldown;
                break;
            }
        }
    }

    // ── 受伤 / 死亡（外部调用）───────────────────────────────
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (currentHealth <= 0)
            StartCoroutine(DieRoutine());
        else
        {
            // 史莱姆没有 hurt 动画，轻微击退即可
            float knockDir = -patrolDir;
            rb.velocity = new Vector2(knockDir * 2f, 1.5f);
        }
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        anim.SetFloat(ParamSpeed, 0f);
        anim.SetBool(ParamIsDead, true);

        GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(1.2f);
        Destroy(gameObject);
    }

    // ── 工具 ─────────────────────────────────────────────────
    void FlipDir()
    {
        if (dirChangeTimer > 0f) return;
        patrolDir = -patrolDir;
        dirChangeTimer = 0.4f;
    }

    void SetFacing(int dir)
    {
        if (dir == 0) return;
        bool right = dir > 0;
        sr.flipX = !right;

        float sign = right ? 1f : -1f;
        wallCheckTr.localPosition = new Vector3(
            Mathf.Abs(wallCheckTr.localPosition.x) * sign,
            wallCheckTr.localPosition.y, 0);
        groundCheckTr.localPosition = new Vector3(
            Mathf.Abs(groundCheckTr.localPosition.x) * sign,
            groundCheckTr.localPosition.y, 0);
    }

    bool IsGroundAhead() =>
        Physics2D.OverlapCircle(groundCheckTr.position, CHECK_RADIUS, groundLayer);

    bool IsWallAhead() =>
        Physics2D.OverlapCircle(wallCheckTr.position, CHECK_RADIUS, groundLayer);

    // ── Gizmos ───────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        float ox = Application.isPlaying ? patrolOriginX : transform.position.x;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(ox - patrolRange, transform.position.y),
                        new Vector3(ox + patrolRange, transform.position.y));

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, damageRange);
    }
}