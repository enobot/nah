using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side roster for the gray two-player arena.
// Health and lives live on each player. This object only decides when the match ends.
public sealed class NAHNetworkMatch : MonoBehaviour
{
    public enum Phase
    {
        Waiting = 0,
        Fighting = 1,
        Finished = 2
    }

    public const int MaxPlayers = 4;

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
        else if (CurrentPhase != Phase.Finished && alive >= 2)
        {
            CurrentPhase = Phase.Fighting;
            WinnerClientId = ulong.MaxValue;
        }
        else if (CurrentPhase != Phase.Finished && players.Count < 2)
        {
            CurrentPhase = Phase.Waiting;
            WinnerClientId = ulong.MaxValue;
        }

        for (int i = 0; i < players.Count; i++)
            players[i].PublishMatch(CurrentPhase, WinnerClientId);
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
