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
        StartTheMatch();

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
        // Its number is far ahead of anything the host's own input has sent, so only the finite check can refuse it.
        submit.Invoke(local, new object[] { float.NaN, float.PositiveInfinity, float.NaN, float.NegativeInfinity, false, false, 1000000u });

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
        StartTheMatch();
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
        StartTheMatch();

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

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator LockOnSwitchesTargetsLeftAndRight()
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

        var right = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        var left = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(right, "First rules opponent did not spawn.");
        Assert.IsNotNull(left, "Second rules opponent did not spawn.");
        yield return null;

        // Facing forward. One opponent slightly right of center, one further left.
        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        right.ServerPlaceForTest(new Vector3(2f, 1.1f, 4f), 180f);
        left.ServerPlaceForTest(new Vector3(-3f, 1.1f, 3f), 180f);

        Assert.IsTrue(local.ToggleLockOn(), "Lock-on found no target.");
        Assert.AreSame(right, local.LockOnTarget, "Lock-on did not pick the opponent nearest the center.");
        Assert.IsTrue(local.SwitchLockOnTarget(-1), "Switching left found nobody.");
        Assert.AreSame(left, local.LockOnTarget, "Switching left did not move to the left opponent.");
        Assert.IsFalse(local.SwitchLockOnTarget(-1), "Switching left again should find nobody further left.");
        Assert.AreSame(left, local.LockOnTarget, "A failed switch dropped the current target.");
        Assert.IsTrue(local.SwitchLockOnTarget(1), "Switching right found nobody.");
        Assert.AreSame(right, local.LockOnTarget, "Switching right did not move back to the right opponent.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Uses roster entries by position and reads their ratios, so it keeps working after the owner renames or retunes them.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator RaceAndClassChoicesApplyTheirRatios()
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

        var roster = local.Roster;
        Assert.IsNotNull(roster, "The fighter has no roster.");
        Assert.Greater(roster.RaceCount, 1, "The roster needs at least two races for this test.");
        Assert.Greater(roster.ClassCount, 1, "The roster needs at least two classes for this test.");

        // Lookup by any capitalization of the name, by number, and a miss.
        Assert.AreEqual(0, roster.FindRace(roster.Race(0).DisplayName.ToUpperInvariant()));
        Assert.AreEqual(1, roster.FindClass("1"));
        Assert.AreEqual(-1, roster.FindRace("no such race"));

        float plainHealth = local.MaxHealth;
        Assert.IsFalse(local.ServerSetBuild(NAHBodyShape.Average, roster.RaceCount, -1), "An out-of-range race was accepted.");
        Assert.IsFalse(local.ServerSetBuild(NAHBodyShape.Average, -1, -2), "An out-of-range class was accepted.");

        Assert.IsTrue(local.ServerSetBuild(NAHBodyShape.Average, 0, 1), "A valid race and class were refused.");
        float expected = plainHealth * roster.Race(0).Modifiers.health * roster.Class(1).Modifiers.health;
        Assert.AreEqual(expected, local.MaxHealth, 0.01f, "Race and class ratios were not applied to health.");
        Assert.AreSame(roster.Race(0), local.Race, "The chosen race is not shared.");
        Assert.AreSame(roster.Class(1), local.FighterClass, "The chosen class is not shared.");

        // The owner's request travels through the real RPC.
        var other = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(other, "Rules opponent did not spawn.");
        yield return null;
        other.RequestBuild(NAHBodyShape.Average, 1, 0);
        for (int i = 0; i < 5 && other.Race == null; i++)
            yield return null;
        Assert.AreSame(roster.Race(1), other.Race, "The build request RPC did not set the race.");
        Assert.AreSame(roster.Class(0), other.FighterClass, "The build request RPC did not set the class.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Owner decision 2026-10-31: shielding blocks a certain amount of damage, then goes into cooldown.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator ShieldBlocksUpToItsAmountThenCoolsDown()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        NAHNetworkPlayer attacker = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && attacker == null)
        {
            var manager = NetworkManager.Singleton;
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                attacker = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(attacker, "Host fighter did not spawn.");

        var defender = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(defender, "Rules opponent did not spawn.");
        yield return null;
        StartTheMatch();

        // Face each other. The defender holds Block through its real owner input message, facing the attacker.
        attacker.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        defender.ServerPlaceForTest(new Vector3(0f, 1.1f, 1.8f), 180f);
        var submit = typeof(NAHNetworkPlayer).GetMethod("SubmitInputRpc", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(submit, "SubmitInputRpc was not found.");
        submit.Invoke(defender, new object[] { 0f, 0f, 180f, 0f, false, true, 1u });
        yield return null;

        float maxShield = defender.MaxShield;
        float damage = attacker.ServerStats.AttackDamage;
        Assert.Greater(maxShield, damage, "This test needs a shield that survives one hit.");
        Assert.IsTrue(defender.Blocking, "The defender is not blocking.");
        Assert.AreEqual(0, defender.ServerTryAttack(), "A blocking fighter was able to swing.");

        // First hit: all of it goes into the shield.
        float health = defender.Health;
        Assert.Greater(attacker.ServerTryAttack(), 0, "The first swing missed.");
        Assert.AreEqual(health, defender.Health, 0.001f, "Health dropped although the shield could hold the whole hit.");
        Assert.AreEqual(maxShield - damage, defender.Shield, 0.01f, "The shield did not take the hit.");

        // Second hit: the shield breaks and the rest goes through armor to health.
        yield return WaitRealSeconds(attacker.ServerStats.SwingInterval + 0.1f);
        float leftInShield = defender.Shield;
        Assert.Greater(attacker.ServerTryAttack(), 0, "The second swing missed.");
        Assert.IsTrue(defender.ShieldBroken, "The shield did not break.");
        float throughArmor = (damage - leftInShield) * (1f - defender.ServerStats.Absorption);
        Assert.AreEqual(health - throughArmor, defender.Health, 0.01f, "The rest of the hit did not reach health through armor.");
        Assert.IsFalse(defender.Blocking, "A broken shield is still blocking.");

        // While broken, the whole hit lands.
        yield return WaitRealSeconds(attacker.ServerStats.SwingInterval + 0.1f);
        float before = defender.Health;
        Assert.Greater(attacker.ServerTryAttack(), 0, "The third swing missed.");
        Assert.AreEqual(before - damage * (1f - defender.ServerStats.Absorption), defender.Health, 0.01f, "A broken shield still blocked.");

        // After the cooldown the shield is full again.
        yield return WaitRealSeconds(3.5f);
        Assert.IsFalse(defender.ShieldBroken, "The shield did not come back after its cooldown.");
        Assert.AreEqual(maxShield, defender.Shield, 0.01f, "The shield did not come back full.");

        // A hit from behind is not blocked.
        defender.ServerPlaceForTest(new Vector3(0f, 1.1f, 1.8f), 0f);
        submit.Invoke(defender, new object[] { 0f, 0f, 0f, 0f, false, true, 2u });
        yield return null;
        float shieldBefore = defender.Shield;
        float healthBefore = defender.Health;
        Assert.Greater(attacker.ServerTryAttack(), 0, "The swing at the defender's back missed.");
        Assert.AreEqual(shieldBefore, defender.Shield, 0.01f, "The shield blocked a hit from behind.");
        Assert.Less(defender.Health, healthBefore, "A hit from behind did not reach health.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // A fifth player is refused during connection approval, before the host makes a fighter for them.
    [Test]
    public void ArenaRefusesAFifthPlayer()
    {
        var waiting = NAHNetworkMatch.Phase.Waiting;
        for (int connected = 0; connected < NAHNetworkMatch.MaxPlayers; connected++)
            Assert.IsNull(NAHNetworkSession.JoinRefusal(connected, waiting), "A player was refused with only " + connected + " connected.");
        Assert.IsNotNull(NAHNetworkSession.JoinRefusal(NAHNetworkMatch.MaxPlayers, waiting), "A fifth player was let in.");
        Assert.IsNotNull(NAHNetworkSession.JoinRefusal(NAHNetworkMatch.MaxPlayers + 3, waiting), "A player was let into an overfull arena.");
    }

    // Owner decision 2026-11-01: nobody joins a match that has already started.
    [Test]
    public void NobodyJoinsAMatchThatHasStarted()
    {
        Assert.AreEqual(NAHNetworkSession.StartedReason, NAHNetworkSession.JoinRefusal(1, NAHNetworkMatch.Phase.Fighting), "A player joined a fight in progress.");
        Assert.AreEqual(NAHNetworkSession.StartedReason, NAHNetworkSession.JoinRefusal(1, NAHNetworkMatch.Phase.Finished), "A player joined a finished match.");
    }

    // Owner decisions 2026-11-01: the host presses Start, only after everyone, the host included, has readied up.
    // Nobody can be hurt before the start, everyone starts at their spawn point, and joins close at the start.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator HostStartsOnlyWhenEveryoneIsReady()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");
        var match = NAHNetworkMatch.Instance;

        // Alone, the host cannot start, even when ready.
        Assert.IsTrue(local.ServerSetReady(true), "The host could not ready up.");
        Assert.IsFalse(match.StartMatch(), "A match started with one player.");
        Assert.IsTrue(local.ServerSetReady(false));

        // A second player does not start the fight by itself, and nobody can be hurt while waiting.
        var opponent = match.SpawnRulesOpponentForTest();
        Assert.IsNotNull(opponent, "Rules opponent did not spawn.");
        yield return null;
        Assert.AreEqual(NAHNetworkMatch.Phase.Waiting, match.CurrentPhase, "The match started by itself.");
        // ServerPlaceForTest also makes the new place that fighter's spawn point.
        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        opponent.ServerPlaceForTest(new Vector3(0f, 1.1f, 1.8f), 180f);
        Vector3 localSpawn = local.transform.position;
        Assert.AreEqual(0, local.ServerTryAttack(), "A swing hit someone before the match started.");
        Assert.IsFalse(opponent.ServerApplyDamage(10f), "A fighter was hurt before the match started.");
        Assert.AreEqual(opponent.MaxHealth, opponent.Health, 0.001f, "Health dropped before the match started.");

        // Players may walk around while waiting.
        float walkUntil = Time.realtimeSinceStartup + 0.6f;
        int walkFrames = 0;
        while (Time.realtimeSinceStartup < walkUntil)
        {
            local.SubmitScriptedInput(new Vector2(0f, -1f), Vector2.zero, false, false, false);
            walkFrames++;
            yield return null;
        }
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        float walked = FlatDistance(localSpawn, local.transform.position);
        Assert.Greater(walked, 0.3f, "The host walked only " + walked.ToString("0.00") + " m in 0.6 s (" + walkFrames + " frames) while waiting.");

        // Start needs everyone ready. The ready choice travels through the real owner RPC.
        Assert.IsFalse(match.StartMatch(), "A match started with nobody ready.");
        local.RequestReady(true);
        for (int i = 0; i < 5 && !local.Ready; i++)
            yield return null;
        Assert.IsTrue(local.Ready, "The ready request did not reach the host.");
        Assert.IsFalse(match.StartMatch(), "A match started before everyone was ready.");
        Assert.IsNotNull(match.StartBlocker(), "The host is not told who is not ready.");
        opponent.RequestReady(true);
        for (int i = 0; i < 5 && !opponent.Ready; i++)
            yield return null;
        Assert.IsNull(match.StartBlocker(), "Everyone is ready but the start is still blocked: " + match.StartBlocker());
        Assert.IsTrue(match.StartMatch(), "The host could not start with everyone ready.");
        Assert.AreEqual(NAHNetworkMatch.Phase.Fighting, match.CurrentPhase);
        Assert.AreEqual(NAHNetworkMatch.Phase.Fighting, local.MatchPhase, "The start was not shared with the fighters.");

        // Everyone starts at their own spawn point, wherever they walked while waiting.
        Assert.Less(FlatDistance(localSpawn, local.transform.position), 0.05f, "The host did not start at its spawn point.");

        // Once started: no second start, readiness is frozen, and new players are refused.
        Assert.IsFalse(match.StartMatch(), "The match started twice.");
        Assert.IsFalse(local.ServerSetReady(false), "Readiness changed after the start.");
        var response = new NetworkManager.ConnectionApprovalResponse();
        manager.ConnectionApprovalCallback(new NetworkManager.ConnectionApprovalRequest { ClientNetworkId = 7 }, response);
        Assert.IsFalse(response.Approved, "A player was let into a match that had started.");
        Assert.IsFalse(response.CreatePlayerObject, "A refused player would get a fighter.");
        Assert.AreEqual(NAHNetworkSession.StartedReason, response.Reason);

        // And now hits count.
        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        opponent.ServerPlaceForTest(new Vector3(0f, 1.1f, 1.8f), 180f);
        Assert.Greater(local.ServerTryAttack(), 0, "A swing missed after the start.");
        Assert.Less(opponent.Health, opponent.MaxHealth, "A hit after the start did no damage.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator HostApprovesPlayersAndUsesTheChosenDisconnectTimeout()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        // The host's own connection goes through approval too, and must still get a fighter.
        Assert.IsNotNull(local, "Host fighter did not spawn with connection approval on.");
        Assert.IsTrue(manager.NetworkConfig.ConnectionApproval, "Connection approval is off.");
        Assert.IsNotNull(manager.ConnectionApprovalCallback, "No connection approval callback is set.");

        // A second player is let in and gets a fighter.
        var response = new NetworkManager.ConnectionApprovalResponse();
        manager.ConnectionApprovalCallback(new NetworkManager.ConnectionApprovalRequest { ClientNetworkId = 7 }, response);
        Assert.IsTrue(response.Approved, "A second player was refused.");
        Assert.IsTrue(response.CreatePlayerObject, "An approved player would get no fighter.");
        Assert.IsFalse(response.Pending, "Approval was left pending.");

        var transport = manager.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        Assert.IsNotNull(transport, "The arena does not use Unity Transport.");
        // Owner decision 2026-11-01: a silent connection is dropped after 30 s.
        Assert.AreEqual(30000, NAHNetworkSession.DisconnectTimeoutMilliseconds, "The disconnect timeout differs from the owner's decision.");
        Assert.AreEqual(NAHNetworkSession.DisconnectTimeoutMilliseconds, transport.DisconnectTimeoutMS, "The chosen disconnect timeout was not applied.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Movement and aim go out once per network tick, not once per rendered frame.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator InputGoesOutOncePerNetworkTick()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        int startTick = manager.LocalTime.Tick;
        int startSent = local.InputMessagesSent;
        int frames = 0;
        float until = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < until)
        {
            // The view turns a little every frame, as it does under a moving mouse.
            local.SubmitScriptedInput(new Vector2(0f, 1f), new Vector2(0.5f, 0f), false, false, false);
            frames++;
            yield return null;
        }
        int ticks = manager.LocalTime.Tick - startTick;
        int sent = local.InputMessagesSent - startSent;
        Assume.That(frames, Is.GreaterThan(ticks * 2), "The editor ran too few frames per tick to tell the two rates apart.");
        Assert.LessOrEqual(sent, ticks + 1, "Input went out " + sent + " times in " + ticks + " ticks (" + frames + " frames).");
        Assert.GreaterOrEqual(sent, ticks - 1, "Input stopped going out every tick: " + sent + " in " + ticks + " ticks.");

        // Within one frame the tick cannot change. An unchanged input does not go out twice in one tick,
        // but pressing Block, starting to walk, and pressing attack each go out at once.
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        int before = local.InputMessagesSent;
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        Assert.AreEqual(before, local.InputMessagesSent, "An unchanged input went out twice in one tick.");
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false, true);
        Assert.AreEqual(before + 1, local.InputMessagesSent, "Pressing Block waited for the next tick.");
        local.SubmitScriptedInput(new Vector2(0f, 1f), Vector2.zero, false, false, false, true);
        Assert.AreEqual(before + 2, local.InputMessagesSent, "Starting to walk waited for the next tick.");
        local.SubmitScriptedInput(new Vector2(0f, 1f), Vector2.zero, false, false, true, true);
        Assert.AreEqual(before + 3, local.InputMessagesSent, "An attack press waited for the next tick.");
        yield return null;
        Assert.IsTrue(local.Blocking, "The host did not receive the Block press.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // A joining player's prediction record lines the host's state up with the prediction for the same moment.
    [Test]
    public void PredictionRecordFindsTheHostsMoment()
    {
        var record = new NAHMotionHistory();
        var velocity = new Vector3(2f, 0f, 0f);
        // Input 1 from 1.00 s and input 2 from 1.10 s, while the prediction walks along x at 2 m/s.
        record.NoteSequenceStart(1, 1.0);
        record.NoteSequenceStart(2, 1.1);
        for (int i = 0; i <= 30; i++)
        {
            double time = 1.0 + i * 0.01;
            record.Record(time, velocity * (float)(time - 1.0), velocity, 0f);
        }

        // The host had moved with input 2 for 55 ms. That is 1.155 s here, where the prediction says x = 0.31.
        Assert.IsTrue(record.TryFindError(2, 0.055f, new Vector3(0.33f, 0f, 0f), velocity, 0f,
            out Vector3 error, out Vector3 horizontalError, out float verticalError), "The host's moment was not found.");
        Assert.AreEqual(0.02f, error.x, 0.0005f, "Wrong disagreement along x.");
        Assert.AreEqual(0f, error.y, 0.0005f);
        Assert.AreEqual(0f, error.z, 0.0005f);
        Assert.AreEqual(0f, horizontalError.magnitude, 0.0005f);
        Assert.AreEqual(0f, verticalError, 0.0005f);

        // Once corrected, the same report shows no disagreement, so it is not applied twice.
        record.Shift(error, horizontalError, verticalError);
        Assert.IsTrue(record.TryFindError(2, 0.055f, new Vector3(0.33f, 0f, 0f), velocity, 0f, out error, out _, out _));
        Assert.AreEqual(0f, error.magnitude, 0.0005f, "A corrected disagreement was counted again.");

        // A moment past the newest step uses the newest step: 0.60 plus the 0.02 correction.
        Assert.IsTrue(record.TryFindError(2, 5f, new Vector3(0.62f, 0f, 0f), velocity, 0f, out error, out _, out _));
        Assert.AreEqual(0f, error.magnitude, 0.0005f, "A late moment did not use the newest step.");

        // Unknown inputs, inputs older than the last report, and broken times cannot be lined up.
        Assert.IsFalse(record.TryFindError(7, 0.01f, Vector3.zero, Vector3.zero, 0f, out _, out _, out _), "An unknown input was lined up.");
        Assert.IsFalse(record.TryFindError(1, 0.01f, Vector3.zero, Vector3.zero, 0f, out _, out _, out _), "An input older than the last report was kept.");
        Assert.IsFalse(record.TryFindError(2, float.NaN, Vector3.zero, Vector3.zero, 0f, out _, out _, out _), "A time that is not a number was used.");
        Assert.IsFalse(record.TryFindError(2, -0.01f, Vector3.zero, Vector3.zero, 0f, out _, out _, out _), "A negative time was used.");

        // The record stays bounded however long the host is silent.
        for (int i = 0; i < NAHMotionHistory.MaxSamples + 50; i++)
            record.Record(2.0 + i * 0.001, Vector3.zero, Vector3.zero, 0f);
        Assert.AreEqual(NAHMotionHistory.MaxSamples, record.SampleCount, "The record grew without a limit.");
    }

    // Netcode copies the host's smoothing setting to every other screen. If the host turned it off for its own fighter,
    // every joining player would see the host move in steps, 30 a second.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator EveryScreenSmoothsOtherFighters()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");
        var opponent = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(opponent, "Rules opponent did not spawn.");
        yield return null;

        foreach (var fighter in new[] { local, opponent })
        {
            var networkTransform = fighter.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            Assert.IsNotNull(networkTransform, "A fighter has no network transform.");
            Assert.IsTrue(networkTransform.Interpolate, (fighter == local ? "The host's own fighter" : "The other fighter") +
                " is not smoothed, so the other screens would see it move in steps.");
        }

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Input travels in unreliable messages, which can arrive out of order. An older one must not undo a newer one.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator HostIgnoresInputOlderThanTheNewest()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        // Once scripted, the host's own input is only what this test sends. Let the body settle on the floor.
        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        yield return WaitRealSeconds(0.3f);
        Vector3 start = local.transform.position;

        // Walk forward (number 1000) arrives before the older walk back (number 999).
        var submit = typeof(NAHNetworkPlayer).GetMethod("SubmitInputRpc", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(submit, "SubmitInputRpc was not found.");
        submit.Invoke(local, new object[] { 0f, 1f, 0f, 0f, false, false, 1000u });
        submit.Invoke(local, new object[] { 0f, -1f, 0f, 0f, false, false, 999u });
        yield return WaitRealSeconds(0.5f);

        float forward = local.transform.position.z - start.z;
        Assert.Greater(forward, 0.1f, "An older input undid a newer one: the fighter moved " + forward.ToString("0.00") + " m forward.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // A joining player predicts its own fighter with the host's movement rules and the input it sent. Here the host
    // runs that prediction beside its own simulation of the same fighter, with the same frames, so both must agree.
    // Then it checks how corrections from the host show: small ones are spread out, big ones are shown at once.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator OwnPredictionMatchesTheHostAndCorrectsSmoothly()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        // A bigger body walks slower than average, so the prediction must use this fighter's own speed.
        Assert.IsTrue(local.ServerSetBody(new NAHBodyShape(1f, 1f, 0.5f)), "The body was refused.");
        Assert.Less(local.ServerStats.MoveSpeedScale, 0.99f, "This test needs a body slower than average.");
        local.ServerPlaceForTest(new Vector3(0f, 2f, 0f), 0f);
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        yield return WaitRealSeconds(0.5f);

        local.StartPredictionForTest();
        Assert.IsTrue(local.Predicting, "The prediction did not start.");
        Assert.Less(Vector3.Distance(local.PredictedPosition, local.transform.position), 0.001f, "The prediction did not start at the body.");
        yield return WaitRealSeconds(0.1f);

        // Walk forward and a little sideways while the view turns, for 0.6 s. The view turns 90 degrees a second, not a
        // fixed amount per frame: at about 5,600 frames a second a per-frame turn spun the heavy body too fast to walk.
        Vector3 start = local.transform.position;
        float until = Time.realtimeSinceStartup + 0.6f;
        int frames = 0;
        while (Time.realtimeSinceStartup < until)
        {
            local.SubmitScriptedInput(new Vector2(0.3f, 1f), new Vector2(90f * Time.deltaTime, 0f), false, false, false);
            frames++;
            yield return null;
        }
        float walked = FlatDistance(start, local.transform.position);
        Assert.Greater(walked, 0.2f, "The host walked only " + walked.ToString("0.00") + " m in 0.6 s (" + frames + " frames).");
        float apart = Vector3.Distance(local.PredictedPosition, local.transform.position);
        Assert.Less(apart, 0.01f, "After walking " + walked.ToString("0.00") + " m the prediction was " + apart.ToString("0.000") +
            " m from where the host moved the body (" + frames + " frames).");

        // Stop. Then the host disagrees by 20 cm: the prediction moves at once, and the view catches up smoothly.
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        yield return WaitRealSeconds(0.6f);
        yield return null;
        Vector3 viewBefore = local.ViewPositionForTest;
        Vector3 predictedBefore = local.PredictedPosition;
        local.CorrectPredictionForTest(new Vector3(0.2f, 0f, 0f));
        Assert.AreEqual(0.2f, local.PredictedPosition.x - predictedBefore.x, 0.001f, "The prediction did not take the correction.");
        yield return null;
        float jumped = FlatDistance(viewBefore, local.ViewPositionForTest);
        Assert.Less(jumped, 0.1f, "The view jumped " + jumped.ToString("0.000") + " m in one frame for a 0.2 m correction.");
        yield return WaitRealSeconds(0.5f);
        float behind = FlatDistance(local.ViewPositionForTest, local.PredictedPosition);
        Assert.Less(behind, 0.01f, "The view was still " + behind.ToString("0.000") + " m behind the prediction 0.5 s after a correction.");

        // A disagreement too big to be a small mistake, such as a respawn, is shown at once.
        int snaps = local.PredictionSnaps;
        local.CorrectPredictionForTest(new Vector3(2f, 0f, 0f));
        yield return null;
        Assert.AreEqual(snaps + 1, local.PredictionSnaps, "A 2 m correction was not counted as a jump.");
        float lag = FlatDistance(local.ViewPositionForTest, local.PredictedPosition);
        Assert.Less(lag, 0.01f, "The view eased toward a 2 m jump instead of showing it (" + lag.ToString("0.000") + " m behind).");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // On the host every fighter's controller moves its body. On a joining player's screen the other fighters'
    // controllers are solid obstacles for that player's prediction, and the player's own networked body is not.
    [Test]
    public void ControllersAreOnWhereTheyShouldBe()
    {
        Assert.IsTrue(NAHNetworkPlayer.ControllerOn(true, true), "The host's own fighter has no controller.");
        Assert.IsTrue(NAHNetworkPlayer.ControllerOn(true, false), "The host cannot move another fighter.");
        Assert.IsTrue(NAHNetworkPlayer.ControllerOn(false, false), "Another fighter is not solid on a joining player's screen.");
        Assert.IsFalse(NAHNetworkPlayer.ControllerOn(false, true), "A joining player's own networked body would block its prediction.");
    }

    // A predicted walk into another fighter stops where the host's real body stops, instead of passing through.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator PredictionStopsAtAnotherFighterLikeTheHost()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");
        var opponent = NAHNetworkMatch.Instance.SpawnRulesOpponentForTest();
        Assert.IsNotNull(opponent, "Rules opponent did not spawn.");
        yield return null;

        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        opponent.ServerPlaceForTest(new Vector3(0f, 1.1f, 2.5f), 180f);
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        yield return WaitRealSeconds(0.4f);
        local.StartPredictionForTest();
        yield return WaitRealSeconds(0.1f);

        float until = Time.realtimeSinceStartup + 1.5f;
        while (Time.realtimeSinceStartup < until)
        {
            local.SubmitScriptedInput(new Vector2(0f, 1f), Vector2.zero, false, false, false);
            yield return null;
        }

        float gap = opponent.transform.position.z - local.transform.position.z;
        Assert.Less(gap, 1.3f, "The host body did not reach the other fighter (" + gap.ToString("0.00") + " m apart).");
        Assert.Greater(gap, 0.9f, "The host body went into the other fighter (" + gap.ToString("0.00") + " m apart).");
        float apart = Vector3.Distance(local.PredictedPosition, local.transform.position);
        Assert.Less(apart, 0.02f, "Pushing into another fighter, the prediction ended " + apart.ToString("0.00") +
            " m from the host body (prediction at z " + local.PredictedPosition.z.ToString("0.00") + ").");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Over a slow connection the host's reports name inputs sent a round trip ago. Right after the prediction starts,
    // those inputs are older than its record. Such a report is skipped, not taken as the truth: taking it would put
    // the view a round trip back and clear the record, so the next report would not match either, again and again.
    [UnityTest]
    [Timeout(120000)]
    public IEnumerator PredictionSkipsReportsOlderThanItsRecord()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsNotNull(local, "Host fighter did not spawn.");

        local.ServerPlaceForTest(new Vector3(0f, 1.1f, 0f), 0f);
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        yield return WaitRealSeconds(0.4f);
        Vector3 start = local.transform.position;
        uint oldInput = local.InputSequenceForTest;
        // One newer input (the next tick), so the old one is not in the prediction's record.
        yield return WaitRealSeconds(0.05f);
        local.SubmitScriptedInput(Vector2.zero, Vector2.zero, false, false, false);
        Assert.Greater(local.InputSequenceForTest, oldInput, "No newer input was sent.");
        local.StartPredictionForTest();
        yield return null;

        float until = Time.realtimeSinceStartup + 0.4f;
        while (Time.realtimeSinceStartup < until)
        {
            local.SubmitScriptedInput(new Vector2(0f, 1f), Vector2.zero, false, false, false);
            yield return null;
        }
        Assert.Greater(FlatDistance(start, local.PredictedPosition), 0.2f, "The prediction did not walk.");

        // A late report: the input from before the prediction started, at the old place.
        local.ReportHostStateForTest(start, oldInput, 0.05f);
        float apart = FlatDistance(local.PredictedPosition, local.transform.position);
        Assert.Less(apart, 0.01f, "A report older than the record moved the prediction " + apart.ToString("0.00") + " m away from the body.");

        // If nothing matches for about two seconds of reports, something is wrong, and the prediction starts over from the host.
        for (int i = 0; i < 61; i++)
            local.ReportHostStateForTest(start, oldInput, 0.05f);
        Assert.Less(FlatDistance(local.PredictedPosition, start), 0.001f, "The prediction never started over although no report matched.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Development builds can add delay, jitter, and packet loss for same-PC tests.
    [Test]
    public void LagSettingsAreReadAndChecked()
    {
        Assert.IsTrue(NAHNetworkLagSimulator.TryParse("75,15,3", out var lag), "Good lag settings were refused.");
        Assert.AreEqual(75, lag.DelayMilliseconds);
        Assert.AreEqual(15, lag.JitterMilliseconds);
        Assert.AreEqual(3, lag.DropPercent);
        Assert.IsTrue(NAHNetworkLagSimulator.TryParse(" 0, 0, 0 ", out _), "No lag at all was refused.");

        foreach (string bad in new[] { null, "", "75,15", "75,15,3,1", "a,b,c", "-1,0,0", "0,-1,0", "0,0,-1",
                     "5000,0,0", "75,100,3", "75,15,80", "7.5,1,1" })
            Assert.IsFalse(NAHNetworkLagSimulator.TryParse(bad, out _), "Bad lag settings were accepted: \"" + bad + "\"");
    }

    // Measures the lag simulator directly: two Unity Transport drivers on this PC, one built by the simulator,
    // and a message sent there and back. The simulator delays both directions on its side, so a round trip
    // should take about twice the delay longer than the same trip with no lag.
    [Test]
    [Timeout(60000)]
    public void LagSimulatorDelaysEachDirectionOnce()
    {
        Assert.IsTrue(NAHNetworkLagSimulator.TryParse("0,0,0", out var none));
        Assert.IsTrue(NAHNetworkLagSimulator.TryParse("75,0,0", out var lag));
        long baseline = MedianRoundTrip(none);
        long lagged = MedianRoundTrip(lag);
        long added = lagged - baseline;
        Debug.Log("Lag simulator: median round trip " + baseline + " ms with no lag, " + lagged + " ms with 75 ms each way.");
        Assert.GreaterOrEqual(added, 2 * lag.DelayMilliseconds - 10, "The simulator added less than the delay in both directions.");
        Assert.Less(added, 2 * lag.DelayMilliseconds + 40, "The simulator added much more than the delay in both directions.");
    }

    static long MedianRoundTrip(NAHNetworkLagSimulator lag)
    {
        var holder = new GameObject("TEST lag transport");
        var transport = holder.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        transport.CreateDriver(transport, out var server, out _, out _, out var serverReliable);
        lag.CreateDriver(transport, out var client, out _, out _, out var clientReliable);
        try
        {
            Assert.AreEqual(0, server.Bind(Unity.Networking.Transport.NetworkEndpoint.LoopbackIpv4.WithPort(0)), "The test server could not bind.");
            Assert.AreEqual(0, server.Listen(), "The test server could not listen.");
            var toServer = client.Connect(server.GetLocalEndpoint());

            var clock = System.Diagnostics.Stopwatch.StartNew();
            var serverSide = default(Unity.Networking.Transport.NetworkConnection);
            bool connected = false;
            while (!connected && clock.ElapsedMilliseconds < 5000)
            {
                Pump(server, client);
                var accepted = server.Accept();
                if (accepted.IsCreated)
                    serverSide = accepted;
                while (client.PopEvent(out _, out _) is var e && e != Unity.Networking.Transport.NetworkEvent.Type.Empty)
                    connected |= e == Unity.Networking.Transport.NetworkEvent.Type.Connect;
            }
            Assert.IsTrue(connected && serverSide.IsCreated, "The test client did not connect.");

            var trips = new System.Collections.Generic.List<long>();
            for (int i = 0; i < 5; i++)
            {
                client.BeginSend(clientReliable, toServer, out var writer);
                writer.WriteInt(i);
                client.EndSend(writer);
                long sent = clock.ElapsedMilliseconds;
                bool echoed = false;
                while (!echoed && clock.ElapsedMilliseconds - sent < 3000)
                {
                    Pump(server, client);
                    while (server.PopEvent(out var from, out var request) is var e && e != Unity.Networking.Transport.NetworkEvent.Type.Empty)
                    {
                        if (e != Unity.Networking.Transport.NetworkEvent.Type.Data)
                            continue;
                        server.BeginSend(serverReliable, from, out var reply);
                        reply.WriteInt(request.ReadInt());
                        server.EndSend(reply);
                    }
                    while (client.PopEvent(out _, out _) is var e && e != Unity.Networking.Transport.NetworkEvent.Type.Empty)
                        echoed |= e == Unity.Networking.Transport.NetworkEvent.Type.Data;
                }
                Assert.IsTrue(echoed, "Message " + i + " never came back.");
                trips.Add(clock.ElapsedMilliseconds - sent);
            }

            trips.Sort();
            return trips[trips.Count / 2];
        }
        finally
        {
            client.Dispose();
            server.Dispose();
            Object.DestroyImmediate(holder);
        }
    }

    // Sleep(0) instead of Sleep(1): on Windows a 1 ms sleep can last about 17 ms, which would blur the measurement.
    static void Pump(Unity.Networking.Transport.NetworkDriver server, Unity.Networking.Transport.NetworkDriver client)
    {
        server.ScheduleUpdate().Complete();
        client.ScheduleUpdate().Complete();
        System.Threading.Thread.Sleep(0);
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator HostStartsWithSimulatedLag()
    {
        NahNetworkArenaBuilder.Create();
        EditorSceneManager.OpenScene(ScenePath);
        yield return new EnterPlayMode();

        var session = Object.FindFirstObjectByType<NAHNetworkSession>();
        Assert.IsNotNull(session, "Network session was not in the arena scene.");
        Assert.IsTrue(NAHNetworkLagSimulator.TryParse("75,15,3", out var lag));
        session.SimulateLag(lag);
        session.StartAsHost();

        var manager = NetworkManager.Singleton;
        NAHNetworkPlayer local = null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline && local == null)
        {
            if (manager.LocalClient != null && manager.LocalClient.PlayerObject != null)
                local = manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
            yield return null;
        }
        Assert.IsTrue(manager.IsListening, "The host did not start with the lag simulator.");
        Assert.IsNotNull(local, "Host fighter did not spawn with the lag simulator.");
        Assert.AreSame(lag, Unity.Netcode.Transports.UTP.UnityTransport.s_DriverConstructor, "The transport is not using the lag simulator.");

        NetworkManager.Singleton.Shutdown();
        yield return null;
        yield return new ExitPlayMode();
    }

    // Hits only count once the host has started the match, after everyone has readied up (owner decisions 2026-11-01).
    static void StartTheMatch()
    {
        foreach (var fighter in Object.FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None))
            Assert.IsTrue(fighter.ServerSetReady(true), "A fighter could not ready up.");
        Assert.IsTrue(NAHNetworkMatch.Instance.StartMatch(), "The host could not start the match: " + NAHNetworkMatch.Instance.StartBlocker());
    }

    // Edit Mode tests resume after one editor tick when they yield WaitForSeconds, so poll real time instead.
    static IEnumerator WaitRealSeconds(float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < until)
            yield return null;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
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
