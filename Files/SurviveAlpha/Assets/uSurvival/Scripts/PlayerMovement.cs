// based on Unity's FirstPersonController & ThirdPersonController scripts
using System;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

// MoveState as byte for minimal bandwidth (otherwise it's int by default)
// note: distinction between WALKING and RUNNING in case we need to know the
//       difference somewhere (e.g. for endurance recovery)
public enum MoveState : byte {IDLE, WALKING, RUNNING, CROUCHING, CRAWLING, JUMPING, CLIMBING, SWIMMING}

[RequireComponent(typeof (CharacterController))]
[RequireComponent(typeof (AudioSource))]
public class PlayerMovement : NetworkBehaviour
{
    // components to be assigned in inspector
    [Header("Components")]
    public Animator animator;
    public Health health;
    public CharacterController controller;
    public AudioSource feetAudio;
    public IKHandling ik;
    public Combat combat;
    public PlayerLook look;
    public NetworkTransformRubberbanding netTransform;
    public Endurance endurance;
    new public Collider collider;
    new Camera camera;

    float lastClientSendTime;

    [Header("State")]
    public MoveState state = MoveState.IDLE;
    MoveState lastSerializedState = MoveState.IDLE;
    [HideInInspector] public Vector3 moveDir;

    [Header("Walking")]
    public float walkSpeed = 5;

    [Header("Running")]
    public float runSpeed = 8;
    [Range(0f, 1f)] public float runStepLength = 0.7f;
    public float runStepInterval = 3;
    public float runCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
    public KeyCode runKey = KeyCode.LeftShift;
    float stepCycle;
    float nextStep;

    [Header("Crouching")]
    public float crouchSpeed = 1.5f;
    public KeyCode crouchKey = KeyCode.C;

    [Header("Crawling")]
    public float crawlSpeed = 1;
    public KeyCode crawlKey = KeyCode.V;

    [Header("Swimming")]
    public float swimSpeed = 4;
    public float swimSurfaceOffset = 0.25f;
    Collider waterCollider;
    bool inWater { get { return waterCollider != null; } } // standing in water / touching it?
    bool underWater; // deep enough in water so we need to swim?
    [Range(0, 1)] public float underwaterThreshold = 0.7f; // percent of body that need to be underwater to start swimming
    public LayerMask canStandInWaterCheckLayers = Physics.DefaultRaycastLayers; // set this to everything except water layer

    [Header("Jumping")]
    public float jumpSpeed = 7;
    [HideInInspector] public float jumpLeg;

    [Header("Fall Damage")]
    public float fallDamageMinimumMagnitude = 15;
    public float fallDamageMultiplier = 2;
    Vector3 lastFall;

    [Header("Climbing")]
    public float climbSpeed = 3;
    Collider ladderCollider;

    [Header("Physics")]
    public float gravityMultiplier = 2;
    bool previouslyGrounded;

    [Header("Sounds")]
    public AudioClip[] footstepSounds;    // an array of footstep sounds that will be randomly selected from.
    public AudioClip jumpSound;           // the sound played when character leaves the ground.
    public AudioClip landSound;           // the sound played when character touches back on ground.

    // velocity in case it's needed. CharacterController.velocity is read-only,
    // so we can't sync it over the network and assign it afterwards. instead we
    // use NetworkTransformRubberbanding's estimated velocity.
    public Vector3 velocity
    {
        get { return isLocalPlayer ? controller.velocity : netTransform.EstimateVelocity(); }
    }

    // synchronization /////////////////////////////////////////////////////////
    // this script is local authority, so we need to send state from client to
    // server and then synchronize it to all other clients, while ignoring the
    // received data for local player, since it has authority

    // send important state to server
    // (position/rotation is synced by NetworkTransformRubberbanding already)
    [Command]
    void CmdSyncToServer(MoveState state)
    {
        this.state = state;
    }

