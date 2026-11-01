using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

// Listen-host fighter. The server moves the body and applies hits.
// A client does not choose the target, the damage, or the position used for a hit.
// Mouse look is shown immediately on the local camera. The server aim catches up from the reported yaw and pitch.
[RequireComponent(typeof(CharacterController), typeof(NetworkObject), typeof(PlayerInput))]
public sealed class NAHNetworkPlayer : NetworkBehaviour
{
    public const int StartingLives = 3;
    public const float RespawnLockSeconds = 1f;

    // Capsule size and eye height of an average body. Body width and height scale these.
    const float BaseRadius = 0.5f;
    const float BaseHalfHeight = 1f;
    const float BaseEyeHeight = 0.7f;
    // Spawn a little above the floor. A controller that starts touching the floor fell through it
    // when the first frame was long (0.33 s in an editor test).
    const float SpawnClearance = 0.1f;
    // How far the swing box reaches above the head and below the feet.
    const float SwingHeightMargin = 0.25f;

    [SerializeField] Transform cameraPivot;
    [SerializeField] Transform attackOrigin;
    [SerializeField] Renderer bodyRenderer;

    [Header("Stats")]
    [SerializeField] NAHStatRules statRules;
    [SerializeField] NAHWeaponDefinition weapon;
    [Tooltip("The races and classes a player may pick. Empty means only no race and no class.")]
    [SerializeField] NAHRoster roster;

    [Header("Horizontal Movement")]
    [SerializeField, Min(0.1f)] float walkSpeed = 2.2f;
    [SerializeField, Min(0.1f)] float sprintSpeed = 3.4f;
    [SerializeField, Min(0.1f)] float groundAcceleration = 6f;
    [SerializeField, Min(0.1f)] float groundDeceleration = 14f;
    [SerializeField, Min(0.1f)] float airAcceleration = 1.5f;

