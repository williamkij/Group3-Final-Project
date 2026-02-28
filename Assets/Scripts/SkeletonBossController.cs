using UnityEngine;
using System.Collections;

public class SkeletonBossController : MonoBehaviour
{
    public float speed = 2f;
    public float dashSpeed = 8f;
    public float detectRange = 7f;
    public float attackRange = 1.2f;
    public int maxHealth = 10;
    public float patrolRange = 3f;

    private int currentHealth;
    private Animator anim;
    private Transform targetPlayer;
    private bool isDead = false;
    private bool isStunned = false;
    private bool isDashing = false;
    private Vector3 startPos;
    private int patrolDirection = 1;
    private float turnCooldown = 0f;
    private float attackCooldown = 0f;
    private float dashCooldown = 0f;
    private Vector2 dashDirection;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        startPos = transform.position;
        patrolDirection = 1;
        transform.localScale = new Vector3(1, 1, 1);
    }

    void Update()
    {
        if (isDead || isStunned) return;

        targetPlayer = GetNearestPlayerInRange();
        float dist = targetPlayer != null ?
            Vector2.Distance(transform.position, targetPlayer.position) : Mathf.Infinity;

        attackCooldown -= Time.deltaTime;
        dashCooldown -= Time.deltaTime;
        turnCooldown -= Time.deltaTime;

        if (isDashing)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);
            return;
        }

        if (targetPlayer != null)
        {
            if (dist <= attackRange && attackCooldown <= 0)
            {
                // ½üÉí¹¥»÷£¬Í£ÏÂÀ´
                anim.SetBool("isRunning", false);
                int rand = Random.Range(0, 2);
                anim.SetTrigger(rand == 0 ? "attack1" : "attack2");
                attackCooldown = 1.5f;
            }
            else if (dist > attackRange)
            {
                // ×·Íæ¼Ò
                anim.SetBool("isRunning", true);
                float dirX = targetPlayer.position.x - transform.position.x;
                Vector2 dir = new Vector2(dirX, 0).normalized;
                transform.Translate(dir * speed * Time.deltaTime);
                transform.localScale = new Vector3(dirX > 0 ? 1 : -1, 1, 1);

                // Ëæ»ú³å´Ì
                if (dashCooldown <= 0 && dist > attackRange * 2f)
                {
                    StartDash(dir);
                }
            }
        }
        else
        {
            // Ñ²Âß
            anim.SetBool("isRunning", true);

            bool hasGround = CheckGroundAhead();
            bool hasWall = CheckWallAhead();
            float distFromStart = transform.position.x - startPos.x;
            bool reachedLimit = (patrolDirection == 1 && distFromStart >= patrolRange) ||
                                (patrolDirection == -1 && distFromStart <= -patrolRange);

            if (turnCooldown <= 0 && (!hasGround || hasWall || reachedLimit))
            {
                patrolDirection *= -1;
                transform.localScale = new Vector3(patrolDirection, 1, 1);
                turnCooldown = 0.8f;
            }

            transform.Translate(Vector2.right * patrolDirection * speed * Time.deltaTime);
        }
    }

    Transform GetNearestPlayerInRange()
    {
        float minDist = Mathf.Infinity;
        Transform nearest = null;

        GameObject p1 = GameObject.FindWithTag("Player1");
        GameObject p2 = GameObject.FindWithTag("Player2");

        if (p1 != null)
        {
            float d = Vector2.Distance(transform.position, p1.transform.position);
            if (d < minDist && d <= detectRange) { minDist = d; nearest = p1.transform; }
        }
        if (p2 != null)
        {
            float d = Vector2.Distance(transform.position, p2.transform.position);
            if (d < minDist && d <= detectRange) { minDist = d; nearest = p2.transform; }
        }

        return nearest;
    }

    void StartDash(Vector2 dir)
    {
        isDashing = true;
        dashDirection = dir;
        anim.SetTrigger("dash");
        dashCooldown = 4f;
        Invoke(nameof(StopDash), 0.4f);
    }

    void StopDash()
    {
        isDashing = false;
    }

    bool CheckGroundAhead()
    {
        // ´Ó½Å²¿Ç°·½ÍùÏÂÉäÏß¼ì²âµØÃæ
        Vector2 origin = (Vector2)transform.position +
                         new Vector2(patrolDirection * 0.6f, -0.3f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 1.0f);
        Debug.DrawRay(origin, Vector2.down * 1.0f, Color.green);
        return hit.collider != null;
    }

    bool CheckWallAhead()
    {
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin,
                           new Vector2(patrolDirection, 0), 0.6f);
        Debug.DrawRay(origin, new Vector2(patrolDirection * 0.6f, 0), Color.red);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            isDead = true;
            anim.SetTrigger("die");
            Destroy(gameObject, 1.5f);
        }
        else
        {
            if (Random.Range(0, 3) == 0)
            {
                StartCoroutine(StunRoutine());
            }
        }
    }

    IEnumerator StunRoutine()
    {
        isStunned = true;
        anim.SetTrigger("stun");
        yield return new WaitForSeconds(1.5f);
        isStunned = false;
        anim.SetBool("isRunning", false);
    }

    void OnDrawGizmosSelected()
    {
        // »ÆÉ«£ºË÷µÐ·¶Î§
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        // ºìÉ«£º¹¥»÷·¶Î§
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        // À¶É«£ºÑ²Âß·¶Î§
        Gizmos.color = Color.cyan;
        Vector3 center = Application.isPlaying ? startPos : transform.position;
        Gizmos.DrawWireCube(center, new Vector3(patrolRange * 2, 1, 0));
    }
}