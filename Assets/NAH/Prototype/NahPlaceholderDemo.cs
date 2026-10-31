using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Drives the gray placeholder so a recording can show the heavy local walk.
// This is not combat, not multiplayer, and not an approved performance.
public sealed class NahPlaceholderDemo : MonoBehaviour
{
    float elapsed;
    int shots;
    string folder;

    void Awake()
    {
        folder = Path.Combine(Application.dataPath, "..", "Recordings", "heavy-local");
        Directory.CreateDirectory(folder);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        elapsed += Time.deltaTime;
        if (elapsed < 3f)
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        else if (elapsed < 5f)
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        else if (elapsed < 5.25f)
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        else if (elapsed < 8f)
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        else
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            enabled = false;
        }

        if (shots < 24 && Time.frameCount % 8 == 0)
        {
            var path = Path.Combine(folder, "frame_" + shots.ToString("D2") + ".png");
            ScreenCapture.CaptureScreenshot(path);
            shots++;
        }
    }
}
