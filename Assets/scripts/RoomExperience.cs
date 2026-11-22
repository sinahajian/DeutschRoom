using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public sealed class RoomExperience : MonoBehaviour
{
    public bool MenuOpen { get; private set; } = true;
    public bool MapExpanded => mapExpanded;
    public bool GrammarAnswerOpen => !MenuOpen && player != null && player.Learning != null && player.Learning.GrammarAnswerOpen;
    public bool CaseReaderOpen => !MenuOpen && player != null && player.Learning != null && player.Learning.IsScenarioTask && player.Learning.ScenarioSession?.CaseReaderOpen == true;
    public bool DialogueOpen => !MenuOpen && !CaseReaderOpen && player != null && player.Learning != null && player.Learning.IsScenarioTask && player.Learning.ScenarioSession?.DialogueOpen == true;
    public bool ModalOpen => translationOpen || GrammarAnswerOpen || CaseReaderOpen || DialogueOpen;
    private bool translationOpen;
    private Vector2 translationScroll;
    private bool grammarWasOpen;
    private bool ScenarioAnswers => player != null && player.Learning != null && player.Learning.IsScenarioTask;
    public Rect GrammarPanel => new Rect((UIArea.width - Mathf.Min(ScenarioAnswers ? 640 : 540, UIArea.width - 36)) / 2,
        ScenarioAnswers ? 124 : 140, Mathf.Min(ScenarioAnswers ? 640 : 540, UIArea.width - 36), ScenarioAnswers ? 300 : 220);
    public Rect GrammarChoiceRect(int index) => ScenarioAnswers
        ? new Rect(GrammarPanel.x + 12, GrammarPanel.y + 120 + index * 58, GrammarPanel.width - 24, 54)
        : new Rect(GrammarPanel.x + 12 + index * (GrammarPanel.width - 24) / 3, GrammarPanel.y + 152, (GrammarPanel.width - 30) / 3, 48);
    private Rect ScenarioStatusRect => new Rect(18, 145, Mathf.Min(480, UIArea.width - 210), 100);
    public Rect ScenarioConfirmRect => new Rect(TaskRect.x, TaskRect.yMax + 90, TaskRect.width, 40);
    public Rect CaseButtonRect => ReviewHintRect;
    public Rect CaseCloseRect => new Rect(GrammarPanel.xMax-110,GrammarPanel.y+6,100,32);
    public Rect CasePageButtonRect(int index) => new Rect(GrammarPanel.x+12+index*(GrammarPanel.width-24)/3,GrammarPanel.y+242,(GrammarPanel.width-36)/3,44);
    private bool entered;
    private bool settingsOpen;
    private const string TutorialSeenKey = "DeutschRoom.ControlsTutorial.v1";
    private bool tutorialOpen;
    private int tutorialPage;
    private Vector2 tutorialScroll, menuScroll;
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
        if (PlayerPrefs.GetInt(TutorialSeenKey, 0) == 0) OpenTutorial();
    }

    private void Update()
    {
        if (translationOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.I)) CloseTranslation();
            return;
        }
        if (!MenuOpen && Input.GetKeyDown(KeyCode.I)) { OpenTranslation(); return; }
        if (ModalOpen) { mapExpanded = false; quickMenuOpen = false; }
        if (grammarWasOpen != ModalOpen)
        {
            grammarWasOpen = ModalOpen;
            controls.RefreshContext();
        }
        if (!MenuOpen && !ModalOpen && player.Learning.ScenarioNeedsConfirmation && Input.GetKeyDown(KeyCode.T)) player.Learning.ConfirmScenario();
        if (CaseReaderOpen)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.Return)) player.Learning.ScenarioSession.NextCasePage();
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.PageUp)) player.Learning.ScenarioSession.PreviousCasePage();
        }
        if (GrammarAnswerOpen || DialogueOpen)
            for (int i = 0; i < 3; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                { if (DialogueOpen) player.Learning.ScenarioSession.SubmitDialogue(player.Learning.CurrentTaskId,i); else player.Learning.SubmitGrammarAnswer(player.Learning.GrammarTask.id, i); break; }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (tutorialOpen) tutorialOpen = false;
            else if (settingsOpen) settingsOpen = false;
            else SetMenu(!MenuOpen);
        }
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
        if (open) translationOpen = false;
        MenuOpen = open;
        if (!open) settingsOpen = false;
        Time.timeScale = open ? 0 : previousTimeScale;
        controls.SetMenuOpen(open);
        if (open && player.Voice != null) player.Voice.Stop();
    }

    public void ShowStageTransition()
    {
        if (translationOpen) CloseTranslation();
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
    public Rect TranslationButtonRect => new Rect(TaskRect.x - 52, 18, 44, 44);
    public Rect TranslationCloseRect => new Rect((UIArea.width - Mathf.Min(680, UIArea.width - 36)) / 2 + 12, UIArea.height - 68, Mathf.Min(680, UIArea.width - 36) - 24, 44);
    public Rect TaskRect => new Rect(UIArea.width - 174, 18, 156, 58);
    public Rect PlacementResetRect => new Rect(TaskRect.x, TaskRect.yMax + 50, TaskRect.width, 34);
    public Rect ReviewHintRect => new Rect(TaskRect.x, TaskRect.yMax + 8, TaskRect.width, 34);
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
        if (translationOpen)
        {
            if (TranslationCloseRect.Contains(ui)) CloseTranslation();
            return true;
        }
        if (InCircle(TranslationButtonRect, ui))
        {
            OpenTranslation();
            return true;
        }
        if (player.Learning.IsScenarioTask && player.Learning.ScenarioSession.HasCase && !CaseReaderOpen && CaseButtonRect.Contains(ui))
        { player.Learning.ScenarioSession.OpenCase(); return true; }
        if (CaseReaderOpen)
        {
            var session=player.Learning.ScenarioSession;
            if(QuickMenuRect.Contains(ui)){SetMenu(true);return true;}
            if(TaskRect.Contains(ui)){session.SpeakCasePage();return true;}
            if(CaseCloseRect.Contains(ui)&&session.CanCloseCase){session.CloseCase();return true;}
            if(CasePageButtonRect(0).Contains(ui))session.PreviousCasePage();
            else if(CasePageButtonRect(1).Contains(ui))session.SpeakCasePage();
            else if(CasePageButtonRect(2).Contains(ui))session.NextCasePage();
            return true;
        }
        if (GrammarAnswerOpen || DialogueOpen)
        {
            if (QuickMenuRect.Contains(ui)) { SetMenu(true); return true; }
            if (TaskRect.Contains(ui)) { player.Learning.RepeatTask(); return true; }
            string taskId = player.Learning.GrammarTask.id;
            for (int i = 0; i < 3; i++)
                if (GrammarChoiceRect(i).Contains(ui)) { if(DialogueOpen)player.Learning.ScenarioSession.SubmitDialogue(taskId,i); else player.Learning.SubmitGrammarAnswer(taskId, i); break; }
            return true;
        }
        if (QuickMenuRect.Contains(ui)) { mapExpanded = false; quickMenuOpen = !quickMenuOpen; return true; }
        if (quickMenuOpen && PauseRect.Contains(ui)) { quickMenuOpen = false; SetMenu(true); return true; }
        if (quickMenuOpen && MusicRect.Contains(ui)) { music.Toggle(); return true; }
        if (TaskRect.Contains(ui)) { player.Learning.RepeatTask(); ShowStageTransition(); return true; }
        if (!mapExpanded && player.Learning.ScenarioNeedsConfirmation && ScenarioConfirmRect.Contains(ui))
        { player.Learning.ConfirmScenario(); return true; }
        if (!mapExpanded && player.Learning.IsScenarioTask && ScenarioStatusRect.Contains(ui)) return true;
        if (!mapExpanded && (player.Learning.IsPlacementTask || player.Learning.IsScenarioTask) && PlacementResetRect.Contains(ui))
        { player.Learning.ResetPlacementPractice(); return true; }
        if (!mapExpanded && player.Learning.IsReviewTask && !player.Learning.ReviewHintRequested && ReviewHintRect.Contains(ui))
        { player.Learning.RevealReviewTarget(); return true; }
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

    private void OpenTutorial()
    {
        SetMenu(true);
        settingsOpen = false;
        tutorialOpen = true;
        tutorialPage = 0;
        tutorialScroll = Vector2.zero;
    }

    private void DrawTutorial(Rect area)
    {
        string[] headings = { "Welcome to DeutschRoom", "Move and look", "Pick up, place and rotate",
            "Names and object parts", "Follow your task", "Help, sound and progress" };
        bool touch = controls.UsesTouch;
        string[] pages = {
            "Learn German by exploring the house and using real objects. Read or listen to the task, then perform the requested action.\n\nYour first task is to pick up the spoon: den Löffel. Hearing its name alone does not complete a pickup task.\n\nThis guide is in English; the learning tasks and object names are in German.",
            touch
                ? "MOVE: drag the joystick at the bottom left.\n\nLOOK: drag on empty space away from buttons and the joystick. Aim the centre of the screen at an object and move close to it.\n\nPOSTURE: swipe down and hold for 0.6 seconds to sit. Release and repeat to lie down. Swipe up and hold to rise. An obstacle may prevent standing."
                : "MOVE: W A S D or the arrow keys. LOOK: move the mouse. Click in the game to capture the mouse.\n\nPOSTURE: C lowers your posture (stand → sit → lie down). X raises it when there is enough space.\n\nPAUSE: Escape opens the menu and releases the mouse.",
            touch
                ? "Aim at a nearby object. The action button shows what you can do, such as AUFHEBEN (pick up), ÖFFNEN (open) or SCHLIESSEN (close). Tap it to act.\n\nCarry one item at a time. Aim at a nearby surface and tap ABLEGEN to place it. DREHEN rotates a held item by 45°. Buttons appear when their action is available."
                : "Aim at a nearby object and press E to pick it up or use it, such as opening a door or switching a device.\n\nCarry one item at a time. Aim at a nearby surface and press G to place it. Press R to rotate a held item by 45°. Free your hand before picking up another object.",
            touch
                ? "ANHÖREN identifies the whole object and its German article. DETAIL identifies the part you are pointing at, when a separate part name exists.\n\nThese buttons identify objects; they do not pick them up or operate them. Some fixed-object tasks require identification rather than pickup."
                : "F identifies the whole object and its German article. Shift + F identifies the part you are pointing at, when a separate part name exists.\n\nIdentification does not pick up or operate the object. Some fixed-object tasks require identification rather than pickup.",
            "The task card at the top right shows your progress. Tap or click it to repeat the instruction. Tap the circular i button beside it, or press I on a keyboard, to read the current instruction in English. Back to task (or Escape) closes the translation. Translation works offline and does not answer or complete the task.\n\nTap the minimap to expand it; tap again to close it. On review tasks, use Ziel zeigen (H on keyboard) if you need the target revealed.\n\nLater tasks may ask you to place objects, operate devices, read a case or choose an answer. Follow the current instruction. Pick the correct choice after completing the required action; keyboard shortcuts are 1, 2 and 3. Confirm a scenario with its confirmation button (T on keyboard) when prompted. Read all required source pages before continuing. If an action breaks a scenario rule, use its reset button to try again.",
            "Open Menü during play, then Pause to reach the main menu. Replay this guide with How to play (English).\n\nIn Einstellungen: Text & Stimme, choose text, voice or both, and test the sound. You can also change the control mode or turn music on and off.\n\nProgress is saved locally. Weiter spielen continues your current course. Switching courses keeps their progress separate. Diesen Lernpfad neu starten resets the selected course.\n\nMistakes let you try again. Take your time and follow the next instruction."
        };
        float width = Mathf.Min(680, area.width - 36);
        Rect card = new Rect((area.width - width) / 2, 12, width, area.height - 24);
        Fill(card, panel);
        GUI.Label(new Rect(card.x + 12, card.y + 8, width - 24, 24),
            "HOW TO PLAY · " + (tutorialPage + 1) + " / " + headings.Length, small);
        GUI.Label(new Rect(card.x + 16, card.y + 36, width - 32, 46), headings[tutorialPage], nameStyle);
        Rect viewport = new Rect(card.x + 16, card.y + 90, width - 32, card.height - 160);
        var bodyStyle = new GUIStyle(subtitle) { fontSize = 17, alignment = TextAnchor.UpperLeft };
        float textHeight = bodyStyle.CalcHeight(new GUIContent(pages[tutorialPage]), viewport.width - 24);
        tutorialScroll = GUI.BeginScrollView(viewport, tutorialScroll,
            new Rect(0, 0, viewport.width - 24, Mathf.Max(viewport.height, textHeight)));
        GUI.Label(new Rect(0, 0, viewport.width - 24, textHeight), pages[tutorialPage], bodyStyle);
        GUI.EndScrollView();
        float buttonWidth = (width - 44) / 3;
        float bottom = card.yMax - 56;
        GUI.enabled = tutorialPage > 0;
        if (MenuButton(new Rect(card.x + 12, bottom, buttonWidth, 44), "Back"))
        { tutorialPage--; tutorialScroll = Vector2.zero; }
        GUI.enabled = true;
        if (MenuButton(new Rect(card.x + 22 + buttonWidth, bottom, buttonWidth, 44), "Menu")) tutorialOpen = false;
        if (MenuButton(new Rect(card.x + 32 + buttonWidth * 2, bottom, buttonWidth, 44),
            tutorialPage == headings.Length - 1 ? "Finish" : "Next", true))
        {
            if (tutorialPage == headings.Length - 1)
            {
                PlayerPrefs.SetInt(TutorialSeenKey, 1);
                PlayerPrefs.Save();
                tutorialOpen = false;
            }
            else { tutorialPage++; tutorialScroll = Vector2.zero; }
        }
    }

    private void OpenTranslation()
    {
        translationOpen = true;
        translationScroll = Vector2.zero;
        mapExpanded = quickMenuOpen = false;
        controls.RefreshContext();
    }

    private void CloseTranslation()
    {
        translationOpen = false;
        controls.RefreshContext();
    }

    private void DrawTranslation(Rect area)
    {
        Fill(area, panel);
        float width = Mathf.Min(680, area.width - 36);
        float x = (area.width - width) / 2;
        GUI.Label(new Rect(x, 14, width, 40), "Task · English translation", nameStyle);
        Rect viewport = new Rect(x + 12, 68, width - 24, area.height - 148);
        var body = new GUIStyle(subtitle) { alignment = TextAnchor.UpperLeft, fontSize = 18 };
        string content = player.Learning.TaskSpeech + "\n\nENGLISH\n" + player.Learning.TaskEnglish;
        float height = body.CalcHeight(new GUIContent(content), viewport.width - 24);
        translationScroll = GUI.BeginScrollView(viewport, translationScroll,
            new Rect(0, 0, viewport.width - 24, Mathf.Max(height, viewport.height)));
        GUI.Label(new Rect(0, 0, viewport.width - 24, height), content, body);
        GUI.EndScrollView();
        Fill(TranslationCloseRect, new Color(.1f, .43f, .36f));
        GUI.Label(TranslationCloseRect, "Back to task (I / Esc)", button);
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
            if (tutorialOpen) DrawTutorial(area);
            else if (settingsOpen) DrawSettings(area);
            else
            {
                float width = Mathf.Min(380, area.width - 36), x = (area.width - width) / 2;
                float viewportHeight = Mathf.Min(474, area.height - 16);
                float top = Mathf.Max(8, (area.height - viewportHeight) / 2);
                menuScroll = GUI.BeginScrollView(new Rect(0, top, area.width, viewportHeight), menuScroll,
                    new Rect(0, 0, area.width - 20, 468));
                x -= 10;
                float y = 0;
                Fill(new Rect(area.width / 2 - 38, y, 56, 4), mint);
                GUI.Label(new Rect(x, y + 14, width, 54), "DeutschRoom", title);
                GUI.Label(new Rect(x, y + 68, width, 36), player.Learning.CourseName +
                    (player.Voice.IntroAudioPending ? "\nText · Neue Stimme folgt" : ""), small);
                if (MenuButton(new Rect(x, y + 108, width, 44), entered || !player.Learning.IsPreview ? "Weiter spielen" : "Spiel starten", true)) Begin();
                if (MenuButton(new Rect(x, y + 160, width, 44), "Diesen Lernpfad neu starten"))
                {
                    LearningProgress.ClearSavedProgress();
                    SceneManager.LoadScene(gameObject.scene.path);
                }
                if (MenuButton(new Rect(x, y + 212, width, 44), player.Learning.IsIntroCourse ? "Zu bisherigen Aufgaben (136)" : "Zum Lernpfad (" + IntroLessonCatalog.ImplementedTaskCount + " Aufgaben)"))
                {
                    LearningProgress.SelectCourse(!player.Learning.IsIntroCourse);
                    SceneManager.LoadScene(gameObject.scene.path);
                }
                if (MenuButton(new Rect(x, y + 264, width, 44), "Einstellungen: Text & Stimme")) settingsOpen = true;
                if (MenuButton(new Rect(x, y + 316, width, 44), controls.UsesTouch ? "Steuerung: Touch" : "Steuerung: Maus + Tastatur"))
                    controls.SetMode(controls.UsesTouch ? PlayerControls.ControlMode.KeyboardAndMouse : PlayerControls.ControlMode.Touch);
                if (MenuButton(new Rect(x, y + 368, width, 44), music.MusicEnabled ? "Musik: an" : "Musik: aus")) music.Toggle();
                if (MenuButton(new Rect(x, y + 420, width, 44), "How to play (English)")) OpenTutorial();
                GUI.EndScrollView();
            }
        }
        else if (translationOpen) DrawTranslation(area);
        else
        {
            DrawActionCircle(TranslationButtonRect, "i");
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
            if (!mapExpanded && player.Learning.IsReviewTask && !player.Learning.ReviewHintRequested)
            {
                Fill(ReviewHintRect, panel);
                GUI.Label(ReviewHintRect, controls.UsesTouch ? "Ziel zeigen" : "Ziel zeigen (H)", small);
            }
            if (!mapExpanded && (player.Learning.IsPlacementTask || player.Learning.IsScenarioTask) && !ModalOpen)
            { Fill(PlacementResetRect, panel); GUI.Label(PlacementResetRect, "Neu anordnen", small); }
            if (!mapExpanded && player.Learning.IsScenarioTask && !ModalOpen)
            {
                Fill(ScenarioStatusRect, panel);
                GUI.Label(new Rect(ScenarioStatusRect.x+8,ScenarioStatusRect.y+4,ScenarioStatusRect.width-16,ScenarioStatusRect.height-8),
                    player.Learning.TaskSpeech + "\n" + player.Learning.ScenarioSession.Status, small);
                if (player.Learning.ScenarioNeedsConfirmation)
                { Fill(ScenarioConfirmRect, panel); GUI.Label(ScenarioConfirmRect, player.Learning.ScenarioSession.ConfirmationLabel + (controls.UsesTouch ? "" : " (T)"), small); }
                var camera = player.GetComponentInChildren<Camera>();
                if (camera != null)
                    foreach (var marker in player.Learning.ScenarioSession.Labels)
                    {
                        Vector3 screen = camera.WorldToScreenPoint(marker.point);
                        if (screen.z <= 0) continue;
                        Vector2 point = ToUI(new Vector2(screen.x,screen.y));
                        var labelRect = new Rect(point.x-65,point.y-14,130,28);
                        if (!labelRect.Overlaps(ScenarioStatusRect)) { Fill(labelRect,panel); GUI.Label(labelRect,marker.label,small); }
                    }
            }
            if (player.Learning.IsScenarioTask && player.Learning.ScenarioSession.HasCase && !CaseReaderOpen)
            { Fill(CaseButtonRect,panel);GUI.Label(CaseButtonRect,"Fall lesen / hören",small); }
            if (CaseReaderOpen)
            {
                var session=player.Learning.ScenarioSession;
                Fill(GrammarPanel,panel);
                GUI.Label(new Rect(GrammarPanel.x+12,GrammarPanel.y+6,GrammarPanel.width-135,34),session.CaseTitle+" · "+(session.CasePageIndex+1)+" / "+session.CasePageCount,small);
                if(session.CanCloseCase){Fill(CaseCloseRect,new Color(.15f,.35f,.39f,1));GUI.Label(CaseCloseRect,"Zur Aufgabe",small);}
                GUI.Label(new Rect(GrammarPanel.x+14,GrammarPanel.y+44,GrammarPanel.width-28,184),session.CurrentCasePage,small);
                string[] captions={"Zurück","Anhören",session.CasePageIndex+1==session.CasePageCount?"Zur Aufgabe":"Weiter"};
                for(int i=0;i<3;i++){Fill(CasePageButtonRect(i),new Color(.15f,.35f,.39f,1));GUI.Label(CasePageButtonRect(i),captions[i],small);}
            }
            else if (GrammarAnswerOpen || DialogueOpen)
            {
                Fill(GrammarPanel, panel);
                var grammar = player.Learning.GrammarTask;
                var dialogue = DialogueOpen ? player.Learning.ScenarioSession.CurrentStep : null;
                string question = dialogue != null ? dialogue.question : grammar.question;
                string feedback = dialogue != null ? player.Learning.ScenarioSession.DialogueFeedback : player.Learning.GrammarFeedback;
                string[] choices = dialogue != null ? dialogue.choices : grammar.choices;
                GUI.Label(new Rect(GrammarPanel.x + 12, GrammarPanel.y + 8, GrammarPanel.width - 24, ScenarioAnswers ? 62 : 90), question, subtitle);
                GUI.Label(new Rect(GrammarPanel.x + 12, GrammarPanel.y + (ScenarioAnswers ? 72 : 98), GrammarPanel.width - 24, ScenarioAnswers ? 40 : 48), feedback, small);
                for (int i = 0; i < 3; i++)
                {
                    Rect choiceRect=GrammarChoiceRect(i);Fill(choiceRect,new Color(.15f,.35f,.39f,1));
                    string choice=(i+1)+" · "+choices[i];var style=new GUIStyle(small);
                    while(style.fontSize>10&&style.CalcHeight(new GUIContent(choice),choiceRect.width)>choiceRect.height-4)style.fontSize--;
                    GUI.Label(choiceRect,choice,style);
                }
            }
            else if (!player.Learning.IsScenarioTask && string.IsNullOrEmpty(player.IdentifiedName)) DrawTransition();

            if (presentation.ShowAudioWarning && !ModalOpen)
            {
                float width = Mathf.Min(540, area.width - 36);
                Rect warning = new Rect((area.width - width) / 2, 180, width, 58);
                Fill(warning, new Color(.35f, .19f, .06f, .98f));
                GUI.Label(new Rect(warning.x + 12, warning.y + 4, warning.width - 24, 50), presentation.WarningText + "  Einstellungen öffnen", small);
            }
            if (!mapExpanded && !ModalOpen && !player.Learning.IsScenarioTask) DrawCaption(area);
            if (!mapExpanded && !ModalOpen && presentation.ShowText && !string.IsNullOrEmpty(player.IdentifiedName))
            {
                float width = Mathf.Min(360, area.width - 36);
                Rect name = new Rect((area.width - width) / 2, Mathf.Max(180, CaptionRect.y - 64), width, 58);
                Fill(name, panel);
                GUI.Label(name, player.IdentifiedName, player.IdentifiedName.Contains("\n") ? partNameStyle : nameStyle);
            }
        }
        if (!MenuOpen && !ModalOpen)
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
        string hint = player.Voice.IntroAudioPending ? "Die neue Stimme ist noch nicht verfügbar. Diese Lektionen zeigen Text." :
            presentation.ShowAudioWarning ? presentation.WarningText : "Kein Ton? Prüfe Medienlautstärke und den Lautlosmodus deines Telefons.";
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
        bool pendingVoice = player.Learning.IsIntroCourse && player.Voice.NeedsTextFallback;
        if (!presentation.ShowText || player.Voice.IsIdentification || player.Learning.IsPreview || (!pendingVoice && captionElapsed >= 7)) return;
        Color before = GUI.color;
        GUI.color = new Color(before.r, before.g, before.b, before.a * (pendingVoice ? 1 : 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(captionElapsed - 6))));
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
