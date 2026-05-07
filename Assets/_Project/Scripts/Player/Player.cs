using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState { Idle, Move, Attack, Dash }

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerDash))]
public class Player : MonoBehaviour
{
    public PlayerData playerData;
    public PlayerState CurrentState { get; private set; }

    public Rigidbody rb { get; private set; }
    public PlayerMovement movement { get; private set; }
    public PlayerAttack attack { get; private set; }
    public Animator animator { get; private set; }
    public PlayerDash dash { get; private set; }

    private Vector2 inputVector;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        dash = GetComponent<PlayerDash>();
        animator = GetComponent<Animator>();

        CurrentState = PlayerState.Idle;
    }

    public void ChangeState(PlayerState newState)
    {
        CurrentState = newState;
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            dash.ExecuteDash();
        }
    }

    public void OnMove(InputValue value)
    {
        inputVector = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && CurrentState != PlayerState.Dash)
        {
            attack.ExecuteAttack();
        }
    }

    public Vector2 GetInputVector() => inputVector;
}