    // server-side serialization
    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        writer.Write((byte)state);
        lastSerializedState = state;
        return true;
    }

    // client-side deserialization
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        // always read so we don't mess up the stream.
        // only assign if not local player (local player has authority over it)
        // => essentially works like a [SyncVar] that ignores local player
        MoveState readState = (MoveState)reader.ReadByte();
        if (!isLocalPlayer) state = readState;
    }

    // input directions ////////////////////////////////////////////////////////
    Vector2 GetInputDirection()
    {
        // get input direction while alive and while not typing in chat
        // (otherwise 0 so we keep falling even if we die while jumping etc.)
        float horizontal = 0;
        float vertical = 0;
        if (health.current > 0 && !UIUtils.AnyInputActive())
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
        }
        return new Vector2(horizontal, vertical).normalized;
    }

    Vector3 GetDesiredDirection(Vector2 inputDir)
    {
        // always move along the camera forward as it is the direction that is being aimed at
        return transform.forward * inputDir.y + transform.right * inputDir.x;
    }

    Vector3 GetDesiredDirectionOnGround(Vector3 desiredDir)
    {
        // get a normal for the surface that is being touched to move along it
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, controller.radius, Vector3.down, out hitInfo,
                               controller.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            return Vector3.ProjectOnPlane(desiredDir, hitInfo.normal).normalized;
        }
        return desiredDir;
    }

    // movement state machine //////////////////////////////////////////////////
    bool EventJumpRequested()
    {
        // the jump state needs to read here to make sure it is not missed
        // => only while grounded so jump key while jumping doesn't start
        //    a new jump immediately after landing
        return controller.isGrounded && !UIUtils.AnyInputActive() && Input.GetButtonDown("Jump");
    }

    bool EventCrouchToggle()
    {
        return !UIUtils.AnyInputActive() && Input.GetKeyDown(crouchKey);
    }

    bool EventCrawlToggle()
    {
        return !UIUtils.AnyInputActive() && Input.GetKeyDown(crawlKey);
    }

    bool EventLanded()
    {
        return !previouslyGrounded && controller.isGrounded;
    }

    bool EventUnderWater()
    {
        // we can't really make it player position dependent, because he might
        // swim to the surface at which point it might be detected as standing
        // in water but not being under water, etc.
        if (inWater) // in water and valid water collider?
        {
            // raycasting from water to the bottom at the position of the player
            // seems like a very precise solution
            Vector3 origin = new Vector3(transform.position.x,
                                         waterCollider.bounds.max.y,
                                         transform.position.z);
            float distance = collider.bounds.size.y * underwaterThreshold;
            Debug.DrawLine(origin, origin + Vector3.down * distance, Color.cyan);

            // we are underwater if the raycast doesn't hit anything
            RaycastHit hit;
            return !Utils.RaycastWithout(origin, Vector3.down, out hit, distance, gameObject, canStandInWaterCheckLayers);
        }
        return false;
    }

    bool EventLadderExit()
    {
        // OnTriggerExit isn't good enough to detect ladder exits because we
        // shouldnt exit as soon as our head sticks out of the ladder collider.
        // only if we fully left it. so check this manually here:
        return ladderCollider != null &&
               !ladderCollider.bounds.Intersects(collider.bounds);
    }

    // helper function to apply gravity based on previous Y direction
    float ApplyGravity(float moveDirY)
    {
        if (controller.isGrounded)
        {
            // apply flat gravity while grounded so we can walk down hills.
            // if we were to keep decreasing .y constantly while walking too,
            // then we'd fall down rapidly as soon as we aren't grounded anymore
            return Physics.gravity.y;
        }
        else
        {
            // gravity needs to be * Time.deltaTime even though we multiply the
            // final controller.Move * Time.deltaTime too, because the unit is
            // 9.81m/s²
            return moveDirY + Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    // helper function to get move or walk speed depending on key press & endurance
    float GetWalkOrRunSpeed()
    {
        bool runRequested = !UIUtils.AnyInputActive() && Input.GetKey(runKey);
        return runRequested && endurance.current > 0 ? runSpeed : walkSpeed;
    }

    MoveState UpdateIDLE(Vector2 inputDir, Vector3 desiredDir)
    {
        // always set move direction
        moveDir.x = 0;
        moveDir.y = ApplyGravity(moveDir.y);
        moveDir.z = 0;

        if (EventJumpRequested())
        {
            // start the jump movement into Y dir, go to jumping
            // note: no endurance>0 check because it feels odd if we can't jump
            moveDir.y = jumpSpeed;
            PlayJumpSound();
            return MoveState.JUMPING;
        }
        else if (EventLanded())
        {
            PlayLandingSound();
            return MoveState.IDLE;
        }
        else if (EventCrouchToggle())
        {
            return MoveState.CROUCHING;
        }
        else if (EventCrawlToggle())
        {
            return MoveState.CRAWLING;
        }
        else if (EventUnderWater())
        {
            return MoveState.SWIMMING;
        }
        else if (inputDir != Vector2.zero)
        {
            return MoveState.WALKING;
        }

        return MoveState.IDLE;
    }

    MoveState UpdateWALKINGandRUNNING(Vector2 inputDir, Vector3 desiredDir)
    {
        // walk or run?
        float speed = GetWalkOrRunSpeed();

        // always set move direction
        moveDir.x = desiredDir.x * speed;
        moveDir.y = ApplyGravity(moveDir.y);
        moveDir.z = desiredDir.z * speed;

        if (EventJumpRequested())
        {
            // start the jump movement into Y dir, go to jumping
            // note: no endurance>0 check because it feels odd if we can't jump
            moveDir.y = jumpSpeed;
            PlayJumpSound();
            return MoveState.JUMPING;
        }
        else if (EventLanded())
        {
            PlayLandingSound();
            return MoveState.IDLE;
        }
        else if (EventCrouchToggle())
        {
            return MoveState.CROUCHING;
        }
        else if (EventCrawlToggle())
        {
            return MoveState.CRAWLING;
        }
        else if (EventUnderWater())
        {
            return MoveState.SWIMMING;
        }
        else if (inputDir == Vector2.zero)
        {
            return MoveState.IDLE;
        }

        ProgressStepCycle(inputDir, speed);
        return speed == walkSpeed ? MoveState.WALKING : MoveState.RUNNING;
    }

    MoveState UpdateCROUCHING(Vector2 inputDir, Vector3 desiredDir)
    {
        // always set move direction
        moveDir.x = desiredDir.x * crouchSpeed;
        moveDir.y = ApplyGravity(moveDir.y);
        moveDir.z = desiredDir.z * crouchSpeed;

        if (EventJumpRequested())
        {
            // stop crouching when pressing jump key. this feels better than
            // jumping from the crouching state.
            return MoveState.IDLE;
        }
        else if (EventLanded())
        {
            PlayLandingSound();
            return MoveState.IDLE;
        }
        else if (EventCrouchToggle())
        {
            return MoveState.IDLE;
        }
        else if (EventCrawlToggle())
        {
            return MoveState.CRAWLING;
        }
        else if (EventUnderWater())
        {
            return MoveState.SWIMMING;
        }

        ProgressStepCycle(inputDir, crouchSpeed);
        return MoveState.CROUCHING;
    }

    MoveState UpdateCRAWLING(Vector2 inputDir, Vector3 desiredDir)
    {
        // always set move direction
        moveDir.x = desiredDir.x * crawlSpeed;
        moveDir.y = ApplyGravity(moveDir.y);
        moveDir.z = desiredDir.z * crawlSpeed;

        if (EventJumpRequested())
        {
            // stop crawling when pressing jump key. this feels better than
            // jumping from the crawling state.
            return MoveState.IDLE;
        }
        else if (EventLanded())
        {
            PlayLandingSound();
            return MoveState.IDLE;
        }
        else if (EventCrouchToggle())
        {
            return MoveState.CROUCHING;
        }
        else if (EventCrawlToggle())
        {
            return MoveState.IDLE;
        }
        else if (EventUnderWater())
        {
            return MoveState.SWIMMING;
        }

        ProgressStepCycle(inputDir, crawlSpeed);
        return MoveState.CRAWLING;
    }

    MoveState UpdateJUMPING(Vector2 inputDir, Vector3 desiredDir)
    {
        // walk or run?
        float speed = GetWalkOrRunSpeed();

        // always set move direction
        moveDir.x = desiredDir.x * speed;
        moveDir.y = ApplyGravity(moveDir.y);
        moveDir.z = desiredDir.z * speed;

        if (EventLanded())
        {
            PlayLandingSound();
            return MoveState.IDLE;
        }
        else if (EventUnderWater())
        {
            return MoveState.SWIMMING;
        }

        return MoveState.JUMPING;
    }

    MoveState UpdateCLIMBING(Vector2 inputDir, Vector3 desiredDir)
    {
        // finished climbing?
        if (EventLadderExit())
        {
            // player rotation was adjusted to ladder rotation before.
            // let's reset it, but also keep look forward
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            ladderCollider = null;
            return MoveState.IDLE;
        }

        // interpret forward/backward movement as upward/downward
        moveDir.x = inputDir.x * climbSpeed;
        moveDir.y = inputDir.y * climbSpeed;
        moveDir.z = 0;

        // make the direction relative to ladder rotation. so when pressing right
        // we always climb to the right of the ladder, no matter how it's rotated
        moveDir = ladderCollider.transform.rotation * moveDir;
        Debug.DrawLine(transform.position, transform.position + moveDir, Color.yellow, 0.1f, false);

        return MoveState.CLIMBING;
    }

    MoveState UpdateSWIMMING(Vector2 inputDir, Vector3 desiredDir)
    {
        // not under water anymore?
        if (!EventUnderWater())
        {
            return MoveState.IDLE;
        }

        // always set move direction
        moveDir.x = desiredDir.x * swimSpeed;
        moveDir.z = desiredDir.z * swimSpeed;

        // gravitate toward surface
        if (waterCollider != null)
        {
            float surface = waterCollider.bounds.max.y;
            float surfaceDirection = surface - controller.bounds.min.y - swimSurfaceOffset;
            moveDir.y = surfaceDirection * swimSpeed;
        }
        else moveDir.y = 0;

        return MoveState.SWIMMING;
    }

    // Update is called once per frame
    void Update()
    {
        // set dirty if state changed since last OnSerialize
        // (uint for compatibility with default HLAPI)
        if (isServer) SetDirtyBit(Convert.ToUInt32(state != lastSerializedState));

        // only control movement for local player
        if (isLocalPlayer)
        {
            // get input and desired direction based on camera and ground
            Vector2 inputDir = GetInputDirection();
            Vector3 desiredDir = GetDesiredDirection(inputDir);
            Vector3 desiredGroundDir = GetDesiredDirectionOnGround(desiredDir);
            Debug.DrawLine(transform.position, transform.position + new Vector3(inputDir.x, 0, inputDir.y), Color.green);
            Debug.DrawLine(transform.position, transform.position + desiredDir, Color.blue);
            //Debug.DrawLine(transform.position, transform.position + desiredGroundDir, Color.cyan);

            // update state machine
            if (state == MoveState.IDLE)           state = UpdateIDLE(inputDir, desiredGroundDir);
            else if (state == MoveState.WALKING)   state = UpdateWALKINGandRUNNING(inputDir, desiredGroundDir);
            else if (state == MoveState.RUNNING)   state = UpdateWALKINGandRUNNING(inputDir, desiredGroundDir);
            else if (state == MoveState.CROUCHING) state = UpdateCROUCHING(inputDir, desiredGroundDir);
            else if (state == MoveState.CRAWLING)  state = UpdateCRAWLING(inputDir, desiredGroundDir);
            else if (state == MoveState.JUMPING)   state = UpdateJUMPING(inputDir, desiredGroundDir);
            else if (state == MoveState.CLIMBING)  state = UpdateCLIMBING(inputDir, desiredGroundDir);
            else if (state == MoveState.SWIMMING)  state = UpdateSWIMMING(inputDir, desiredGroundDir);
            else Debug.LogError("Unhandled Movement State: " + state);

            // cache this move's state to detect landing etc. next time
            previouslyGrounded = controller.isGrounded;
            if (!controller.isGrounded) lastFall = controller.velocity;

            // move depending on latest moveDir changes
            controller.Move(moveDir * Time.deltaTime); // note: returns CollisionFlags if needed

            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            float runCycle = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime + runCycleLegOffset, 1);
            jumpLeg = (runCycle < 0.5f ? 1 : -1);// * move.z;

            // send movement state to server to broadcast to other clients
            // (velocity can be calculated from last position, jumpleg isn't
            //   worth it except for local player)
            //
            // send only each 'sendinterval', otherwise we send at whatever
            // the player's tick rate is, which is like DDOS
            // (SendInterval doesn't seem to apply to Cmd, so we have to do
            //  it manually)
            if (Time.time - lastClientSendTime >= syncInterval)
            {
                CmdSyncToServer(state);
                lastClientSendTime = Time.time;
            }
        }

        // apply fall damage on server
        // (also in mode because we can't apply fall damage on client)
        // previous and grounded are always the same on server hm.
        // need to assign it in nettransf before .move?
        if (isServer && EventLanded())
        {
            if(lastFall.magnitude > fallDamageMinimumMagnitude)
            {
                int damage = Mathf.RoundToInt(lastFall.magnitude * fallDamageMultiplier);
                health.current -= damage;
                combat.RpcOnDamageReceived(damage, transform.position, -lastFall);
            }
        }

        // server needs to cache some stuff too, AFTER everything else happened
        // (not in host mode because we already cached before .Move() there)
        if (isServer && !isClient)
        {
            previouslyGrounded = controller.isGrounded;

            // assign lastFall to .velocity (works on server), not controller.velocity
            if (!controller.isGrounded) lastFall = velocity;
        }
    }

    void PlayLandingSound()
    {
        feetAudio.clip = landSound;
        feetAudio.Play();
        nextStep = stepCycle + .5f;
    }

    void PlayJumpSound()
    {
        feetAudio.clip = jumpSound;
        feetAudio.Play();
    }

    void ProgressStepCycle(Vector3 inputDir, float speed)
    {
        if (controller.velocity.sqrMagnitude > 0 && (inputDir.x != 0 || inputDir.y != 0))
        {
            stepCycle += (controller.velocity.magnitude + (speed*(state == MoveState.WALKING ? 1 : runStepLength)))*
                         Time.deltaTime;
        }

        if (stepCycle > nextStep)
        {
            nextStep = stepCycle + runStepInterval;
            PlayFootStepAudio();
        }
    }

    void PlayFootStepAudio()
    {
        if (!controller.isGrounded) return;

        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, footstepSounds.Length);
        feetAudio.clip = footstepSounds[n];
        feetAudio.PlayOneShot(feetAudio.clip);

        // move picked sound to index 0 so it's not picked next time
        footstepSounds[n] = footstepSounds[0];
        footstepSounds[0] = feetAudio.clip;
    }

    [ClientCallback] // client authoritative movement, don't do this on
    //[ServerCallback] <- disabled for now, since movement is client authoritative
    void OnTriggerEnter(Collider co)
    {
        if (co.tag == "Ladder" && state != MoveState.CLIMBING) // only do initialization once
        {
            state = MoveState.CLIMBING;
            ladderCollider = co;

            // make player look directly at ladder forward. but we also initialize
            // freelook manually already to overwrite the initial rotation, so
            // that in the end, the camera keeps looking at the same angle even
            // though we did modify transform.forward.
            // note: even though we set the rotation perfectly here, there's
            //       still one frame where it seems to interpolate between the
            //       new and the old rotation, which causes 1 odd camera frame.
            //       this could be avoided by overwriting transform.forward once
            //       more in LateUpdate.
            if (isLocalPlayer)
            {
                look.InitializeFreeLook();
                Quaternion original = transform.rotation;
                transform.forward = co.transform.forward;
            }
        }

        // touching water? then set water collider
        if (co.tag == "Water")
        {
            waterCollider = co;
        }
    }

    void OnTriggerExit(Collider co)
    {
        if (co.tag == "Water")
        {
            waterCollider = null;
        }
    }
}
