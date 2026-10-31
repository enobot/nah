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
    bool hadFighter;
    bool sawFinish;

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
            return;
        }
        hadFighter = true;

        NAHNetworkPlayer opponent = NearestOpponent(me);
        bool fighting = me.MatchPhase == NAHNetworkMatch.Phase.Fighting;
        bool finished = me.MatchPhase == NAHNetworkMatch.Phase.Finished;

        if (fighting || finished)
            Capture(now);
        if (now >= nextLog)
        {
            nextLog = now + 0.5f;
            Log(me, opponent);
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
            return;
        }

        if (!fighting || opponent == null)
        {
            me.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
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

        // Walk into swinging range, then hold there.
        float forward = distance > 1.9f ? 1f : (distance < 1.2f ? -1f : 0f);
        var move = new Vector2(strafe, forward);

        bool swing = Mathf.Abs(yawError) < 15f && distance < 2.6f && now >= nextSwing;
        if (swing)
            nextSwing = now + swingInterval;

        me.SubmitScriptedInput(move, look, false, false, swing);
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

    static void Log(NAHNetworkPlayer me, NAHNetworkPlayer opponent)
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
