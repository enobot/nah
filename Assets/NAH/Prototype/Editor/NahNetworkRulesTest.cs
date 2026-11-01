using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class NahNetworkRulesTest
{
    const string ScenePath = "Assets/NAH/Prototype/NetworkArena.unity";

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator HostHitCostsHealthAndLastLifeEndsTheMatch()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            var players = Object.FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].IsLocalPlayer)
                    local = players[i];
            }
            yield return null;
        }

        Assert.IsNotNull(local, "Host fighter did not spawn.");
        Assert.AreEqual(NAHNetworkPlayer.StartingLives, local.Lives);

        var opponent = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(opponent, "Rules opponent did not spawn.");
        yield return null;
        AssertOnlyLocalViewIsOn(local);

        local.ServerPlaceForTest(new Vector3(0f, 1f, 0f), 0f);
        opponent.ServerPlaceForTest(new Vector3(0f, 1f, 1.8f), 180f);
        int hits = local.ServerTryAttack();
        Assert.Greater(hits, 0, "Server attack did not hit the opponent.");
        Assert.Less(opponent.Health, opponent.MaxHealth);
        // The hit follows the stat rules: the attacker's damage less the share the target absorbs.
        float expected = opponent.MaxHealth - local.ServerStats.AttackDamage * (1f - opponent.ServerStats.Absorption);
        Assert.AreEqual(expected, opponent.Health, 0.01f, "The hit did not follow the stat rules.");

        int guard = 0;
        while (!opponent.Eliminated && guard < 16)
        {
            guard++;
            if (opponent.ServerApplyDamage(local.ServerStats.AttackDamage))
                continue;

            // An Edit Mode test resumes after one editor tick when it yields WaitForSeconds,
            // so wait on real time until the respawn lock ends and health refills.
            float respawnDeadline = Time.realtimeSinceStartup + NAHNetworkPlayer.RespawnLockSeconds + 2f;
            while (opponent.Health < opponent.MaxHealth && Time.realtimeSinceStartup < respawnDeadline)
                yield return null;
        }

        Assert.IsTrue(opponent.Eliminated, "Opponent was not eliminated.");
        Assert.AreEqual(NAHNetworkMatch.Phase.Finished, NAHNetworkMatch.Instance.CurrentPhase);
        Assert.AreEqual(local.OwnerClientId, NAHNetworkMatch.Instance.WinnerClientId);
        Assert.AreEqual(NAHNetworkMatch.Phase.Finished, local.MatchPhase);

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator ServerIgnoresNonFiniteInput()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            var manager = NetworkManager.Singleton;
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        // A modified client can put any float into the input message. Send it through the real owner RPC.
        var submit = typeof(NAHNetworkPlayer).GetMethod("SubmitInputRpc", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(submit, "SubmitInputRpc was not found.");
        submit.Invoke(local, new object[] { float.NaN, float.PositiveInfinity, float.NaN, float.NegativeInfinity, false });

        for (int i = 0; i < 10; i++)
            yield return null;

        Vector3 position = local.transform.position;
        Vector3 angles = local.transform.eulerAngles;
        Assert.IsTrue(IsFinite(position), "Server position became non-finite: " + position);
        Assert.IsTrue(IsFinite(angles), "Server rotation became non-finite: " + angles);

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator BodyShapeIsChosenOnceBeforeCombat()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            var manager = NetworkManager.Singleton;
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        float averageMax = local.MaxHealth;
        Assert.Greater(averageMax, 0f, "An average body has no health.");
        Assert.AreEqual(averageMax, local.Health, 0.001f, "A new fighter does not start at full health.");

        // Invalid choices are refused and do not use up the one choice.
        Assert.IsFalse(local.ServerSetBody(new NAHBodyShape(float.NaN, 0.5f, 0.5f)), "A NaN body was accepted.");
        Assert.IsFalse(local.ServerSetBody(new NAHBodyShape(1.5f, 0.5f, 0.5f)), "An out-of-range body was accepted.");

        Assert.IsTrue(local.ServerSetBody(new NAHBodyShape(1f, 1f, 1f)), "The first valid body was refused.");
        Assert.Greater(local.MaxHealth, averageMax, "A bigger body did not get more health.");
        Assert.AreEqual(local.MaxHealth, local.Health, 0.001f, "Health did not fill to the new maximum.");

        // The bigger body is a bigger target: the host's real collider matches the shared body size.
        var collider = local.GetComponent<CharacterController>();
        Assert.Greater(local.BodySize.y, 1f, "A bigger body did not get a taller hitbox.");
        Assert.Greater(local.BodySize.x, 1f, "A bigger body did not get a wider hitbox.");
        Assert.AreEqual(local.BodySize.y * 2f, collider.height, 0.001f, "The host's hitbox height does not match the body size.");
        Assert.AreEqual(local.BodySize.x * 0.5f, collider.radius, 0.001f, "The host's hitbox width does not match the body size.");
        Assert.GreaterOrEqual(local.transform.position.y - collider.height * 0.5f, -0.01f, "The taller body was placed inside the floor.");
        Assert.IsFalse(local.ServerSetBody(new NAHBodyShape(0f, 0f, 0f)), "A second body choice was accepted.");

        // The owner's request travels through the real RPC.
        var small = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(small, "Rules opponent did not spawn.");
        yield return null;
        small.RequestBodyShape(new NAHBodyShape(0f, 0f, 0f));
        for (int i = 0; i < 5 && small.MaxHealth >= averageMax; i++)
            yield return null;
        Assert.Less(small.MaxHealth, averageMax, "The body request RPC did not reach the host.");
        Assert.Less(small.BodySize.x, 1f, "A smaller body did not get a smaller hitbox.");

        // A fighter who has already been hit cannot pick a tougher body.
        var hurt = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(hurt, "Second rules opponent did not spawn.");
        yield return null;
        Assert.IsTrue(hurt.ServerApplyDamage(1f), "The second opponent could not be damaged.");
        Assert.IsFalse(hurt.ServerSetBody(new NAHBodyShape(1f, 1f, 1f)), "The body changed after the fighter was hit.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Owner decision 2026-10-31: fighting-game style. A swing covers anyone in front of the attacker,
    // from feet to head, wherever the camera points up or down.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator SwingCoversTheTargetFromFeetToHead()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        NAHNetworkPlayer giant = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && giant == null)
        {
            var manager = NetworkManager.Singleton;
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                giant = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(giant, "Host fighter did not spawn.");

        var tiny = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(tiny, "Rules opponent did not spawn.");
        yield return null;
        Assert.IsTrue(giant.ServerSetBody(new NAHBodyShape(1f, 1f, 1f)), "The giant body was refused.");
        Assert.IsTrue(tiny.ServerSetBody(new NAHBodyShape(0f, 0f, 0f)), "The tiny body was refused.");

        // Both stand on the floor, facing each other, looking straight ahead.
        float giantHalf = giant.BodySize.y;
        float tinyHalf = tiny.BodySize.y;
        giant.ServerPlaceForTest(new Vector3(0f, giantHalf + 0.05f, 0f), 0f);
        tiny.ServerPlaceForTest(new Vector3(0f, tinyHalf + 0.05f, 1.6f), 180f);
        Assert.Greater(giant.ServerTryAttack(), 0, "A straight-ahead swing from a giant missed a tiny fighter standing in front of it.");
        Assert.Greater(tiny.ServerTryAttack(), 0, "A tiny fighter missed a giant standing in front of it.");

        // Still only in front, and still only within reach.
        yield return WaitRealSeconds(giant.ServerStats.SwingInterval + 0.1f);
        tiny.ServerPlaceForTest(new Vector3(0f, tinyHalf + 0.05f, -1.6f), 0f);
        Assert.AreEqual(0, giant.ServerTryAttack(), "A swing hit a fighter standing behind the attacker.");

        yield return WaitRealSeconds(giant.ServerStats.SwingInterval + 0.1f);
        tiny.ServerPlaceForTest(new Vector3(0f, tinyHalf + 0.05f, 6f), 180f);
        Assert.AreEqual(0, giant.ServerTryAttack(), "A swing hit a fighter far beyond its reach.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator LockOnKeepsTheViewOnTheTarget()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            var manager = NetworkManager.Singleton;
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        var opponent = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(opponent, "Rules opponent did not spawn.");
        yield return null;

        // The opponent stands 45 degrees to the right, inside the lock-on cone.
        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        opponent.ServerPlaceForTest(new Vector3(3f, 1.1f, 3f), 180f);
        Assert.IsTrue(local.ToggleLockOn(), "Lock-on found no target in front.");
        Assert.AreSame(opponent, local.LockOnTarget, "Lock-on picked the wrong fighter.");

        yield return WaitRealSeconds(0.6f);
        Vector3 toOpponent = opponent.transform.position - local.transform.position;
        toOpponent.y = 0f;
        Assert.Less(Vector3.Angle(local.transform.forward, toOpponent), 5f, "Lock-on did not turn the fighter to face the target.");

        Assert.IsFalse(local.ToggleLockOn(), "Pressing lock-on again did not release it.");
        Assert.IsNull(local.LockOnTarget, "The target stayed locked after release.");

        // A lock breaks when the target goes out of range.
        Assert.IsTrue(local.ToggleLockOn(), "Could not lock on again.");
        opponent.ServerPlaceForTest(local.transform.position + local.transform.forward * 40f, 180f);
        yield return null;
        yield return null;
        Assert.IsNull(local.LockOnTarget, "Lock-on did not break when the target left range.");

        // Nobody behind can be locked.
        opponent.ServerPlaceForTest(local.transform.position - local.transform.forward * 3f, 0f);
        yield return null;
        Assert.IsFalse(local.ToggleLockOn(), "Lock-on grabbed a fighter standing behind.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Edit Mode tests resume after one editor tick when they yield WaitForSeconds, so poll real time instead.
    static IEnumerator WaitRealSeconds(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < until)
            yield return null;
    }

    static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    // Only the local fighter may render the view and hear the arena.
    // The opponent here is owned by the host's client id too, so this also checks IsLocalPlayer, not just ownership.
    static void AssertOnlyLocalViewIsOn(NAHNetworkPlayer local)
    {
        int cameras = 0;
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (!camera.isActiveAndEnabled)
                continue;
            cameras++;
            Assert.AreSame(local, camera.GetComponentInParent<NAHNetworkPlayer>(), "A camera other than the local fighter's is on: " + camera.name);
        }
        Assert.AreEqual(1, cameras, "Exactly one camera should be on.");

        int listeners = 0;
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (!listener.isActiveAndEnabled)
                continue;
            listeners++;
            Assert.AreSame(local, listener.GetComponentInParent<NAHNetworkPlayer>(), "An audio listener other than the local fighter's is on: " + listener.name);
        }
        Assert.AreEqual(1, listeners, "Exactly one audio listener should be on.");
    }
}
