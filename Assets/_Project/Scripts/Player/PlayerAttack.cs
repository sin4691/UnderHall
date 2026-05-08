using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    private Player player;

    private int currentCombo = 0;
    private bool isNextAttackBuffered = false;
    private readonly int maxCombo = 3;

    private Coroutine attackCoroutine;
    private bool isAttackOnCooldown = false;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    public void ExecuteAttack()
    {
        if (isAttackOnCooldown) return;

        if (player.CurrentState != PlayerState.Attack)
        {
            attackCoroutine = StartCoroutine(ComboAttackRoutine());
        }
        else
        {
            if (currentCombo < maxCombo)
            {
                isNextAttackBuffered = true;
            }
        }
    }

    public void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        currentCombo = 0;
        isNextAttackBuffered = false;
        isAttackOnCooldown = false;
    }

    private IEnumerator ComboAttackRoutine()
    {
        player.ChangeState(PlayerState.Attack);
        player.animator.SetBool("isMoving", false);
        currentCombo = 1;

        while (currentCombo <= maxCombo)
        {
            isNextAttackBuffered = false;

            LookAtMouse();

            player.animator.CrossFade("attack" + currentCombo, 0.02f);

            yield return new WaitForSeconds(0.05f);

            ExecuteHitDetection();

            AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);

            float waitTime = stateInfo.length * 0.5f;
            yield return new WaitForSeconds(waitTime);

            if (isNextAttackBuffered)
            {
                currentCombo++;
            }
            else
            {
                break;
            }
        }

        currentCombo = 0;
        player.animator.CrossFade("idle", 0.15f);
        player.ChangeState(PlayerState.Idle);

        StartCoroutine(AttackCooldownRoutine());
    }

    private void ExecuteHitDetection()
    {   
        Vector3 hitCenter = transform.position + transform.forward * (player.playerData.attackRange * 0.5f);
        hitCenter.y += 1f;

        float hitRadius = player.playerData.attackRange * 0.5f;

        Collider[] colliders = Physics.OverlapSphere(hitCenter, hitRadius);

        foreach (Collider col in colliders)
        {
            if (col.gameObject == player.gameObject) continue;

            EnemyBase target = col.GetComponentInParent<EnemyBase>();

            if (target != null)
            {
                target.TakeDamage(player.playerData.damage);
            }
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isAttackOnCooldown = true;
        yield return new WaitForSeconds(player.playerData.attackCooldown);
        isAttackOnCooldown = false;
    }

    private void LookAtMouse()
    { 
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            Vector3 lookDirection = (hitPoint - transform.position).normalized;
            lookDirection.y = 0f;

            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null || player.playerData == null) return;

        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position + transform.forward * (player.playerData.attackRange * 0.5f);
        hitCenter.y += 1f;
        float hitRadius = player.playerData.attackRange * 0.5f;


        Gizmos.DrawWireSphere(hitCenter, hitRadius);
    }
}