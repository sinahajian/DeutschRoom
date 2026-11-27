using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public sealed partial class RoomExperience : MonoBehaviour
{
    public bool MenuOpen { get; private set; } = true;
    public bool MapExpanded => mapExpanded;
    public bool GrammarAnswerOpen => !MenuOpen && !HandsOnActive && player != null && player.Learning != null && player.Learning.GrammarAnswerOpen;
    public bool CaseReaderOpen => !MenuOpen && !HandsOnActive && player != null && player.Learning != null && player.Learning.IsScenarioTask && player.Learning.ScenarioSession?.CaseReaderOpen == true;
    public bool DialogueOpen => !MenuOpen && !HandsOnActive && !CaseReaderOpen && player != null && player.Learning != null && player.Learning.IsScenarioTask && player.Learning.ScenarioSession?.DialogueOpen == true;
    public HandsOnTutorial HandsOn { get; private set; }
    public bool HandsOnActive => HandsOn != null && HandsOn.Active;
    public bool TutorialOpen => tutorialOpen || HandsOnActive;
    public int TutorialStep => tutorialPage;
    public bool ModalOpen => tutorialOpen || languageChoiceOpen || languageCheckpointOpen || GrammarAnswerOpen || CaseReaderOpen || DialogueOpen;
    private bool grammarWasOpen;
    private bool ScenarioAnswers => player != null && player.Learning != null && player.Learning.IsScenarioTask;
    public Rect GrammarPanel => new Rect((UIArea.width - Mathf.Min(ScenarioAnswers ? 640 : 540, UIArea.width - 36)) / 2,
        ScenarioAnswers ? 124 : 140, Mathf.Min(ScenarioAnswers ? 640 : 540, UIArea.width - 36), ScenarioAnswers ? 300 : 220);
    public Rect GrammarChoiceRect(int index) => ScenarioAnswers
        ? new Rect(GrammarPanel.x + 12, GrammarPanel.y + 120 + index * 58, GrammarPanel.width - 24, 54)
        : new Rect(GrammarPanel.x + 12 + index * (GrammarPanel.width - 24) / 3, GrammarPanel.y + 152, (GrammarPanel.width - 30) / 3, 48);
    private Rect ScenarioStatusRect => new Rect(18,145,Mathf.Min(480,UIArea.width-210),100);
    public Rect ScenarioConfirmRect => new Rect(TaskRect.x,TaskRect.yMax+90,TaskRect.width,40);
    public Rect CaseButtonRect => ReviewHintRect;
    public Rect CaseCloseRect => new Rect(GrammarPanel.xMax-110,GrammarPanel.y+6,100,32);
    public Rect CasePageButtonRect(int index) => new Rect(GrammarPanel.x+12+index*(GrammarPanel.width-24)/3,GrammarPanel.y+242,(GrammarPanel.width-36)/3,44);
    private bool entered;
    private bool settingsOpen, resetConfirmation;
    private const string TutorialSeenKey = "DeutschRoom.ControlsTutorial.v1";
    private const string HandsOnSeenKey = "DeutschRoom.HandsOnTutorial.v1";
    private bool tutorialOpen, tutorialAutoPending;
    private int tutorialPage;
    private int handsOnRenderedStep = -1;
    private int tutorialFocusFrame = -1, tutorialFocusPage = -1;
    private bool tutorialFocusTouch;
    private Rect tutorialFocusArea, tutorialFocusSafeArea, tutorialFocus;
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
    private GUIStyle tutorialTitle, tutorialBody, tutorialLead, tutorialMeta, answerChoice;
    private readonly GUIContent tutorialBodyContent = new GUIContent(), tutorialLeadContent = new GUIContent();
    private float transitionElapsed = 100;
    private int transitionStage;
    private bool transitionComplete;
    private float previousTimeScale = 1;
    private readonly Color mint = new Color(.3f, .94f, .75f);
    private readonly Color panel = new Color(.045f, .065f, .10f, .97f);

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
        languageChoiceOpen = !SupportLanguages.HasSelection;
        tutorialAutoPending = PlayerPrefs.GetInt(HandsOnSeenKey, 0) == 0;
        checkpointPending = true;
    }

    private void Update()
    {
        if (languageChoiceOpen || languageCheckpointOpen) return;
        HandleHelpTextDrag();
        if (checkpointPending) { CheckLanguageCheckpoint(); if (languageCheckpointOpen) return; }
        if (tutorialAutoPending) { tutorialAutoPending = false; OpenTutorial(); }
        if (HandsOnActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseTutorial();
            else if (HandsOn.Complete && Input.GetKeyDown(KeyCode.Return)) CloseTutorial();
            return;
        }
        if (tutorialOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseTutorial();
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow)) AdvanceTutorial();
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) { tutorialPage = Mathf.Max(0, tutorialPage - 1); tutorialScroll = Vector2.zero; }
            return;
        }
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
            if(resetConfirmation)resetConfirmation=false;
            else if (settingsOpen) settingsOpen = false;
            else SetMenu(!MenuOpen);
        }
        if (!MenuOpen)
        {
            transitionElapsed=TaskNoticeTiming.Advance(transitionElapsed,Time.unscaledDeltaTime,
                ModalOpen || !string.IsNullOrEmpty(player.IdentifiedName) || NoticePointerReading);
            string caption = player.Voice.CurrentCaption;
            if (!player.Voice.IsIdentification && caption != lastCaption && !string.IsNullOrEmpty(caption)) captionElapsed = 0;
            lastCaption = caption;
            captionElapsed += Time.unscaledDeltaTime;
        }
    }

    public void SetMenu(bool open)
    {
        if (HandsOnActive || (!open && (languageChoiceOpen || languageCheckpointOpen))) return;
        if (tutorialOpen) return;
        if (open && Time.timeScale > 0) previousTimeScale = Time.timeScale;
        MenuOpen = open;
        if (!open) settingsOpen = false;
        Time.timeScale = open ? 0 : previousTimeScale;
        if(!open)resetConfirmation=false;
        controls.SetMenuOpen(open);
        if (open && player.Voice != null) player.Voice.Stop();
    }

    public void ShowStageTransition()
    {
        checkpointPending = true;
        transitionStage = player.Learning.Stage;
        transitionComplete = player.Learning.IsComplete;
        transitionElapsed = 0;
        noticeScroll=Vector2.zero;noticeHeld=false;
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
    public Rect TaskRect => new Rect(UIArea.width-198,18,180,58);
    public Rect PlacementResetRect => new Rect(TaskRect.x,TaskRect.yMax+50,TaskRect.width,34);
    public Rect ReviewHintRect => new Rect(TaskRect.x,TaskRect.yMax+8,TaskRect.width,34);
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
        Vector2 ui = ToUI(position);
        if (languageChoiceOpen || languageCheckpointOpen) return true;
        if(TaskNoticeVisible)
        {
            if(!taskNoticeRect.Contains(ui) || NoticeCloseRect.Contains(ui))
            {
                transitionElapsed=TaskNoticeDuration;noticeHeld=false;textScrollFinger=-1;
            }
            // Consume dismissal so the same press cannot activate a control or object behind it.
            return true;
        }
        if (!MenuOpen && !ModalOpen && !TutorialOpen && !mapExpanded && TaskRect.Contains(ui))
        { player.Learning.RepeatTask(); ShowStageTransition(); return true; }
        if (HandsOnActive)
        {
            if (HandsOnSkipRect.Contains(ui) || HandsOn.Complete && HandsOnFinishRect.Contains(ui)) { CloseTutorial(); return true; }
            if (HandsOnCard.Contains(ui) || HandsOn.Complete) return true;
            Rect hear = controls.UsesTouch && mobile != null ? mobile.Layout.HearButton : HearRect;
            Vector2 at = controls.UsesTouch ? position : ui;
            if (player.CanIdentify && InCircle(hear, at)) { player.IdentifyFocused(); return true; }
            return false;
        }
        if (tutorialOpen)
        {
            if (TutorialNextRect.Contains(ui)) AdvanceTutorial();
            else if (TutorialBackRect.Contains(ui)) { tutorialPage = Mathf.Max(0, tutorialPage - 1); tutorialScroll = Vector2.zero; }
            else if (TutorialSkipRect.Contains(ui)) CloseTutorial();
            return true;
        }
        if (MenuOpen) return true;
        if (player.Learning.IsScenarioTask && player.Learning.ScenarioSession.HasCase && !CaseReaderOpen && CaseButtonRect.Contains(ui))
        { player.Learning.ScenarioSession.OpenCase(); return true; }
        if (CaseReaderOpen)
        {
            var session=player.Learning.ScenarioSession;
            if(QuickMenuRect.Contains(ui)){SetMenu(true);return true;}
            if(CaseCloseRect.Contains(ui)&&session.CanCloseCase){session.CloseCase();return true;}
            if(CasePageButtonRect(0).Contains(ui))session.PreviousCasePage();
            else if(CasePageButtonRect(1).Contains(ui))session.SpeakCasePage();
            else if(CasePageButtonRect(2).Contains(ui))session.NextCasePage();
            return true;
        }
        if (GrammarAnswerOpen || DialogueOpen)
        {
            if (QuickMenuRect.Contains(ui)) { SetMenu(true); return true; }
            string taskId = player.Learning.GrammarTask.id;
            for (int i = 0; i < 3; i++)
                if (GrammarChoiceRect(i).Contains(ui)) { if(DialogueOpen)player.Learning.ScenarioSession.SubmitDialogue(taskId,i); else player.Learning.SubmitGrammarAnswer(taskId, i); break; }
            return true;
        }
        if (QuickMenuRect.Contains(ui)) { mapExpanded = false; quickMenuOpen = !quickMenuOpen; return true; }
        if (quickMenuOpen && PauseRect.Contains(ui)) { quickMenuOpen = false; SetMenu(true); return true; }
        if (quickMenuOpen && MusicRect.Contains(ui)) { music.Toggle(); return true; }
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
        // The rounded-texture overload uses its explicit tint, not GUI.color.
        // Supply the surface colour there so white textures cannot hide light text.
        GUI.color = Color.white;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0,
            new Color(color.r, color.g, color.b, color.a * previous.a), 0,
            Mathf.Min(12, Mathf.Min(rect.width,rect.height)*.2f));
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
        tutorialTitle = new GUIStyle(nameStyle) { fontSize = 22, alignment = TextAnchor.UpperLeft };
        tutorialBody = new GUIStyle(subtitle) { fontSize = 16, alignment = TextAnchor.UpperLeft };
        tutorialLead = new GUIStyle(tutorialBody) { fontSize = 18, fontStyle = FontStyle.Bold };
        tutorialLead.normal.textColor = mint;
        tutorialMeta = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
        answerChoice=new GUIStyle(small);
    }

    private bool MenuButton(Rect rect, string caption, bool primary = false)
    {
        string focusName="menu:"+caption;
        bool focus=GUI.GetNameOfFocusedControl()==focusName;
        bool hover=rect.Contains(Event.current.mousePosition);
        bool press=hover && Input.GetMouseButton(0);
        UiSurfaceBox(rect,hover||focus||primary);
        Color fill=primary?new Color(.085f,.27f,.24f):new Color(.08f,.12f,.17f);
        if(hover)fill=Color.Lerp(fill,UiAccent,press?.22f:.10f);
        Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),fill);
        var textStyle=SupportStyle(true);
        GUI.SetNextControlName(focusName);
        return GUI.Button(rect, SupportLayout(caption,textStyle,rect.width-24,false),textStyle);
    }

    // English How to play source, localized through SupportLanguages; neither guide completes tasks.
    private readonly string[] tutorialTitles = {
        "Move", "Look and aim", "Sit, lie down and stand", "Pick up and use", "Hear an object's name",
        "Hear a part's name", "Place an object", "Rotate an object", "Your map", "Your current task",
        "Your support language", "A hint for review tasks", "Reset a practice", "Scenario instructions",
        "Confirm an action", "Read and listen to a case", "Choose an answer", "Open the quick menu",
        "Pause", "Music", "Settings and saved progress"
    };
    public int TutorialStepCount => tutorialTitles.Length;

    public void OpenTutorial()
    {
        if (TutorialOpen) return;
        tutorialAutoPending = false;
        if (!entered) Begin(); else SetMenu(false);
        settingsOpen = mapExpanded = quickMenuOpen = false;
        if (HandsOn == null) HandsOn = gameObject.AddComponent<HandsOnTutorial>();
        tutorialScroll = Vector2.zero; handsOnRenderedStep = -1;
        if (!HandsOn.Begin(player)) { OpenControlsReference(); return; }
        PlayerPrefs.SetInt(HandsOnSeenKey, 1); PlayerPrefs.Save();
        Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1;
        controls.RefreshContext();
    }

    public void OpenControlsReference()
    {
        if (TutorialOpen) return;
        tutorialAutoPending = false;
        if (!entered) Begin();
        else SetMenu(false);
        settingsOpen = mapExpanded = quickMenuOpen = false;
        tutorialPage = 0;
        tutorialOpen = true;
        // Remember the first presentation, including a skipped/interrupted tour.
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
        Time.timeScale = 0;
        player.Voice.Stop();
        controls.RefreshContext();
    }

    public void AdvanceTutorial()
    {
        if (!tutorialOpen) return;
        if (tutorialPage + 1 == TutorialStepCount) CloseTutorial();
        else { tutorialPage++; tutorialScroll = Vector2.zero; }
    }

    public void CloseTutorial()
    {
        if (HandsOnActive)
        {
            HandsOn.Finish(); controls.RefreshContext();
            player.Learning.RepeatTask(); ShowStageTransition(); return;
        }
        if (!tutorialOpen) return;
        tutorialOpen = false;
        SetMenu(false);
        player.Learning.RepeatTask();
        ShowStageTransition();
    }

    public Rect HandsOnCard => new Rect(14, 14, Mathf.Min(370, UIArea.width - 140), Mathf.Min(300,UIArea.height*.5f-30));
    public Rect HandsOnSkipRect => new Rect(UIArea.width - 118, 14, 104, 44);
    private Rect HandsOnTextViewport => new Rect(HandsOnCard.x+16,HandsOnCard.y+44,HandsOnCard.width-32,HandsOnCard.height-(HandsOn.Complete?116:76));
    public Rect HandsOnFinishRect => new Rect(HandsOnCard.x + 16, HandsOnCard.yMax - 60, HandsOnCard.width - 32, 44);

    private void DrawHandsOn(Rect area)
    {
        if (handsOnRenderedStep != (int)HandsOn.Step) { handsOnRenderedStep = (int)HandsOn.Step; tutorialScroll = Vector2.zero; }
        Rect card = HandsOnCard;
        UiSurfaceBox(card,true);
        SupportLabel(new Rect(card.x+16,card.y+10,card.width-32,24),
            SupportStageNumber(Mathf.Min((int)HandsOn.Step+1,HandsOn.StepCount))+" / "+SupportStageNumber(HandsOn.StepCount),false,false);
        string heading=HandsOn.Complete?"You're ready!":HandsOn.Instruction.title;
        string text=HandsOn.Complete?"Nice work! You can now explore the room and start your lesson.":
            SupportLanguages.Text(HandsOn.Instruction.englishInstruction)+"\n\n"+SupportLanguages.Text(controls.UsesTouch?HandsOn.Instruction.touchHelp:HandsOn.Instruction.keyboardHelp);
        var localizedStyle=SupportStyle();
        float textWidth=card.width-68;
        string titleText=SupportLayout(heading,localizedStyle,textWidth);
        tutorialBodyContent.text=SupportLayout(text,localizedStyle,textWidth,HandsOn.Complete);
        // All copy shares one measured scroll area, so heading/name/body cannot overlap.
        Rect viewport=HandsOnTextViewport;
        float headingHeight=localizedStyle.CalcHeight(new GUIContent(titleText),textWidth);
        float bodyHeight=localizedStyle.CalcHeight(tutorialBodyContent,textWidth);
        string name=HandsOn.GermanName;
        float nameHeight=string.IsNullOrEmpty(name)?0:nameStyle.CalcHeight(new GUIContent(name),textWidth)+14;
        float height=headingHeight+16+bodyHeight+nameHeight;
        tutorialScroll=GUI.BeginScrollView(viewport,tutorialScroll,new Rect(0,0,textWidth,Mathf.Max(height,viewport.height)));
        GUI.Label(new Rect(0,0,textWidth,headingHeight),titleText,localizedStyle);
        if(nameHeight>0) GUI.Label(new Rect(0,headingHeight+12,textWidth,nameHeight-14),name,nameStyle);
        GUI.Label(new Rect(0,headingHeight+16+nameHeight,textWidth,bodyHeight),tutorialBodyContent,localizedStyle);
        GUI.EndScrollView();
        float progress=(float)(int)HandsOn.Step/HandsOn.StepCount;
        Fill(new Rect(card.x+16,card.yMax-16,card.width-32,3),new Color(.16f,.28f,.32f));
        Fill(new Rect(card.x+16,card.yMax-16,(card.width-32)*progress,3),mint);
        Fill(HandsOnSkipRect,new Color(.15f,.22f,.26f));SupportLabel(HandsOnSkipRect,"Skip",true);
        if (HandsOn.Complete)
        {Fill(HandsOnFinishRect,new Color(.1f,.43f,.36f));SupportLabel(HandsOnFinishRect,"Start lesson",true);return;}
        string key="";Rect action=default;
        string control = HandsOn.Step==HandsOnTutorial.PracticeStep.Name?"Hear":HandsOn.Step==HandsOnTutorial.PracticeStep.Pickup?"Interact":
            HandsOn.Step==HandsOnTutorial.PracticeStep.Rotate?"Rotate":HandsOn.Step==HandsOnTutorial.PracticeStep.Place?"Place":"";
        if (!string.IsNullOrEmpty(control))
        {
            if (controls.UsesTouch)
            {
                Rect screen=control=="Hear"?mobile.Layout.HearButton:control=="Interact"?mobile.Layout.InteractButton:
                    control=="Rotate"?mobile.Layout.RotateButton:mobile.Layout.PlaceButton;
                if (screen.width>0) {Vector2 point=ToUI(new Vector2(screen.xMin,screen.yMax));action=new Rect(point.x,point.y,screen.width/UIScale,screen.height/UIScale);}
            }
            else
            {
                action=control=="Hear"?HearRect:new Rect(area.width-104,area.height-180,84,76);
                key=control=="Hear"?"ANHÖREN\nF":control=="Interact"?"AUFHEBEN\nE":control=="Rotate"?"DREHEN\nR":"ABLEGEN\nG";
                DrawActionCircle(action,key);
            }
            if (action.width>0)
            {
                Fill(new Rect(action.x-4,action.y-4,action.width+8,3),mint);Fill(new Rect(action.x-4,action.yMax+1,action.width+8,3),mint);
                Fill(new Rect(action.x-4,action.y-4,3,action.height+8),mint);Fill(new Rect(action.xMax+1,action.y-4,3,action.height+8),mint);
            }
        }
    }

    private string TutorialText
    {
        get
        {
            bool touch = controls.UsesTouch;
            switch (tutorialPage)
            {
                case 0: return "New here? Choose How to play in the menu. We'll show you around, one step at a time. You can leave the guide and come back later. This reference is paused; use Next, Back or Skip.";
                case 1: return "Look around by dragging the right side of the screen or moving the mouse. The mark in the middle helps you point at things. You can look around next to walls normally; the camera keeps nearby walls visible automatically, with no extra touch gesture or key. Look down to see your character’s body and hands, or look in the bathroom mirror to see the whole character. Your character’s steps follow your movement, including moving backwards and sideways. No extra buttons are needed.";
                case 2: return "To sit, swipe down and hold briefly. Let go and repeat to lie down; swipe up and hold to get up. On a keyboard, use C to sit or lie down and X to get up. Move away from low furniture if you cannot stand.";
                case 3: return "Look at a nearby object and tap AUFHEBEN to pick it up, or press E. The same button opens and closes doors and drawers. You can carry one thing at a time. Your hand holds the object automatically; cutlery is held by its handle.";
                case 4: return "Curious about something nearby? Tap it to see its German name. On a computer, aim at it and click; hold Alt if you prefer to move the pointer. You can also use Anh\u00f6ren or F. Looking up a name does not pick up or open the object.";
                case 5: return "Want to know the name of a part, such as a handle? Look at it and tap DETAIL, or press Shift + F. The button appears when that part has its own name.";
                case 6: return "To put something down, look at a nearby tabletop or shelf and tap ABLEGEN, or press G. Wait for your hand to let go before doing the next thing.";
                case 7: return "While holding something, tap DREHEN or press R to turn it. Try turning it before you put it down.";
                case 8: return "Lost your way? Tap the map to make it bigger. It shows where you are and helps you find your way through the rooms. Tap again to return to the game.";
                case 9: return "The task appears briefly in the centre. Read the large German instruction, then your translation underneath. Hold a finger on the notice to take more time; drag to read longer text. On a computer, hold Alt and point at the notice to keep it open; use the mouse wheel to scroll. Tap or click the visible X in the top-right corner, or anywhere outside the notice, to close it immediately. That press only dismisses the notice. Tap the small task card with the replay icon to show it again and repeat German; on desktop use Alt and click. Settings changes language or translation; every 50 completed tasks you can choose again. Help never gives answers or completes tasks.";
                case 10: return "The task appears briefly in the centre. Read the large German instruction, then your translation underneath. Hold a finger on the notice to take more time; drag to read longer text. On a computer, hold Alt and point at the notice to keep it open; use the mouse wheel to scroll. Tap or click the visible X in the top-right corner, or anywhere outside the notice, to close it immediately. That press only dismisses the notice. Tap the small task card with the replay icon to show it again and repeat German; on desktop use Alt and click. Settings changes language or translation; every 50 completed tasks you can choose again. Help never gives answers or completes tasks.";
                case 11: return touch ? "During reviews, Ziel zeigen reveals the target on the map. The next review hides the hint again." : "During reviews, the target is hidden at first. Ziel zeigen (H on keyboard) reveals its current position on the map. The next review hides the hint again.";
                case 12: return "Later placement and scenario tasks offer Neu anordnen. Use it to restore the practice setup and retry the same task.";
                case 13: return "Later scenarios show instructions and the current situation here. Follow their rules and requested actions. These controls are previews when the current task does not use them.";
                case 14: return touch ? "When a scenario asks for confirmation, tap its Bestätigen button after doing the requested action." : "When a scenario asks for confirmation, its button appears here. Tap it or press T. Only confirm after performing the requested action.";
                case 15: return touch ? "Open Fall lesen / hören. Read the required pages, tap Anhören to listen, Weiter or Zurück to change pages and Zur Aufgabe to return." : "Some later tasks offer Fall lesen / hören. Open the case, read each required page, use Anhören to listen, and use Weiter or Zurück to change pages. Return with Zur Aufgabe. Arrow keys also change pages.";
                case 16: return touch ? "After the required action, tap one of the three answers. You can retry a mistake; this guide does not reveal answers." : "After the required action, a question may appear with three choices. Tap or click an answer, or press 1, 2 or 3. A mistake lets you try again; this tour does not reveal answers.";
                case 17: return touch ? "Tap Menu to open Pause and Musik." : "Tap or click Menu to open the quick controls for Pause and Musik.";
                case 18: return touch ? "Tap Pause to open the main menu. The world pauses; Start / Continue resumes." : "Choose Pause to open the main menu. On a keyboard, Escape also pauses. Pausing freezes movement and world animation; Start / Continue resumes.";
                case 19: return "Musik toggles background music separately from the spoken German instructions. Your choice is saved.";
                case 20: return "Settings changes your support language, bilingual display, text/voice mode and sound tests. Translation starts enabled; every 50 completed tasks you can choose again. Instructions appear briefly in the centre with translation below German, then fade together. Tap the small task-number card to repeat; on desktop hold Alt and click it. Progress saves locally. Start / Continue resumes; restarting asks for confirmation. How to play and Controls reference replay training in your selected language.";
                default: return "";
            }
        }
    }

    public Rect TutorialHighlightRect
    {
        get
        {
            Rect area = UIArea;
            Rect safe = SafeScreenArea;
            bool touch = controls.UsesTouch;
            if (tutorialFocusFrame != Time.frameCount || tutorialFocusPage != tutorialPage ||
                tutorialFocusTouch != touch || !tutorialFocusArea.Equals(area) || !tutorialFocusSafeArea.Equals(safe))
            {
                tutorialFocusFrame = Time.frameCount; tutorialFocusPage = tutorialPage;
                tutorialFocusTouch = touch; tutorialFocusArea = area; tutorialFocusSafeArea = safe;
                tutorialFocus = ComputeTutorialHighlight(area);
            }
            return tutorialFocus;
        }
    }

    private Rect ComputeTutorialHighlight(Rect area)
    {
        if (tutorialPage == 0) return controls.UsesTouch ? TouchTutorialRect("Move") : new Rect(24, area.height - 146, 160, 100);
        if (tutorialPage <= 2) return new Rect(area.width / 2 - 44, area.height / 2 - 44, 88, 88);
        if (tutorialPage == 3) return controls.UsesTouch ? TouchTutorialRect("Interact") : new Rect(area.width - 94, area.height - 124, 76, 76);
        if (tutorialPage == 4) return controls.UsesTouch ? TouchTutorialRect("Hear") : HearRect;
        if (tutorialPage == 5) return controls.UsesTouch ? TouchTutorialRect("Part") : PartRect;
        if (tutorialPage == 6) return controls.UsesTouch ? TouchTutorialRect("Place") : new Rect(area.width - 180, area.height - 124, 76, 76);
        if (tutorialPage == 7) return controls.UsesTouch ? TouchTutorialRect("Rotate") : new Rect(area.width - 266, area.height - 124, 76, 76);
        switch (tutorialPage)
        {
            case 8: return MinimapRect;
            case 9: return TaskRect;
            case 10: return TaskRect;
            case 11: return ReviewHintRect;
            case 12: return PlacementResetRect;
            case 13: return ScenarioStatusRect;
            case 14: return ScenarioConfirmRect;
            case 15: return CaseButtonRect;
            case 16: return GrammarChoiceRect(0);
            case 18: return PauseRect;
            case 19: return MusicRect;
            default: return QuickMenuRect;
        }
    }

    private Rect TouchTutorialRect(string control)
    {
        Rect screen = mobile.TutorialScreenRect(control);
        Vector2 topLeft = ToUI(new Vector2(screen.xMin, screen.yMax));
        return new Rect(topLeft.x, topLeft.y, screen.width / UIScale, screen.height / UIScale);
    }

    private string TutorialAction
    {
        get
        {
            bool touch = controls.UsesTouch;
            switch (tutorialPage)
            {
                case 0: return touch ? "Walk: drag the joystick." : "Walk: use W A S D or arrow keys.";
                case 1: return touch ? "Look: drag empty space. Aim with the centre mark." : "Look: move the mouse. Aim with the centre mark.";
                case 2: return "Swipe down and hold briefly to sit. On a keyboard, press C.";
                case 3: return touch ? "Aim first, then tap AUFHEBEN or BENUTZEN." : "Aim first, then press E to pick up or use.";
                case 4: return touch ? "ANHÖREN names the whole object." : "F / Anhören names the whole object.";
                case 5: return touch ? "DETAIL names the part you are aiming at." : "Shift + F / Detail names the part you are aiming at.";
                case 6: return touch ? "Carry, aim at a surface, then tap ABLEGEN." : "Carry, aim at a surface, then press G.";
                case 7: return touch ? "Tap DREHEN to turn the carried object 45 degrees." : "Press R to turn the carried object 45 degrees.";
                case 8: return touch ? "Tap the map to enlarge or close it." : "Click the map to enlarge or close it.";
                case 9: return "Tap the small task-number card to repeat the temporary bilingual instruction. On desktop, hold Alt and click it.";
                case 10: return "Tap the small task-number card to repeat the temporary bilingual instruction. On desktop, hold Alt and click it.";
                case 11: return touch ? "Review hint: tap Ziel zeigen to reveal the target." : "Review hint: Ziel zeigen / H reveals the target.";
                case 12: return "Neu anordnen resets the practice, not your progress.";
                case 13: return "Read the scenario status before taking action.";
                case 14: return touch ? "Do the action first. Then tap Bestätigen." : "Do the action first. Then confirm with the button / T.";
                case 15: return "Open Fall lesen / hören and read the required pages.";
                case 16: return touch ? "Do the task first. Then tap an answer." : "Do the task first. Then choose with a click or 1 / 2 / 3.";
                case 17: return touch ? "Tap Menu for the quick controls." : "Click Menu for the quick controls.";
                case 18: return touch ? "Tap Pause to open the main menu." : "Pause / Escape opens the main menu.";
                case 19: return "Musik changes background music only.";
                default: return "Replay this reference anytime from Controls reference.";
            }
        }
    }

    // Prefer a roomy side on short landscape screens, keeping the spotlight unobstructed.
    public static Rect LayoutTutorialCard(Rect area, Rect focus)
    {
        Rect best = default;
        float bestScore = -1;
        Consider(new Rect(14, 14, area.width - 28, focus.yMin - 26));
        Consider(new Rect(14, focus.yMax + 12, area.width - 28, area.height - focus.yMax - 26));
        Consider(new Rect(14, 14, focus.xMin - 26, area.height - 28));
        Consider(new Rect(focus.xMax + 12, 14, area.width - focus.xMax - 26, area.height - 28));
        return best;

        void Consider(Rect space)
        {
            float width = Mathf.Min(420, space.width), height = Mathf.Min(320, space.height);
            if (width < 220 || height < 140) return;
            float score = width * height;
            if (score <= bestScore) return;
            bestScore = score;
            best = new Rect(Mathf.Clamp(focus.center.x - width / 2, space.xMin, space.xMax - width),
                Mathf.Clamp(focus.center.y - height / 2, space.yMin, space.yMax - height), width, height);
        }
    }
    private Rect TutorialCard => LayoutTutorialCard(UIArea, TutorialHighlightRect);
    private float TutorialButtonY => TutorialCard.yMax - (TutorialCard.height < 240 ? 54 : 60);
    public Rect TutorialNextRect => new Rect(TutorialCard.xMax - 16 - Mathf.Min(104, TutorialCard.width - 164), TutorialButtonY, Mathf.Min(104, TutorialCard.width - 164), 44);
    public Rect TutorialBackRect => new Rect(TutorialCard.x + 16, TutorialButtonY, 54, 44);
    public Rect TutorialSkipRect => new Rect(TutorialCard.x + 78, TutorialButtonY, 54, 44);

    private void DrawTutorial(Rect area)
    {
        Rect focus = TutorialHighlightRect;
        focus = Rect.MinMaxRect(Mathf.Max(0, focus.xMin - 6), Mathf.Max(0, focus.yMin - 6), Mathf.Min(area.width, focus.xMax + 6), Mathf.Min(area.height, focus.yMax + 6));
        // Four panels leave a real transparent hole over the control being explained.
        Color shade = new Color(.005f, .015f, .025f, .83f);
        Rect safe = SafeScreenArea;
        float left = -safe.xMin / UIScale, top = -(Screen.height - safe.yMax) / UIScale;
        float right = left + Screen.width / UIScale, bottom = top + Screen.height / UIScale;
        Fill(Rect.MinMaxRect(left, top, right, focus.yMin), shade);
        Fill(Rect.MinMaxRect(left, focus.yMax, right, bottom), shade);
        Fill(Rect.MinMaxRect(left, focus.yMin, focus.xMin, focus.yMax), shade);
        Fill(Rect.MinMaxRect(focus.xMax, focus.yMin, right, focus.yMax), shade);
        Fill(new Rect(focus.xMin,focus.yMin,focus.width,2),mint);
        Fill(new Rect(focus.xMin,focus.yMax-2,focus.width,2),mint);
        Fill(new Rect(focus.xMin,focus.yMin,2,focus.height),mint);
        Fill(new Rect(focus.xMax-2,focus.yMin,2,focus.height),mint);
        Rect card = TutorialCard;
        bool compact = card.height < 240;
        DrawTutorialLink(focus, card);
        Fill(new Rect(card.x + 4, card.y + 6, card.width, card.height), new Color(0, 0, 0, .28f));
        Fill(card, panel);
        Fill(new Rect(card.x, card.y, 4, card.height), mint);
        string section = tutorialPage < 8 ? "BASICS" : tutorialPage < 13 ? "LESSON TOOLS" : tutorialPage < 17 ? "LATER TASKS" : "MENU & SOUND";
        SupportLabel(new Rect(card.x+16,card.y+(compact?6:10),card.width-106,22),section);
        GUI.Label(new Rect(card.xMax-88,card.y+(compact?6:10),72,18),(tutorialPage+1)+" / "+TutorialStepCount,small);
        // Keep title, action and explanation in one scroll area on short screens too.
        var referenceStyle=SupportStyle();
        Rect viewport=new Rect(card.x+16,card.y+42,card.width-32,Mathf.Max(24,TutorialButtonY-card.y-60));
        referenceTextViewport=viewport;
        float textWidth=viewport.width-20;
        string heading=SupportLayout(tutorialTitles[tutorialPage],referenceStyle,textWidth);
        tutorialLeadContent.text=SupportLayout(TutorialAction,referenceStyle,textWidth);
        tutorialBodyContent.text=SupportLayout(TutorialText,referenceStyle,textWidth);
        float headingHeight=referenceStyle.CalcHeight(new GUIContent(heading),textWidth);
        float leadHeight=referenceStyle.CalcHeight(tutorialLeadContent,textWidth);
        float bodyHeight=referenceStyle.CalcHeight(tutorialBodyContent,textWidth);
        float height=headingHeight+12+leadHeight+12+bodyHeight;
        tutorialScroll=GUI.BeginScrollView(viewport,tutorialScroll,new Rect(0,0,textWidth,Mathf.Max(height,viewport.height)));
        GUI.Label(new Rect(0,0,textWidth,headingHeight),heading,referenceStyle);
        GUI.Label(new Rect(0,headingHeight+12,textWidth,leadHeight),tutorialLeadContent,referenceStyle);
        GUI.Label(new Rect(0,headingHeight+12+leadHeight+12,textWidth,bodyHeight),tutorialBodyContent,referenceStyle);
        GUI.EndScrollView();
        float segment = (card.width - 32) / TutorialStepCount;
        for (int i = 0; i < TutorialStepCount; i++)
            Fill(new Rect(card.x+16+i*segment,card.yMax-(compact?60:74),segment-2,3),i<=tutorialPage?mint:new Color(.16f,.28f,.32f));
        Fill(TutorialBackRect, new Color(.08f,.17f,.21f));
        Color previous = GUI.color;
        if (tutorialPage == 0) GUI.color = new Color(1, 1, 1, .35f);
        SupportLabel(TutorialBackRect,"Back",true); GUI.color = previous;
        Fill(TutorialSkipRect,new Color(.08f,.17f,.21f)); SupportLabel(TutorialSkipRect,"Skip",true);
        Fill(TutorialNextRect,new Color(.1f,.43f,.36f)); SupportLabel(TutorialNextRect,tutorialPage+1==TutorialStepCount?"Finish":"Next",true);
    }

    private void DrawTutorialLink(Rect focus, Rect card)
    {
        Vector2 from, to;
        if (card.yMin >= focus.yMax)
        { from = new Vector2(focus.center.x, focus.yMax); to = new Vector2(Mathf.Clamp(from.x,card.x+12,card.xMax-12),card.y); }
        else if (card.yMax <= focus.yMin)
        { from = new Vector2(focus.center.x, focus.yMin); to = new Vector2(Mathf.Clamp(from.x,card.x+12,card.xMax-12),card.yMax); }
        else if (card.xMin >= focus.xMax)
        { from = new Vector2(focus.xMax, focus.center.y); to = new Vector2(card.x,Mathf.Clamp(from.y,card.y+12,card.yMax-12)); }
        else
        { from = new Vector2(focus.xMin, focus.center.y); to = new Vector2(card.xMax,Mathf.Clamp(from.y,card.y+12,card.yMax-12)); }
        Matrix4x4 before = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
        Fill(new Rect(from.x,from.y-1,Vector2.Distance(from,to),2),mint);
        GUI.matrix = before;
    }

    private void DrawTutorialControl()
    {
        Rect rect = TutorialHighlightRect;
        bool touch = controls.UsesTouch;
        string caption;
        switch (tutorialPage)
        {
            case 0: caption = touch ? "BEWEGEN" : "W A S D"; break;
            case 1: caption = "+"; break;
            case 2: caption = touch ? "Swipe + hold" : "C / X"; break;
            case 3: caption = touch ? "AUFHEBEN" : "AUFHEBEN\nE"; break;
            case 4: caption = touch ? "ANHÖREN" : "ANHÖREN\nF"; break;
            case 5: caption = touch ? "DETAIL" : "DETAIL\nShift + F"; break;
            case 6: caption = touch ? "ABLEGEN" : "ABLEGEN\nG"; break;
            case 7: caption = touch ? "DREHEN" : "DREHEN\nR"; break;
            case 11: caption = "Ziel zeigen"; break;
            case 12: caption = "Neu anordnen"; break;
            case 13: caption = "Scenario status\nRead the instruction, then do the action."; break;
            case 14: caption = "Bestätigen"; break;
            case 15: caption = "Fall lesen / hören"; break;
            case 16: caption = touch ? "Tap an answer here after the required action." : "Answer choices appear here after the required action.\n1     2     3"; break;
            case 18: caption = "Pause"; break;
            case 19: caption = "Musik"; break;
            default: return;
        }
        if (tutorialPage >= 3 && tutorialPage <= 7 || tutorialPage == 0 && touch)
            DrawActionCircle(rect, caption);
        else { Fill(rect,panel); GUI.Label(rect,caption,small); }
    }

    private float TaskNoticeDuration => TaskNoticeTiming.Duration(player.Learning.TaskSpeech.Length+
        (SupportLanguages.Bilingual?SupportLanguages.Text(TaskSupportSource).Length:0));
    private void DrawTransition() => DrawTaskNotice();

    private void OnGUI()
    {
        Styles();
        Matrix4x4 before = GUI.matrix;
        Color previousColor = GUI.color;
        Rect safe = SafeScreenArea;
        GUI.matrix = Matrix4x4.TRS(new Vector3(safe.xMin, Screen.height - safe.yMax, 0), Quaternion.identity, Vector3.one * UIScale);
        GUI.color = Color.white;
        Rect area = UIArea;
        if (languageChoiceOpen || languageCheckpointOpen)
        {
            if (languageChoiceOpen) DrawLanguageChoice(area); else DrawLanguageCheckpoint(area);
            GUI.matrix = before; GUI.color = previousColor; return;
        }
        if (HandsOnActive)
        {
            DrawHandsOn(area);
            GUI.matrix = before; GUI.color = previousColor; return;
        }
        if (MenuOpen)
        {
            Fill(new Rect(-safe.xMin / UIScale, -(Screen.height - safe.yMax) / UIScale, Screen.width / UIScale, Screen.height / UIScale), new Color(.025f, .065f, .09f, .97f));
            if (settingsOpen) DrawLanguageSettings(area);
            else
            {
                bool menuEnabled=GUI.enabled;
                GUI.enabled=menuEnabled && !resetConfirmation;
                float width = Mathf.Min(380, area.width - 36), x = (area.width - width) / 2;
                float viewportHeight = Mathf.Min(478, area.height - 16);
                float top = Mathf.Max(8, (area.height - viewportHeight) / 2);
                menuTextViewport=new Rect(0,top,area.width,viewportHeight);
                menuScroll = GUI.BeginScrollView(menuTextViewport, menuScroll,
                    new Rect(0, 0, area.width - 20, 472));
                x -= 10;
                float y = 0;
                Fill(new Rect(area.width / 2 - 38, y, 56, 4), mint);
                GUI.Label(new Rect(x, y + 14, width, 54), "DeutschRoom", title);
                GUI.Label(new Rect(x, y + 68, width, 36), player.Learning.CourseName +
                    (player.Voice.IntroAudioPending ? "\nText · Neue Stimme folgt" : ""), small);
                if (MenuButton(new Rect(x, y + 108, width, 44), SupportLanguages.Text("Start / Continue"), true)) Begin();

                if (MenuButton(new Rect(x, y + 160, width, 44), SupportLanguages.Text("Settings"))) settingsOpen = true;
                if (MenuButton(new Rect(x, y + 264, width, 44), SupportLanguages.Text(controls.UsesTouch?"Controls: touch":"Controls: mouse + keyboard")))
                    controls.SetMode(controls.UsesTouch ? PlayerControls.ControlMode.KeyboardAndMouse : PlayerControls.ControlMode.Touch);
                if (MenuButton(new Rect(x, y + 316, width, 44), SupportLanguages.Text(music.MusicEnabled?"Music on":"Music off"))) music.Toggle();
                if (MenuButton(new Rect(x, y + 212, width, 44), SupportLanguages.Text("How to play"))) OpenTutorial();
                if (MenuButton(new Rect(x, y + 368, width, 44), SupportLanguages.Text("Controls reference"))) OpenControlsReference();
                if (MenuButton(new Rect(x, y + 420, width, 44), SupportLanguages.Text("Restart course"))) resetConfirmation=true;
                GUI.EndScrollView();
                GUI.enabled=menuEnabled;
                if(resetConfirmation)
                {
                    Fill(new Rect(0,0,area.width,area.height),new Color(.015f,.025f,.045f,.98f));
                    float w=Mathf.Min(420,area.width-36),left=(area.width-w)/2,topY=Mathf.Max(16,(area.height-248)/2);
                    Fill(new Rect(left-10,topY-10,w+20,248),panel);
                    GUI.Label(new Rect(left,topY,w,42),SupportLayout("Restart this course?",SupportStyle(true),w),SupportStyle(true));
                    GUI.Label(new Rect(left+12,topY+52,w-24,60),SupportLayout("Your saved course progress will be cleared.",SupportStyle(),w-24),SupportStyle());
                    if(MenuButton(new Rect(left,topY+124,w,44),SupportLanguages.Text("Keep my progress"),true))resetConfirmation=false;
                    if(MenuButton(new Rect(left,topY+178,w,44),SupportLanguages.Text("Start over")))
                    {LearningProgress.ClearSavedProgress();SupportLanguages.ResetCheckpoints();SceneManager.LoadScene(gameObject.scene.path);}
                }
            }
        }
        else
        {
            Fill(QuickMenuRect, panel);
            SupportLabel(QuickMenuRect,"Menu",true);
            if (quickMenuOpen)
            {
                Fill(PauseRect, panel);
                SupportLabel(PauseRect,"Pause",true);
                Fill(MusicRect, panel);
                GUI.Label(MusicRect, SupportLanguages.Text(music.MusicEnabled?"Music on":"Music off"), button);
            }
            if (!controls.UsesTouch && !mapExpanded)
            {
                if (player.CanIdentify) DrawActionCircle(HearRect, "Anhören");
                if (player.CanIdentifyPart) DrawActionCircle(PartRect, "Detail");
            }
            DrawTaskBadge();
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
                    player.Learning.ScenarioSession.Status, small);
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
                UiSurfaceBox(GrammarPanel);
                var grammar = player.Learning.GrammarTask;
                var dialogue = DialogueOpen ? player.Learning.ScenarioSession.CurrentStep : null;
                string question = dialogue != null ? dialogue.question : grammar.question;
                string feedback = dialogue != null ? player.Learning.ScenarioSession.DialogueFeedback : player.Learning.GrammarFeedback;
                string[] choices = dialogue != null ? dialogue.choices : grammar.choices;
                GUI.Label(new Rect(GrammarPanel.x + 12, GrammarPanel.y + 8, GrammarPanel.width - 24, ScenarioAnswers ? 62 : 90), question, subtitle);
                GUI.Label(new Rect(GrammarPanel.x + 12, GrammarPanel.y + (ScenarioAnswers ? 72 : 98), GrammarPanel.width - 24, ScenarioAnswers ? 40 : 48), feedback, small);
                for (int i = 0; i < 3; i++)
                {
                    Rect choiceRect=GrammarChoiceRect(i);UiSurfaceBox(choiceRect,choiceRect.Contains(Event.current.mousePosition));
                    string choice=controls.UsesTouch?choices[i]:(i+1)+" · "+choices[i];var style=answerChoice;style.fontSize=small.fontSize;
                    while(style.fontSize>10&&style.CalcHeight(new GUIContent(choice),choiceRect.width)>choiceRect.height-4)style.fontSize--;
                    GUI.Label(choiceRect,choice,style);
                }
            }

            if (presentation.ShowAudioWarning && !ModalOpen)
            {
                float width = Mathf.Min(540, area.width - 36);
                Rect warning = new Rect((area.width - width) / 2, 180, width, 58);
                Fill(warning, new Color(.35f, .19f, .06f, .98f));
                GUI.Label(new Rect(warning.x + 12, warning.y + 4, warning.width - 24, 50), presentation.WarningText + "  Einstellungen öffnen", small);
            }
            if (!mapExpanded && !ModalOpen && !player.Learning.IsScenarioTask) DrawCaption(area);
            if (!mapExpanded && !ModalOpen && (presentation.ShowText || player.PointerNameVisible) && !string.IsNullOrEmpty(player.IdentifiedName))
            {
                float width = Mathf.Min(360, area.width - 36);
                Rect name = new Rect((area.width - width) / 2, Mathf.Max(180, CaptionRect.y - 64), width, 58);
                Fill(name, panel);
                GUI.Label(name, player.IdentifiedName, player.IdentifiedName.Contains("\n") ? partNameStyle : nameStyle);
            }
        }
        if (!MenuOpen && (!ModalOpen || tutorialOpen))
        {
            if (mapExpanded) Fill(new Rect(0, 80, area.width, area.height - 80), new Color(.025f, .065f, .09f, .97f));
            minimap.Draw(MinimapRect);
        }
        if(!MenuOpen && !TutorialOpen && !mapExpanded && !ModalOpen) DrawTransition();
        if (tutorialOpen)
        {
            DrawTutorialControl();
            DrawTutorial(area);
        }
        GUI.matrix = before;
        GUI.color = previousColor;
    }

    private void DrawAudioSettings(Rect area)
    {
        float width = Mathf.Min(480, area.width - 36), x = (area.width - width) / 2;
        float y = Mathf.Max(12, (area.height - 400) / 2);
        SupportLabel(new Rect(x, y, width, 42), "Text & voice");
        string[] labels = { "Text only", "Voice only", "Both" };
        float optionWidth = (width - 16) / 3;
        for (int i = 0; i < 3; i++)
            if (MenuButton(new Rect(x + i * (optionWidth + 8), y + 52, optionWidth, 54), SupportLanguages.Text(labels[i]), (int)presentation.Mode == i))
            {
                presentation.SetMode((LessonPresentation.OutputMode)i);
                player.Voice.Stop();
            }
        SupportLabel(new Rect(x, y + 114, width, 28), SupportLanguages.Text("Voice")+": "+Mathf.RoundToInt(presentation.VoiceVolume*100)+"%",false,false);
        float volume = GUI.HorizontalSlider(new Rect(x + 16, y + 150, width - 32, 28), presentation.VoiceVolume, 0, 1);
        if (Mathf.Abs(volume - presentation.VoiceVolume) > .001f) presentation.SetVoiceVolume(volume);
        if (MenuButton(new Rect(x, y + 184, width, 48), SupportLanguages.Text("Test voice"))) player.Voice.PlayTest();
        if (MenuButton(new Rect(x, y + 242, (width - 10) / 2, 48), SupportLanguages.Text("I cannot hear sound"))) { presentation.ReportNoSound(true); player.Voice.Stop(); }
        if (MenuButton(new Rect(x + (width + 10) / 2, y + 242, (width - 10) / 2, 48), SupportLanguages.Text("Sound works"))) presentation.ReportNoSound(false);
        string hint = player.Voice.IntroAudioPending ? "Die neue Stimme ist noch nicht verfügbar. Diese Lektionen zeigen Text." :
            presentation.ShowAudioWarning ? presentation.WarningText : (Application.isMobilePlatform ? "Kein Ton? Prüfe Medienlautstärke und den Lautlosmodus deines Telefons." : "Kein Ton? Prüfe Ausgabegerät und Lautstärke.");
        GUI.Label(new Rect(x + 4, y + 296, width - 8, 55), hint, small);
        if (MenuButton(new Rect(x, y + 354, width, 46), SupportLanguages.Text("Save & back"), true))
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
        if(text==player.Learning.TaskSpeech || transitionElapsed<TaskNoticeDuration) {GUI.color=before;return;}
        Rect card = CaptionRect;
        Fill(card, panel);
        GUI.Label(new Rect(card.x + 14, card.y + 7, card.width - 28, card.height - 14), text, subtitle);
        GUI.color = before;
    }

    private void OnDestroy()
    {
        if (HandsOnActive) HandsOn.Finish();
        if (actionCircle != null) Destroy(actionCircle);
        if (player != null && player.Learning != null) player.Learning.TaskChanged -= ShowStageTransition;
        if (MenuOpen || tutorialOpen) Time.timeScale = previousTimeScale;
    }
}
