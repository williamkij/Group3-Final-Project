using UnityEngine;

public class BatController : MonoBehaviour
{
    public float speed = 2f;
    public float patrolDistance = 3f;
    public float detectRange = 6f;
    public float attackRange = 1.5f;
    public bool startHanging = true;
    public int maxHealth = 3;

    private Vector3 startPos;
    private int direction = 1;
    private Animator anim;
    private bool isHanging;
    private bool isDead = false;
    private Transform player1;
    private Transform player2;

    void Start()
    {
        startPos = transform.position;
        anim = GetComponent<Animator>();
        isHanging = startHanging;
        anim.SetBool("isHanging", isHanging);

        GameObject p1 = GameObject.FindWithTag("Player1");
        GameObject p2 = GameObject.FindWithTag("Player2");
        if (p1 != null) player1 = p1.transform;
        if (p2 != null) player2 = p2.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (isHanging)
        {
            if (GetNearestPlayerDistance() < detectRange)
            {
                isHanging = false;
                anim.SetBool("isHanging", false);
            }
            return;
        }

        if (GetNearestPlayerDistance() < attackRange)
        {
            anim.SetTrigger("attack");
            return;
        }

        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, startPos) >= patrolDistance)
        {
            direction *= -1;
            transform.localScale = new Vector3(-transform.localScale.x,
                                                transform.localScale.y, 1);
        }
    }

    float GetNearestPlayerDistance()
    {
        float minDist = Mathf.Infinity;
        if (player1 != null)
        {
            float d1 = Vector2.Distance(transform.position, player1.position);
            if (d1 < minDist) minDist = d1;
        }
        if (player2 != null)
        {
            float d2 = Vector2.Distance(transform.position, player2.position);
            if (d2 < minDist) minDist = d2;
        }
        return minDist;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        // 之后加血量系统时在这里减血
        isDead = true;
        anim.SetTrigger("die");
        Destroy(gameObject, 1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}