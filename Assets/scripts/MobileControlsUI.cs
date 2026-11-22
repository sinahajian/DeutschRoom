using UnityEngine;
using UnityEngine.UI;

public sealed class MobileControlsUI : MonoBehaviour
{
    private PlayerControls controls;
    private PlayerInteraction interaction;
    private Canvas canvas;
    private RectTransform safeRoot;
    private RectTransform joystick;
    private RectTransform knob;
    private Image interactButton;
    private Image placeButton;
    private Image rotateButton;
    private Image hearButton;
    private Image partButton;
    private Text interactText;
    private Font font;
    private Sprite circle;
    private Vector2 lastSize;
    private Rect lastSafeArea;
    private bool visible;
    private readonly Vector3[] corners = new Vector3[4];

    public TouchControlLayout Layout { get; private set; }
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;

    public void Initialize(PlayerControls owner)
    {
        if (canvas != null)
            return;
        controls = owner;
        interaction = GetComponent<PlayerInteraction>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        circle = CreateCircle();
        GameObject root = new GameObject("Mobile Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        safeRoot = Rect("Safe area", root.transform);
        joystick = Image("Movement joystick", safeRoot, new Color(.06f, .1f, .13f, .55f), circle).rectTransform;
        Anchor(joystick, new Vector2(0, 0), new Vector2(145, 135), new Vector2(164, 164));
        knob = Image("Thumb", joystick, new Color(.7f, .84f, .9f, .88f), circle).rectTransform;
        Anchor(knob, new Vector2(.5f, .5f), Vector2.zero, new Vector2(62, 62));
        Text move = Label("Move hint", safeRoot, "BEWEGEN", 19);
        Anchor(move.rectTransform, Vector2.zero, new Vector2(145, 31), new Vector2(180, 30));

        interactButton = ActionButton("Interact", new Vector2(-125, 130), "BENUTZEN", out interactText);
        placeButton = ActionButton("Place", new Vector2(-125, 265), "ABLEGEN", out _);
        rotateButton = ActionButton("Rotate", new Vector2(-265, 265), "DREHEN", out _);
        hearButton = ActionButton("Hear object", new Vector2(-265, 130), "ANHÖREN", out _);
        partButton = ActionButton("Hear part", new Vector2(-405, 130), "DETAIL", out _);
        Text reticle = Label("Aim", root.transform, "+", 30);
        Anchor(reticle.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(36, 36));
        RefreshLayout();
        SetVisible(false);
    }

    public void SetVisible(bool value)
    {
        visible = value;
        if (canvas == null)
            return;
        canvas.gameObject.SetActive(value);
        if (value)
        {
            lastSize = Vector2.zero;
            RefreshLayout();
        }
    }

    public void RefreshLayout()
    {
        if (canvas == null)
            return;
        Vector2 size = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        Rect area = Screen.safeArea;
        if (area.width <= 0 || area.height <= 0)
            area = new Rect(Vector2.zero, size);
        if (size != lastSize || area != lastSafeArea)
        {
            safeRoot.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safeRoot.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            lastSize = size;
            lastSafeArea = area;
            Canvas.ForceUpdateCanvases();
        }
        bool targeting = interaction != null && interaction.CanUseFocused;
        bool holding = interaction != null && interaction.HeldItem != null;
        bool canPlace = interaction != null && interaction.CanPlaceHeld;
        bool canHear = interaction != null && interaction.CanIdentify;
        bool canDetail = interaction != null && interaction.CanIdentifyPart;
        hearButton.rectTransform.anchoredPosition = new Vector2(targeting ? -265 : -125, 130);
        partButton.rectTransform.anchoredPosition = new Vector2(targeting ? -405 : -265, 130);
        interactButton.gameObject.SetActive(targeting);
        hearButton.gameObject.SetActive(canHear);
        partButton.gameObject.SetActive(canDetail);
        placeButton.gameObject.SetActive(canPlace);
        rotateButton.gameObject.SetActive(holding);
        Tint(interactButton, true);
        Tint(placeButton, true);
        Tint(rotateButton, true);
        Tint(hearButton, true);
        Tint(partButton, true);
        Rect stick = ScreenRect(joystick);
        Layout = new TouchControlLayout {
            SafeArea = area, JoystickCenter = stick.center, JoystickRadius = stick.width / 2,
            InteractButton = targeting ? ScreenRect(interactButton.rectTransform) : UnityEngine.Rect.zero,
            PlaceButton = canPlace ? ScreenRect(placeButton.rectTransform) : UnityEngine.Rect.zero,
            RotateButton = holding ? ScreenRect(rotateButton.rectTransform) : UnityEngine.Rect.zero,
            HearButton = canHear ? ScreenRect(hearButton.rectTransform) : UnityEngine.Rect.zero,
            PartButton = canDetail ? ScreenRect(partButton.rectTransform) : UnityEngine.Rect.zero
        };
    }

    private void LateUpdate()
    {
        if (!visible || controls == null)
            return;
        RefreshLayout();
        knob.anchoredPosition = controls.JoystickValue * 69f;
        bool targeting = interaction != null && interaction.CanUseFocused;
        interactText.text = targeting ? interaction.FocusedObject.GetAction(interaction) : "BENUTZEN";
    }

    private Image ActionButton(string name, Vector2 position, string caption, out Text text)
    {
        Image button = Image(name, safeRoot, Color.white, circle);
        Anchor(button.rectTransform, new Vector2(1, 0), position, new Vector2(116, 116));
        text = Label(name + " label", button.transform, caption, 20);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(8, 8);
        text.rectTransform.offsetMax = new Vector2(-8, -8);
        return button;
    }

    private static void Tint(Image image, bool active)
    {
        image.color = active ? new Color(.12f, .38f, .46f, .9f) : new Color(.1f, .13f, .15f, .55f);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image Image(string name, Transform parent, Color color, Sprite sprite)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private Text Label(string name, Transform parent, string value, int size)
    {
        Text label = Rect(name, parent).gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = value;
        label.color = Color.white;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = size;
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, .8f);
        outline.effectDistance = new Vector2(1, -1);
        return label;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private Rect ScreenRect(RectTransform rect)
    {
        rect.GetWorldCorners(corners);
        return new Rect(corners[0], corners[2] - corners[0]);
    }

    private static Sprite CreateCircle()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Touch control circle";
        texture.hideFlags = HideFlags.DontSave;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(size / 2f, size / 2f));
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(size / 2f - distance));
            }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    private void OnGUI()
    {
        if (GetComponent<RoomExperience>()?.MenuOpen == true) return;
        if (controls == null || controls.UsesTouch || Cursor.lockState == CursorLockMode.Locked)
            return;
        GUI.Box(PlayerControls.ModeButtonRect, "Touch-Steuerung (M)");
    }

    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
        if (circle != null)
        {
            Destroy(circle.texture);
            Destroy(circle);
        }
    }
}
