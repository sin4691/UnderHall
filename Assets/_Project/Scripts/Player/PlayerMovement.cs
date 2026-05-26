using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Player player;
    private Vector3 moveDirection;
    public float rotationSpeed = 15f;
    private Camera mainCam;

    private readonly int isMovingHash = Animator.StringToHash("isMoving");

    private void Awake()
    {
        player = GetComponent<Player>();
        mainCam = Camera.main;
    }

    private void Update()
    {
        Vector2 input = player.GetInputVector();
        Transform camTransform = mainCam.transform;
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        moveDirection = (forward * input.y + right * input.x).normalized;

        if (player.CurrentState == PlayerState.Idle || player.CurrentState == PlayerState.Move)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                player.ChangeState(PlayerState.Move);
            }
            else
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
    }

    private void FixedUpdate()
    {
        switch (player.CurrentState)
        {
            case PlayerState.Idle:
                StopMovement();
                break;
            case PlayerState.Move:
                ApplyMovement();
                break;
            case PlayerState.Attack:
                StopMovement();
                break;
            case PlayerState.Dead:
                StopMovement();
                break;
            case PlayerState.Dash:            
                break;
            case PlayerState.SpecialAttack:
                ApplyMovement();
                break;
        }
    }

    private void ApplyMovement()
    {
        Vector3 targetVelocity = moveDirection * player.playerData.moveSpeed;

        // 바닥 레이캐스트 (발목 살짝 위에서 0.5m 아래로 발사)
        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.5f))
        {
            // 💡 [핵심 기술!] 대시에서 쓰셨던 지형 적응 로직을 걷기에도 적용!
            // 이동 방향(targetVelocity)을 바닥의 기울기(hit.normal)에 맞춰 비스듬하게 꺾어줍니다.
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, hit.normal).normalized * player.playerData.moveSpeed;

            // 계단을 타고 올라가는 중(targetVelocity.y > 0)이 아닐 때만 땅에 붙여줍니다.
            if (targetVelocity.y <= 0f)
            {
                // 평지나 내리막길에서는 살짝만 눌러줘서 붕 뜨는 것을 방지
                targetVelocity.y -= 2f;
            }
            else
            {
                // 계단이나 오르막을 오를 때는 물리 엔진이 캐릭터를 위로 밀어 올리려는 힘을 방해하지 않고 그대로 존중합니다!
                targetVelocity.y = Mathf.Max(targetVelocity.y, player.rb.linearVelocity.y);
            }
        }
        else
        {
            // 공중에 완전히 떠 있을 때는 기존 중력(낙하 속도) 유지
            targetVelocity.y = player.rb.linearVelocity.y;
        }

        player.rb.linearVelocity = targetVelocity;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        player.animator.SetBool(isMovingHash, true);
    }

    private void StopMovement()
    {
        player.rb.linearVelocity = new Vector3(0f, player.rb.linearVelocity.y, 0f);

        if (player.CurrentState == PlayerState.Idle)
        {
            player.animator.SetBool(isMovingHash, false);
        }
    }

    public void PlayFootstepSound()
    {
        int randomIndex = Random.Range(1, 11);

        string soundName = "Footstep_" + randomIndex;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }
}