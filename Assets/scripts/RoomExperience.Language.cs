using UnityEngine;

public sealed partial class RoomExperience
{
    private bool languageChoiceOpen, languageCheckpointOpen;
    private bool checkpointPending;
    private Vector2 languageSettingsScroll;
    private GUIStyle supportBody, supportButton;
    private SupportLanguage styledLanguage = (SupportLanguage)(-1);
    public bool LanguageChoiceOpen => languageChoiceOpen;
    public bool LanguageCheckpointOpen => languageCheckpointOpen;

    private GUIStyle SupportStyle(bool centered = false)
    {
        // Copies are cached: a long task must not allocate styles every frame.
        if (supportBody == null || styledLanguage != SupportLanguages.Selected)
        {
            styledLanguage = SupportLanguages.Selected;
            var font = SupportLanguages.FontFor(styledLanguage);
            supportBody = new GUIStyle(tutorialBody) { font = font, fontStyle = FontStyle.Normal,
                alignment = SupportLanguages.IsRightToLeft ? TextAnchor.UpperRight : TextAnchor.UpperLeft,
                wordWrap = false, richText = false };
            supportButton = new GUIStyle(button) { font = font, fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = false };
        }
        return centered ? supportButton : supportBody;
    }
    private string SupportLayout(string source, GUIStyle style, float width, bool translate = true) =>
        SupportTextLayout.Wrap(translate ? SupportLanguages.Text(source) : source, style, width, SupportLanguages.IsRightToLeft);
    private void SupportLabel(Rect rect, string source, bool centered = false, bool translate = true)
    {
        var style = SupportStyle(centered);
        GUI.Label(rect, SupportLayout(source, style, rect.width, translate), style);
    }
    private bool SupportButton(Rect rect,string source,bool primary=false) => MenuButton(rect,SupportLanguages.Text(source),primary);
    private void DrawLanguageChoice(Rect area)
    {
        Fill(area, panel);
        float width = Mathf.Min(480, area.width - 36), left = (area.width-width)/2;
        float top = Mathf.Max(8, (area.height-328)/2);
        GUI.Label(new Rect(left,top,width,38), "DeutschRoom · Language", nameStyle);
        for (int i=0;i<4;i++)
        {
            var language = (SupportLanguage)i;
            var style = new GUIStyle(button) { font = SupportLanguages.FontFor(language), fontStyle = FontStyle.Normal };
            string caption = SupportLanguages.Names[i];
            bool rtl = i==0 || i==2;
            caption = SupportTextLayout.Wrap(caption,style,width-24,rtl);
            var rect = new Rect(left,top+48+i*62,width,52);
            UiSurfaceBox(rect,rect.Contains(Event.current.mousePosition));
            if (GUI.Button(rect,caption,style))
            {
                SupportLanguages.Select(language, player.Learning.Stage);
                languageChoiceOpen=false;
                // A migrated player also sees onboarding in the newly selected language.
                tutorialAutoPending=true;
            }
        }
    }
    private void CheckLanguageCheckpoint()
    {
        if (SupportLanguages.AcknowledgedCheckpoint > SupportLanguageRules.Checkpoint(player.Learning.Stage))
            SupportLanguages.ResetCheckpoints();
        if (!SupportLanguages.HasSelection || TutorialOpen || languageChoiceOpen || languageCheckpointOpen) return;
        if (SupportLanguageRules.ShouldAsk(player.Learning.Stage,player.Learning.TotalStages,SupportLanguages.AcknowledgedCheckpoint))
        {
            languageCheckpointOpen=true;
            SetMenu(true);
        }
        checkpointPending=false;
    }
    private void DrawLanguageCheckpoint(Rect area)
    {
        Fill(area,panel);
        float width=Mathf.Min(540,area.width-36),left=(area.width-width)/2;
        float top=Mathf.Max(12,(area.height-280)/2);
        UiSurfaceBox(new Rect(left-12,top-16,width+24,272),true);
        SupportLabel(new Rect(left,top,width,100),"Keep showing German and your language for the next 50 tasks?");
        if (SupportButton(new Rect(left,top+112,width,58),"Continue with both languages",true)) ResolveLanguageCheckpoint(true);
        if (SupportButton(new Rect(left,top+184,width,58),"Continue in German only")) ResolveLanguageCheckpoint(false);
    }
    private void ResolveLanguageCheckpoint(bool keepBoth)
    {
        SupportLanguages.Acknowledge(player.Learning.Stage,keepBoth);
        languageCheckpointOpen=false;
        SetMenu(false);
        player.Learning.RepeatTask();
    }
    private string SupportStageNumber(int number)
    {
        string text=number.ToString();
        if(!SupportLanguages.IsRightToLeft)return text;
        char zero=SupportLanguages.Selected==SupportLanguage.Persian?'\u06f0':'\u0660';
        char[] digits=text.ToCharArray();
        for(int i=0;i<digits.Length;i++)if(digits[i]>='0' && digits[i]<='9')digits[i]=(char)(zero+digits[i]-'0');
        return new string(digits);
    }
    private int textScrollFinger = -1;
    private int textScrollTarget;
    private Rect referenceTextViewport, menuTextViewport;
    private void HandleHelpTextDrag()
    {
        noticeHeld=false;
        if(Input.touchCount==0)textScrollFinger=-1;
        if (languageChoiceOpen || languageCheckpointOpen || mapExpanded) { textScrollFinger=-1; return; }
        Rect viewport=default;
        int target=0;
        if(TaskNoticeVisible){viewport=taskNoticeRect;target=1;}
        else if (HandsOnActive)
        {
            Rect card=HandsOnCard;
            viewport=HandsOnTextViewport;target=2;
        }
        else if (tutorialOpen) {viewport=referenceTextViewport;target=2;}
        else if (MenuOpen && settingsOpen)
        {
            float width=Mathf.Min(480,UIArea.width-36);
            viewport=new Rect((UIArea.width-width)/2,12,width,UIArea.height-24);target=3;
        }
        else if(MenuOpen && !resetConfirmation){viewport=menuTextViewport;target=4;}
        if (target==0) {textScrollFinger=-1;return;}
        for (int i=0;i<Input.touchCount;i++)
        {
            Touch touch=Input.GetTouch(i);
            if (touch.phase==TouchPhase.Began && textScrollFinger<0 && viewport.Contains(ToUI(touch.position)))
            {textScrollFinger=touch.fingerId;textScrollTarget=target;}
            if (touch.fingerId!=textScrollFinger) continue;
            if(target==1)noticeHeld=touch.phase!=TouchPhase.Ended && touch.phase!=TouchPhase.Canceled;
            if (touch.phase==TouchPhase.Moved && textScrollTarget==target)
            {
                float delta=touch.deltaPosition.y/UIScale;
                if(target==1)noticeScroll.y=Mathf.Max(0,noticeScroll.y+delta);
                else if(target==2)tutorialScroll.y=Mathf.Max(0,tutorialScroll.y+delta);
                else if(target==3)languageSettingsScroll.y=Mathf.Max(0,languageSettingsScroll.y+delta);
                else menuScroll.y=Mathf.Max(0,menuScroll.y+delta);
            }
            if (touch.phase==TouchPhase.Ended || touch.phase==TouchPhase.Canceled) textScrollFinger=-1;
        }
    }
    private string TaskSupportSource => player.Learning.IsComplete ?
        "Well done! You have completed the course. You can keep practising freely." : player.Learning.TaskEnglish;
    private void DrawLanguageSettings(Rect area)
    {
        float width=Mathf.Min(480,area.width-36),x=(area.width-width)/2;
        float viewportHeight=Mathf.Max(100,area.height-24);
        languageSettingsScroll=GUI.BeginScrollView(new Rect(x,12,width,viewportHeight),languageSettingsScroll,new Rect(0,0,width-20,850));
        width-=20;
        SupportLabel(new Rect(0,0,width,40),"Your support language");
        for (int i=0;i<4;i++)
        {
            var language=(SupportLanguage)i;
            var style=new GUIStyle(button) { font=SupportLanguages.FontFor(language),fontStyle=FontStyle.Normal };
            Rect rect=new Rect(0,48+i*48,width,42);
            UiSurfaceBox(rect,language==SupportLanguages.Selected);
            if(language==SupportLanguages.Selected)UiCheck(new Rect(rect.x+16,rect.center.y-8,18,16),UiAccent);
            if (GUI.Button(rect,SupportTextLayout.Wrap(SupportLanguages.Names[i],style,width-20,i==0||i==2),style))
                SupportLanguages.Select(language,player.Learning.Stage);
        }
        float half=(width-10)/2;
        if(UiSelectionButton(new Rect(0,250,half,62),"German + translation",SupportLanguages.Bilingual))SupportLanguages.SetBilingual(true);
        if(UiSelectionButton(new Rect(half+10,250,half,62),"German only",!SupportLanguages.Bilingual))SupportLanguages.SetBilingual(false);
        SupportLabel(new Rect(0,318,width,120),"German instructions and your selected translation appear together in the centre, then disappear. Every 50 completed tasks, choose whether to keep both languages. Change language or translation in Settings.");
        GUI.BeginGroup(new Rect(-x,450,area.width,400));
        DrawAudioSettings(new Rect(0,0,area.width,400));
        GUI.EndGroup();
        GUI.EndScrollView();
    }
}
