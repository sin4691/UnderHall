using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private Transform target;

    [Header("--- 상태 설정 ---")]
    [SerializeField] private float maxHp = 50f;
    private float currentHp;
    private bool isDead = false;

    [Header("--- 설정 ---")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackDelay = 2.0f;
    private float timer;

    [Header("--- 히트박스 설정 ---")]
    [SerializeField] private float hitDelay = 0.5f;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitOffset = 1.5f;
    [SerializeField] private float attackDamage = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        target = GameObject.FindGameObjectWithTag("Player").transform;

        currentHp = maxHp;
        agent.stoppingDistance = attackRange;
    }

    void Update()
    {
        if (isDead || target == null || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= attackRange)
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
        if (timer >= attackDelay)
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
                Debug.Log($"<color=red>플레이어 히트! 데미지: {attackDamage}</color>");
                hasHitPlayer = true;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHp -= damage;
        Debug.Log($"몬스터 피격! 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            anim.SetTrigger("Hurt");
        }
    }

    void Die()
    {
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false; 

        anim.SetTrigger("Death");
        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 hitPosition = transform.position + transform.forward * hitOffset;
        Gizmos.DrawWireSphere(hitPosition, hitRadius);
    }
}