    [Header("Jump and Gravity")]
    [SerializeField, Min(0.1f)] float jumpHeight = 0.65f;
    [SerializeField] float gravity = -28f;
    [SerializeField, Min(0f)] float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] float jumpBufferTime = 0.1f;

    [Header("Camera")]
    [SerializeField, Min(0.01f)] float mouseDegreesPerPixel = 0.12f;
    [SerializeField, Min(1f)] float gamepadDegreesPerSecond = 160f;
    [SerializeField, Range(1f, 89f)] float pitchLimit = 85f;

    // Lock-on only turns this player's own view. The host still decides every hit.
    [Header("Lock-on")]
    [SerializeField, Min(1f)] float lockOnRange = 12f;
    [SerializeField, Min(1f)] float lockOnBreakRange = 16f;
    [SerializeField, Range(5f, 90f)] float lockOnConeDegrees = 60f;
    [SerializeField, Min(10f)] float lockOnTurnDegreesPerSecond = 360f;
    [Tooltip("How far a mouse or stick flick must turn the view, in degrees, to switch to the next target on that side.")]
    [SerializeField, Min(1f)] float lockOnSwitchDegrees = 25f;

    // Reach and time between swings come from the stat rules, so weapon weight and length change them.
    [Header("Attack")]
    [SerializeField, Min(0.1f)] float hitRadius = 0.75f;
    [SerializeField] LayerMask hittableLayers = ~0;
    [SerializeField] LayerMask environmentOccluders;

    readonly NetworkVariable<float> health = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<float> maxHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Width (x) and height (y) of the body, where 1 is average. Every peer sizes the capsule from this.
    readonly NetworkVariable<Vector2> bodySize = new NetworkVariable<Vector2>(
        Vector2.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // The shield blocks hits while the fighter blocks, until it is used up; then it is broken and cools down.
    readonly NetworkVariable<float> shield = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<float> maxShield = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<bool> shieldBroken = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<bool> blocking = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Indexes into the roster, -1 for none. Shared so every screen can show the names.
    readonly NetworkVariable<int> raceIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<int> classIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<int> lives = new NetworkVariable<int>(
        StartingLives, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<bool> eliminated = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<int> matchPhase = new NetworkVariable<int>(
        (int)NAHNetworkMatch.Phase.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<ulong> winnerClientId = new NetworkVariable<ulong>(
        ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    readonly Collider[] hitBuffer = new Collider[32];
    readonly HashSet<NAHNetworkPlayer> alreadyHit = new HashSet<NAHNetworkPlayer>();

    CharacterController controller;
    PlayerInput playerInput;
    Camera playerCamera;
    AudioListener listener;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction attackAction;
    InputAction lockOnAction;
    InputAction blockAction;
    NAHNetworkPlayer lockTarget;
    float lockSwitchPush;
    float nextLockSwitchTime;

    NAHBodyShape body = NAHBodyShape.Average;
    NAHRaceDefinition race;
    NAHClassDefinition fighterClass;
    NAHFighterStats stats;
    bool bodyLocked;

    Vector3 horizontalVelocity;
    Vector3 spawnPosition;
    float verticalVelocity;
    float coyoteTimer;
    float jumpBufferTimer;
    float serverYaw;
    float lookYaw;
    float reportedPitch;
    float moveX;
    float moveY;
    float nextAttackTime;
    float downTimer;
    bool sprintHeld;
    bool blockHeld;
    float shieldBrokenUntil;
    float lastShieldHitTime;
    bool jumpQueued;
    bool scriptedInput;
    bool registered;
    int spawnSlot;
    int lastInputTick = int.MinValue;
    bool lastSentSprint;
    bool lastSentBlock;
    bool lastSentMoving;

    public float Health => health.Value;
    public float MaxHealth => maxHealth.Value;
    public Vector2 BodySize => bodySize.Value;
    public float Shield => shield.Value;
    public float MaxShield => maxShield.Value;
    public bool ShieldBroken => shieldBroken.Value;
    public bool Blocking => blocking.Value;
    public NAHRoster Roster => roster;
    public NAHRaceDefinition Race => roster != null ? roster.Race(raceIndex.Value) : null;
    public NAHClassDefinition FighterClass => roster != null ? roster.Class(classIndex.Value) : null;
    // Local only. Null when not locked on.
    public NAHNetworkPlayer LockOnTarget => lockTarget;
    // Server only. Clients never receive these numbers.
    public NAHFighterStats ServerStats => stats;
    public int Lives => lives.Value;
    public bool Eliminated => eliminated.Value;
    public NAHNetworkMatch.Phase MatchPhase => (NAHNetworkMatch.Phase)matchPhase.Value;
    public ulong WinnerClientId => winnerClientId.Value;
    // Owner only. How many movement and aim messages this fighter has sent, for tests and the demo log.
    public int InputMessagesSent { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        if (cameraPivot != null)
        {
            playerCamera = cameraPivot.GetComponent<Camera>();
            listener = cameraPivot.GetComponent<AudioListener>();
        }

        var actions = playerInput.actions;
        moveAction = actions.FindAction("Move", true);
        lookAction = actions.FindAction("Look", true);
        jumpAction = actions.FindAction("Jump", true);
        sprintAction = actions.FindAction("Sprint", true);
        attackAction = actions.FindAction("Attack", true);
        lockOnAction = actions.FindAction("LockOn", false);
        blockAction = actions.FindAction("Block", false);

        if (playerCamera != null)
            playerCamera.enabled = false;
        if (listener != null)
            listener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        bool local = IsLocalPlayer;
        if (playerInput != null)
            playerInput.enabled = local;
        if (playerCamera != null)
            playerCamera.enabled = local;
        if (listener != null)
            listener.enabled = local;
        if (bodyRenderer != null)
            bodyRenderer.enabled = !local;

        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
            networkTransform.Interpolate = !local;

        bodySize.OnValueChanged += OnBodySizeChanged;
        ApplyBodySize(bodySize.Value);

        if (!IsServer)
        {
            if (controller != null)
                controller.enabled = false;
            lookYaw = transform.eulerAngles.y;
            return;
        }

        if (controller != null)
            controller.enabled = true;

        body = NAHBodyShape.Average;
        race = null;
        fighterClass = null;
        raceIndex.Value = -1;
        classIndex.Value = -1;
        bodyLocked = false;
        ServerRecomputeStats();
        health.Value = maxHealth.Value;
        ServerRefillShield();
        lives.Value = StartingLives;
        eliminated.Value = false;
        spawnSlot = NAHNetworkMatch.Instance != null ? NAHNetworkMatch.Instance.Register(this) : 0;
        registered = NAHNetworkMatch.Instance != null;
        spawnPosition = SlotPosition(spawnSlot);
        serverYaw = SlotYaw(spawnSlot);
        ServerPlace(SpawnCenter(spawnPosition), serverYaw);
        if (local)
            lookYaw = serverYaw;
    }

    public override void OnNetworkDespawn()
    {
        bodySize.OnValueChanged -= OnBodySizeChanged;
        if (IsServer && NAHNetworkMatch.Instance != null)
            NAHNetworkMatch.Instance.Unregister(this);
    }

    void OnBodySizeChanged(Vector2 previous, Vector2 current)
    {
        ApplyBodySize(current);
    }

    // The capsule is the hitbox, so its size must match on every screen. The camera rides at eye height.
    void ApplyBodySize(Vector2 size)
    {
        if (bodyRenderer != null && bodyRenderer.transform != transform)
            bodyRenderer.transform.localScale = new Vector3(size.x, size.y, size.x);
        if (controller != null)
        {
            controller.radius = BaseRadius * size.x;
            controller.height = Mathf.Max(BaseHalfHeight * 2f * size.y, controller.radius * 2f);
        }
        if (cameraPivot != null)
            cameraPivot.localPosition = new Vector3(0f, BaseEyeHeight * size.y, 0f);
    }

    // Spawn points are written for an average body. Raise or lower the center so the feet stay on the same floor.
    Vector3 SpawnCenter(Vector3 averageCenter)
    {
        return averageCenter + Vector3.up * ((bodySize.Value.y - 1f) * BaseHalfHeight + SpawnClearance);
    }

    void Update()
    {
        if (!IsSpawned)
            return;

        if (IsServer && !registered && NAHNetworkMatch.Instance != null)
        {
            spawnSlot = NAHNetworkMatch.Instance.Register(this);
            registered = true;
            spawnPosition = SlotPosition(spawnSlot);
            serverYaw = SlotYaw(spawnSlot);
            ServerPlace(SpawnCenter(spawnPosition), serverYaw);
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        if (IsLocalPlayer && !Eliminated && !scriptedInput && playerInput != null && playerInput.enabled)
            ReadLocalInput();

        if (IsServer)
        {
            ServerUpdateShield(dt);
            ServerSimulate(dt);
        }
    }

    void LateUpdate()
    {
        if (!IsSpawned || !IsLocalPlayer || cameraPivot == null)
            return;

        float yawOffset = Mathf.DeltaAngle(transform.eulerAngles.y, lookYaw);
        cameraPivot.localRotation = Quaternion.Euler(reportedPitch, yawOffset, 0f);
    }

    void ReadLocalInput()
    {
        float dt = Time.deltaTime;
        Vector2 look = lookAction.ReadValue<Vector2>();
        bool mouseLook = lookAction.activeControl?.device is Mouse;
        Vector2 angles = mouseLook
            ? look * mouseDegreesPerPixel
            : look * gamepadDegreesPerSecond * dt;

        if (lockOnAction != null && lockOnAction.WasPressedThisFrame())
            ToggleLockOn();

        // While locked, a quick sideways flick of the mouse or stick switches to the next target on that side.
        if (lockTarget != null)
        {
            lockSwitchPush = Mathf.MoveTowards(lockSwitchPush + angles.x, 0f, 60f * dt);
            if (Mathf.Abs(lockSwitchPush) >= lockOnSwitchDegrees && Time.time >= nextLockSwitchTime)
            {
                SwitchLockOnTarget(lockSwitchPush > 0f ? 1 : -1);
                lockSwitchPush = 0f;
                nextLockSwitchTime = Time.time + 0.25f;
            }
        }
        else
        {
            lockSwitchPush = 0f;
        }

        SubmitLocalInput(moveAction.ReadValue<Vector2>(), angles, sprintAction.IsPressed(),
            jumpAction.WasPressedThisFrame(), attackAction.WasPressedThisFrame(),
            blockAction != null && blockAction.IsPressed());
    }

    // Owner only. Locks the view on the opponent nearest the center of view, or releases the lock.
    // Returns true when a lock is now held.
    public bool ToggleLockOn()
    {
        if (!IsSpawned || !IsOwner)
            return false;
        if (lockTarget != null)
        {
            lockTarget = null;
            return false;
        }

        lockTarget = FindLockOnTarget();
        return lockTarget != null;
    }

    // Owner only. Moves the lock to the nearest opponent to the right (direction 1) or left (direction -1)
    // of the current view. Returns false and keeps the current target when nobody is on that side.
    public bool SwitchLockOnTarget(int direction)
    {
        if (!IsSpawned || !IsOwner || lockTarget == null || direction == 0)
            return false;

        Vector3 eye = cameraPivot != null ? cameraPivot.position : transform.position;
        NAHNetworkPlayer best = null;
        float bestOffset = float.MaxValue;
        foreach (var other in FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None))
        {
            if (other == this || other == lockTarget || !other.IsSpawned || other.Eliminated)
                continue;
            Vector3 toOther = other.transform.position - eye;
            toOther.y = 0f;
            if (toOther.sqrMagnitude < 0.0001f || toOther.magnitude > lockOnRange)
                continue;
            float yawToOther = Mathf.Atan2(toOther.x, toOther.z) * Mathf.Rad2Deg;
            float offset = Mathf.DeltaAngle(lookYaw, yawToOther) * Mathf.Sign(direction);
            if (offset > 0f && offset < bestOffset)
            {
                bestOffset = offset;
                best = other;
            }
        }

        if (best == null)
            return false;
        lockTarget = best;
        return true;
    }

    NAHNetworkPlayer FindLockOnTarget()
    {
        Vector3 eye = cameraPivot != null ? cameraPivot.position : transform.position;
        Vector3 viewForward = Quaternion.Euler(0f, lookYaw, 0f) * Vector3.forward;
        NAHNetworkPlayer best = null;
        float bestAngle = lockOnConeDegrees;
        foreach (var other in FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None))
        {
            if (other == this || !other.IsSpawned || other.Eliminated)
                continue;
            Vector3 toOther = other.transform.position - eye;
            toOther.y = 0f;
            if (toOther.sqrMagnitude < 0.0001f || toOther.magnitude > lockOnRange)
                continue;
            float angle = Vector3.Angle(viewForward, toOther);
            if (angle <= bestAngle)
            {
                bestAngle = angle;
                best = other;
            }
        }
        return best;
    }

    // While locked on, the view turns toward the target instead of following the mouse or stick.
    Vector2 LockOnLook(Vector2 manualLook)
    {
        if (lockTarget == null || !lockTarget.IsSpawned || lockTarget.Eliminated)
        {
            lockTarget = null;
            return manualLook;
        }

        Vector3 eye = cameraPivot != null ? cameraPivot.position : transform.position;
        Vector3 toTarget = lockTarget.transform.position - eye;
        float flat = new Vector2(toTarget.x, toTarget.z).magnitude;
        if (flat > lockOnBreakRange)
        {
            lockTarget = null;
            return manualLook;
        }

        float desiredYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float desiredPitch = Mathf.Clamp(-Mathf.Atan2(toTarget.y, Mathf.Max(0.1f, flat)) * Mathf.Rad2Deg, -pitchLimit, pitchLimit);
        float step = lockOnTurnDegreesPerSecond * Time.deltaTime;
        float yaw = Mathf.Clamp(Mathf.DeltaAngle(lookYaw, desiredYaw), -step, step);
        float pitch = Mathf.Clamp(desiredPitch - reportedPitch, -step, step);
        // SubmitLocalInput subtracts the vertical part, so a positive pitch change is sent as negative.
        return new Vector2(yaw, -pitch);
    }

    void SubmitLocalInput(Vector2 move, Vector2 lookDegrees, bool sprint, bool jumpPressed, bool attackPressed, bool block)
    {
        if (lockTarget != null)
            lookDegrees = LockOnLook(lookDegrees);

        lookYaw = Mathf.Repeat(lookYaw + lookDegrees.x, 360f);
        reportedPitch = Mathf.Clamp(reportedPitch - lookDegrees.y, -pitchLimit, pitchLimit);

        move = Vector2.ClampMagnitude(move, 1f);
        // Movement and aim go out once per network tick (30 a second), the rate the host sends positions back,
        // instead of every rendered frame. Each message carries the whole current state, so a lost one is
        // replaced by the next. Moments where a wait would be felt go out at once: a held button changing,
        // starting or stopping to move, and a jump or attack press, so the host acts on the latest aim.
        int tick = NetworkManager.LocalTime.Tick;
        bool moving = move.sqrMagnitude > 0.0001f;
        if (tick != lastInputTick || sprint != lastSentSprint || block != lastSentBlock || moving != lastSentMoving ||
            jumpPressed || attackPressed)
        {
            lastInputTick = tick;
            lastSentSprint = sprint;
            lastSentBlock = block;
            lastSentMoving = moving;
            InputMessagesSent++;
            SubmitInputRpc(move.x, move.y, lookYaw, reportedPitch, sprint, block);
        }
        if (jumpPressed)
            JumpPressedRpc();
        if (attackPressed)
            RequestAttackRpc();
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    // Development builds only. NAHNetworkDemoDriver plays this fighter through the same owner RPCs as the keyboard,
    // so the server still decides movement, hits, and lives. Once called, the keyboard and mouse are ignored.
    public void SubmitScriptedInput(Vector2 move, Vector2 lookDegrees, bool sprint, bool jumpPressed, bool attackPressed, bool block = false)
    {
        if (!IsSpawned || !IsLocalPlayer || Eliminated)
            return;
        scriptedInput = true;
        SubmitLocalInput(move, lookDegrees, sprint, jumpPressed, attackPressed, block);
    }
#endif

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner, Delivery = RpcDelivery.Unreliable)]
    void SubmitInputRpc(float x, float y, float yaw, float pitch, bool sprint, bool block)
    {
        // A modified client can send NaN or infinity. One such value would spread into this fighter's server position.
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(yaw) || !float.IsFinite(pitch))
            return;

        Vector2 move = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        moveX = move.x;
        moveY = move.y;
        float yawStep = Mathf.Clamp(Mathf.DeltaAngle(serverYaw, yaw), -120f, 120f);
        serverYaw = Mathf.Repeat(serverYaw + yawStep, 360f);
        reportedPitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
        sprintHeld = sprint;
        blockHeld = block;
    }

    // Owner only. A real character select will send this before joining. For now it is sent right after spawning.
    public void RequestBodyShape(NAHBodyShape shape)
    {
        RequestBuild(shape, -1, -1);
    }

    // Owner only. Body, race, and class travel together, because the host accepts one choice per fighter.
    // An index of -1 means no race or no class.
    public void RequestBuild(NAHBodyShape shape, int raceChoice, int classChoice)
    {
        if (IsSpawned && IsOwner)
            RequestBuildRpc(shape.size, shape.fatness, shape.roundness, raceChoice, classChoice);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void RequestBuildRpc(float size, float fatness, float roundness, int raceChoice, int classChoice)
    {
        ServerSetBuild(new NAHBodyShape(size, fatness, roundness), raceChoice, classChoice);
    }

    public bool ServerSetBody(NAHBodyShape shape)
    {
        return ServerSetBuild(shape, -1, -1);
    }

    // The first valid choice wins. After that, or once this fighter has attacked or been hit,
    // the build is locked, so nobody can switch to a tougher body, race, or class in the middle of a fight.
    public bool ServerSetBuild(NAHBodyShape shape, int raceChoice, int classChoice)
    {
        if (!IsServer || bodyLocked || !shape.IsValid || statRules == null || weapon == null)
            return false;
        if (!InRoster(raceChoice, roster != null ? roster.RaceCount : 0) || !InRoster(classChoice, roster != null ? roster.ClassCount : 0))
            return false;

        var chosenRace = roster != null ? roster.Race(raceChoice) : null;
        var chosenClass = roster != null ? roster.Class(classChoice) : null;
        if ((raceChoice >= 0 && chosenRace == null) || (classChoice >= 0 && chosenClass == null))
            return false;
        if ((chosenRace != null && !chosenRace.Modifiers.IsValid) || (chosenClass != null && !chosenClass.Modifiers.IsValid))
        {
            Debug.LogError("A roster entry has a ratio of zero or less, or one that is not a number. Fix it in the Inspector.", this);
            return false;
        }

        float oldHeight = bodySize.Value.y;
        body = shape;
        race = chosenRace;
        fighterClass = chosenClass;
        raceIndex.Value = raceChoice;
        classIndex.Value = classChoice;
        bodyLocked = true;
        ServerRecomputeStats();
        health.Value = maxHealth.Value;
        ServerRefillShield();

        // Keep the feet where they were, so a taller body does not start inside the floor.
        float rise = (bodySize.Value.y - oldHeight) * BaseHalfHeight;
        ServerPlace(transform.position + Vector3.up * Mathf.Max(0f, rise), serverYaw);
        return true;
    }

    static bool InRoster(int index, int count)
    {
        return index >= -1 && index < count;
    }

    void ServerRecomputeStats()
    {
        if (statRules == null || weapon == null)
        {
            Debug.LogError("This fighter has no stat rules or weapon assigned, so it has no health. Run NAH > Create Network Arena.", this);
            return;
        }

        var raceRatios = race != null ? race.Modifiers : NAHStatModifiers.None;
        var classRatios = fighterClass != null ? fighterClass.Modifiers : NAHStatModifiers.None;
        stats = statRules.Compute(body, weapon, raceRatios, classRatios, false);
        maxHealth.Value = stats.MaxHealth;
        maxShield.Value = stats.Shielding;
        bodySize.Value = new Vector2(stats.BodyWidth, stats.BodyHeight);
        ApplyBodySize(bodySize.Value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void JumpPressedRpc()
    {
        jumpQueued = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    void RequestAttackRpc()
    {
        ServerTryAttack();
    }

    public int ServerTryAttack()
    {
        if (!IsServer || Eliminated || health.Value <= 0f || downTimer > 0f)
            return 0;
        if (NAHNetworkMatch.Instance != null && NAHNetworkMatch.Instance.CurrentPhase == NAHNetworkMatch.Phase.Finished)
            return 0;
        if (Time.time < nextAttackTime || attackOrigin == null || statRules == null)
            return 0;
        // Placeholder rule: a fighter who is blocking cannot swing at the same time.
        if (IsBlockingNow())
            return 0;

        nextAttackTime = Time.time + stats.SwingInterval;
        bodyLocked = true;
        alreadyHit.Clear();
        Physics.SyncTransforms();

        // Fighting-game style (owner decision 2026-10-31): the swing is a box in front of the body that runs from
        // below the feet to above the head, so camera pitch never decides a hit. Facing and reach still do.
        // The body only turns left and right, so its forward is flat.
        Vector3 flatForward = transform.forward;
        float radius = controller != null ? controller.radius : BaseRadius;
        float length = radius + stats.Reach;
        Vector3 center = transform.position + flatForward * (length * 0.5f);
        var halfExtents = new Vector3(hitRadius, BaseHalfHeight * bodySize.Value.y + SwingHeightMargin, length * 0.5f);
        int count = Physics.OverlapBoxNonAlloc(
            center, halfExtents, hitBuffer, transform.rotation, hittableLayers, QueryTriggerInteraction.Ignore);
        if (count == hitBuffer.Length)
            Debug.LogWarning("Melee collider buffer may be full.", this);

        int hits = 0;
        for (int i = 0; i < count; i++)
        {
            var target = hitBuffer[i].GetComponentInParent<NAHNetworkPlayer>();
            if (target == null || target == this || target.Eliminated || alreadyHit.Contains(target))
                continue;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f || Vector3.Dot(flatForward, toTarget.normalized) < 0.2f)
                continue;
            if (Physics.Linecast(attackOrigin.position, hitBuffer[i].bounds.center, environmentOccluders, QueryTriggerInteraction.Ignore))
                continue;

            alreadyHit.Add(target);
            target.ServerReceiveHit(stats.AttackDamage, transform.position);
            hits++;
        }

        return hits;
    }

    public bool ServerApplyDamage(float amount)
    {
        if (!IsServer || Eliminated || !float.IsFinite(amount) || amount <= 0f || downTimer > 0f || health.Value <= 0f)
            return false;
        if (NAHNetworkMatch.Instance != null && NAHNetworkMatch.Instance.CurrentPhase == NAHNetworkMatch.Phase.Finished)
            return false;

        bodyLocked = true;
        health.Value = Mathf.Max(0f, health.Value - amount);
        if (health.Value > 0f)
            return true;

        lives.Value = Mathf.Max(0, lives.Value - 1);
        if (lives.Value <= 0)
        {
            eliminated.Value = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            NAHNetworkMatch.Instance?.Recompute();
            return true;
        }

        downTimer = RespawnLockSeconds;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        return true;
    }

    // A swing from another fighter. If this fighter is blocking toward the attacker, the shield takes the hit first.
    // Whatever the shield cannot hold goes through armor to health.
    public void ServerReceiveHit(float attackDamage, Vector3 attackerPosition)
    {
        if (!IsServer || Eliminated || statRules == null || !float.IsFinite(attackDamage) || attackDamage <= 0f)
            return;
        if (NAHNetworkMatch.Instance != null && NAHNetworkMatch.Instance.CurrentPhase == NAHNetworkMatch.Phase.Finished)
            return;

        float remaining = attackDamage;
        if (IsBlockingNow() && FacesToward(attackerPosition))
        {
            float blocked = Mathf.Min(shield.Value, remaining);
            shield.Value -= blocked;
            remaining -= blocked;
            bodyLocked = true;
            lastShieldHitTime = Time.time;
            if (shield.Value <= 0.001f)
            {
                shield.Value = 0f;
                shieldBroken.Value = true;
                blocking.Value = false;
                shieldBrokenUntil = Time.time + statRules.ShieldBreakCooldownSeconds;
            }
        }

        if (remaining > 0.001f)
            ServerApplyDamage(statRules.DamageTaken(remaining, stats));
    }

    bool IsBlockingNow()
    {
        return blockHeld && !shieldBroken.Value && !Eliminated && downTimer <= 0f && shield.Value > 0f;
    }

    bool FacesToward(Vector3 point)
    {
        Vector3 toPoint = point - transform.position;
        toPoint.y = 0f;
        if (toPoint.sqrMagnitude < 0.0001f)
            return true;
        float halfCone = statRules != null ? statRules.BlockConeDegrees * 0.5f : 60f;
        return Vector3.Angle(transform.forward, toPoint) <= halfCone;
    }

    void ServerRefillShield()
    {
        shield.Value = maxShield.Value;
        shieldBroken.Value = false;
        shieldBrokenUntil = 0f;
    }

    // A broken shield waits out its cooldown and comes back full. A damaged one refills slowly after a short pause.
    void ServerUpdateShield(float dt)
    {
        if (statRules == null)
            return;
        if (shieldBroken.Value)
        {
            if (Time.time >= shieldBrokenUntil)
                ServerRefillShield();
        }
        else if (shield.Value < maxShield.Value && Time.time - lastShieldHitTime >= statRules.ShieldRefillDelaySeconds)
        {
            shield.Value = Mathf.Min(maxShield.Value, shield.Value + statRules.ShieldRefillPerSecond * dt);
        }

        bool nowBlocking = IsBlockingNow();
        if (blocking.Value != nowBlocking)
            blocking.Value = nowBlocking;
    }

    public void PublishMatch(NAHNetworkMatch.Phase phase, ulong winner)
    {
        if (!IsServer || !IsSpawned)
            return;
        matchPhase.Value = (int)phase;
        winnerClientId.Value = winner;
    }

    public void ServerPlaceForTest(Vector3 position, float yaw)
    {
        ServerPlace(position, yaw);
        spawnPosition = position;
    }

    void ServerPlace(Vector3 position, float yaw)
    {
        if (!IsServer)
            return;

        serverYaw = yaw;
        if (controller != null)
            controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (controller != null)
            controller.enabled = true;
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(reportedPitch, 0f, 0f);
        Physics.SyncTransforms();
    }

    void ServerSimulate(float dt)
    {
        if (controller == null || !controller.enabled)
            return;

        if (Eliminated)
            return;

        if (downTimer > 0f)
        {
            downTimer -= dt;
            if (downTimer <= 0f)
            {
                health.Value = maxHealth.Value;
                ServerRefillShield();
                horizontalVelocity = Vector3.zero;
                verticalVelocity = 0f;
                ServerPlace(SpawnCenter(spawnPosition), serverYaw);
            }
            return;
        }

        bool grounded = controller.isGrounded;
        coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - dt);
        if (jumpQueued)
        {
            jumpBufferTimer = jumpBufferTime;
            jumpQueued = false;
        }
        else
        {
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);
        }

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && gravity < 0f)
        {
            verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        verticalVelocity += gravity * dt;
        transform.rotation = Quaternion.Euler(0f, serverYaw, 0f);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(reportedPitch, 0f, 0f);

        Vector3 direction = transform.right * moveX + transform.forward * moveY;
        // Bigger bodies, races, and classes scale the approved heavy walk. An average body keeps it exactly.
        float speed = (sprintHeld ? sprintSpeed : walkSpeed) * stats.MoveSpeedScale;
        float acceleration = grounded
            ? (moveX * moveX + moveY * moveY > 0.001f ? groundAcceleration : groundDeceleration)
            : airAcceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, direction * speed, acceleration * dt);
        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
    }

    static Vector3 SlotPosition(int slot)
    {
        switch (slot)
        {
            case 0: return new Vector3(0f, 1f, -3f);
            case 1: return new Vector3(0f, 1f, 3f);
            case 2: return new Vector3(-3f, 1f, -3f);
            default: return new Vector3(-3f, 1f, 3f);
        }
    }

    static float SlotYaw(int slot)
    {
        return slot % 2 == 0 ? 0f : 180f;
    }
}
