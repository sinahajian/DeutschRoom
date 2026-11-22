using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Synchronous native harness: real interactions, then explicitly settled animation poses.
// It validates the event/settled gate; it does not claim real-time animation/device performance.
public static class ScenarioLessonValidation
{
    private static void Check(bool valid,string text){if(!valid)throw new InvalidOperationException(text);}
    private static void Tick(LessonScenarioSession s)=>s.SendMessage("Update");
    private static PickupItem Item(string name)=>UnityEngine.Object.FindObjectsByType<PickupItem>(FindObjectsInactive.Include).Single(i=>i.name==name);
    private static void Place(PlayerInteraction p,PickupItem i,Vector3 at,Collider surface)
    {
        if(p.HeldItem==null)Check(p.TryPickUp(i),"Cannot take scenario item " + i.name);
        Check(surface.Raycast(new Ray(at+Vector3.up*2,Vector3.down),out RaycastHit hit,4)&&p.TryPlaceHeld(hit),"Cannot place scenario target " + i.name);
    }
    public static void RunTask(PlayerInteraction player,RoomExperience room,IntroLessonCatalog.Task task,int index)
    {
        var l=player.Learning;var session=l.ScenarioSession;
        Check(session!=null&&session.Active&&l.Stage==index,"Missing active scenario.");
        l.SubmitGrammarAnswer(task.id,task.answerIndex);Check(l.Stage==index,"Answer bypassed physical scenario.");
        l.ConfirmScenario();Tick(session);
        Check(!l.GrammarAnswerOpen,"Early confirmation opened answers.");
        if(task.id=="CUR-326")
        {
            session.Devices.Get("washer").Interact(player);Tick(session);
            Check(session.Failed,"Forbidden drum-door interaction was accepted.");
            l.ResetPlacementPractice();Check(!session.Failed&&session.StepIndex==0,"Scenario reset retained failure.");
        }
        if(task.unchangedDevices != null && task.unchangedDevices.Length>0)
        {
            session.Devices.Get(task.unchangedDevices[0]).Interact(player);Tick(session);
            Check(session.Failed,"Forbidden change of preserved device accepted.");
            l.ResetPlacementPractice();Check(!session.Failed,"Reset retained preserved-device failure.");
        }
        if(task.id=="CUR-321")
            Check(!player.TryPickUp(Item("Loeffel_Spoon")),"Spoon was reachable through closed drawer gate.");
        int guard=0;
        while(session.CurrentStep!=null)
        {
            Check(++guard<30,"Scenario step loop did not finish.");
            var step=session.CurrentStep;int before=session.StepIndex;
            switch(step.op)
            {
                case "read":
                    if(!session.CaseKnown){session.CloseCase();Check(session.CaseReaderOpen,"Unread first source skipped by close.");}
                    int pageGuard=0;
                    while(session.CaseReaderOpen)
                    {
                        Check(++pageGuard<10,"Case page loop failed.");
                        if(session.CaseKnown)session.CloseCase();
                        else
                        {
                            float sourceScale=(float)typeof(RoomExperience).GetProperty("UIScale",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(room);
                            var nextPage=room.CasePageButtonRect(2).center;var sourceSafe=RoomExperience.SafeScreenArea;
                            room.TryPointer(new Vector2(sourceSafe.xMin+nextPage.x*sourceScale,sourceSafe.yMax-nextPage.y*sourceScale));
                        }
                    }
                    Tick(session);break;
                case "dialogue":
                    session.SubmitDialogue(task.id,(step.answerIndex+1)%3);Tick(session);
                    Check(session.StepIndex==before&&!string.IsNullOrEmpty(session.DialogueFeedback),"Wrong clarification accepted.");
                    session.SubmitDialogue("stale-task",step.answerIndex);Tick(session);Check(session.StepIndex==before,"Stale dialogue task accepted.");
                    float dialogScale=(float)typeof(RoomExperience).GetProperty("UIScale",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(room);
                    var dialogButton=room.GrammarChoiceRect(step.answerIndex).center;var dialogSafe=RoomExperience.SafeScreenArea;
                    room.TryPointer(new Vector2(dialogSafe.xMin+dialogButton.x*dialogScale,dialogSafe.yMax-dialogButton.y*dialogScale));Tick(session);break;
                case "blocked":
                    session.Devices.Get(step.key).Interact(player);Tick(session);
                    Check(!session.Devices.State(step.key),"Near-start blocked attempt opened the door.");break;
                case "clearance":
                    var blockedDoor=(Openable)session.Devices.Get(step.key);
                    var clearanceController=player.GetComponent<CharacterController>();bool clearanceEnabled=clearanceController.enabled;
                    clearanceController.enabled=false;var clearAt=blockedDoor.LessonControlPoint-blockedDoor.transform.parent.forward*1.2f;clearAt.y=player.transform.position.y;
                    player.transform.position=clearAt;clearanceController.enabled=clearanceEnabled;Tick(session);break;
                case "state":
                    // A preset in the desired state, without a new interaction, cannot satisfy it.
                    session.Devices.SetState(step.key,step.state);Tick(session);
                    Check(session.StepIndex==before,"Preset bypassed a fresh interaction.");
                    session.Devices.SetState(step.key,!step.state);
                    session.Devices.Get(step.key).Interact(player);
                    if(session.Devices.Get(step.key) is Openable)
                    {Tick(session);Check(session.StepIndex==before,"Moving door satisfied settled-state gate.");}
                    session.Devices.SetState(step.key,step.state);Tick(session);break;
                case "allStates":
                    for(int j=step.keys.Length-1;j>=0;j--)
                    {session.Devices.Get(step.keys[j]).Interact(player);session.Devices.SetState(step.keys[j],step.states[j]);}
                    Tick(session);break;
                case "flush":
                    session.Devices.Get(step.key).Interact(player);Tick(session);
                    Check(session.StepIndex==before,"Unfinished flush cycle accepted.");
                    var flush=(LessonDevice)session.Devices.Get(step.key);
                    typeof(LessonDevice).GetField("remaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(flush,.00001f);
                    flush.SendMessage("Update");Tick(session);break;
                case "pickup":Check(player.TryPickUp(Item(step.key)),"Cannot pick scenario target.");Tick(session);break;
                case "place":
                    Vector3 at=l.MapTable.bounds.center;at.y=l.MapTable.bounds.max.y;
                    if(step.zone!="table") {var z=task.zones.Single(z=>z.id==step.zone);at+=new Vector3(z.x,0,z.z);}
                    Place(player,Item(step.key),at,l.MapTable);Tick(session);break;
                case "offTable":
                    var floor=GameObject.Find("Floor").GetComponent<Collider>();
                    Place(player,Item(step.key),new Vector3(1.3f,floor.bounds.max.y,-1.5f),floor);Tick(session);break;
                case "arrange":
                    for(int j=step.keys.Length-1;j>=0;j--)
                    {
                        var z=task.zones.Single(z=>z.id==step.zones[j]);var atZone=l.MapTable.bounds.center+new Vector3(z.x,0,z.z);atZone.y=l.MapTable.bounds.max.y;
                        Place(player,Item(step.keys[j]),atZone,l.MapTable);
                    }
                    Tick(session);break;
                case "confirm":
                    float scale=(float)typeof(RoomExperience).GetProperty("UIScale",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(room);
                    var point=room.ScenarioConfirmRect.center;var safe=RoomExperience.SafeScreenArea;
                    Check(room.TryPointer(new Vector2(safe.xMin+point.x*scale,safe.yMax-point.y*scale)),"Confirm pointer not consumed.");Tick(session);break;
                case "cue":
                    typeof(LessonScenarioSession).GetField("stepTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(session,step.seconds+.01f);Tick(session);break;
                case "hall":
                    var cc=player.GetComponent<CharacterController>();bool enabled=cc.enabled;Vector3 previous=player.transform.position;
                    cc.enabled=false;player.transform.position=new Vector3(1,previous.y,-9.9f);Tick(session);
                    player.transform.position=new Vector3(2.8f,previous.y,-9.9f);Tick(session);player.transform.position=previous;cc.enabled=enabled;break;
            }
            Check(session.StepIndex==before+1&&!session.Failed,"Scenario action did not complete exactly one step: "+task.id+" / "+step.op);
        }
        foreach(string excluded in task.excludedItems??Array.Empty<string>())
        {
            var z=task.zones[0];var into=l.MapTable.bounds.center+new Vector3(z.x,0,z.z);into.y=l.MapTable.bounds.max.y;
            Place(player,Item(excluded),into,l.MapTable);Tick(session);Check(!l.GrammarAnswerOpen,"Excluded item in target zone accepted.");
            var away=l.MapTable.bounds.center+new Vector3(.72f,0,-.38f);away.y=l.MapTable.bounds.max.y;
            Place(player,Item(excluded),away,l.MapTable);Tick(session);
        }
        Check(l.GrammarAnswerOpen&&l.Stage==index,"Scenario completed before report or did not open it.");
        l.SubmitGrammarAnswer(task.id,(task.answerIndex+1)%3);Check(l.Stage==index,"Wrong report advanced.");
        float uiScale=(float)typeof(RoomExperience).GetProperty("UIScale",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(room);
        var button=room.GrammarChoiceRect(task.answerIndex).center;var area=RoomExperience.SafeScreenArea;
        room.TryPointer(new Vector2(area.xMin+button.x*uiScale,area.yMax-button.y*uiScale));
        l.SubmitGrammarAnswer(task.id,task.answerIndex);
        Check(l.Stage==index+1,"Correct report pointer/stale answer guard failed.");
    }
}
