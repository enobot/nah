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
        Assert.IsFalse(local.ServerSetBody(new NAHBodyShape(0f, 0f, 0f)), "A second body choice was accepted.");

        // The owner's request travels through the real RPC.
        var small = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(small, "Rules opponent did not spawn.");
        yield return null;
        small.RequestBodyShape(new NAHBodyShape(0f, 0f, 0f));
        for (int i = 0; i < 5 && small.MaxHealth >= averageMax; i++)
            yield return null;
        Assert.Less(small.MaxHealth, averageMax, "The body request RPC did not reach the host.");

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
