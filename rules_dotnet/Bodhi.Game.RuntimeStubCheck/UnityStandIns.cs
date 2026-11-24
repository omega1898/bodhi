// Purpose: stand-ins for the few Unity types the scene scripts (game/Runtime) use, so those
// scripts can be compiled in the cloud where Unity isn't installed. Each one has only the
// members the scripts call, with the same names and shapes as Unity's real ones (as written in
// Unity 6's documentation), and does nothing. They are never run and never shipped: Unity
// brings its own real versions. If a script needs a new Unity call, add its stand-in here.
#pragma warning disable CS1591
using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator *(Vector2 a, float scale) => new Vector2(a.x * scale, a.y * scale);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static float Distance(Vector2 a, Vector2 b) => 0;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float smooth_time) => target;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color Lerp(Color a, Color b, float t) => a;
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { this.width = width; this.height = height; }
        public float width { get; }
        public float height { get; }
    }

    public struct Bounds
    {
        public Vector3 size => Vector3.zero;
    }

    public static class Mathf
    {
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        public static float Clamp01(float value) => Clamp(value, 0, 1);
        public static float MoveTowards(float current, float target, float max_delta) => target;
        public static float SmoothDamp(float current, float target, ref float velocity, float smooth_time) => target;
    }

    public static class Random
    {
        public static Vector2 insideUnitCircle => Vector2.zero;
    }

    public static class Time
    {
        public static float deltaTime => 0;
    }

    public struct RefreshRate
    {
        public double value => 60;
    }

    public struct Resolution
    {
        public RefreshRate refreshRateRatio => new RefreshRate();
    }

    public static class Screen
    {
        public static Resolution currentResolution => new Resolution();
    }

    public static class Debug
    {
        public static void LogWarning(object message) { }
    }

    public static class Application
    {
        public static int targetFrameRate { get; set; }
        public static string persistentDataPath => "";
        public static bool isMobilePlatform => false;
    }

    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object thing) { }
        public static T FindAnyObjectByType<T>() where T : Object => null;
    }

    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
        public T GetComponent<T>() where T : Component => null;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour { }

    public class GameObject : Object
    {
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform => null;
        public string tag { get; set; }
        public T AddComponent<T>() where T : Component => null;
        public T GetComponent<T>() where T : Component => null;
        public void SetActive(bool active) { }
        public bool activeSelf => false;
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale => Vector3.zero;
        public void SetParent(Transform parent, bool world_position_stays) { }
        public void SetAsLastSibling() { }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 pivot { get; set; }
    }

    public enum CameraClearFlags { Skybox, SolidColor }

    public class Camera : Behaviour
    {
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float aspect => 1;
        public Vector3 WorldToScreenPoint(Vector3 position) => position;
    }

    public class AudioListener : Behaviour { }

    public class AudioClip : Object
    {
        public static AudioClip Create(string name, int length_samples, int channels, int frequency, bool stream) => null;
        public bool SetData(float[] data, int offset_samples) => true;
    }

    public class AudioSource : Behaviour
    {
        public bool loop { get; set; }
        public float volume { get; set; }
        public AudioClip clip { get; set; }
        public void Play() { }
        public void Stop() { }
        public void PlayOneShot(AudioClip clip) { }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => null;
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
        public static void UnloadAsset(Object asset) { }
    }

    public class Material : Object { }

    public class Font : Object
    {
        public Material material => null;
    }

    public enum FilterMode { Point, Bilinear }

    public enum TextureFormat { RGBA32 }

    public class Texture2D : Object
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mip_chain) { }
        public FilterMode filterMode { get; set; }
        public void SetPixel(int x, int y, Color color) { }
        public void Apply() { }
    }

    public enum SpriteMeshType { FullRect, Tight }

    public class Sprite : Object
    {
        public Bounds bounds => default;
        public Vector2 pivot => Vector2.zero;
        public Rect rect => default;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) => null;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixels_per_unit) => null;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixels_per_unit, uint extrude, SpriteMeshType mesh_type) => null;
    }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public int sortingOrder { get; set; }
        public Material sharedMaterial { get; set; }
    }

    public class SpriteRenderer : Renderer
    {
        public Sprite sprite { get; set; }
        public Color color { get; set; }
    }

    public class MeshRenderer : Renderer { }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

    public enum FontStyle { Normal, Bold, Italic }

    public class TextMesh : Component
    {
        public string text { get; set; }
        public TextAnchor anchor { get; set; }
        public float characterSize { get; set; }
        public int fontSize { get; set; }
        public Color color { get; set; }
        public Font font { get; set; }
    }

    public enum RenderMode { ScreenSpaceOverlay }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
    }

    // Fades a group of UI pieces together and turns their taps on or off.
    public class CanvasGroup : Behaviour
    {
        public float alpha { get; set; } = 1f;
        public bool interactable { get; set; } = true;
        public bool blocksRaycasts { get; set; } = true;
    }

    public enum HorizontalWrapMode { Wrap, Overflow }

    public enum VerticalWrapMode { Truncate, Overflow }
}

namespace UnityEngine.Events
{
    public class UnityEvent
    {
        public void AddListener(Action call) { }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : MonoBehaviour { }
    public class PointerEventData { }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData data); }
    public interface IPointerUpHandler { void OnPointerUp(PointerEventData data); }
    public interface IPointerExitHandler { void OnPointerExit(PointerEventData data); }
}

namespace UnityEngine.UI
{
    public class CanvasScaler : MonoBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : MonoBehaviour { }

    public class Graphic : MonoBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
    }

    public class Image : Graphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public FillMethod fillMethod { get; set; }
        public float fillAmount { get; set; }
    }

    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public FontStyle fontStyle { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
    }

    public class Button : MonoBehaviour
    {
        public class ButtonClickedEvent : Events.UnityEvent { }
        public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent();
        public bool interactable { get; set; } = true;  // Unity's Selectable.interactable.
    }

    public class Shadow : MonoBehaviour
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
    }
}

namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }

    public class InputAction : IDisposable
    {
        public InputAction(string name = null, InputActionType type = InputActionType.Value) { }
        public BindingSyntax AddBinding(string path) => new BindingSyntax();
        public CompositeSyntax AddCompositeBinding(string composite) => new CompositeSyntax();
        public void Enable() { }
        public void Dispose() { }
        public bool IsPressed() => false;
        public bool WasPressedThisFrame() => false;
        public T ReadValue<T>() where T : struct => default;

        public struct BindingSyntax { }

        public struct CompositeSyntax
        {
            public CompositeSyntax With(string name, string binding) => this;
        }
    }

    public class Touchscreen
    {
        public static Touchscreen current => null;
    }

    // A mouse, pen or finger: whichever the player last used.
    public class Pointer
    {
        public static Pointer current => null;
        public Controls.ButtonControl press => null;
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl
    {
        public bool isPressed => false;
        public bool wasPressedThisFrame => false;
    }
}

namespace UnityEngine.InputSystem.UI
{
    public class InputSystemUIInputModule : MonoBehaviour { }
}
