using UnityEngine;

public class ZonePauseFrames : MonoBehaviour
{
    public static bool IsGamePaused = false;
    [Header("UI Frames")]
    public GameObject frame1;
    public GameObject frame2;
    private enum State { Idle, Showing23, Showing24 }
    private State state = State.Idle;
    private float prevTimeScale = 1f;
    private bool hasTriggered = false;
    private void Awake()
    {
        if (frame1) frame1.SetActive(false);
        if (frame2) frame2.SetActive(false);
    }

    private void Update()
    {
        if (state == State.Showing23 && Clicked())
        {
            Showframe2();
        }
        else if (state == State.Showing24 && Clicked())
        {
            ResumeGame();
        }
    }

    private bool Clicked()
    {
        if (Input.GetMouseButtonDown(0)) return true;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) return true;
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (state != State.Idle) return;

        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            hasTriggered = true;
            PauseAndShowframe1();
        }
    }

    private void PauseAndShowframe1()
    {
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsGamePaused = true;
        if (frame2) frame2.SetActive(false);
        if (frame1) frame1.SetActive(true);
        state = State.Showing23;
    }

    private void Showframe2()
    {
        if (frame1) frame1.SetActive(false);
        if (frame2) frame2.SetActive(true);
        state = State.Showing24;
    }

    private void ResumeGame()
    {
        if (frame1) frame1.SetActive(false);
        if (frame2) frame2.SetActive(false);
        Time.timeScale = prevTimeScale <= 0 ? 1f : prevTimeScale;
        IsGamePaused = false;
        state = State.Idle;
    }
}