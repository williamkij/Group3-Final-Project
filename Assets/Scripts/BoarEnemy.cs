using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 野猪敌人控制器
/// 挂载到野猪 GameObject 上，需要：
///   - Rigidbody2D (Dynamic, Freeze Rotation Z, Collision Detection: Continuous)
///   - CapsuleCollider2D
///   - SpriteRenderer
///   - Animator (绑定 Boar.controller)
/// 玩家 Tag 设为 Player1 / Player2
/// 地面 Tilemap Layer 设为 Ground，并在脚本 Inspector 的 Ground Layer 选 Ground
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class BoarEnemy : MonoBehaviour
{
    [Header("移动参数")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float patrolRange = 3f;   // 左右巡逻半径

    [Header("检测参数")]
    public float detectionRange = 8f;
    public float attackRange = 1.2f;
    public LayerMask groundLayer;    // 选 Ground
    public LayerMask playerLayer;    // 选玩家所在层（可不填，用 Tag 检测）

    [Header("战斗参数")]
    public int maxHealth = 80;
    public int attackDamage = 15;
    public float attackCooldown = 1.2f;
    public float hurtStunTime = 0.3f;

    // ── Animator 参数（需与 Controller 中名称一致）───────────
    private static readonly int ParamSpeed = Animator.StringToHash("speed");
    private static readonly int ParamIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int ParamIsDead = Animator.StringToHash("isDead");
    private static readonly int ParamIsHurt = Animator.StringToHash("isHurt");

    // ── 组件引用 ─────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;

    // ── 玩家列表 ─────────────────────────────────────────────
    private List<Transform> players = new List<Transform>();
    private Transform target; // 当前锁定目标

    // ── 状态 ─────────────────────────────────────────────────
    private enum State { Patrol, Chase, Attack }
    private State state = State.Patrol;

    private int currentHealth;
    private bool isDead = false;
    private bool isHurt = false;
    private bool isAttacking = false;
    private float attackTimer = 0f;

    // ── 巡逻 ─────────────────────────────────────────────────
    private float patrolOriginX;  // 出生时 X 坐标
    private int patrolDir = 1;  // 1=右 -1=左
    private float dirChangeTimer = 0f; // 防抖：切换方向冷却

    // ── 地面 / 墙壁检测（脚本内部生成，不需要手动挂）──────────
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

        // 自动创建检测点子对象
        groundCheckTr = CreateCheckPoint("_GroundCheck", new Vector3(0.25f, -0.18f, 0));
        wallCheckTr = CreateCheckPoint("_WallCheck", new Vector3(0.35f, 0.05f, 0));
    }

    Transform CreateCheckPoint(string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    void Start()
    {
        // 收集场景内两个玩家
        foreach (var o in GameObject.FindGameObjectsWithTag("Player1"))
            players.Add(o.transform);
        foreach (var o in GameObject.FindGameObjectsWithTag("Player2"))
            players.Add(o.transform);
    }

    // ── 主循环 ───────────────────────────────────────────────
    void Update()
    {
        if (isDead) return;

        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (dirChangeTimer > 0f) dirChangeTimer -= Time.deltaTime;

        if (!isHurt && !isAttacking)
        {
            DecideState();
            ExecuteState();
        }
        else
        {
            // 受伤 / 攻击中停止横向移动
            rb.velocity = new Vector2(0, rb.velocity.y);
            anim.SetFloat(ParamSpeed, 0f);
        }
    }

    // ── 状态决策 ─────────────────────────────────────────────
    void DecideState()
    {
        Transform nearest = GetNearestPlayerInRange(detectionRange);

        if (nearest != null)
        {
            target = nearest;
            float dist = Vector2.Distance(transform.position, target.position);
            state = dist <= attackRange ? State.Attack : State.Chase;
        }
        else
        {
            target = null;
            state = State.Patrol;
        }
    }

    // 找 detectionRange 以内最近的玩家
    Transform GetNearestPlayerInRange(float range)
    {
        Transform nearest = null;
        float minDist = range;

        foreach (var p in players)
        {
            if (p == null) continue;
            float d = Vector2.Distance(transform.position, p.position);
            if (d < minDist) { minDist = d; nearest = p; }
        }
        return nearest;
    }

    // ── 状态执行 ─────────────────────────────────────────────
    void ExecuteState()
    {
        switch (state)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase: DoChase(); break;
            case State.Attack: DoAttack(); break;
        }
    }

    // ── 巡逻：固定方向走，遇边缘/墙才换向，有防抖 ────────────
    void DoPatrol()
    {
        // 超出巡逻范围 → 强制反向
        float relX = transform.position.x - patrolOriginX;
        if (relX > patrolRange && patrolDir == 1) TryFlipPatrol();
        if (relX < -patrolRange && patrolDir == -1) TryFlipPatrol();

        // 前方无地 或 碰到墙 → 换向
        if (dirChangeTimer <= 0f && IsWallAhead())
            TryFlipPatrol();

        rb.velocity = new Vector2(patrolDir * walkSpeed, rb.velocity.y);
        SetFacing(patrolDir);
        anim.SetFloat(ParamSpeed, walkSpeed);
    }

    void TryFlipPatrol()
    {
        if (dirChangeTimer > 0f) return; // 冷却中，忽略
        patrolDir = -patrolDir;
        dirChangeTimer = 0.4f; // 0.4 秒内不再换向，防止抖动
    }

    // ── 追击 ─────────────────────────────────────────────────
    void DoChase()
    {
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        rb.velocity = new Vector2(dir * runSpeed, rb.velocity.y);
        SetFacing((int)dir);
        anim.SetFloat(ParamSpeed, runSpeed);
    }

    // ── 攻击 ─────────────────────────────────────────────────
    void DoAttack()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        anim.SetFloat(ParamSpeed, 0f);

        if (target != null)
            SetFacing((int)Mathf.Sign(target.position.x - transform.position.x));

        if (attackTimer <= 0f)
            StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        anim.SetBool(ParamIsAttacking, true);

        yield return new WaitForSeconds(0.3f); // 命中帧

        foreach (var p in players)
        {
            if (p == null) continue;
            if (Vector2.Distance(transform.position, p.position) <= attackRange * 1.3f)
                p.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }

        yield return new WaitForSeconds(0.5f);

        anim.SetBool(ParamIsAttacking, false);
        isAttacking = false;
    }

    // ── 受伤（外部调用）──────────────────────────────────────
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (currentHealth <= 0) StartCoroutine(DieRoutine());
        else StartCoroutine(HurtRoutine());
    }

    IEnumerator HurtRoutine()
    {
        isHurt = true;
        anim.SetBool(ParamIsHurt, true);

        float knockDir = (patrolDir == 1) ? -1f : 1f;
        rb.velocity = new Vector2(knockDir * 3f, 2f);

        yield return new WaitForSeconds(hurtStunTime);

        anim.SetBool(ParamIsHurt, false);
        isHurt = false;
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        anim.SetFloat(ParamSpeed, 0f);
        anim.SetBool(ParamIsAttacking, false);
        anim.SetBool(ParamIsDead, true);

        GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    // ── 工具 ─────────────────────────────────────────────────
    void SetFacing(int dir)
    {
        if (dir == 0) return;
        bool right = dir > 0;
        sr.flipX = !right;

        // WallCheck 和 GroundCheck 随朝向镜像
        float sign = right ? 1f : -1f;
        wallCheckTr.localPosition = new Vector3(
            Mathf.Abs(wallCheckTr.localPosition.x) * sign,
            wallCheckTr.localPosition.y, 0);
        groundCheckTr.localPosition = new Vector3(
            Mathf.Abs(groundCheckTr.localPosition.x) * sign,
            groundCheckTr.localPosition.y, 0);
    }

    // 前方脚下有无地面
    bool IsGroundAhead() =>
        Physics2D.OverlapCircle(groundCheckTr.position, CHECK_RADIUS, groundLayer);

    // 前方有无墙壁
    bool IsWallAhead() =>
        Physics2D.OverlapCircle(wallCheckTr.position, CHECK_RADIUS, groundLayer);

    // ── Gizmos ───────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // 巡逻范围
        float ox = Application.isPlaying ? patrolOriginX : transform.position.x;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(ox - patrolRange, transform.position.y),
                        new Vector3(ox + patrolRange, transform.position.y));

        // 索敌范围
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 攻击范围
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 检测点
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckTr.position, CHECK_RADIUS);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallCheckTr.position, CHECK_RADIUS);
        }
    }
}