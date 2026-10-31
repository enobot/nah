using System;
using System.Globalization;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

// Placeholder connection screen for a direct UDP host. Not the final menu.
// Relay, Lobby, and voice chat are intentionally absent.
public sealed class NAHNetworkSession : MonoBehaviour
{
    public const ushort Port = 7777;

    [SerializeField] NetworkManager manager;
    [SerializeField] Camera lobbyCamera;

    string address = "127.0.0.1";
    string listenAddress = "0.0.0.0";
    string status = "Not connected.";
    NAHBodyShape? requestedBody;

    void Awake()
    {
        Application.runInBackground = true;
        if (manager == null)
            manager = FindFirstObjectByType<NetworkManager>();
    }

    void Start()
    {
        var args = Environment.GetCommandLineArgs();
        string role = ReadArg(args, "--nah-role");
        string requested = ReadArg(args, "--nah-address");
        if (!string.IsNullOrWhiteSpace(requested))
            address = requested;
        // "--nah-listen 127.0.0.1" keeps a same-PC test off the network, so Windows does not ask to open the firewall.
        string listen = ReadArg(args, "--nah-listen");
        if (!string.IsNullOrWhiteSpace(listen))
            listenAddress = listen;
        // "--nah-body 0.5,0.5,0.5" picks size, fatness, and roundness until a character creator exists.
        string bodyText = ReadArg(args, "--nah-body");
        if (!string.IsNullOrWhiteSpace(bodyText))
        {
            if (TryParseBody(bodyText, out NAHBodyShape shape))
                requestedBody = shape;
            else
                Debug.LogWarning("Ignored --nah-body \"" + bodyText + "\". Use three numbers from 0 to 1, such as 0.5,0.5,0.5.");
        }

        if (role == "host")
            StartAsHost();
        else if (role == "client")
            StartAsClient();
    }

    void Update()
    {
        if (manager == null || !manager.IsListening)
            return;

        var local = LocalPlayer();
        if (local != null && requestedBody.HasValue)
        {
            local.RequestBodyShape(requestedBody.Value);
            requestedBody = null;
        }

        if (lobbyCamera == null)
            return;
        bool playing = local != null;
        lobbyCamera.enabled = !playing;
        var listener = lobbyCamera.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = !playing;
    }

    public void StartAsHost()
    {
        if (manager == null || manager.IsListening)
            return;
        ApplyConnection();
        status = manager.StartHost()
            ? "Hosting on UDP port " + Port + "."
            : "Host did not start.";
    }

    public void StartAsClient()
    {
        if (manager == null || manager.IsListening)
            return;
        ApplyConnection();
        status = manager.StartClient()
            ? "Joining " + address + ":" + Port + "."
            : "Join did not start.";
    }

    void ApplyConnection()
    {
        if (manager.NetworkConfig.NetworkTransport is not UnityTransport transport)
        {
            status = "Unity Transport is missing.";
            return;
        }

        var data = transport.ConnectionData;
        data.Address = address;
        data.Port = Port;
        data.ServerListenAddress = listenAddress;
        transport.ConnectionData = data;
        transport.SetConnectionData(address, Port, listenAddress);

        manager.OnClientConnectedCallback -= LimitPlayers;
        manager.OnClientConnectedCallback += LimitPlayers;
    }

    void LimitPlayers(ulong clientId)
    {
        if (manager == null || !manager.IsServer)
            return;
        if (manager.ConnectedClientsIds.Count > NAHNetworkMatch.MaxPlayers)
            manager.DisconnectClient(clientId, "This arena holds four players.");
    }

    void OnGUI()
    {
        const float width = 460f;
        GUILayout.BeginArea(new Rect(12f, 12f, width, 280f), GUI.skin.box);
        GUILayout.Label("PLACEHOLDER connection. Not final UI.");
        GUILayout.Label("Direct UDP. Same computer: 127.0.0.1. Same local network: the host machine's address.");
        GUILayout.Label("This is not internet matchmaking. Relay is off.");
        GUILayout.Label(status);

        if (manager == null)
        {
            GUILayout.Label("NetworkManager is missing.");
            GUILayout.EndArea();
            return;
        }

        if (!manager.IsListening)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Address", GUILayout.Width(64f));
            address = GUILayout.TextField(address, GUILayout.Width(180f));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Host"))
                StartAsHost();
            if (GUILayout.Button("Join"))
                StartAsClient();
        }
        else
        {
            var local = LocalPlayer();
            if (local == null)
                GUILayout.Label("Waiting for your fighter.");
            else
                GUILayout.Label("You are client " + local.OwnerClientId + ". " + DescribePhase(local));

            var players = FindObjectsByType<NAHNetworkPlayer>(FindObjectsSortMode.None);
            Array.Sort(players, (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                string state = player.Eliminated
                    ? "out"
                    : player.Health.ToString("0") + "/" + player.MaxHealth.ToString("0") + " health, " + player.Lives + " lives";
                GUILayout.Label("Client " + player.OwnerClientId + ": " + state);
            }

            GUILayout.Label("Temporary keys: WASD, mouse, Space, Shift, left click.");
            if (GUILayout.Button("Leave"))
            {
                manager.Shutdown();
                status = "Left the match.";
            }
        }

        GUILayout.EndArea();
    }

    static string DescribePhase(NAHNetworkPlayer local)
    {
        switch (local.MatchPhase)
        {
            case NAHNetworkMatch.Phase.Fighting:
                return "Match in progress.";
            case NAHNetworkMatch.Phase.Finished:
                return local.WinnerClientId == ulong.MaxValue
                    ? "Match over."
                    : "Match over. Client " + local.WinnerClientId + " wins.";
            default:
                return "Waiting for a second player.";
        }
    }

    static NAHNetworkPlayer LocalPlayer()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
            return null;
        return manager.LocalClient.PlayerObject.GetComponent<NAHNetworkPlayer>();
    }

    static bool TryParseBody(string text, out NAHBodyShape shape)
    {
        shape = NAHBodyShape.Average;
        string[] parts = text.Split(',');
        if (parts.Length != 3)
            return false;
        var values = new float[3];
        for (int i = 0; i < 3; i++)
        {
            if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                return false;
        }
        shape = new NAHBodyShape(values[0], values[1], values[2]);
        return shape.IsValid;
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
