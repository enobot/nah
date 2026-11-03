using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side roster for the gray arena (up to four players).
// Health and lives live on each player. This object decides when the match starts and when it ends.
// Owner decisions (2026-11-01): the host presses Start, only after everyone, the host included, has readied up,
// and nobody may join once the match has started.
public sealed class NAHNetworkMatch : MonoBehaviour
{
    public enum Phase
    {
        Waiting = 0,
        Fighting = 1,
        Finished = 2
    }

    public const int MaxPlayers = 4;
    public const int MinPlayersToStart = 2;

    public static NAHNetworkMatch Instance { get; private set; }

    readonly List<NAHNetworkPlayer> players = new List<NAHNetworkPlayer>();

    public Phase CurrentPhase { get; private set; } = Phase.Waiting;
    public ulong WinnerClientId { get; private set; } = ulong.MaxValue;
    public int PlayerCount => players.Count;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one NAHNetworkMatch is allowed.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int Register(NAHNetworkPlayer player)
    {
        if (player == null)
            return 0;
        if (!players.Contains(player))
            players.Add(player);
        Recompute();
        return players.IndexOf(player);
    }

    public void Unregister(NAHNetworkPlayer player)
    {
        players.Remove(player);
        Recompute();
    }

    public void Recompute()
    {
        players.RemoveAll(player => player == null);

        int alive = 0;
        NAHNetworkPlayer lastAlive = null;
        for (int i = 0; i < players.Count; i++)
        {
            if (!players[i].IsSpawned || players[i].Eliminated)
                continue;
            alive++;
            lastAlive = players[i];
        }

        // The fight no longer starts by itself when a second player arrives. Only StartMatch starts it.
        if (players.Count == 0)
        {
            CurrentPhase = Phase.Waiting;
            WinnerClientId = ulong.MaxValue;
        }
        else if (CurrentPhase == Phase.Fighting && alive == 1 && lastAlive != null)
        {
            CurrentPhase = Phase.Finished;
            WinnerClientId = lastAlive.OwnerClientId;
        }

        for (int i = 0; i < players.Count; i++)
            players[i].PublishMatch(CurrentPhase, WinnerClientId);
    }

    // Why the host cannot start yet, or null when it can.
    public string StartBlocker()
    {
        if (CurrentPhase != Phase.Waiting)
            return "The match has already started.";

        players.RemoveAll(player => player == null);
        int present = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsSpawned)
                present++;
        }
        if (present < MinPlayersToStart)
            return "Needs at least " + MinPlayersToStart + " players.";

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsSpawned && !players[i].Ready)
                return "Waiting for client " + players[i].OwnerClientId + " to ready up.";
        }
        return null;
    }

    // Host only. Starts the fight when StartBlocker allows it. Everyone begins at their own spawn point at full health.
    // Returns false and changes nothing otherwise.
    public bool StartMatch()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer || StartBlocker() != null)
            return false;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsSpawned)
                players[i].ServerPrepareForFight();
        }
        CurrentPhase = Phase.Fighting;
        WinnerClientId = ulong.MaxValue;
        Recompute();
        return true;
    }

    public NAHNetworkPlayer SpawnRulesOpponentForTest()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer || manager.NetworkConfig.PlayerPrefab == null)
            return null;

        var opponent = Instantiate(manager.NetworkConfig.PlayerPrefab);
        opponent.GetComponent<NetworkObject>().Spawn();
        return opponent.GetComponent<NAHNetworkPlayer>();
    }
}
