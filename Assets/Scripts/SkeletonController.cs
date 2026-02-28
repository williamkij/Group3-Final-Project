using UnityEngine;

public class SkeletonController : MonoBehaviour
{
    public float speed = 2f;
    public float patrolDistance = 3f;
    public float detectRange = 5f;
    public float attackRange = 1f;
    public int maxHealth = 3;

    private int currentHealth;
    private Animator anim;
    private Transform targetPlayer;
    private bool isDead = false;
    private Vector3 startPos;
    private int patrolDirection = 1;
    private float turnCooldown = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        startPos = transform.position;
    }

    void Update()
    {
        if (isDead) return;

        targetPlayer = GetNearestPlayer();
        float dist = targetPlayer != null ?
            Vector2.Distance(transform.position, targetPlayer.position) : Mathf.Infinity;

        if (dist <= attackRange)
        {
            anim.SetBool("isRunning", false);
            if (Random.Range(0, 2) == 0)
                anim.SetTrigger("attack1");
            else
                anim.SetTrigger("attack2");
        }
        else if (dist <= detectRange)
        {
            // 追玩家
            anim.SetBool("isRunning", true);
            Vector2 dir = (targetPlayer.position - transform.position).normalized;
            transform.Translate(dir * speed * Time.deltaTime);

            if (dir.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else
                transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            // 巡逻
            anim.SetBool("isRunning", true);
            turnCooldown -= Time.deltaTime;

            // 检测前方地面
            bool hasGround = CheckGroundAhead();
            // 检测前方墙壁
            bool hasWall = CheckWallAhead();

            float distFromStart = transform.position.x - startPos.x;
            bool reachedLimit = Mathf.Abs(distFromStart) >= patrolDistance;

            if (turnCooldown <= 0 && (!hasGround || hasWall || reachedLimit))
            {
                patrolDirection *= -1;
                transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
                turnCooldown = 0.5f;
            }

            transform.Translate(Vector2.right * patrolDirection * speed * Time.deltaTime);
        }
    }

    bool CheckGroundAhead()
    {
        // 在骷髅前方脚下发射向下的射线
        Vector2 origin = (Vector2)transform.position +
                         new Vector2(patrolDirection * 0.5f, -0.5f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.5f);
        Debug.DrawRay(origin, Vector2.down * 0.5f, Color.green); // Scene视图可见
        return hit.collider != null;
    }

    bool CheckWallAhead()
    {
        // 在骷髅前方发射水平射线检测墙壁
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin,
                           new Vector2(patrolDirection, 0), 0.4f);
        Debug.DrawRay(origin, new Vector2(patrolDirection * 0.4f, 0), Color.red);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    Transform GetNearestPlayer()
    {
        float minDist = Mathf.Infinity;
        Transform nearest = null;

        GameObject p1 = GameObject.FindWithTag("Player1");
        GameObject p2 = GameObject.FindWithTag("Player2");

        if (p1 != null)
        {
            float d = Vector2.Distance(transform.position, p1.transform.position);
            if (d < minDist) { minDist = d; nearest = p1.transform; }
        }
        if (p2 != null)
        {
            float d = Vector2.Distance(transform.position, p2.transform.position);
            if (d < minDist) { minDist = d; nearest = p2.transform; }
        }

        return nearest;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            isDead = true;
            anim.SetTrigger("die");
            Destroy(gameObject, 1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}