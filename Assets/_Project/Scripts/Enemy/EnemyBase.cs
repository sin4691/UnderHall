using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform target;

    private float currentHp;
    private float timer;
    private bool isDead = false;

    [SerializeField] private float hitDelay = 0.5f;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitOffset = 1.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Player").transform;

        if (enemyData != null)
        {
            currentHp = enemyData.maxHealth;
            agent.speed = enemyData.moveSpeed;
            agent.stoppingDistance = enemyData.attackRange;
        }
    }

    void Update()
    {
        if (isDead || target == null || enemyData == null || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= enemyData.attackRange + 0.5f)
        {
            agent.isStopped = true;
            Attack();
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        float speed = agent.isStopped ? 0 : agent.velocity.magnitude;
        anim.SetFloat("MoveSpeed", speed);
    }

    void Attack()
    {
        timer += Time.deltaTime;
        if (timer >= enemyData.attackCooldown)
        {
            anim.SetTrigger("Attack");
            timer = 0;
            StartCoroutine(DealDamageCoroutine());
        }
    }

    IEnumerator DealDamageCoroutine()
    {
        yield return new WaitForSeconds(hitDelay);
        if (isDead) yield break;

        Vector3 hitPosition = transform.position + transform.forward * hitOffset;
        Collider[] hitColliders = Physics.OverlapSphere(hitPosition, hitRadius);

        bool hasHitPlayer = false;

        foreach (Collider hit in hitColliders)
        {
            if (hasHitPlayer) break;

            if (hit.CompareTag("Player"))
            {
                Debug.Log($"<color=red>플레이어 히트! 데미지: {enemyData.damage}</color>");
                hasHitPlayer = true;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;
        Debug.Log($"{enemyData.enemyName} 피격! 남은 체력: {currentHp}");

        if (currentHp <= 0) Die();
        else anim.SetTrigger("Hurt");
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false;
        anim.SetTrigger("Death");

        Debug.Log($"{enemyData.enemyName} 처치! {enemyData.goldDrop} 골드 획득!");
        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;
        Gizmos.color = Color.red;
        Vector3 hitPosition = transform.position + transform.forward * hitOffset;
        Gizmos.DrawWireSphere(hitPosition, hitRadius);
    }
}