using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(250)]
public sealed class LessonScenarioSession : MonoBehaviour
{
    private readonly Dictionary<PickupItem, LessonPlacementScenario.OriginalPose> originals = new Dictionary<PickupItem, LessonPlacementScenario.OriginalPose>();
    private readonly Dictionary<string,bool> originalStates = new Dictionary<string,bool>();
    private readonly Dictionary<string,PickupItem> items = new Dictionary<string,PickupItem>();
    private readonly Dictionary<string,uint> initialCarry = new Dictionary<string,uint>(), initialPlace = new Dictionary<string,uint>(), stepVersions = new Dictionary<string,uint>();
    private readonly Dictionary<string,Vector3> fixedPositions = new Dictionary<string,Vector3>();
    private readonly Dictionary<string,bool> finalStates = new Dictionary<string,bool>();
    private readonly List<(Vector3 point,string label)> labels = new List<(Vector3,string)>();
    private readonly Dictionary<string,uint> unchangedVersions = new Dictionary<string,uint>();
    private readonly HashSet<string> reviewedCases = new HashSet<string>();
    private LessonCaseCatalog caseCatalog;
    private LessonCaseCatalog.Case currentCase;
    private bool caseAcknowledged, dialogueConfirmed;
    private uint blockedAtActivation;
    private bool hasPlayerReturn;
    private Vector3 playerReturnPosition;
    private Quaternion playerReturnRotation;
    public bool CaseReaderOpen { get; private set; }
    public int CasePageIndex { get; private set; }
    public bool HasCase => Active && currentCase != null;
    public bool CaseKnown => HasCase && reviewedCases.Contains(currentCase.id);
    public bool CanCloseCase => caseAcknowledged || CaseKnown;
    public string CaseTitle => currentCase?.title ?? "";
    public int CasePageCount => currentCase?.pages.Length ?? 0;
    public string CurrentCasePage => HasCase ? currentCase.pages[CasePageIndex] : "";
    public bool DialogueOpen => !Failed && CurrentStep?.op == "dialogue";
    public string DialogueFeedback { get; private set; } = "";
    public string ConfirmationLabel => string.IsNullOrEmpty(CurrentStep?.buttonText) ?
        (CurrentStep?.text.Length > 26 ? "Bestätigen" : CurrentStep?.text ?? "Bestätigen") : CurrentStep.buttonText;
    public void OpenCase()
    { if (!HasCase) return; CaseReaderOpen = true; CasePageIndex = 0; player.Voice.Speak(CurrentCasePage); }
    public void SpeakCasePage() { if (CaseReaderOpen) player.Voice.Speak(CurrentCasePage); }
    public void PreviousCasePage()
    { if (!CaseReaderOpen || CasePageIndex == 0) return; CasePageIndex--; SpeakCasePage(); }
    public void NextCasePage()
    {
        if (!CaseReaderOpen) return;
        if (CasePageIndex + 1 < CasePageCount) { CasePageIndex++; SpeakCasePage(); }
        else AcknowledgeCase();
    }
    public void CloseCase() { if (CanCloseCase) AcknowledgeCase(); }
    private void AcknowledgeCase()
    { caseAcknowledged = true; reviewedCases.Add(currentCase.id); CaseReaderOpen = false; player.Voice.Stop(); }
    public void SubmitDialogue(string taskId, int choice)
    {
        if (!Active || task.id != taskId || CaseReaderOpen || !DialogueOpen || dialogueConfirmed) return;
        if (!CurrentStep.AcceptsDialogue(choice))
        { DialogueFeedback = "Die Rückfrage passt noch nicht. Versuche es noch einmal."; player.Voice.Speak(DialogueFeedback, CurrentStep.question); return; }
        dialogueConfirmed = true;
    }
    private IntroLessonCatalog.Task task;
    private PlayerInteraction player;
    private LearningProgress learning;
    private Collider table;
    private LessonDeviceRegistry devices;
    private GameObject markers;
    private Material markerMaterial;
    private int stepIndex;
    private uint stepCarry, stepPlace, stepCycles, forbiddenVersion;
    private float stepTime;
    private bool confirmed, hallWasOutside, readySpoken;
    public LessonDeviceRegistry Devices => devices;
    public bool Active => task != null;
    public bool Failed { get; private set; }
    public int StepIndex => stepIndex;
    public IntroLessonCatalog.ScenarioStep CurrentStep => Active && stepIndex < task.steps.Length ? task.steps[stepIndex] : null;
    public string Status => Failed ? "Die Reihenfolge wurde verletzt. Starte diese Übung mit „Neu anordnen“ erneut." : CurrentStep?.text ?? (Active && !IsReady ? task.instruction : "Wähle die passende Antwort.");
    public bool NeedsConfirmation => !Failed && CurrentStep?.op == "confirm";
    public bool IsReady => Active && !Failed && !CaseReaderOpen && stepIndex == task.steps.Length && FinalConditions();
    public bool TryGetOriginal(PickupItem item, out LessonPlacementScenario.OriginalPose pose) => originals.TryGetValue(item,out pose);
    public bool IsFixed(PickupItem item) => Active && item != null && fixedPositions.ContainsKey(item.name);
    public bool CanPickup(PickupItem item)
    {
        if (!Active || item == null) return true;
        if (IsFixed(item)) return false;
        foreach (var i in task.scenarioItems)
            if (i.name == item.name && i.source != "table") return devices.State(i.source) && devices.Settled(i.source);
        return true;
    }
    public void Prepare(IntroLessonCatalog.Task next, PickupItem[] all, Collider surface, PlayerInteraction actor, LearningProgress progress)
    {
        actor.CancelCarry();
        player=actor; learning=progress; table=surface;
        if (devices==null) devices=LessonDeviceRegistry.Create();
        if (originals.Count==0)
        {
            foreach(var i in all) originals.Add(i,new LessonPlacementScenario.OriginalPose { parent=i.transform.parent,position=i.transform.position,rotation=i.transform.rotation,
                localPosition=i.transform.localPosition,localRotation=i.transform.localRotation,active=i.gameObject.activeSelf });
            foreach(string key in devices.Keys) originalStates.Add(key,devices.State(key));
        }
        Restore(); ClearMarkers(); task=next; Failed=false; readySpoken=false; stepIndex=0;
        currentCase = null; CaseReaderOpen = false; CasePageIndex = 0; caseAcknowledged = false;
        if (!string.IsNullOrEmpty(task.caseId))
        { if (caseCatalog == null) caseCatalog = LessonCaseCatalog.Load(); currentCase = caseCatalog.Get(task.caseId); }
        items.Clear(); fixedPositions.Clear(); initialCarry.Clear();initialPlace.Clear();finalStates.Clear();labels.Clear();
        foreach(var i in all) items.Add(i.name,i);
        foreach(var p in task.presets) devices.SetState(p.key,p.state);
        foreach(var spec in task.scenarioItems) if (!items.ContainsKey(spec.name)) throw new InvalidOperationException("Missing scenario item " + spec.name);
        if (table==null) throw new InvalidOperationException("Missing scenario table.");
        Bounds top=table.bounds;
        // Hide only unrelated tabletop clutter. Furniture and independent room inventories remain.
        foreach(var i in all)
        {
            bool included=Array.Exists(task.scenarioItems,s=>s.name==i.name);
            Bounds b=i.BoundsAt(i.transform.position,i.transform.rotation);
            if(!included && b.min.x<top.max.x && b.max.x>top.min.x && b.min.z<top.max.z && b.max.z>top.min.z &&
                b.min.y<top.max.y+.4f && b.max.y>top.max.y-.1f) i.gameObject.SetActive(false);
        }
        int count=task.scenarioItems.Length;
        foreach(var spec in task.scenarioItems)
        {
            var i=items[spec.name]; i.gameObject.SetActive(true);
            if(spec.source=="table")
            {
                float x=count==1?-.65f:count==2?-.45f+.9f*spec.slot:count==3?-.64f+.64f*spec.slot:-.72f+.48f*spec.slot;
                Vector3 at=new Vector3(top.center.x+x,top.max.y,top.center.z-.38f);
                if(Array.IndexOf(task.fixedItems??Array.Empty<string>(),spec.name)>=0) at=new Vector3(top.center.x,top.max.y,top.center.z);
                if(task.id=="CUR-297") at.z=top.center.z+.2f;
                if (!string.IsNullOrEmpty(spec.zone)) at=Zone(spec.zone).center;
                Put(i,at,null);
            }
            else
            {
                Collider shelf=SourceShelf(spec.source);
                Vector3 at=shelf.bounds.center;at.y=shelf.bounds.max.y;
                at.x += spec.source=="cupboard"?-.25f:(spec.slot==0?-.12f:.12f);
                Transform parent=spec.source=="drawer"?devices.Get("drawer").transform:null;
                Put(i,at,parent);
            }
        }
        foreach(var i in all) {initialCarry[i.name]=i.CarrySequence; initialPlace[i.name]=i.PlacementSequence;}
        foreach(var name in task.fixedItems??Array.Empty<string>()) fixedPositions[name]=items[name].transform.position;
        foreach(var s in task.steps)
        {
            if(s.op=="state") finalStates[s.key]=s.state;
            if(s.op=="allStates") for(int i=0;i<s.keys.Length;i++) finalStates[s.keys[i]]=s.states[i];
        }
        if (!string.IsNullOrEmpty(task.startHeld) && !player.TryPickUp(items[task.startHeld])) throw new InvalidOperationException("Cannot prepare held item.");
        unchangedVersions.Clear();
        foreach (string key in task.unchangedDevices ?? Array.Empty<string>()) unchangedVersions[key]=devices.Sequence(key);
        if (!string.IsNullOrEmpty(task.blockedDevice))
        {
            var door=(Openable)devices.Get(task.blockedDevice);door.RequireLessonClearance(true);blockedAtActivation=door.BlockedByPlayerSequence;
            hasPlayerReturn=true;playerReturnPosition=player.transform.position;playerReturnRotation=player.transform.rotation;
            Vector3 at=door.LessonControlPoint;at.y=player.transform.position.y;
            Vector3 front=door.transform.parent != null ? -door.transform.parent.forward : Vector3.back;
            MovePlayer(at+front*.60f,Quaternion.LookRotation(-front,Vector3.up));
        }
        if(!string.IsNullOrEmpty(task.forbiddenDevice)) forbiddenVersion=devices.Sequence(task.forbiddenDevice);
        DrawMarkers(); BeginStep();Physics.SyncTransforms();
    }
    private Collider SourceShelf(string source)
    {
        string name=source=="drawer"?"Drawer_Bottom":source=="cupboard"?"Cabinet_Base":"Fridge_InteriorShelf";
        var found=UnityEngine.Object.FindObjectsByType<Collider>().Where(c=>c.name==name).OrderBy(c=>source=="fridge"?Mathf.Abs(c.bounds.center.y-1f):c.bounds.center.y).FirstOrDefault();
        if(found==null)throw new InvalidOperationException("Missing scenario source " + name);
        return found;
    }
    private void Put(PickupItem item,Vector3 at,Transform parent)
    {
        Quaternion rotation=item.PlacementRotation(0);Bounds b=item.BoundsAt(Vector3.zero,rotation);
        item.transform.SetParent(parent,true);item.transform.SetPositionAndRotation(at-new Vector3(b.center.x,b.min.y,b.center.z)+Vector3.up*.01f,rotation);
    }
    private void BeginStep()
    {
        confirmed=false;dialogueConfirmed=false;DialogueFeedback="";stepTime=0;stepVersions.Clear();
        var s=CurrentStep;if(s==null)return;
        if(s.op=="state"||s.op=="flush") {stepVersions[s.key]=devices.Sequence(s.key);stepCycles=devices.Cycles(s.key);}
        if(s.op=="allStates")foreach(var key in s.keys)stepVersions[key]=devices.Sequence(key);
        if(s.op=="pickup"||s.op=="place"||s.op=="offTable") {stepCarry=items[s.key].CarrySequence;stepPlace=items[s.key].PlacementSequence;}
        if(s.op=="read") { CaseReaderOpen=true; CasePageIndex=0; }
        hallWasOutside=!InsideHall();
    }
    public void Confirm(string taskId)
    { if(Active && task.id==taskId && NeedsConfirmation && (string.IsNullOrEmpty(CurrentStep.key)||devices.State(CurrentStep.key)==CurrentStep.state))confirmed=true; }
    private void Update()
    {
        if (learning != null && learning.ControlsPracticeActive) return;
        if(!Active||Time.timeScale<=0)return;
        foreach(var pair in unchangedVersions)if(devices.Sequence(pair.Key)!=pair.Value)Fail();
        if(CaseReaderOpen)return;
        if(!string.IsNullOrEmpty(task.forbiddenDevice)&&devices.Sequence(task.forbiddenDevice)!=forbiddenVersion) Fail();
        var s=CurrentStep;
        if(Failed)return;
        if(s!=null)
        {
            if((s.op=="cue"||s.op=="confirm")&&!string.IsNullOrEmpty(s.key)&&devices.State(s.key)!=s.state){Fail();return;}
            stepTime+=Time.deltaTime;
            if(!InsideHall())hallWasOutside=true;
            if(StepSatisfied(s))
            {
                stepIndex++;BeginStep();
                if(CurrentStep!=null) player.Voice.Speak(CurrentStep.text);
            }
        }
        if(IsReady&&!readySpoken) {readySpoken=true;player.Voice.Speak("Wähle die passende Antwort.",task.question);}
    }
    private void Fail()
    { if(Failed)return;Failed=true;player.Voice.Speak(Status); }
    private bool StepSatisfied(IntroLessonCatalog.ScenarioStep s)
    {
        switch(s.op)
        {
            case "state":return LessonScenarioRules.FreshState(devices.Sequence(s.key),stepVersions[s.key],devices.State(s.key),s.state,devices.Settled(s.key));
            case "flush":return devices.Cycles(s.key)!=stepCycles&&devices.Settled(s.key);
            case "pickup":return player.HeldItem==items[s.key]&&items[s.key].CarrySequence!=stepCarry;
            case "place":return items[s.key].PlacementSequence!=stepPlace&&Placed(s.key,s.zone);
            case "offTable":return items[s.key].PlacementSequence!=stepPlace&&FreshPlaced(items[s.key])&&!OnTable(items[s.key]);
            case "arrange":for(int i=0;i<s.keys.Length;i++)if(!Placed(s.keys[i],s.zones[i]))return false;return true;
            case "allStates":for(int i=0;i<s.keys.Length;i++)if(!LessonScenarioRules.FreshState(devices.Sequence(s.keys[i]),stepVersions[s.keys[i]],devices.State(s.keys[i]),s.states[i],devices.Settled(s.keys[i])))return false;return true;
            case "blocked":return ((Openable)devices.Get(s.key)).BlockedByPlayerSequence!=blockedAtActivation&&!devices.State(s.key);
            case "clearance":return LessonScenarioRules.ClearanceReached(((Openable)devices.Get(s.key)).BlockedByPlayerSequence!=blockedAtActivation,((Openable)devices.Get(s.key)).LessonPlayerDistance(player));
            case "read":return caseAcknowledged;
            case "dialogue":return dialogueConfirmed;
            case "confirm":return confirmed;
            case "cue":return stepTime>=s.seconds;
            case "hall":return hallWasOutside&&InsideHall();
            default:return false;
        }
    }
    private bool InsideHall() => player!=null&&player.transform.position.x>=2.2f&&player.transform.position.x<=3.4f&&player.transform.position.z>=-10.4f&&player.transform.position.z<=-9.4f;
    private bool FreshPlaced(PickupItem i) => i.LastPlacementSurface!=null&&LessonScenarioRules.FreshPlacement(i.CarrySequence,initialCarry[i.name],i.PlacementSequence,initialPlace[i.name],i.LastPlacedCarrySequence,i.IsHeld);
    private bool OnTable(PickupItem i)
    {if(!i.gameObject.activeInHierarchy)return false;Bounds b=i.BoundsAt(i.transform.position,i.transform.rotation),t=table.bounds;return !i.IsHeld&&LessonPlacementRules.Accepts(b.min.x,b.max.x,b.min.z,b.max.z,b.min.y,t.min.x,t.max.x,t.min.z,t.max.z,t.max.y);}
    private Bounds Zone(string id)
    {
        var z=Array.Find(task.zones,q=>q.id==id);if(z==null)throw new InvalidOperationException("Missing zone " + id);
        return new Bounds(new Vector3(table.bounds.center.x+z.x,table.bounds.max.y,table.bounds.center.z+z.z),new Vector3(z.width,.01f,z.depth));
    }
    private bool Placed(string name,string zone)
    {
        var i=items[name];if(!FreshPlaced(i)||i.LastPlacementSurface!=table.transform)return false;
        if(zone=="table")return OnTable(i);
        Bounds b=i.BoundsAt(i.transform.position,i.transform.rotation),z=Zone(zone);
        return LessonPlacementRules.Accepts(b.min.x,b.max.x,b.min.z,b.max.z,b.min.y,z.min.x,z.max.x,z.min.z,z.max.z,table.bounds.max.y);
    }
    private PickupItem ExcludedInZone()
    {
        if(!Active)return null;
        foreach(string name in task.excludedItems??Array.Empty<string>())
        {
            var target=items[name];if(!target.gameObject.activeInHierarchy||target.IsHeld)continue;
            Bounds bounds=target.BoundsAt(target.transform.position,target.transform.rotation);
            foreach(var z in task.zones)
            {Bounds forbidden=Zone(z.id);if(bounds.min.x<forbidden.max.x&&bounds.max.x>forbidden.min.x&&bounds.min.z<forbidden.max.z&&bounds.max.z>forbidden.min.z&&OnTable(target))return target;}
        }
        return null;
    }
    private bool FinalConditions()
    {
        foreach(var pair in unchangedVersions)if(devices.Sequence(pair.Key)!=pair.Value)return false;
        if(ExcludedInZone()!=null)return false;
        foreach(var pair in finalStates)if(devices.State(pair.Key)!=pair.Value||!devices.Settled(pair.Key))return false;
        foreach(var pair in fixedPositions)if(items[pair.Key].IsHeld||(items[pair.Key].transform.position-pair.Value).sqrMagnitude>.0001f)return false;
        foreach(string name in task.keepOnTable??Array.Empty<string>())if(!OnTable(items[name]))return false;
        foreach(var s in task.steps)
        {
            if(s.op=="arrange")for(int j=0;j<s.keys.Length;j++){if(!Placed(s.keys[j],s.zones[j]))return false;}
            if(s.op=="place"&&!Placed(s.key,s.zone))return false;
            if(s.op=="offTable"&&(items[s.key].IsHeld||OnTable(items[s.key])))return false;
            if(s.op=="flush"&&!devices.Settled(s.key))return false;
        }
        if(task.id=="CUR-321"&&player.HeldItem!=items["Loeffel_Spoon"])return false;
        if(task.id=="CUR-322"&&player.HeldItem!=items["Contents_Milk"])return false;
        return true;
    }
    public bool TryGetTarget(out Vector3 at)
    {
        at=default;if(!Active)return false;
        if(CurrentStep==null)
        {var wrong=ExcludedInZone();if(wrong==null)return false;at=wrong.transform.position;return true;}
        var s=CurrentStep;
        if(s.op=="hall"){at=new Vector3(2.8f,0,-9.9f);return true;}
        if(s.op=="state"||s.op=="flush"||s.op=="blocked"){at=devices.Point(s.key);return true;}
        if(s.op=="clearance") {var door=(Openable)devices.Get(s.key);at=door.LessonControlPoint-door.transform.parent.forward*1.2f;return true;}
        if(s.op=="allStates")
        {for(int i=0;i<s.keys.Length;i++)if(devices.State(s.keys[i])!=s.states[i]||devices.Sequence(s.keys[i])==stepVersions[s.keys[i]]){at=devices.Point(s.keys[i]);return true;}}
        if(s.op=="pickup"||s.op=="offTable"||s.op=="place")
        {at=s.op=="place"&&player.HeldItem==items[s.key]?(s.zone=="table"?table.bounds.center:Zone(s.zone).center):items[s.key].transform.position;return true;}
        if(s.op=="arrange")
        {for(int i=0;i<s.keys.Length;i++)if(!Placed(s.keys[i],s.zones[i])){at=player.HeldItem==items[s.keys[i]]?Zone(s.zones[i]).center:items[s.keys[i]].transform.position;return true;}}
        return false;
    }
    public IEnumerable<(Vector3 point,string label)> Labels => labels;
    private void DrawMarkers()
    {
        markers=new GameObject("LessonScenarioMarkers"){hideFlags=HideFlags.DontSave};
        markerMaterial=new Material(Resources.Load<Material>("Lessons/PlacementZone")){hideFlags=HideFlags.DontSave};
        foreach(var z in task.zones)
        {var b=Zone(z.id);Outline(b);labels.Add((b.center+Vector3.up*.02f,z.label));}
        if(task.id=="CUR-324")
        {var b=new Bounds(new Vector3(2.8f,.02f,-9.9f),new Vector3(1.2f,.01f,1));Outline(b);labels.Add((b.center+Vector3.up*.04f,"Flurzone"));}
    }
    private void Outline(Bounds b)
    {
        var go=new GameObject("ZoneOutline");go.transform.SetParent(markers.transform,false);var line=go.AddComponent<LineRenderer>();
        line.sharedMaterial=markerMaterial;line.positionCount=5;line.startWidth=line.endWidth=.01f;float y=b.center.y+.008f;
        line.SetPositions(new[]{new Vector3(b.min.x,y,b.min.z),new Vector3(b.max.x,y,b.min.z),new Vector3(b.max.x,y,b.max.z),new Vector3(b.min.x,y,b.max.z),new Vector3(b.min.x,y,b.min.z)});
        line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    private void Restore()
    {
        foreach(var p in originals)
        {
            if(p.Key==null)continue;p.Key.transform.SetParent(p.Value.parent,false);
            if(p.Value.parent!=null){p.Key.transform.localPosition=p.Value.localPosition;p.Key.transform.localRotation=p.Value.localRotation;}
            else p.Key.transform.SetPositionAndRotation(p.Value.position,p.Value.rotation);
            p.Key.gameObject.SetActive(p.Value.active);
        }
        foreach(var p in originalStates)if(devices.Get(p.Key)!=null)
        {devices.SetState(p.Key,p.Value); if(devices.Get(p.Key) is Openable door)door.RequireLessonClearance(false);}
        if(hasPlayerReturn){MovePlayer(playerReturnPosition,playerReturnRotation);hasPlayerReturn=false;}
    }
    private void MovePlayer(Vector3 position,Quaternion rotation)
    {
        if(player==null)return;
        var cc=player.GetComponent<CharacterController>();bool active=cc.enabled;cc.enabled=false;
        player.transform.SetPositionAndRotation(position,rotation);cc.enabled=active;Physics.SyncTransforms();
    }
    private void ClearMarkers(){if(markers!=null){markers.SetActive(false);Destroy(markers);}if(markerMaterial!=null)Destroy(markerMaterial);}
    public void Finish()
    {if(originals.Count==0)return;player.CancelCarry();Restore();originals.Clear();originalStates.Clear();task=null;CaseReaderOpen=false;currentCase=null;ClearMarkers();Physics.SyncTransforms();}
    private void OnDestroy(){Restore();ClearMarkers();}
}
