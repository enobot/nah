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
        Assert.Less(opponent.Health, NAHNetworkPlayer.MaxHealthValue);

        int guard = 0;
        while (!opponent.Eliminated && guard < 16)
        {
            guard++;
            if (opponent.ServerApplyDamage(NAHNetworkPlayer.HitDamage))
                continue;

            // An Edit Mode test resumes after one editor tick when it yields WaitForSeconds,
            // so wait on real time until the respawn lock ends and health refills.
            float respawnDeadline = Time.realtimeSinceStartup + NAHNetworkPlayer.RespawnLockSeconds + 2f;
            while (opponent.Health < NAHNetworkPlayer.MaxHealthValue && Time.realtimeSinceStartup < respawnDeadline)
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
