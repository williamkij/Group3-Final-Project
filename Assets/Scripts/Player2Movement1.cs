using UnityEngine;

public class Player2Movement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (ZonePauseFrames.IsGamePaused)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        float moveInput = 0;
        if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1;
        if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1;
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground")) isGrounded = false;
    }
}
// using UnityEngine;

// public class Player2Movement : MonoBehaviour
// {
//     public float moveSpeed = 5f;
//     public float jumpForce = 10f;
//     private Rigidbody2D rb;
//     private bool isGrounded;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//     }

//     void Update()
//     {
//         float moveInput = 0;
//         if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1;
//         if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1;

//         rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

//         if (Input.GetKeyDown(KeyCode.UpArrow) && isGrounded)
//         {
//             rb.velocity = new Vector2(rb.velocity.x, jumpForce);
//         }

//         if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
//         else if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);
//     }

//     void OnCollisionEnter2D(Collision2D col)
//     {
//         if (col.gameObject.CompareTag("Ground")) isGrounded = true;
//     }

//     void OnCollisionExit2D(Collision2D col)
//     {
//         if (col.gameObject.CompareTag("Ground")) isGrounded = false;
//     }
// }