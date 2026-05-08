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
        player.rb.linearVelocity = moveDirection * player.playerData.moveSpeed;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        player.animator.SetBool(isMovingHash, true);
    }

    private void StopMovement()
    {
        player.rb.linearVelocity = Vector3.zero;

        if (player.CurrentState == PlayerState.Idle)
        {
            player.animator.SetBool(isMovingHash, false);
        }
    }
}