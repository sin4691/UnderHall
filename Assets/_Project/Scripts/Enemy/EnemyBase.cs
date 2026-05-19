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

    protected float currentHealth; // 자식 클래스에서 접근 가능하도록 변경
    protected float timer;
    protected bool isDead = false;

    [Header("Attack Settings")]
    [SerializeField] private float hitDelay = 0.5f;
    [SerializeField] private float hitRadius = 1.5f;
    [SerializeField] private float hitOffset = 1.5f;
    [SerializeField] private float hitHeight = 1.5f;

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
            timer = enemyData.attackCooldown;
            agent.updateRotation = false;
            agent.stoppingDistance = attackRange;
        }
    }

    protected virtual void Update()
    {
        if (isDead || target == null || enemyData == null || !agent.isOnNavMesh) return;

        timer += Time.deltaTime;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= enemyData.detectionRange)
        {
            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

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

        float speed = (agent.isStopped || dist <= attackRange) ? 0f : agent.velocity.magnitude;
        anim.SetFloat("MoveSpeed", speed, 0.05f, Time.deltaTime);
    }

    protected virtual void Attack()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (timer >= enemyData.attackCooldown)
        {
            anim.SetTrigger("Attack");
            timer = 0f;
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(enemyData.attackSoundName))
            {
                AudioManager.Instance.PlaySFX(enemyData.attackSoundName);
            }
            StartCoroutine(DealDamageCoroutine());
        }
    }

    IEnumerator DealDamageCoroutine()
    {
        yield return new WaitForSeconds(hitDelay);
        if (isDead) yield break;

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

    public virtual void TakeDamage(float damage, bool isCritical = false) // 오버라이드 가능하도록 변경
                                                                          //창우_데미지넘버 스포너에서 치명타 여부도 전달받도록 수정 bool isCritical = false 추가
    {
        currentHealth -= damage;

        //창우_데미지넘버 스포너에 데미지 정보 전달
        if (DamageNumberSpawner.Instance != null)
            DamageNumberSpawner.Instance.Show(damage, transform.position, isCritical, gameObject);


        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            anim.SetTrigger("Hurt");

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

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(enemyData.deathSoundName))
        {
            AudioManager.Instance.PlaySFX(enemyData.deathSoundName);
        }

        if (vfxPoint != null)
        {
            VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);         
        }
        Destroy(gameObject, 3f);
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