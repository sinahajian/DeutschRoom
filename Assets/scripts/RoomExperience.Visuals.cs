using UnityEngine;

public sealed partial class RoomExperience
{
    // Shared visual tokens: opaque surfaces keep text contrast independent of the room.
    private static readonly Color UiSurface = new Color(.055f,.078f,.11f,1);
    private static readonly Color UiBorder = new Color(.22f,.29f,.36f,1);
    private static readonly Color UiMuted = new Color(.72f,.79f,.85f,1);
    private static readonly Color UiText = new Color(.96f,.98f,1,1);
    private static readonly Color UiAccent = new Color(.43f,.94f,.78f,1);
    private GUIStyle noticeMetaStyle, noticeMainStyle, noticeTranslationStyle, noticeHintStyle, badgeHintStyle;
    private Rect taskNoticeRect;
    private Vector2 noticeScroll;
    private bool noticeHeld;
    private bool TaskNoticeVisible => !MenuOpen && !TutorialOpen && !ModalOpen && !mapExpanded &&
        !player.Learning.IsPreview && transitionElapsed<TaskNoticeDuration && string.IsNullOrEmpty(player.IdentifiedName);
    private Rect NoticeCloseRect => new Rect(taskNoticeRect.xMax-54,taskNoticeRect.y+10,44,44);

