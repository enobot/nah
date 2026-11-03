#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Development builds only. Plays the local fighter with scripted input so two windows can be recorded
// without a person at the keyboard. It starts only when the player is launched with --nah-demo.
// Input goes through NAHNetworkPlayer.SubmitScriptedInput, the same owner RPCs the keyboard uses,
// so the server still decides movement, hits, lives, and the winner.
// This is a test tool. It is not an AI opponent and not an approved fighting style.
public sealed class NAHNetworkDemoDriver : MonoBehaviour
{
    const float TurnDegreesPerSecond = 120f;
    // 10 frames a second by default; --nah-capture-fps raises it to show how smoothly things move.
    float captureInterval = 0.1f;
    const float SecondsAfterMatchEnds = 4f;
    // Ready up a moment after arriving, so a recording shows the waiting screen.
    const float ReadyDelaySeconds = 1f;

    int startPlayers = NAHNetworkMatch.MinPlayersToStart;
    float fighterSince = -1f;
    float nextReadyRequest;
    float swingInterval = 0.6f;
    float strafe;
    float timeLimit = 120f;
    string captureFolder;
    float startTime;
    float nextSwing;
    float nextLog;
    float nextCapture;
    float quitTime = -1f;
    int frameIndex;
    bool wasListening;
    bool hadFighter;
    bool sawFinish;
    bool lockOn;
    bool block;
    int lastLoggedInputs;
    float lastLogTime;
    // Own-movement probe: when this fighter starts walking from a standstill, how long until this screen shows it move.
    // It watches the camera, because that is what the player sees of their own fighter.
    Vector2 lastMove;
    Vector3 lastPosition;
    Vector3 stillAnchor;
    bool positionChanged;
    float stillSince;
    float probeStart = -1f;
    Vector3 probeFrom;
    // --nah-walk-test N: before readying up, stand still and walk N times to measure the probe above many times.
    int walkTests;
    int walkTestsDone;
    int walkProbes;
    float walkDirection = 1f;
    // How many frames the own view and the nearest opponent moved on this screen since the last log line.
    // A smoothed body moves every frame; an unsmoothed one only when a network update arrives (30 a second).
    int viewMoveFrames;
    int otherMoveFrames;
    Vector3 lastOtherPosition;
    // Joining player: the largest correction the host made to this screen's prediction since the last log line.
    float largestCorrection;
    int lastCorrections;
    int loggedCorrections;
    // --nah-push: keep walking into the opponent instead of holding at swinging range, to measure corrections while
    // two bodies push against each other.
    bool push;
    // --nah-smoothing type,lerp,seconds: how this screen smooths the other fighters. An experiment switch for
    // choosing Netcode's smoothing settings; without it the prefab's settings apply.
    bool smoothingSet;
    NetworkTransform.InterpolationTypes smoothingType;
    bool smoothingLerp;
    float smoothingSeconds;
    // --nah-track file: 100 times a second, write where this fighter's body and the nearest opponent are on this
    // screen, on the PC's shared clock. Lining up the opponent's own file with this one shows how far in the past
    // this screen shows other fighters. A separate buffered file, because Unity's log adds a stack trace per line.
    StreamWriter track;
    float nextTrack;
    // Stall share: between the first and the last frame the opponent moved in a log interval, the share of frames it
    // stood still on this screen. Smooth movement gives about 0 %; steps from 30 updates a second give most frames.
    int frameCounter;
    int otherFirstMove = -1;
    int otherLastMove;
    int otherExactMoves;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args, "--nah-demo") < 0)
            return;

        var driverObject = new GameObject("DEV Demo Driver");
        DontDestroyOnLoad(driverObject);
        var driver = driverObject.AddComponent<NAHNetworkDemoDriver>();
        driver.swingInterval = ReadFloat(args, "--nah-swing-interval", driver.swingInterval);
        driver.strafe = Mathf.Clamp(ReadFloat(args, "--nah-strafe", 0f), -1f, 1f);
        driver.timeLimit = ReadFloat(args, "--nah-demo-seconds", driver.timeLimit);
        driver.lockOn = Array.IndexOf(args, "--nah-lock-on") >= 0;
        driver.block = Array.IndexOf(args, "--nah-block") >= 0;
        // On the host: press Start once this many players are in and everyone is ready.
        driver.startPlayers = Mathf.Clamp(Mathf.RoundToInt(ReadFloat(args, "--nah-start-players", driver.startPlayers)),
            NAHNetworkMatch.MinPlayersToStart, NAHNetworkMatch.MaxPlayers);
        driver.walkTests = Mathf.Clamp(Mathf.RoundToInt(ReadFloat(args, "--nah-walk-test", 0f)), 0, 100);
        driver.push = Array.IndexOf(args, "--nah-push") >= 0;
        string trackFile = ReadArg(args, "--nah-track");
        if (!string.IsNullOrEmpty(trackFile))
            driver.track = new StreamWriter(trackFile, false);
        string smoothing = ReadArg(args, "--nah-smoothing");
        if (smoothing != null)
        {
            if (driver.TryReadSmoothing(smoothing))
                Debug.Log("NAHDEMO smoothing of other fighters on this screen: " + smoothing);
            else
                Debug.LogError("NAHDEMO --nah-smoothing must be legacy, lerp, or damp, then 0 or 1, then seconds, for example legacy,1,0.1. Ignored.");
        }
        driver.captureInterval = 1f / Mathf.Clamp(ReadFloat(args, "--nah-capture-fps", 10f), 1f, 60f);
        driver.captureFolder = ReadArg(args, "--nah-capture");
        if (!string.IsNullOrEmpty(driver.captureFolder))
            Directory.CreateDirectory(driver.captureFolder);
        Debug.Log("NAHDEMO driver started. Swing every " + driver.swingInterval + " s. Capture: " + (driver.captureFolder ?? "off"));
    }

    void Start()
    {
        startTime = Time.realtimeSinceStartup;
    }

    void Update()
    {
        float now = Time.realtimeSinceStartup;
        if (now - startTime > timeLimit)
        {
            Debug.Log("NAHDEMO time limit reached. Quitting.");
            Application.Quit();
            enabled = false;
            return;
        }

        var manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening)
            wasListening = true;

        NAHNetworkPlayer me = LocalFighter();
        if (me == null)
        {
            // The host quits a few seconds after the match ends, which disconnects this client.
            if (hadFighter)
            {
                Debug.Log("NAHDEMO disconnected from the host " + (sawFinish ? "after" : "before") + " the match finished.");
                Application.Quit();
                enabled = false;
            }
            else if (wasListening && (manager == null || !manager.IsListening))
            {
                string reason = manager != null ? manager.DisconnectReason : null;
                Debug.Log("NAHDEMO the connection ended before this window got a fighter. Reason: " +
                    (string.IsNullOrEmpty(reason) ? "none given" : reason));
                Application.Quit();
                enabled = false;
            }
            return;
        }
        hadFighter = true;
        if (fighterSince < 0f)
            fighterSince = now;
        TrackStillness(me, now);

        ApplySmoothing();
        NAHNetworkPlayer opponent = NearestOpponent(me);
        CountMoveFrames(opponent);
        Track(me, opponent, now);
        if (me.PredictionCorrections != lastCorrections)
        {
            lastCorrections = me.PredictionCorrections;
            largestCorrection = Mathf.Max(largestCorrection, me.LastPredictionError);
        }
        bool fighting = me.MatchPhase == NAHNetworkMatch.Phase.Fighting;
        bool finished = me.MatchPhase == NAHNetworkMatch.Phase.Finished;

        Capture(now);
        if (now >= nextLog)
            LogNow(me, opponent, now);
        if (me.MatchPhase == NAHNetworkMatch.Phase.Waiting && walkTestsDone < walkTests)
        {
            WalkTest(me, now);
            return;
        }
        if (me.MatchPhase == NAHNetworkMatch.Phase.Waiting)
        {
            if (!me.Ready && now - fighterSince >= ReadyDelaySeconds && now >= nextReadyRequest)
            {
                me.RequestReady(true);
                nextReadyRequest = now + 1f;
            }
            var match = NAHNetworkMatch.Instance;
            if (manager.IsServer && match != null && match.PlayerCount >= startPlayers && match.StartMatch())
                Debug.Log("NAHDEMO host pressed Start with " + match.PlayerCount + " players, all ready.");
        }

        if (finished)
        {
            sawFinish = true;
            if (quitTime < 0f)
            {
                quitTime = now + SecondsAfterMatchEnds;
                Debug.Log("NAHDEMO match finished. Winner client " + me.WinnerClientId + ".");
            }
            else if (now >= quitTime)
            {
                Application.Quit();
                enabled = false;
            }
            me.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
            lastMove = Vector2.zero;
            return;
        }

        if (!fighting || opponent == null)
        {
            me.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
            lastMove = Vector2.zero;
            return;
        }

        // Turn the view toward the opponent at a fixed rate.
        Transform view = ViewOf(me);
        Vector3 toOpponent = opponent.transform.position - me.transform.position;
        toOpponent.y = 0f;
        float distance = toOpponent.magnitude;
        float desiredYaw = Mathf.Atan2(toOpponent.x, toOpponent.z) * Mathf.Rad2Deg;
        float yawError = Mathf.DeltaAngle(view.eulerAngles.y, desiredYaw);
        float maxTurn = TurnDegreesPerSecond * Time.deltaTime;
        // Look up or down at the opponent's middle, as a person would. Positive pitch looks down.
        float rise = opponent.transform.position.y - view.position.y;
        float desiredPitch = -Mathf.Atan2(rise, Mathf.Max(0.1f, distance)) * Mathf.Rad2Deg;
        float pitchError = desiredPitch - Mathf.DeltaAngle(0f, view.localEulerAngles.x);
        var look = new Vector2(Mathf.Clamp(yawError, -maxTurn, maxTurn), -Mathf.Clamp(pitchError, -maxTurn, maxTurn));

        // With --nah-lock-on, the lock-on button does the aiming instead of this script.
        if (lockOn && me.LockOnTarget == null)
            me.ToggleLockOn();
        if (lockOn && me.LockOnTarget != null)
            look = Vector2.zero;

        // Walk into swinging range, then hold there.
        float forward = push ? 1f : (distance > 1.9f ? 1f : (distance < 1.2f ? -1f : 0f));
        var move = new Vector2(strafe, forward);

        bool swing = Mathf.Abs(yawError) < 15f && distance < 2.6f && now >= nextSwing;
        if (swing)
            nextSwing = now + swingInterval;

        ProbeOwnMovement(me, move, now);

        // With --nah-block, hold Block the whole fight (a blocking fighter cannot swing).
        me.SubmitScriptedInput(move, look, false, false, swing && !block, block);
    }

    // How long this screen takes to show the fighter's own walk: on a joining player's screen that is its prediction,
    // on the host its own simulation. This logs that delay each time the fighter starts walking from a standstill.
    // On the host the same number shows how long the body takes to cover 2 cm with no network at all.
    // Still means the view has stayed within 2 mm of one point. Comparing frame to frame instead would call a slow
    // drift still at several hundred frames a second, and the next probe would then count that drift as walking.
    void TrackStillness(NAHNetworkPlayer me, float now)
    {
        Vector3 position = ViewOf(me).position;
        if (FlatDistance(position, stillAnchor) > 0.002f)
        {
            // The first change on a client is the spawn position arriving from the host, not movement.
            if (stillSince > 0f)
                positionChanged = true;
            stillSince = now;
            stillAnchor = position;
        }
        if (FlatDistance(position, lastPosition) > 0.0001f)
            viewMoveFrames++;
        lastPosition = position;
    }

    void CountMoveFrames(NAHNetworkPlayer opponent)
    {
        frameCounter++;
        if (opponent == null)
            return;
        Vector3 position = opponent.transform.position;
        if (FlatDistance(position, lastOtherPosition) > 0.0001f)
            otherMoveFrames++;
        if (position != lastOtherPosition)
        {
            otherExactMoves++;
            if (otherFirstMove < 0)
                otherFirstMove = frameCounter;
            otherLastMove = frameCounter;
        }
        lastOtherPosition = position;
    }

    // One line: clock in ms, phase, own client id and body x z, then the opponent's client id and x z (or "none").
    void Track(NAHNetworkPlayer me, NAHNetworkPlayer opponent, float now)
    {
        if (track == null || now < nextTrack)
            return;
        nextTrack = now + 0.01f;
        Vector3 mine = me.transform.position;
        string other = opponent == null
            ? "none"
            : opponent.OwnerClientId + " " + opponent.transform.position.x.ToString("0.0000", CultureInfo.InvariantCulture) + " " +
              opponent.transform.position.z.ToString("0.0000", CultureInfo.InvariantCulture);
        track.WriteLine(ClockMilliseconds() + " " + me.MatchPhase + " " + me.OwnerClientId + " " +
            mine.x.ToString("0.0000", CultureInfo.InvariantCulture) + " " + mine.z.ToString("0.0000", CultureInfo.InvariantCulture) +
            " " + other);
    }

    void OnDestroy()
    {
        if (track != null)
        {
            track.Dispose();
            track = null;
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool QueryPerformanceCounter(out long count);
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern bool QueryPerformanceFrequency(out long frequency);
#endif

    // Windows' performance counter in milliseconds, which every process on one PC reads the same. Stopwatch is not
    // used: in this player it counted from each process's start (two windows started 4 s apart differed by 4.1 s).
    static string ClockMilliseconds()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (QueryPerformanceCounter(out long count) && QueryPerformanceFrequency(out long frequency) && frequency > 0)
            return (count * 1000.0 / frequency).ToString("0.0", CultureInfo.InvariantCulture);
#endif
        return "unavailable";
    }

    bool TryReadSmoothing(string text)
    {
        string[] parts = text.Split(',');
        if (parts.Length != 3)
            return false;
        switch (parts[0])
        {
            case "legacy": smoothingType = NetworkTransform.InterpolationTypes.LegacyLerp; break;
            case "lerp": smoothingType = NetworkTransform.InterpolationTypes.Lerp; break;
            case "damp": smoothingType = NetworkTransform.InterpolationTypes.SmoothDampening; break;
            default: return false;
        }
        if (parts[1] != "0" && parts[1] != "1")
            return false;
        smoothingLerp = parts[1] == "1";
        if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out smoothingSeconds) ||
            !(smoothingSeconds > 0f) || smoothingSeconds > 1f)
            return false;
        smoothingSet = true;
        return true;
    }

    // Smoothing happens on the screen that watches, so only this screen's copies of the other fighters change.
    void ApplySmoothing()
    {
        if (!smoothingSet)
            return;
        foreach (var player in FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None))
        {
            if (player.IsLocalPlayer)
                continue;
            var networkTransform = player.GetComponent<NetworkTransform>();
            if (networkTransform == null)
                continue;
            networkTransform.PositionInterpolationType = smoothingType;
            networkTransform.RotationInterpolationType = smoothingType;
            networkTransform.PositionLerpSmoothing = smoothingLerp;
            networkTransform.RotationLerpSmoothing = smoothingLerp;
            networkTransform.PositionMaxInterpolationTime = smoothingSeconds;
            networkTransform.RotationMaxInterpolationTime = smoothingSeconds;
        }
    }

    // Stand still for a moment, walk until the probe sees the view move 2 cm, and repeat, turning around each time
    // so the fighter stays near its spawn point. Then the driver readies up as usual.
    void WalkTest(NAHNetworkPlayer me, float now)
    {
        bool standing = probeStart < 0f;
        var move = Vector2.zero;
        if (!standing || (positionChanged && now - stillSince >= 0.6f))
            move = new Vector2(0f, walkDirection);
        else if (!positionChanged && now - fighterSince >= 1f)
        {
            // Nothing has moved on this screen yet, so the probe cannot tell a still view from one that never updates.
            // One short walk gives it a first movement to compare with.
            move = new Vector2(0f, walkDirection);
        }
        int before = walkProbes;
        ProbeOwnMovement(me, move, now);
        if (walkProbes != before)
        {
            walkTestsDone++;
            walkDirection = -walkDirection;
            move = Vector2.zero;
            lastMove = Vector2.zero;
            if (walkTestsDone == walkTests)
                Debug.Log("NAHDEMO walk test finished after " + walkTests + " walks.");
        }
        me.SubmitScriptedInput(move, Vector2.zero, false, false, false);
    }

    void ProbeOwnMovement(NAHNetworkPlayer me, Vector2 move, float now)
    {
        Vector3 position = ViewOf(me).position;
        if (probeStart < 0f && positionChanged && move != Vector2.zero && lastMove == Vector2.zero && now - stillSince >= 0.25f)
        {
            probeStart = now;
            probeFrom = position;
        }
        else if (probeStart >= 0f && FlatDistance(position, probeFrom) > 0.02f)
        {
            Debug.Log("NAHDEMO own movement showed " + ((now - probeStart) * 1000f).ToString("0") + " ms after the walk input. clock=" + ClockMilliseconds());
            probeStart = -1f;
            walkProbes++;
        }
        else if (probeStart >= 0f && now - probeStart > 2f)
        {
            Debug.Log("NAHDEMO own movement probe gave up after 2 s (the fighter was blocked or stopped).");
            probeStart = -1f;
            walkProbes++;
        }
        lastMove = move;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void Capture(float now)
    {
        if (string.IsNullOrEmpty(captureFolder) || now < nextCapture)
            return;
        // Keep a steady capture rate so the video plays back at real speed.
        nextCapture = now - nextCapture > captureInterval ? now + captureInterval : nextCapture + captureInterval;
        ScreenCapture.CaptureScreenshot(Path.Combine(captureFolder, "frame_" + frameIndex.ToString("D4") + ".png"));
        frameIndex++;
    }

    void LogNow(NAHNetworkPlayer me, NAHNetworkPlayer opponent, float now)
    {
        nextLog = now + 0.5f;
        float elapsed = Mathf.Max(0.001f, now - lastLogTime);
        float inputsPerSecond = lastLogTime > 0f ? (me.InputMessagesSent - lastLoggedInputs) / elapsed : 0f;
        string moves = lastLogTime > 0f
            ? " | view moves/s=" + (viewMoveFrames / elapsed).ToString("0") + " other moves/s=" + (otherMoveFrames / elapsed).ToString("0")
            : "";
        int span = otherFirstMove < 0 ? 0 : otherLastMove - otherFirstMove + 1;
        if (span >= 20)
            moves += " other stalls=" + (100f * (span - otherExactMoves) / span).ToString("0") + "%";
        otherFirstMove = -1;
        otherExactMoves = 0;
        if (me.Predicting)
        {
            moves += " | corrections=" + (me.PredictionCorrections - loggedCorrections) + " max=" +
                (largestCorrection * 100f).ToString("0.0") + " cm jumps=" + me.PredictionSnaps;
            loggedCorrections = me.PredictionCorrections;
        }
        lastLoggedInputs = me.InputMessagesSent;
        lastLogTime = now;
        viewMoveFrames = 0;
        otherMoveFrames = 0;
        largestCorrection = 0f;
        Log(me, opponent, inputsPerSecond, moves);
    }

    static void Log(NAHNetworkPlayer me, NAHNetworkPlayer opponent, float inputsPerSecond, string moves)
    {
        var manager = NetworkManager.Singleton;
        string rtt = manager.IsServer
            ? "host"
            : manager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId) + " ms";
        string other = opponent == null
            ? "none"
            : "client " + opponent.OwnerClientId + " at " + opponent.transform.position.ToString("F2") + " " + opponent.Health.ToString("0") + " hp " + opponent.Lives + " lives";
        Debug.Log("NAHDEMO t=" + Time.realtimeSinceStartup.ToString("0.0") +
            " me=client " + me.OwnerClientId + " at " + me.transform.position.ToString("F2") +
            " " + me.Health.ToString("0") + " hp " + me.Lives + " lives" + (me.Eliminated ? " OUT" : "") +
            " | other=" + other +
            " | phase=" + me.MatchPhase +
            " | rtt=" + rtt +
            " | inputs/s=" + inputsPerSecond.ToString("0") +
            " | fps=" + (1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)).ToString("0") + moves);
    }

    static NAHNetworkPlayer LocalFighter()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
            return null;
        return manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
    }

    static NAHNetworkPlayer NearestOpponent(NAHNetworkPlayer me)
    {
        NAHNetworkPlayer best = null;
        float bestDistance = float.MaxValue;
        foreach (var player in FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None))
        {
            if (player == me || player.Eliminated)
                continue;
            float d = (player.transform.position - me.transform.position).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = player;
            }
        }
        return best;
    }

    static Transform ViewOf(NAHNetworkPlayer me)
    {
        var camera = me.GetComponentInChildren<Camera>(true);
        return camera != null ? camera.transform : me.transform;
    }

    static float ReadFloat(string[] args, string name, float fallback)
    {
        string text = ReadArg(args, name);
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
            ? value
            : fallback;
    }

    static string ReadArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }
}
#endif
