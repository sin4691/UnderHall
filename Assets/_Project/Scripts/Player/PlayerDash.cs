using UnityEngine;
using System.Collections;

public class PlayerDash : MonoBehaviour
{
    [Header("VFX")]
    public Transform dashVFXPoint; // VFXPoint_Dash 드래그

    private Player player;
    private readonly int doDashHash = Animator.StringToHash("doDash");

    private int currentDashCount;
    private Coroutine cooldownCoroutine; 

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {

        currentDashCount = player.playerData.maxDashCount;
    }

    public void ExecuteDash()
    {
        if (currentDashCount <= 0 || player.CurrentState == PlayerState.Dash) return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        currentDashCount--;

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
        }
        cooldownCoroutine = StartCoroutine(DashCooldownRoutine());

        if (player.CurrentState == PlayerState.Attack)
        {
            player.attack.CancelAttack();
        }

        player.GrantInvincibility(player.playerData.dashInvincibilityTime);
        player.ChangeState(PlayerState.Dash);
        player.animator.SetTrigger(doDashHash);
        player.animator.SetBool("isMoving", false);

        Vector2 input = player.GetInputVector();
        Vector3 dashDirection;

        if (input.sqrMagnitude > 0)
        {
            Transform camTransform = Camera.main.transform;
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            dashDirection = (forward * input.y + right * input.x).normalized;
            transform.rotation = Quaternion.LookRotation(dashDirection);
        }
        else
        {
            dashDirection = transform.forward;
        }

        //창우_대시 VFX 재생
        VFXManager.Instance.PlayDash
        (
          dashVFXPoint.position,
          dashDirection,
          player.playerData.dashDuration
        );
        //창우_대시

        float startTime = Time.time;
        while (Time.time < startTime + player.playerData.dashDuration)
        {
            player.rb.linearVelocity = dashDirection * player.playerData.dashSpeed;
            yield return null;
        }

        player.rb.linearVelocity = Vector3.zero;
        player.animator.CrossFade("idle", 0.1f);
        player.ChangeState(PlayerState.Idle);
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(player.playerData.dashCooldown);
        currentDashCount = player.playerData.maxDashCount;
    }
}