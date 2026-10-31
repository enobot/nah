using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Builds the gray direct-connect arena. The shapes are placeholders, not approved art.
public static class NahNetworkArenaBuilder
{
    const string ActionsPath = "Assets/NAH/Prototype/NAHPrototype.inputactions";
    const string MaterialPath = "Assets/NAH/Prototype/PlaceholderGray.mat";
    const string PrefabPath = "Assets/NAH/Prototype/NAHNetworkPlayer.prefab";
    const string ScenePath = "Assets/NAH/Prototype/NetworkArena.unity";
    const string DefaultPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

    [MenuItem("NAH/Create Network Arena")]
    public static void Create()
    {
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (actions == null)
        {
            Debug.LogError("Missing prototype input actions at " + ActionsPath);
            return;
        }

        var gray = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (gray == null)
        {
            gray = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            gray.color = new Color(0.55f, 0.55f, 0.55f);
            AssetDatabase.CreateAsset(gray, MaterialPath);
        }

        var prefab = BuildPlayerPrefab(actions, gray);
        if (prefab == null || prefab.GetComponent<NetworkObject>().PrefabIdHash == 0)
        {
            Debug.LogError("Network player prefab has no prefab hash.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightObject = new GameObject("PLACEHOLDER Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "PLACEHOLDER Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(20f, 1f, 20f);
        floor.GetComponent<Renderer>().sharedMaterial = gray;

        var sessionObject = new GameObject("PLACEHOLDER Session");
        var lobby = sessionObject.AddComponent<Camera>();
        lobby.clearFlags = CameraClearFlags.SolidColor;
        lobby.backgroundColor = new Color(0.45f, 0.45f, 0.45f);
        lobby.tag = "MainCamera";
        sessionObject.AddComponent<AudioListener>();
        sessionObject.AddComponent<NAHNetworkMatch>();
        var session = sessionObject.AddComponent<NAHNetworkSession>();

        var networkObject = new GameObject("NetworkManager");
        var manager = networkObject.AddComponent<NetworkManager>();
        // NGO fills NetworkConfig from an editor reset hook. Entering Play Mode clears that hook until the
        // next script reload, so building twice in one editor session got null here. Do what the hook does.
        if (manager.NetworkConfig == null)
        {
            manager.NetworkConfig = new NetworkConfig();
            var defaultPrefabs = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(DefaultPrefabsPath);
            if (defaultPrefabs != null)
                manager.NetworkConfig.Prefabs.NetworkPrefabsLists = new System.Collections.Generic.List<NetworkPrefabsList> { defaultPrefabs };
        }
        var transport = networkObject.AddComponent<UnityTransport>();
        manager.NetworkConfig.NetworkTopology = NetworkTopologyTypes.ClientServer;
        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.PlayerPrefab = prefab;
        var connection = transport.ConnectionData;
        connection.Address = "127.0.0.1";
        connection.Port = NAHNetworkSession.Port;
        connection.ServerListenAddress = "0.0.0.0";
        transport.ConnectionData = connection;

        SetReference(session, "manager", manager);
        SetReference(session, "lobbyCamera", lobby);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        Debug.Log("Saved network arena. It is a gray placeholder and it uses direct UDP, not Relay. " + ScenePath);
    }

    static GameObject BuildPlayerPrefab(InputActionAsset actions, Material gray)
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "PLACEHOLDER Network Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var renderer = player.GetComponent<Renderer>();
        renderer.sharedMaterial = gray;
        var body = player.AddComponent<CharacterController>();
        body.height = 2f;
        body.center = Vector3.zero;

        var pivot = new GameObject("PLACEHOLDER Camera Pivot").transform;
        pivot.SetParent(player.transform, false);
        pivot.localPosition = new Vector3(0f, 0.7f, 0f);
        var camera = pivot.gameObject.AddComponent<Camera>();
        camera.enabled = false;
        var listener = pivot.gameObject.AddComponent<AudioListener>();
        listener.enabled = false;

        var attackOrigin = new GameObject("PLACEHOLDER Attack Origin").transform;
        attackOrigin.SetParent(pivot, false);
        attackOrigin.localPosition = new Vector3(0f, 0f, 0.8f);

        var input = player.AddComponent<PlayerInput>();
        input.actions = actions;
        input.defaultActionMap = "Player";
        input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        input.camera = camera;

        player.AddComponent<NetworkObject>();
        var networkTransform = player.AddComponent<NetworkTransform>();
        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;

        var motor = player.AddComponent<NAHNetworkPlayer>();
        SetReference(motor, "cameraPivot", pivot);
        SetReference(motor, "attackOrigin", attackOrigin);
        SetReference(motor, "bodyRenderer", renderer);

        PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
        Object.DestroyImmediate(player);
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void SetReference(Object component, string propertyName, Object value)
    {
        var serialized = new SerializedObject(component);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("Missing property " + propertyName + " on " + component.GetType().Name);
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
