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
            currentHealth = enemyData.maxHealth;
            agent.speed = enemyData.moveSpeed;
            agent.stoppingDistance = enemyData.attackRange;
        }
    }

    void Update()
    {
        if (isDead || target == null || enemyData == null || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= enemyData.detectionRange)
        {
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
        }
        else
        {
            agent.isStopped = true;
        }

        float speed = agent.isStopped ? 0 : agent.velocity.magnitude;
        anim.SetFloat("MoveSpeed", speed, 0.1f, Time.deltaTime);
    }

    protected virtual void Attack()
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

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(enemyData.damage);
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

        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyData == null) return;

        Gizmos.color = Color.red;
        Vector3 hitPosition = transform.position + transform.forward * hitOffset;
        Gizmos.DrawWireSphere(hitPosition, hitRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, enemyData.detectionRange);
    }
}