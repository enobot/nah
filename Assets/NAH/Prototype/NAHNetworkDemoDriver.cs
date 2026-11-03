#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using Unity.Netcode;
using UnityEngine;

// Development builds only. Plays the local fighter with scripted input so two windows can be recorded
// without a person at the keyboard. It starts only when the player is launched with --nah-demo.
// Input goes through NAHNetworkPlayer.SubmitScriptedInput, the same owner RPCs the keyboard uses,
// so the server still decides movement, hits, lives, and the winner.
// This is a test tool. It is not an AI opponent and not an approved fighting style.
public sealed class NAHNetworkDemoDriver : MonoBehaviour
{
    const float TurnDegreesPerSecond = 120f;
    const float CaptureInterval = 0.1f;
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
    Vector2 lastMove;
    Vector3 lastPosition;
    bool positionChanged;
    float stillSince;
    float probeStart = -1f;
    Vector3 probeFrom;

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

        NAHNetworkPlayer opponent = NearestOpponent(me);
        bool fighting = me.MatchPhase == NAHNetworkMatch.Phase.Fighting;
        bool finished = me.MatchPhase == NAHNetworkMatch.Phase.Finished;

        Capture(now);
        if (now >= nextLog)
        {
            nextLog = now + 0.5f;
            float inputsPerSecond = lastLogTime > 0f ? (me.InputMessagesSent - lastLoggedInputs) / Mathf.Max(0.001f, now - lastLogTime) : 0f;
            lastLoggedInputs = me.InputMessagesSent;
            lastLogTime = now;
            Log(me, opponent, inputsPerSecond);
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
        float forward = distance > 1.9f ? 1f : (distance < 1.2f ? -1f : 0f);
        var move = new Vector2(strafe, forward);

        bool swing = Mathf.Abs(yawError) < 15f && distance < 2.6f && now >= nextSwing;
        if (swing)
            nextSwing = now + swingInterval;

        ProbeOwnMovement(me, move, now);

        // With --nah-block, hold Block the whole fight (a blocking fighter cannot swing).
        me.SubmitScriptedInput(move, look, false, false, swing && !block, block);
    }

    // There is no client-side prediction yet, so a joining client sees its own body move only after the host has
    // moved it and sent the position back. This logs that delay each time the fighter starts walking from a
    // standstill. On the host the same number shows how long the body takes to cover 2 cm with no network at all.
    void TrackStillness(NAHNetworkPlayer me, float now)
    {
        Vector3 position = me.transform.position;
        if (FlatDistance(position, lastPosition) > 0.001f)
        {
            // The first change on a client is the spawn position arriving from the host, not movement.
            if (stillSince > 0f)
                positionChanged = true;
            stillSince = now;
        }
        lastPosition = position;
    }

    void ProbeOwnMovement(NAHNetworkPlayer me, Vector2 move, float now)
    {
        Vector3 position = me.transform.position;
        if (probeStart < 0f && positionChanged && move != Vector2.zero && lastMove == Vector2.zero && now - stillSince >= 0.25f)
        {
            probeStart = now;
            probeFrom = position;
        }
        else if (probeStart >= 0f && FlatDistance(position, probeFrom) > 0.02f)
        {
            Debug.Log("NAHDEMO own movement showed " + ((now - probeStart) * 1000f).ToString("0") + " ms after the walk input.");
            probeStart = -1f;
        }
        else if (probeStart >= 0f && now - probeStart > 2f)
        {
            Debug.Log("NAHDEMO own movement probe gave up after 2 s (the fighter was blocked or stopped).");
            probeStart = -1f;
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
        // Keep a steady 10 frames per second so the video plays back at real speed.
        nextCapture = now - nextCapture > CaptureInterval ? now + CaptureInterval : nextCapture + CaptureInterval;
        ScreenCapture.CaptureScreenshot(Path.Combine(captureFolder, "frame_" + frameIndex.ToString("D4") + ".png"));
        frameIndex++;
    }

    static void Log(NAHNetworkPlayer me, NAHNetworkPlayer opponent, float inputsPerSecond)
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
            " | fps=" + (1f / Mathf.Max(0.0001f, Time.smoothDeltaTime)).ToString("0"));
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
