using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RunnerPlayer : MonoBehaviour
{
    public float runSpeed = 6f;
    public float jumpPower = 12f;

    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.12f;

    public int maxLives = 3;
    public TMP_Text livesText;
    public float invincibleTime = 1f;

    public float playTime = 30f;
    public TMP_Text timerText;
    public TMP_Text resultText;

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

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
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
        {
            if (restartPressed)
            {
                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().name
                );
            }

            return;
        }

        // 바닥 아래로 떨어져도 실패
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
            livesText.text = "HP : " + currentLives;
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

    private void FinishGame(bool success)
    {
        if (isFinished)
            return;

        isFinished = true;
        jumpRequested = false;

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);

            resultText.text = success
                ? "SUCCESS!\nPress R to restart"
                : "GAME OVER\nPress R to restart";
        }

        Debug.Log(success ? "게임 성공!" : "게임 실패!");
    }
}