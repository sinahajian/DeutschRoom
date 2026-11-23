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
    private Sprite circle, tile;
    private Vector2 lastSize;
    private Rect lastSafeArea;
    private bool visible;
    private readonly Vector3[] corners = new Vector3[4];
    private TouchControlLayout geometry;
    private readonly System.Collections.Generic.List<Sprite> icons = new System.Collections.Generic.List<Sprite>();
    private Image pressedAction;
    private Color pressedBase;
    private float pressedUntil;

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
        tile = CreateCircle(true);
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
            Rect stick=ScreenRect(joystick);
            geometry=new TouchControlLayout { SafeArea=area,JoystickCenter=stick.center,JoystickRadius=stick.width/2,
                InteractButton=ScreenRect(interactButton.rectTransform),PlaceButton=ScreenRect(placeButton.rectTransform),
                RotateButton=ScreenRect(rotateButton.rectTransform),HearButton=ScreenRect(hearButton.rectTransform),PartButton=ScreenRect(partButton.rectTransform) };
        }
        bool targeting = interaction != null && interaction.CanUseFocused;
        bool holding = interaction != null && interaction.HeldItem != null;
        bool canPlace = interaction != null && interaction.CanPlaceHeld;
        bool canHear = interaction != null && interaction.CanIdentify;
        bool canDetail = interaction != null && interaction.CanIdentifyPart;
        // Keep each action in its initialized slot when neighboring controls are hidden.
        SetActionVisible(interactButton,targeting);
        SetActionVisible(hearButton,canHear);
        SetActionVisible(partButton,canDetail);
        SetActionVisible(placeButton,canPlace);
        SetActionVisible(rotateButton,holding && interaction.CanRotateHeld);
        Layout = new TouchControlLayout {
            SafeArea = area, JoystickCenter = geometry.JoystickCenter, JoystickRadius = geometry.JoystickRadius,
            InteractButton = targeting ? geometry.InteractButton : UnityEngine.Rect.zero,
            PlaceButton = canPlace ? geometry.PlaceButton : UnityEngine.Rect.zero,
            RotateButton = holding && interaction.CanRotateHeld ? geometry.RotateButton : UnityEngine.Rect.zero,
            HearButton = canHear ? geometry.HearButton : UnityEngine.Rect.zero,
            PartButton = canDetail ? geometry.PartButton : UnityEngine.Rect.zero
        };
    }

    // Read actual Canvas geometry even when the modal tour hides gameplay input.
    public Rect TutorialScreenRect(string control)
    {
        if (canvas == null) return UnityEngine.Rect.zero;
        bool active = canvas.gameObject.activeSelf;
        canvas.gameObject.SetActive(true);
        RefreshLayout();
        Canvas.ForceUpdateCanvases();
        RectTransform target = control == "Move" ? joystick :
            control == "Interact" ? interactButton.rectTransform : control == "Hear" ? hearButton.rectTransform :
            control == "Part" ? partButton.rectTransform : control == "Place" ? placeButton.rectTransform : rotateButton.rectTransform;
        Rect result = ScreenRect(target);
        canvas.gameObject.SetActive(active);
        return result;
    }

    private void LateUpdate()
    {
        if (!visible || controls == null)
            return;
        RefreshLayout();
        if(pressedAction!=null)
        {
            float remaining=Mathf.Clamp01((pressedUntil-Time.unscaledTime)/.18f);
            pressedAction.color=Color.Lerp(pressedBase,new Color(.25f,.68f,.58f,1),remaining);
            if(remaining<=0)pressedAction=null;
        }
        Vector2 position=controls.JoystickValue*69f;
        if(knob.anchoredPosition!=position)knob.anchoredPosition=position;
        bool targeting = interaction != null && interaction.CanUseFocused;
        string caption=targeting ? interaction.FocusedObject.GetAction(interaction) : "BENUTZEN";
        if(interactText.text!=caption)interactText.text=caption;
    }
    public void ShowPress(Vector2 point)
    {
        Image hit = Layout.InteractButton.Contains(point)?interactButton:
            Layout.PlaceButton.Contains(point)?placeButton:Layout.RotateButton.Contains(point)?rotateButton:
            Layout.HearButton.Contains(point)?hearButton:Layout.PartButton.Contains(point)?partButton:null;
        if(hit==null)return;
        if(pressedAction!=null)pressedAction.color=pressedBase;
        pressedAction=hit;pressedBase=hit.color;pressedUntil=Time.unscaledTime+.18f;
    }

    private static void SetActionVisible(Image action,bool show)
    {if(action.gameObject.activeSelf!=show)action.gameObject.SetActive(show);}

    private Image ActionButton(string name, Vector2 position, string caption, out Text text)
    {
        bool primary = name == "Interact" || name == "Place";
        Color fill = primary ? new Color(.055f, .34f, .32f, .97f) : new Color(.065f, .09f, .14f, .95f);
        Image button = Image(name, safeRoot, fill, tile);
        var shadow = button.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .35f);
        shadow.effectDistance = new Vector2(0, -4);
        Image accent = Image("Accent", button.transform, primary ? new Color(.36f,.91f,.73f) : new Color(.43f,.61f,.77f), tile);
        Anchor(accent.rectTransform, new Vector2(.5f, 0), new Vector2(0, 9), new Vector2(22, 3));
        Sprite symbol=CreateIcon(name);icons.Add(symbol);
        Image icon=Image("Action icon",button.transform,primary?new Color(.57f,1,.83f):new Color(.68f,.80f,.94f),symbol);
        Anchor(icon.rectTransform,new Vector2(.5f,1),new Vector2(0,-29),new Vector2(28,28));
        Anchor(button.rectTransform, new Vector2(1, 0), position, new Vector2(126, 100));
        text = Label(name + " label", button.transform, caption, 18);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.fontStyle = FontStyle.Bold;
        text.rectTransform.offsetMin = new Vector2(6, 14);
        text.rectTransform.offsetMax = new Vector2(-6, -49);
        return button;
    }

    private static Sprite CreateIcon(string action)
    {
        const int size=48;
        var lines=new System.Collections.Generic.List<Vector4>();
        void Line(float x,float y,float x2,float y2)=>lines.Add(new Vector4(x,y,x2,y2));
        if(action=="Interact" || action=="Place")
        {
            bool up=action=="Interact";float end=up?37:16,tail=up?15:38,wing=up?29:24;
            Line(24,tail,24,end);Line(24,end,16,wing);Line(24,end,32,wing);
            Line(11,12,37,12);
        }
        else if(action=="Hear object")
        {
            Line(10,19,17,19);Line(17,19,25,12);Line(25,12,25,36);
            Line(25,36,17,29);Line(17,29,10,29);Line(10,29,10,19);
            Line(31,18,34,24);Line(34,24,31,30);Line(37,13,41,24);Line(41,24,37,35);
        }
        else if(action=="Hear part")
        {
            Line(11,12,11,36);Line(11,36,36,36);Line(36,36,36,12);Line(36,12,11,12);
            Line(18,28,29,28);Line(18,21,29,21);Line(18,15,24,15);
        }
        else
        {
            for(int i=0;i<16;i++)
            {float a=(40+i*18)*Mathf.Deg2Rad,b=(40+(i+1)*18)*Mathf.Deg2Rad;
             Line(24+13*Mathf.Cos(a),24+13*Mathf.Sin(a),24+13*Mathf.Cos(b),24+13*Mathf.Sin(b));}
            Line(34,33,35,23);Line(35,23,25,25);
        }
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float distance=100;Vector2 point=new Vector2(x+.5f,y+.5f);
            foreach(var l in lines)
            {Vector2 a=new Vector2(l.x,l.y),d=new Vector2(l.z-l.x,l.w-l.y);
             distance=Mathf.Min(distance,Vector2.Distance(point,a+d*Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude)));}
            pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(2-distance));
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));
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

    private static Sprite CreateCircle(bool rounded = false)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Touch control circle";
        texture.hideFlags = HideFlags.DontSave;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 q = new Vector2(Mathf.Abs(x + .5f - size / 2f), Mathf.Abs(y + .5f - size / 2f));
                float alpha;
                if (rounded)
                {
                    Vector2 edge = Vector2.Max(q - Vector2.one * (size / 2f - 20), Vector2.zero);
                    alpha = Mathf.Clamp01(20 - edge.magnitude);
                }
                else alpha = Mathf.Clamp01(size / 2f - q.magnitude);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
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
        foreach(var icon in icons) { Destroy(icon.texture); Destroy(icon); }
        if (tile != null) { Destroy(tile.texture); Destroy(tile); }
        if (circle != null)
        {
            Destroy(circle.texture);
            Destroy(circle);
        }
    }
}