    private static void UiLine(Vector2 start,Vector2 end,Color color,float thickness=2)
    {
        Matrix4x4 before=GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg,start);
        Fill(new Rect(start.x,start.y-thickness/2,Vector2.Distance(start,end),thickness),color);
        GUI.matrix=before;
    }
    private static void UiSurfaceBox(Rect rect,bool emphasized=false)
    {
        Fill(new Rect(rect.x,rect.y+5,rect.width,rect.height),new Color(0,0,0,.22f));
        Fill(rect,emphasized?new Color(.27f,.48f,.47f):UiBorder);
        Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),UiSurface);
    }
    private static void UiCheck(Rect rect,Color color)
    {
        UiLine(new Vector2(rect.x+2,rect.center.y),new Vector2(rect.x+7,rect.yMax-3),color);
        UiLine(new Vector2(rect.x+7,rect.yMax-3),new Vector2(rect.xMax-1,rect.y+3),color);
    }
    private static void UiReplay(Rect rect,Color color)
    {
        Vector2 previous=default;
        for(int i=0;i<=16;i++)
        {
            float angle=(-.2f+i*(Mathf.PI*1.65f)/16);
            Vector2 point=rect.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rect.width*.32f;
            if(i>0)UiLine(previous,point,color,1.7f);
            previous=point;
        }
        UiLine(previous,previous+new Vector2(-5,-1),color,1.7f);
        UiLine(previous,previous+new Vector2(1,-5),color,1.7f);
    }
    private GUIStyle selectionStyle;
    private bool UiSelectionButton(Rect rect,string source,bool selected)
    {
        bool hover=rect.Contains(Event.current.mousePosition);
        string focusName="selection:"+source;
        UiSurfaceBox(rect,selected||hover||GUI.GetNameOfFocusedControl()==focusName);
        if(selected)Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),new Color(.08f,.22f,.20f));
        if(selectionStyle==null)selectionStyle=new GUIStyle(SupportStyle(true)) {fontSize=16,padding=new RectOffset(34,10,0,0)};
        selectionStyle.font=SupportLanguages.FontFor(SupportLanguages.Selected);
        if(selected)UiCheck(new Rect(rect.x+12,rect.center.y-6,14,12),UiAccent);
        GUI.SetNextControlName(focusName);
        return GUI.Button(rect,SupportLayout(source,selectionStyle,rect.width-50),selectionStyle);
    }
    private void DrawTaskBadge()
    {
        bool hover=TaskRect.Contains(Event.current.mousePosition);
        UiSurfaceBox(TaskRect,hover);
        Fill(new Rect(TaskRect.x+1,TaskRect.y+12,3,TaskRect.height-24),UiAccent);
        taskNumber.fontSize=16;
        string number=player.Learning.IsComplete?"Geschafft!":"Aufgabe "+(player.Learning.Stage+1);
        GUI.Label(new Rect(TaskRect.x+12,TaskRect.y+7,TaskRect.width-50,24),number,taskNumber);
        UiReplay(new Rect(TaskRect.xMax-36,TaskRect.y+10,26,26),hover?UiAccent:UiMuted);
        if(badgeHintStyle==null)badgeHintStyle=new GUIStyle(SupportStyle()) {fontSize=12,alignment=TextAnchor.MiddleLeft};
        badgeHintStyle.font=SupportLanguages.FontFor(SupportLanguages.Selected);
        badgeHintStyle.alignment=SupportLanguages.IsRightToLeft?TextAnchor.MiddleRight:TextAnchor.MiddleLeft;
        badgeHintStyle.normal.textColor=UiMuted;
        string hint=SupportLayout("Repeat instruction",badgeHintStyle,TaskRect.width-24);
        GUI.Label(new Rect(TaskRect.x+12,TaskRect.y+29,TaskRect.width-24,19),hint,badgeHintStyle);
        float progress=player.Learning.IsComplete?1:(float)player.Learning.Stage/Mathf.Max(1,player.Learning.TotalStages);
        Fill(new Rect(TaskRect.x+12,TaskRect.yMax-6,TaskRect.width-24,2),UiBorder);
        Fill(new Rect(TaskRect.x+12,TaskRect.yMax-6,(TaskRect.width-24)*progress,2),UiAccent);
    }
    private void DrawTaskNotice()
    {
        if(!TaskNoticeVisible)return;
        if(noticeMetaStyle==null)
        {
            noticeMetaStyle=new GUIStyle(small) {fontSize=13,alignment=TextAnchor.MiddleLeft,wordWrap=false};
            noticeMainStyle=new GUIStyle(subtitle) {fontSize=22,fontStyle=FontStyle.Bold,alignment=TextAnchor.UpperLeft,richText=false};
            noticeTranslationStyle=new GUIStyle(SupportStyle()) {fontSize=18,richText=false};
            noticeHintStyle=new GUIStyle(SupportStyle(true)) {fontSize=12,wordWrap=false};
        }
        noticeMetaStyle.normal.textColor=UiMuted;
        noticeMainStyle.normal.textColor=UiText;
        noticeTranslationStyle.font=noticeHintStyle.font=SupportLanguages.FontFor(SupportLanguages.Selected);
        noticeTranslationStyle.alignment=SupportLanguages.IsRightToLeft?TextAnchor.UpperRight:TextAnchor.UpperLeft;
        noticeTranslationStyle.normal.textColor=UiText;
        noticeHintStyle.normal.textColor=UiMuted;
        Rect area=UIArea;
        float width=Mathf.Min(570,area.width-40),padding=24,textWidth=width-padding*2-18;
        float germanHeight=noticeMainStyle.CalcHeight(new GUIContent(player.Learning.TaskSpeech),textWidth);
        string translated=SupportLanguages.Bilingual?SupportLayout(TaskSupportSource,noticeTranslationStyle,textWidth):"";
        float translationHeight=translated.Length==0?0:noticeTranslationStyle.CalcHeight(new GUIContent(translated),textWidth);
        float supportHeight=translationHeight==0?0:translationHeight+55;
        float contentHeight=germanHeight+supportHeight;
        float height=Mathf.Min(contentHeight+112,Mathf.Min(420,Mathf.Max(150,area.height-160)));
        taskNoticeRect=new Rect((area.width-width)/2,(area.height-height)/2,width,height);
        Color before=GUI.color;
        float alpha=NoticePointerReading?1:Mathf.Min(Mathf.Clamp01(transitionElapsed/.18f),Mathf.Clamp01((TaskNoticeDuration-transitionElapsed)/.5f));
        GUI.color=new Color(before.r,before.g,before.b,before.a*alpha);
        UiSurfaceBox(taskNoticeRect,true);
        Rect chip=new Rect(taskNoticeRect.x+padding,taskNoticeRect.y+16,Mathf.Min(212,width-100),28);
        Fill(chip,new Color(.09f,.17f,.20f));
        Fill(new Rect(chip.x+10,chip.y+10,7,7),UiAccent);
        string step=transitionComplete?"Geschafft!":"Aufgabe "+(transitionStage+1)+" / "+player.Learning.TotalStages;
        GUI.Label(new Rect(chip.x+25,chip.y,chip.width-29,chip.height),step,noticeMetaStyle);
        bool closeHover=NoticeCloseRect.Contains(Event.current.mousePosition);
        // Draw the cross from solid squares: no font glyph or rotated GUI matrix is required.
        UiSurfaceBox(NoticeCloseRect,closeHover);
        Fill(new Rect(NoticeCloseRect.x+2,NoticeCloseRect.y+2,40,40),new Color(.14f,.20f,.25f));
        for(int i=0;i<18;i++)
        {
            float x=NoticeCloseRect.center.x-9+i;
            Fill(new Rect(x,NoticeCloseRect.center.y-9+i,3,3),UiText);
            Fill(new Rect(x,NoticeCloseRect.center.y+8-i,3,3),UiText);
        }
        Rect viewport=new Rect(taskNoticeRect.x+padding,taskNoticeRect.y+58,width-padding*2,height-110);
        noticeScroll=GUI.BeginScrollView(viewport,noticeScroll,new Rect(0,0,textWidth,Mathf.Max(viewport.height,contentHeight)));
        GUI.Label(new Rect(0,0,textWidth,germanHeight),player.Learning.TaskSpeech,noticeMainStyle);
        if(translationHeight>0)
        {
            Fill(new Rect(0,germanHeight+20,textWidth,1),UiBorder);
            string supportHeading=transitionComplete?SupportLanguages.Names[(int)SupportLanguages.Selected]:
                SupportLanguages.Text("Stage")+" "+SupportStageNumber(transitionStage+1);
            var meta=noticeTranslationStyle;int previous=meta.fontSize;meta.fontSize=13;
            meta.normal.textColor=UiAccent;
            GUI.Label(new Rect(0,germanHeight+30,textWidth,22),SupportLayout(supportHeading,meta,textWidth,false),meta);
            meta.fontSize=previous;meta.normal.textColor=UiText;
            GUI.Label(new Rect(0,germanHeight+55,textWidth,translationHeight),translated,meta);
        }
        GUI.EndScrollView();
        string help=noticeHeld?"Take your time":controls.UsesTouch?(contentHeight>viewport.height?"Hold and scroll to read · X or outside to close":"Hold to read · X or outside to close"):"Alt + scroll to read · X or outside to close";
        GUI.Label(new Rect(taskNoticeRect.x+padding,taskNoticeRect.yMax-43,width-padding*2,28),SupportLayout(help,noticeHintStyle,width-padding*2),noticeHintStyle);
        float progress=transitionComplete?1:(float)transitionStage/Mathf.Max(1,player.Learning.TotalStages);
        Fill(new Rect(taskNoticeRect.x+padding,taskNoticeRect.yMax-12,width-padding*2,3),UiBorder);
        Fill(new Rect(taskNoticeRect.x+padding,taskNoticeRect.yMax-12,(width-padding*2)*progress,3),UiAccent);
        GUI.color=before;
    }
    private bool NoticePointerReading => TaskNoticeVisible && (noticeHeld || (!controls.UsesTouch &&
        (Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)) && taskNoticeRect.Contains(ToUI(Input.mousePosition))));
}
