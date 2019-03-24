using UnityEngine;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    // components to be assigned in the inspector
    public Health health;
    public CharacterController controller;
    public PlayerMovement movement;
    public PlayerHotbar hotbar;

    ////////////////////////////////////////////////////////////////////////////
    float GetJumpLeg()
    {
        return isLocalPlayer
               ? movement.jumpLeg
               : 0; // always left leg. saves Cmd+SyncVar bandwidth and no one will notice.
    }
    ////////////////////////////////////////////////////////////////////////////
    [ClientCallback] // don't animate on the server
    void Update()
    {
        // local velocity (based on rotation) for animations
        Vector3 localVelocity = transform.InverseTransformDirection(movement.velocity);
        float jumpLeg = GetJumpLeg();

        // grounded detection for other players works best via .state
        bool grounded = isLocalPlayer
            ? controller.isGrounded
            : movement.state != MoveState.JUMPING;

        // apply animation parameters to all animators.
        // there might be multiple if we use skinned mesh equipment.
        foreach (Animator animator in GetComponentsInChildren<Animator>())
        {
            animator.SetBool("DEAD", health.current == 0);
            animator.SetFloat("DirX", localVelocity.x);
            animator.SetFloat("DirY", localVelocity.y);
            animator.SetFloat("DirZ", localVelocity.z);
            animator.SetBool("CROUCHING", movement.state == MoveState.CROUCHING);
            animator.SetBool("CRAWLING", movement.state == MoveState.CRAWLING);
            animator.SetBool("CLIMBING", movement.state == MoveState.CLIMBING);
            animator.SetBool("SWIMMING", movement.state == MoveState.SWIMMING);
            // smoothest way to do climbing-idle is to stop right where we were
            if (movement.state == MoveState.CLIMBING)
                animator.speed = localVelocity.y == 0 ? 0 : 1;
            else
                animator.speed = 1;

            animator.SetBool("OnGround", grounded);
            if (controller.isGrounded) animator.SetFloat("JumpLeg", jumpLeg);

            // upper body layer
            // note: UPPERBODY_USED is fired from PlayerHotbar.OnUsedItem
            animator.SetBool("UPPERBODY_HANDS", hotbar.slots[hotbar.selection].amount == 0);
            // -> tool parameters are all set to false and then the current tool is
            //    set to true
            animator.SetBool("UPPERBODY_RIFLE", false);
            animator.SetBool("UPPERBODY_PISTOL", false);
            animator.SetBool("UPPERBODY_AXE", false);
            if (movement.state != MoveState.CLIMBING && // not while climbing
                hotbar.slots[hotbar.selection].amount > 0 &&
                hotbar.slots[hotbar.selection].item.data is WeaponItem)
            {
                WeaponItem weapon = (WeaponItem)hotbar.slots[hotbar.selection].item.data;
                if (!string.IsNullOrWhiteSpace(weapon.upperBodyAnimationParameter))
                    animator.SetBool(weapon.upperBodyAnimationParameter, true);
            }
        }
    }
}
