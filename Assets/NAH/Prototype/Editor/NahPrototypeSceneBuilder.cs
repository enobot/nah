using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Builds a gray test scene for the offline prototype. The shapes are placeholders, not approved art.
public static class NahPrototypeSceneBuilder
{
    const string ActionsPath = "Assets/NAH/Prototype/NAHPrototype.inputactions";
    const string ScenePath = "Assets/NAH/Prototype/PlaceholderArena.unity";

    [MenuItem("NAH/Create Placeholder Prototype Scene")]
    public static void Create()
    {
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        if (actions == null)
        {
            Debug.LogError("Missing prototype input actions at " + ActionsPath);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var gray = new Material(shader);
        gray.color = new Color(0.55f, 0.55f, 0.55f);
        var folder = "Assets/NAH/Prototype";
        AssetDatabase.CreateAsset(gray, folder + "/PlaceholderGray.mat");

        var lightObject = new GameObject("PLACEHOLDER Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "PLACEHOLDER Floor";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(20f, 1f, 20f);
        floor.GetComponent<Renderer>().sharedMaterial = gray;

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "PLACEHOLDER Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<Renderer>().enabled = false;
        var body = player.AddComponent<CharacterController>();
        body.height = 2f;
        body.center = Vector3.zero;

        var pivot = new GameObject("PLACEHOLDER Camera Pivot").transform;
        pivot.SetParent(player.transform, false);
        pivot.localPosition = new Vector3(0f, 0.7f, 0f);
        var camera = pivot.gameObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        pivot.gameObject.AddComponent<AudioListener>();

        var attackOrigin = new GameObject("PLACEHOLDER Attack Origin").transform;
        attackOrigin.SetParent(pivot, false);
        attackOrigin.localPosition = new Vector3(0f, 0f, 0.8f);

        var input = player.AddComponent<PlayerInput>();
        input.actions = actions;
        input.defaultActionMap = "Player";
        input.camera = camera;

        var motor = player.AddComponent<NAHLocalMotor>();
        SetReference(motor, "cameraPivot", pivot);
        player.AddComponent<NAHLocalHealth>();
        var melee = player.AddComponent<NAHLocalMelee>();
        SetReference(melee, "attackOrigin", attackOrigin);
        var interactor = player.AddComponent<NAHLocalInteractor>();
        SetReference(interactor, "playerCamera", camera);
        var demo = player.AddComponent<NahPlaceholderDemo>();
        SetBool(demo, "runDemo", false);

        var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = "PLACEHOLDER Target";
        dummy.transform.position = new Vector3(0f, 1f, 3f);
        dummy.GetComponent<Renderer>().sharedMaterial = gray;
        dummy.AddComponent<NAHLocalHealth>();

        var pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pickup.name = "PLACEHOLDER Heal Pickup";
        pickup.transform.position = new Vector3(2f, 0.5f, 2f);
        pickup.transform.localScale = Vector3.one * 0.4f;
        pickup.GetComponent<Renderer>().sharedMaterial = gray;
        pickup.AddComponent<NAHLocalHealPickup>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Saved placeholder scene. It is not approved art and it is not multiplayer. " + ScenePath);
    }

    public static void CreateAndPlay()
    {
        Create();
        EditorApplication.delayCall += () =>
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        };
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

    static void SetBool(Object component, string propertyName, bool value)
    {
        var serialized = new SerializedObject(component);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("Missing property " + propertyName + " on " + component.GetType().Name);
            return;
        }
        property.boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
