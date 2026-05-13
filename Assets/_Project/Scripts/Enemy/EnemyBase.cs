using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    [SerializeField] protected EnemyData enemyData;

    public float detectRange = 10f;
    public float attackRange = 2f;

    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform target;

    private float currentHealth;
    private float timer;
    protected bool isDead = false;

    [Header("Attack Settings")]
    [SerializeField] private float hitDelay = 0.5f;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitOffset = 1.5f;
    [SerializeField] private float hitHeight = 1.5f;

    //VFX 코드를 실행하기 위한 위치 변수
    [Header("VFX Settings")]
    [SerializeField] protected Transform vfxPoint;

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
        }

        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            agent.speed = enemyData.moveSpeed;

            // 스폰 즉시 공격할 수 있도록 쿨타임을 꽉 채워둡니다.
            timer = enemyData.attackCooldown;

            // 내브메쉬가 지멋대로 몸 돌리는 걸 금지시킵니다.
            agent.updateRotation = false;

            // 몬스터가 플레이어 안으로 파고들지 않도록 네비메쉬 정지 거리를 공격 거리로 맞춥니다.
            agent.stoppingDistance = attackRange;
        }
    }

    protected virtual void Update()
    {
        if (isDead || target == null || enemyData == null || !agent.isOnNavMesh) return;

        // 이동 중이든 대기 중이든 항상 쿨타임을 회복합니다.
        timer += Time.deltaTime;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= enemyData.detectionRange)
        {
            // 쫓아갈 때 무조건 플레이어 쪽으로 몸을 부드럽게 돌립니다.
            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0; // 몬스터가 땅을 파거나 하늘을 보지 않게 Y축 고정

            if (lookDir != Vector3.zero)
            {
                // 숫자가 클수록 고개를 빨리 돌립니다.
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

            // agent.stoppingDistance 대신, 직접 설정한 attackRange(공격 사거리)를 사용합니다.
            if (dist <= attackRange)
            {
                Attack();
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
        else
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 애니메이션 속도 조절
        float speed = (agent.isStopped || dist <= attackRange) ? 0f : agent.velocity.magnitude;
        anim.SetFloat("MoveSpeed", speed, 0.05f, Time.deltaTime);
    }

    protected virtual void Attack()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Update에서 이미 부드럽게 회전하고 있으므로, 여기서 갑자기 확 돌아보는 코드는 삭제했습니다.

        // 쿨타임이 다 찼을 때만 공격을 실행합니다.
        if (timer >= enemyData.attackCooldown)
        {
            anim.SetTrigger("Attack");
            timer = 0f; // 공격을 실행했으므로 쿨타임을 초기화합니다.
            StartCoroutine(DealDamageCoroutine());
        }
    }

    IEnumerator DealDamageCoroutine()
    {
        yield return new WaitForSeconds(hitDelay);
        if (isDead) yield break;

        // ] 공격 VFX 실행
        VFXManager.Instance.PlayMonsterAttack(
            transform.position + transform.forward * hitOffset,
            transform.forward
        );

        Vector3 hitPosition = transform.position + (transform.forward * hitOffset) + (Vector3.up * hitHeight);
        Collider[] hitColliders = Physics.OverlapSphere(hitPosition, hitRadius);

        bool hasHitPlayer = false;
        foreach (Collider hit in hitColliders)
        {
            if (hasHitPlayer) break;
            if (hit.gameObject.CompareTag("Player"))
            {
                Player player = hit.GetComponent<Player>();
                if (player != null)
                {
                    player.TakeDamage(enemyData.damage);
                }
                hasHitPlayer = true;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            anim.SetTrigger("Hurt");

            //피격 VFX 실행
            if (vfxPoint != null)
            {
                VFXManager.Instance.PlayMonsterHit(vfxPoint.position, Vector3.up, gameObject);
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        agent.isStopped = true;
        agent.enabled = false;
        anim.SetTrigger("Death");
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;


        //  사망 VFX 
        if (vfxPoint != null)
        {
            VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;
        Gizmos.color = Color.red;
        Vector3 hitPosition = transform.position + (transform.forward * hitOffset) + (Vector3.up * hitHeight);
        Gizmos.DrawWireSphere(hitPosition, hitRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);
    }
}