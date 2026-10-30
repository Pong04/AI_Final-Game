using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RunnerPlayer : MonoBehaviour
{
    [Header("이동")]
    public float runSpeed = 6f;
    public float jumpPower = 12f;

    [Header("바닥 확인")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.12f;

    [Header("목숨")]
    public int maxLives = 3;
    public TMP_Text livesText;
    public float invincibleTime = 1f;

    [Header("제한시간")]
    public float playTime = 60f;
    public TMP_Text timerText;

    [Header("결과창")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    public MiniGameSceneFlow sceneFlow;

    private int currentLives;
    private float remainingTime;

    private Rigidbody2D rb;
    private bool jumpRequested;
    private bool isFinished;
    private bool isInvincible;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        currentLives = maxLives;
        remainingTime = playTime;

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // 기존 코드에서 꺼진 텍스트도 다시 활성화
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
        }

        UpdateLivesText();
        UpdateTimerText();
    }

    private void Update()
    {
        bool jumpPressed;
        bool restartPressed;

#if ENABLE_INPUT_SYSTEM
        jumpPressed = Keyboard.current != null &&
                      Keyboard.current.spaceKey.wasPressedThisFrame;

        restartPressed = Keyboard.current != null &&
                         Keyboard.current.rKey.wasPressedThisFrame;
#else
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        restartPressed = Input.GetKeyDown(KeyCode.R);
#endif

        if (isFinished)
            return;

        if (transform.position.y < -10f)
        {
            FinishGame(false);
            return;
        }

        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime
        );

        UpdateTimerText();

        if (remainingTime <= 0f)
        {
            FinishGame(true);
            return;
        }

        if (jumpPressed)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (isFinished)
            return;

        bool isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        ) != null;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = runSpeed;

        if (jumpRequested && isGrounded)
        {
            velocity.y = jumpPower;
        }

        rb.linearVelocity = velocity;
        jumpRequested = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFinished || !other.CompareTag("Obstacle"))
            return;

        // 장애물을 통과하고 계속 달리기
        other.gameObject.SetActive(false);

        if (isInvincible)
            return;

        currentLives = Mathf.Max(0, currentLives - 1);
        UpdateLivesText();

        if (currentLives <= 0)
        {
            FinishGame(false);
            return;
        }

        StartCoroutine(Invincibility());
    }

    private IEnumerator Invincibility()
    {
        isInvincible = true;

        yield return new WaitForSeconds(invincibleTime);

        isInvincible = false;
    }

    private void UpdateLivesText()
    {
        if (livesText != null)
        {
            livesText.text = "LIFE : " + currentLives;
        }
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text =
                "TIME : " + Mathf.CeilToInt(remainingTime);
        }
    }

    private void FinishGame(bool survived)
    {
        if (isFinished)
            return;

        isFinished = true;
        jumpRequested = false;

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        string grade;

        if (!survived)
        {
            grade = "F";
        }
        else if (currentLives == maxLives)
        {
            grade = "A";
        }
        else
        {
            grade = "B";
        }

        float elapsedTime = Mathf.Clamp(
            playTime - remainingTime,
            0f,
            playTime
        );

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultText != null)
        {
            string title = survived ? "클리어!" : "실패";

            resultText.text =
                title + "\n\n" +
                "등급 : " + grade + "\n" +
                "생존 시간 : " + elapsedTime.ToString("F1") + "초\n" +
                "남은 목숨 : " + currentLives;
        }

        if (sceneFlow != null)
        {
            sceneFlow.OnGameFinished();
        }
    }
}