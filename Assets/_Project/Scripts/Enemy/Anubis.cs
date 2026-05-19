using UnityEngine;

public class Anubis : EnemyBase
{
    protected override void Update()
    {
        timer += Time.deltaTime;
        if (isDead || target == null) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isAttackingNow = stateInfo.IsName("Attack");
        bool isHurtNow = stateInfo.IsName("Hurt");

        if (isAttackingNow || isHurtNow)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetFloat("MoveSpeed", 0f);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= attackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
            transform.LookAt(lookPos);
            anim.SetFloat("MoveSpeed", 0f);

            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Attack") == false)
            {
                Attack();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);

            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

            float speed = agent.velocity.magnitude;
            anim.SetFloat("MoveSpeed", speed, 0.05f, Time.deltaTime);
        }
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isAttackingNow = stateInfo.IsName("Attack");
        base.TakeDamage(damage, isCritical);

        if (isAttackingNow)
        {
            anim.ResetTrigger("Hurt");
        }
        else
        {
            StopAllCoroutines();
        }
    }
}