using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public sealed class RoomExperience : MonoBehaviour
{
    public bool MenuOpen { get; private set; } = true;
    public bool MapExpanded => mapExpanded;
    private bool entered;
    private bool settingsOpen;
    private bool quickMenuOpen, mapExpanded;
    private string lastCaption = "";
    private float captionElapsed;
    private LessonPresentation presentation;
    private PlayerControls controls;
    private PlayerInteraction player;
    private BackgroundMusic music;
    private MobileControlsUI mobile;
    private LessonMinimap minimap;
    private GUIStyle title, subtitle, button, nameStyle, partNameStyle, small, taskNumber;
    private float transitionElapsed = 10;
    private int transitionStage;
    private bool transitionComplete;
    private float previousTimeScale = 1;
    private readonly Color mint = new Color(.3f, .94f, .75f);
    private readonly Color panel = new Color(.035f, .09f, .12f, .96f);

    private void Awake()
    {
        controls = GetComponent<PlayerControls>();
        player = GetComponent<PlayerInteraction>();
        mobile = GetComponent<MobileControlsUI>();
        presentation = GetComponent<LessonPresentation>();
        if (presentation == null) presentation = gameObject.AddComponent<LessonPresentation>();
        minimap = GetComponent<LessonMinimap>();
        if (minimap == null) minimap = gameObject.AddComponent<LessonMinimap>();
        music = GetComponent<BackgroundMusic>();
        if (music == null) music = gameObject.AddComponent<BackgroundMusic>();
    }

    private void Start()
    {
        player.Learning.TaskChanged += ShowStageTransition;
        SetMenu(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetMenu(!MenuOpen);
        if (!MenuOpen)
        {
            transitionElapsed += Time.unscaledDeltaTime;
            string caption = player.Voice.CurrentCaption;
            if (!player.Voice.IsIdentification && caption != lastCaption && !string.IsNullOrEmpty(caption)) captionElapsed = 0;
            lastCaption = caption;
            captionElapsed += Time.unscaledDeltaTime;
        }
    }

    public void SetMenu(bool open)
    {
        if (open && Time.timeScale > 0) previousTimeScale = Time.timeScale;
        MenuOpen = open;
        if (!open) settingsOpen = false;
        Time.timeScale = open ? 0 : previousTimeScale;
        controls.SetMenuOpen(open);
        if (open && player.Voice != null) player.Voice.Stop();
    }

    public void ShowStageTransition()
    {
        transitionStage = player.Learning.Stage;
        transitionComplete = player.Learning.IsComplete;
        transitionElapsed = 0;
        captionElapsed = 0;
    }

    private void Begin()
    {
        entered = true;
        SetMenu(false);
        if (player.Learning.IsPreview) player.Learning.StartLessons();
        else { player.Learning.RepeatTask(); ShowStageTransition(); }
    }

    // Both drawing and hit testing use this exact safe-area coordinate system.
    public static Rect SafeScreenArea
    {
        get
        {
            Rect area = Screen.safeArea;
            return area.width > 0 && area.height > 0 ? area : new Rect(0, 0, Screen.width, Screen.height);
        }
    }
    private float UIScale
    {
        get
        {
            Rect safe = SafeScreenArea;
            float reference = controls != null && controls.UsesTouch ? 430f : 650f;
            return Mathf.Max(.5f, Mathf.Min(safe.width / reference, safe.height / reference));
        }
    }
    private Rect UIArea => new Rect(0, 0, SafeScreenArea.width / UIScale, SafeScreenArea.height / UIScale);
    public Rect QuickMenuRect => new Rect(148, 18, 48, 48);
    public Rect PauseRect => new Rect(148, 74, 120, 44);
    public Rect MusicRect => new Rect(148, 124, 120, 44);
    public Rect HearRect => new Rect(UIArea.width - 94, UIArea.height - 210, 76, 76);
    public Rect PartRect => new Rect(UIArea.width - 180, UIArea.height - 210, 76, 76);
    private Texture2D actionCircle;
    public Rect TaskRect => new Rect(UIArea.width - 174, 18, 156, 58);
    public Rect MinimapRect => mapExpanded
        ? new Rect(18, 86, UIArea.width - 36, Mathf.Max(120, UIArea.height - 180))
        : LessonMinimap.Layout(UIArea.width);
    private Vector2 ToUI(Vector2 screen)
    {
        Rect safe = SafeScreenArea;
        return new Vector2((screen.x - safe.xMin) / UIScale, (safe.yMax - screen.y) / UIScale);
    }

    public bool TryPointer(Vector2 position)
    {
        if (MenuOpen) return true;
        Vector2 ui = ToUI(position);
        if (QuickMenuRect.Contains(ui)) { mapExpanded = false; quickMenuOpen = !quickMenuOpen; return true; }
        if (quickMenuOpen && PauseRect.Contains(ui)) { quickMenuOpen = false; SetMenu(true); return true; }
        if (quickMenuOpen && MusicRect.Contains(ui)) { music.Toggle(); return true; }
        if (TaskRect.Contains(ui)) { player.Learning.RepeatTask(); ShowStageTransition(); return true; }
        if (MinimapRect.Contains(ui)) { mapExpanded = !mapExpanded; quickMenuOpen = false; controls.ResetInput(); return true; }
        if (mapExpanded) { mapExpanded = false; return true; }
        if (controls.UsesTouch && mobile != null)
        {
            TouchControlLayout buttons = mobile.Layout;
            if (player.CanIdentify && InCircle(buttons.HearButton, position)) { player.IdentifyFocused(); return true; }
            if (player.CanIdentifyPart && InCircle(buttons.PartButton, position)) { player.IdentifyFocusedPart(); return true; }
            if (InCircle(buttons.InteractButton, position) || InCircle(buttons.PlaceButton, position) || InCircle(buttons.RotateButton, position)) return false;
        }
        else
        {
            if (player.CanIdentify && InCircle(HearRect, ui)) { player.IdentifyFocused(); return true; }
            if (player.CanIdentifyPart && InCircle(PartRect, ui)) { player.IdentifyFocusedPart(); return true; }
        }
        if (presentation.ShowAudioWarning && ui.y >= 180 && ui.y <= 238)
        { SetMenu(true); settingsOpen = true; return true; }
        if (presentation.ShowText && !player.Voice.IsIdentification && !player.Learning.IsPreview && captionElapsed < 7 && CaptionRect.Contains(ui)) return true;
        if (controls.UsesTouch && mobile != null)
        {
            TouchControlLayout layout = mobile.Layout;
            if (layout.InteractButton.Contains(position) || layout.PlaceButton.Contains(position) ||
                layout.RotateButton.Contains(position) || Vector2.Distance(position, layout.JoystickCenter) <= layout.JoystickRadius * 1.4f)
                return false;
        }
        return false;
    }

    private static bool InCircle(Rect rect, Vector2 point) => rect.width > 0 &&
        Vector2.Distance(rect.center, point) <= rect.width / 2;

    private void DrawActionCircle(Rect rect, string caption)
    {
        if (actionCircle == null)
        {
            actionCircle = new Texture2D(64, 64, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                actionCircle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)))));
            actionCircle.Apply();
        }
        Color before = GUI.color; GUI.color = new Color(.12f, .38f, .46f, .9f);
        GUI.DrawTexture(rect, actionCircle); GUI.color = before;
        GUI.Label(rect, caption, small);
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = new Color(color.r, color.g, color.b, color.a * previous.a);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void Styles()
    {
        if (title != null) return;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 38, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        title.normal.textColor = Color.white;
        subtitle = new GUIStyle(title) { fontSize = 18, fontStyle = FontStyle.Normal, wordWrap = true };
        button = new GUIStyle(subtitle) { fontSize = 19, fontStyle = FontStyle.Bold };
        nameStyle = new GUIStyle(button) { fontSize = 24 };
        partNameStyle = new GUIStyle(nameStyle) { fontSize = 18 };
        small = new GUIStyle(subtitle) { fontSize = 13 };
        taskNumber = new GUIStyle(button) { fontSize = 16, wordWrap = false };
    }

    private bool MenuButton(Rect rect, string caption, bool primary = false)
    {
        Fill(rect, primary ? new Color(.1f, .43f, .36f) : new Color(.08f, .17f, .21f));
        Fill(new Rect(rect.x, rect.y, 3, rect.height), primary ? mint : new Color(.2f, .36f, .4f));
        return GUI.Button(rect, caption, button);
    }

    private void DrawTransition()
    {
        const float duration = 3.5f;
        if (transitionElapsed >= duration) return;
        Rect area = UIArea;
        float enter = Mathf.SmoothStep(0, 1, Mathf.Clamp01(transitionElapsed / .4f));
        float exit = Mathf.SmoothStep(0, 1, Mathf.Clamp01((transitionElapsed - 3) / .5f));
        float width = Mathf.Min(380, area.width - 36);
        float x = Mathf.Lerp(area.width + 20, (area.width - width) / 2, enter) - exit * (area.width + width);
        bool compact = presentation.ShowAudioWarning && area.height < 520;
        Rect card = new Rect(x, presentation.ShowAudioWarning ? 248 : 180, width, compact ? 42 : 136);
        Fill(new Rect(card.x + 4, card.y + 7, card.width, card.height), new Color(0, 0, 0, .18f));
        Fill(card, panel);
        Fill(new Rect(card.x, card.y, 5, card.height), mint);
        if (compact)
        {
            GUI.Label(new Rect(x + 12, card.y + 3, width - 24, 34), transitionComplete ? "Geschafft!" : "Aufgabe " + (transitionStage + 1) + " / " + player.Learning.TotalStages, nameStyle);
            return;
        }
        string heading = transitionComplete ? "GESCHAFFT!" : transitionStage > 0 ? "WEITER GEHT'S" : "LOS GEHT'S";
        GUI.Label(new Rect(x + 16, card.y + 12, width - 32, 24), heading, small);
        string number = transitionComplete ? "Alle Aufgaben geschafft" : "Aufgabe " + (transitionStage + 1).ToString("00") + " / " + player.Learning.TotalStages;
        GUI.Label(new Rect(x + 12, card.y + 40, width - 24, 42), number, nameStyle);
        GUI.Label(new Rect(x + 12, card.y + 86, width - 24, 25), transitionComplete ? "Du kannst weiter üben." : "Entdecke den Raum.", small);
        float progress = player.Learning.TotalStages == 0 ? 0 : (float)transitionStage / player.Learning.TotalStages;
        Fill(new Rect(x + 22, card.yMax - 14, width - 44, 3), new Color(.15f, .25f, .28f));
        Fill(new Rect(x + 22, card.yMax - 14, (width - 44) * progress, 3), mint);
    }

    private void OnGUI()
    {
        Styles();
        Matrix4x4 before = GUI.matrix;
        Color previousColor = GUI.color;
        Rect safe = SafeScreenArea;
        GUI.matrix = Matrix4x4.TRS(new Vector3(safe.xMin, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * UIScale);
        GUI.color = Color.white;
        Rect area = UIArea;
        if (MenuOpen)
        {
            Fill(new Rect(-safe.xMin / UIScale, -(Screen.height - safe.yMax) / UIScale, Screen.width / UIScale, Screen.height / UIScale), new Color(.025f, .065f, .09f, .97f));
            if (settingsOpen) DrawSettings(area);
            else
            {
                float width = Mathf.Min(380, area.width - 36), x = (area.width - width) / 2;
                float y = Mathf.Max(14, (area.height - 400) / 2);
                Fill(new Rect(area.width / 2 - 28, y, 56, 4), mint);
                GUI.Label(new Rect(x, y + 14, width, 54), "DeutschRoom", title);
                GUI.Label(new Rect(x, y + 68, width, 36), presentation.ShowAudioWarning ? "Kein Ton. Öffne die Einstellungen für Text." : "Entdecke dein Zuhause.", small);
                if (MenuButton(new Rect(x, y + 112, width, 52), entered || !player.Learning.IsPreview ? "Weiter spielen" : "Spiel starten", true)) Begin();
                if (MenuButton(new Rect(x, y + 174, width, 48), "Neu starten"))
                {
                    LearningProgress.ClearSavedProgress();
                    SceneManager.LoadScene(gameObject.scene.path);
                }
                if (MenuButton(new Rect(x, y + 232, width, 48), "Einstellungen: Text & Stimme")) settingsOpen = true;
                if (MenuButton(new Rect(x, y + 290, width, 44), controls.UsesTouch ? "Steuerung: Touch" : "Steuerung: Maus + Tastatur"))
                    controls.SetMode(controls.UsesTouch ? PlayerControls.ControlMode.KeyboardAndMouse : PlayerControls.ControlMode.Touch);
                if (MenuButton(new Rect(x, y + 344, width, 44), music.MusicEnabled ? "Musik: an" : "Musik: aus")) music.Toggle();
            }
        }
        else
        {
            Fill(QuickMenuRect, panel);
            GUI.Label(QuickMenuRect, "Menü", small);
            if (quickMenuOpen)
            {
                Fill(PauseRect, panel);
                GUI.Label(PauseRect, "Pause", button);
                Fill(MusicRect, panel);
                GUI.Label(MusicRect, music.MusicEnabled ? "Musik: an" : "Musik: aus", button);
            }
            if (!controls.UsesTouch && !mapExpanded)
            {
                if (player.CanIdentify) DrawActionCircle(HearRect, "Anhören");
                if (player.CanIdentifyPart) DrawActionCircle(PartRect, "Detail");
            }
            Fill(TaskRect, panel);
            string task = player.Learning.IsComplete ? "Geschafft!" : "Aufgabe " + (player.Learning.Stage + 1) + " / " + player.Learning.TotalStages;
            taskNumber.fontSize = 16;
            GUIContent taskContent = new GUIContent(task);
            while (taskNumber.fontSize > 10 && taskNumber.CalcSize(taskContent).x > TaskRect.width - 16)
                taskNumber.fontSize--;
            GUI.Label(new Rect(TaskRect.x + 8, TaskRect.y + 5, TaskRect.width - 16, 27), taskContent, taskNumber);
            GUI.Label(new Rect(TaskRect.x, TaskRect.y + 31, TaskRect.width, 21), presentation.Mode == LessonPresentation.OutputMode.Text ? "Aufgabe wiederholen" : "Noch einmal hören", small);
            if (string.IsNullOrEmpty(player.IdentifiedName)) DrawTransition();

            if (presentation.ShowAudioWarning)
            {
                float width = Mathf.Min(540, area.width - 36);
                Rect warning = new Rect((area.width - width) / 2, 180, width, 58);
                Fill(warning, new Color(.35f, .19f, .06f, .98f));
                GUI.Label(new Rect(warning.x + 12, warning.y + 4, warning.width - 24, 50), presentation.WarningText + "  Einstellungen öffnen", small);
            }
            if (!mapExpanded) DrawCaption(area);
            if (!mapExpanded && presentation.ShowText && !string.IsNullOrEmpty(player.IdentifiedName))
            {
                float width = Mathf.Min(360, area.width - 36);
                Rect name = new Rect((area.width - width) / 2, Mathf.Max(180, CaptionRect.y - 64), width, 58);
                Fill(name, panel);
                GUI.Label(name, player.IdentifiedName, player.IdentifiedName.Contains("\n") ? partNameStyle : nameStyle);
            }
        }
        if (!MenuOpen)
        {
            if (mapExpanded) Fill(new Rect(0, 80, area.width, area.height - 80), new Color(.025f, .065f, .09f, .97f));
            minimap.Draw(MinimapRect);
        }
        GUI.matrix = before;
        GUI.color = previousColor;
    }

    private void DrawSettings(Rect area)
    {
        float width = Mathf.Min(480, area.width - 36), x = (area.width - width) / 2;
        float y = Mathf.Max(12, (area.height - 400) / 2);
        GUI.Label(new Rect(x, y, width, 42), "Text & Stimme", nameStyle);
        string[] labels = { "Nur Text", "Nur Stimme", "Beides" };
        float optionWidth = (width - 16) / 3;
        for (int i = 0; i < 3; i++)
            if (MenuButton(new Rect(x + i * (optionWidth + 8), y + 52, optionWidth, 54), labels[i], (int)presentation.Mode == i))
            {
                presentation.SetMode((LessonPresentation.OutputMode)i);
                player.Voice.Stop();
            }
        GUI.Label(new Rect(x, y + 114, width, 28), "Stimme: " + Mathf.RoundToInt(presentation.VoiceVolume * 100) + "%", subtitle);
        float volume = GUI.HorizontalSlider(new Rect(x + 16, y + 150, width - 32, 28), presentation.VoiceVolume, 0, 1);
        if (Mathf.Abs(volume - presentation.VoiceVolume) > .001f) presentation.SetVoiceVolume(volume);
        if (MenuButton(new Rect(x, y + 184, width, 48), "Stimme testen")) player.Voice.PlayTest();
        if (MenuButton(new Rect(x, y + 242, (width - 10) / 2, 48), "Ich höre nichts")) { presentation.ReportNoSound(true); player.Voice.Stop(); }
        if (MenuButton(new Rect(x + (width + 10) / 2, y + 242, (width - 10) / 2, 48), "Ton funktioniert")) presentation.ReportNoSound(false);
        string hint = presentation.ShowAudioWarning ? presentation.WarningText : "Kein Ton? Prüfe Medienlautstärke und den Lautlosmodus deines Telefons.";
        GUI.Label(new Rect(x + 4, y + 296, width - 8, 55), hint, small);
        if (MenuButton(new Rect(x, y + 354, width, 46), "Speichern & zurück", true))
        { presentation.Save(); player.Voice.Stop(); settingsOpen = false; }
    }

    private Rect CaptionRect
    {
        get
        {
            Rect area = UIArea;
            float width = Mathf.Min(590, area.width > 700 ? area.width - 500 : area.width - 36);
            float y = area.height - 198;
            if (controls.UsesTouch && area.width < 700) y = Mathf.Min(y, 302);
            if (presentation.ShowAudioWarning) y = Mathf.Max(y, 248);
            if (!string.IsNullOrEmpty(player.IdentifiedName)) y = Mathf.Max(y, presentation.ShowAudioWarning ? 316 : 246);
            else if (transitionElapsed < 3.5f)
                y = Mathf.Max(y, presentation.ShowAudioWarning ? (area.height < 520 ? 300 : 394) : 326);
            return new Rect((area.width - width) / 2, y, width, 82);
        }
    }

    private void DrawCaption(Rect area)
    {
        if (!presentation.ShowText || player.Voice.IsIdentification || player.Learning.IsPreview || captionElapsed >= 7) return;
        Color before = GUI.color;
        GUI.color = new Color(before.r, before.g, before.b, before.a * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(captionElapsed - 6))));
        string text = string.IsNullOrEmpty(player.Voice.CurrentCaption) ? player.Learning.TaskSpeech : player.Voice.CurrentCaption;
        Rect card = CaptionRect;
        Fill(card, panel);
        GUI.Label(new Rect(card.x + 14, card.y + 7, card.width - 28, card.height - 14), text, subtitle);
        GUI.color = before;
    }

    private void OnDestroy()
    {
        if (actionCircle != null) Destroy(actionCircle);
        if (player != null && player.Learning != null) player.Learning.TaskChanged -= ShowStageTransition;
        if (MenuOpen) Time.timeScale = previousTimeScale;
    }
}
