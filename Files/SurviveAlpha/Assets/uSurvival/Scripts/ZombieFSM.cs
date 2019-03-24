using UnityEngine;
using UnityEngine.AI;
using Mirror;

public class ZombieFSM : FSM
{
    // Used components. Assign in Inspector. Easier than GetComponent caching.
    new public Collider collider;
    public NavMeshAgent agent;
    public Combat combat;
    public AudioSource audioSource;

    [Header("Movement")]
    public float walkSpeed = 1;
    public float runSpeed = 5;
    [Range(0, 1)] public float moveProbability = 0.1f; // chance per second
    public float moveDistance = 10;
    // monsters should follow their targets even if they run out of the movement
    // radius. the follow dist should always be bigger than the biggest archer's
    // attack range, so that archers will always pull aggro, even when attacking
    // from far away.
    public float followDistance = 20;
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.5f; // move as close as 0.5 * attackRange to a target

    [Header("Attack")]
    public float attackRange = 3f;
    public float attackInterval = 0.5f; // how long one attack takes (seconds): ideally the attack animation time
    double attackEndTime;  // double for long term precision
    public AudioClip attackSound;

    // save the start position for random movement distance and respawning
    Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.position;
    }

    // helper functions ////////////////////////////////////////////////////////
    // look at a transform while only rotating on the Y axis (to avoid weird
    // tilts)
    public void LookAtY(Vector3 position)
    {
        transform.LookAt(new Vector3(position.x, transform.position.y, position.z));
    }

    // note: client can find out if moving by simply checking the state!
    public bool IsMoving()
    {
        // -> agent.hasPath will be true if stopping distance > 0, so we can't
        //    really rely on that.
        // -> pathPending is true while calculating the path, which is good
        // -> remainingDistance is the distance to the last path point, so it
        //    also works when clicking somewhere onto a obstacle that isn'
        //    directly reachable.
        return agent.pathPending ||
               agent.remainingDistance > agent.stoppingDistance ||
               agent.velocity != Vector3.zero;
    }

    // finite state machine events /////////////////////////////////////////////
    bool EventTargetDisappeared()
    {
        return target == null;
    }

    bool EventTargetDied()
    {
        return target != null && target.GetComponent<Health>().current == 0;
    }

    bool EventTargetTooFarToAttack()
    {
        return target != null &&
               Utils.ClosestDistance(collider, target.GetComponentInChildren<Collider>()) > attackRange;
    }

    bool EventTargetTooFarToFollow()
    {
        return target != null &&
               Vector3.Distance(startPosition, target.GetComponentInChildren<Collider>().ClosestPoint(transform.position)) > followDistance;
    }

    bool EventTargetUnreachable()
    {
        // targets might be close enough but behind a wall or a door, in which
        // case the zombie shouldn't be able to attack it
        //Debug.DrawLine(transform.position, target.transform.position, IsReachable(target) ? Color.green : Color.red);
        return target != null && !Utils.IsReachableVertically(collider, target.GetComponentInChildren<Collider>());
    }

    bool EventAggro()
    {
        return target != null && target.GetComponent<Health>().current > 0;
    }

    bool EventAttackFinished()
    {
        return NetworkTime.time >= attackEndTime;
    }

    bool EventMoveEnd()
    {
        return state == "MOVING" && !IsMoving();
    }

    bool EventMoveRandomly()
    {
        return Random.value <= moveProbability * Time.deltaTime;
    }

    // finite state machine - server ///////////////////////////////////////////
    [Server]
    string UpdateServer_IDLE()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventTargetDied())
        {
            // we had a target before, but it died now. clear it.
            target = null;
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            agent.speed = walkSpeed;
            agent.stoppingDistance = 0;
            agent.destination = startPosition;
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange * attackToMoveRangeRatio;
            agent.destination = target.GetComponentInChildren<Collider>().ClosestPoint(transform.position);
            return "MOVING";
        }
        if (EventTargetUnreachable())
        {
            // we have a target in attack range, but it's behind a door or wall
            return "IDLE";
        }
        if (EventAggro())
        {
            // target in attack range. try to attack it
            // -> start attack timer and go to casting
            attackEndTime = NetworkTime.time + attackInterval;
            RpcOnAttackStarted();
            return "ATTACKING";
        }
        if (EventMoveRandomly())
        {
            // walk to a random position in movement radius (from 'start')
            // note: circle y is 0 because we add it to start.y
            Vector2 circle2D = Random.insideUnitCircle * moveDistance;
            agent.speed = walkSpeed;
            agent.stoppingDistance = 0;
            agent.destination = startPosition + new Vector3(circle2D.x, 0, circle2D.y);
            return "MOVING";
        }
        if (EventMoveEnd()) {} // don't care
        //if (EventSkillFinished()) {} // don't care
        if (EventTargetDisappeared()) {} // don't care

        return "IDLE"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_MOVING()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventMoveEnd())
        {
            // we reached our destination.
            return "IDLE";
        }
        if (EventTargetDied())
        {
            // we had a target before, but it died now. clear it.
            target = null;
            agent.ResetMovement();
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            agent.speed = walkSpeed;
            agent.stoppingDistance = 0;
            agent.destination = startPosition;
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange * attackToMoveRangeRatio;
            agent.destination = target.GetComponentInChildren<Collider>().ClosestPoint(transform.position);
            return "MOVING";
        }
        if (EventTargetUnreachable())
        {
            // we have a target in attack range, but it's behind a door or wall
            return "IDLE";
        }
        if (EventAggro())
        {
            // the target is close, but we are probably moving towards it already
            // so let's just move a little bit closer into attack range so that
            // we can keep attacking it if it makes one step backwards
            if (Vector3.Distance(transform.position, target.GetComponentInChildren<Collider>().ClosestPoint(transform.position)) <= attackRange * attackToMoveRangeRatio)
            {
                // target in attack range. try to attack it
                // -> start attack timer and go to casting
                // (we may get a target while randomly wandering around)
                attackEndTime = NetworkTime.time + attackInterval;
                agent.ResetMovement();
                RpcOnAttackStarted();
                return "ATTACKING";
            }
        }
        if (EventAttackFinished()) {} // don't care
        if (EventTargetDisappeared()) {} // don't care
        if (EventMoveRandomly()) {} // don't care

        return "MOVING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_ATTACKING()
    {
        // keep looking at the target for server & clients (only Y rotation)
        if (target) LookAtY(target.transform.position);

        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventTargetDisappeared())
        {
            // target disappeared, stop attacking
            target = null;
            return "IDLE";
        }
        if (EventTargetDied())
        {
            // target died, stop attacking
            target = null;
            return "IDLE";
        }
        if (EventAttackFinished())
        {
            // finished attacking. apply the damage on the target
            combat.DealDamageAt(target, combat.damage, target.transform.position, -transform.forward, target.GetComponentInChildren<Collider>());

            // did the target die? then clear it so that the monster doesn't
            // run towards it if the target respawned
            if (target.GetComponent<Health>().current == 0)
                target = null;

            // go back to IDLE
            return "IDLE";
        }
        if (EventMoveEnd()) {} // don't care
        if (EventTargetTooFarToAttack())
        {
            // allow players to kite/dodge attacks by running far away. most
            // people want this feature in survival games (unlike MMOs where
            // kiting is protected against)
            // => run closer to target if out of range now
            agent.speed = runSpeed;
            agent.stoppingDistance = attackRange * attackToMoveRangeRatio;
            agent.destination = target.GetComponentInChildren<Collider>().ClosestPoint(transform.position);
            return "MOVING";
        }
        if (EventTargetTooFarToFollow())
        {
            // allow players to kite/dodge attacks by running far away. most
            // people want this feature in survival games (unlike MMOs where
            // kiting is protected against)
            // => way too far to even run there, so let's cancel the attack
            target = null;
            return "IDLE";
        }
        if (EventTargetUnreachable()) {} // don't care, we were close enough when starting to cast
        if (EventAggro()) {} // don't care, always have aggro while attacking
        if (EventMoveRandomly()) {} // don't care

        return "ATTACKING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_DEAD()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventAttackFinished()) {} // don't care
        if (EventMoveEnd()) {} // don't care
        if (EventTargetDisappeared()) {} // don't care
        if (EventTargetDied()) {} // don't care
        if (EventTargetTooFarToFollow()) {} // don't care
        if (EventTargetTooFarToAttack()) {} // don't care
        if (EventTargetUnreachable()) {} // don't care
        if (EventAggro()) {} // don't care
        if (EventMoveRandomly()) {} // don't care

        return "DEAD"; // nothing interesting happened
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE")    return UpdateServer_IDLE();
        if (state == "MOVING")  return UpdateServer_MOVING();
        if (state == "ATTACKING") return UpdateServer_ATTACKING();
        if (state == "DEAD")    return UpdateServer_DEAD();
        Debug.LogError("invalid state: " + state);
        return "IDLE";
    }

    // finite state machine - client ///////////////////////////////////////////
    [Client]
    protected override void UpdateClient()
    {
        if (state == "ATTACKING")
        {
            // keep looking at the target for server & clients (only Y rotation)
            if (target) LookAtY(target.transform.position);
        }
    }

    [Server]
    public void OnDeath()
    {
        state = "DEAD";

        // stop any movement
        agent.ResetMovement();

        // clear target
        target = null;
    }

    [Server]
    public void OnRespawn()
    {
        // respawn at start position
        // (always use Warp instead of transform.position for NavMeshAgents)
        agent.Warp(startPosition);

        state = "IDLE";
    }

    // check if we can attack someone else
    public bool CanAttack(GameObject go)
    {
        return go.tag == "Player" &&
               go.GetComponent<Health>().current > 0 &&
               health.current > 0;
    }

    // OnDrawGizmos only happens while the Script is not collapsed
    void OnDrawGizmos()
    {
        // draw the movement area (around 'start' if game running,
        // or around current position if still editing)
        Vector3 startHelp = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startHelp, moveDistance);

        // draw the follow dist
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(startHelp, followDistance);
    }

    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by AggroArea
    [ServerCallback]
    public override void OnAggro(GameObject go)
    {
        // can we attack that type?
        if (go != null && CanAttack(go))
        {
            // no target yet(==self), or closer than current target?
            // => has to be at least 20% closer to be worth it, otherwise we
            //    may end up nervously switching between two targets
            // => we also switch if current target is unreachable and we found
            //    a new target that is reachable, even if it's further away
            // => we do NOT use Utils.ClosestDistance, because then we often
            //    also end up nervously switching between two animated targets,
            //    since their collides moves with the animation.
            //    => we don't even need closestdistance here because they are in
            //       the aggro area anyway. transform.position is perfectly fine
            if (target == null)
            {
                target = go;
            }
            else if (target != go) // different one? evaluate if we should target it
            {
                // select closest target, but also always select the reachable
                // one if the current one is unreachable.
                float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                float newDistance = Vector3.Distance(transform.position, go.transform.position);
                if ((newDistance < oldDistance * 0.8) ||
                    (!Utils.IsReachableVertically(collider, target.GetComponentInChildren<Collider>()) &&
                     Utils.IsReachableVertically(collider, go.GetComponentInChildren<Collider>())))
                {
                    target = go;
                }
            }
        }
    }

    // this function is called by people who attack us. simply forwarded to
    // OnAggro.
    [Server]
    public void OnReceivedDamage(GameObject attacker, int damage)
    {
        OnAggro(attacker);
    }

    // attack rpc //////////////////////////////////////////////////////////////
    [ClientRpc]
    public void RpcOnAttackStarted()
    {
        // play sound (if any)
        if (attackSound) audioSource.PlayOneShot(attackSound);
    }
}
