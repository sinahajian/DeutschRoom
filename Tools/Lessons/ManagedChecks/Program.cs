using System.Text.Json;
var path = args[0];
IntroLessonCatalog Load() => JsonSerializer.Deserialize<IntroLessonCatalog>(File.ReadAllText(path), new JsonSerializerOptions { IncludeFields = true })!;
void Check(bool valid, string reason) { if (!valid) throw new Exception(reason); }
void Reject(Action<IntroLessonCatalog> change, string reason)
{
    var c = Load(); change(c);
    try { c.Validate(); } catch (InvalidOperationException) { return; }
    throw new Exception(reason);
}
var catalog = Load(); catalog.Validate();
Check(catalog.tasks.Length == 428, "Wrong total");
Check(catalog.tasks.Count(t => t.kind == "P") == 106 && catalog.tasks.Count(t => t.kind == "I") == 92 &&
    catalog.tasks.Count(t => t.kind == "G") == 90 && catalog.tasks.Count(t=>t.IsScenario) == 140, "Wrong category counts");
Check(catalog.tasks.Count(t => t.IsGrammar && t.InteractionKind == "I") == 10 &&
    catalog.tasks.Count(t => t.RequiresPlacement) == 20, "New action modes missing");
foreach (int previous in new[] {17,30,53,80,130,133,150,180,200,230,240,250,260,270,280,290,300,310,320,330,340,350,360,361,370,380,390,400,410,420,430})
    Check(catalog.ResumeStage(catalog.tasks.Take(previous).Select(t=>t.id).ToList()) == Math.Min(previous,428), "Lost progress " + previous);
foreach (int oldCount in new[] { 101,102,103,190,191,192,400,430 })
{
    var completed = Enumerable.Range(1,oldCount).Select(n=>$"CUR-{n:000}").ToList();
    int expected = oldCount - (oldCount>=102?1:0) - (oldCount>=191?1:0);
    Check(catalog.ResumeStage(completed)==expected,"Hall mirror removal reset/skipped a save at "+oldCount);
}
Check(catalog.tasks.All(t=>t.targetName!="Hall_Mirror"),"Removed mirror still playable");
Check(catalog.ResumeStage(new List<string>{"CUR-001","CUR-003","unknown"}) == 1, "Skipped unfinished ID");
foreach (var task in catalog.tasks.Where(t=>t.IsGrammar && !t.IsScenario))
{
    Check(task.AcceptsChoice(task.answerIndex), "Right answer rejected " + task.id);
    for (int choice=-1;choice<=3;choice++)
        Check(task.AcceptsChoice(choice) == (choice==task.answerIndex), "Wrong answer accepted " + task.id);
    for(int mask=0;mask<8;mask++)
    {
        bool pickup=(mask&1)!=0, identified=(mask&2)!=0, placed=(mask&4)!=0;
        bool expected=task.action=="P"?pickup:task.action=="I"?identified:placed;
        Check(task.ActionSatisfied(pickup,identified,placed)==expected,"Wrong physical gate " + task.id);
    }
}
Reject(c=>c.tasks.Single(t=>t.id=="CUR-231").answerIndex=4,"Bad answer index");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-241").choices[0]="", "Empty choice");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-241").action="P", "Identification prompt accepted as pickup");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-251").relation="unter", "Unsupported placement relation");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-251").referenceName=c.tasks.Single(t=>t.id=="CUR-251").targetName,"Self reference");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-251").reviewOf="CUR-251","Future/self review");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-134").reviewOf=null,"Unmarked duplicate target");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-134").kind="I","Review action mismatch");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-002").id=c.tasks.Single(t=>t.id=="CUR-001").id,"Duplicate ID");
// 43 x 36 cm zone, normal pickup footprints, table contact and tolerance boundaries.
bool InZone(float x0,float x1,float z0,float z1,float y) => LessonPlacementRules.Accepts(x0,x1,z0,z1,y,-.215f,.215f,-.18f,.18f,.9f);
Check(InZone(-.12f,.12f,-.12f,.12f,.91f),"Correct tabletop placement rejected");
Check(InZone(-.215f,.215f,-.18f,.18f,.91f),"Exact footprint rejected");
Check(!InZone(-.1f,.3f,-.1f,.1f,.91f),"Centre-only acceptance leaked outside edge");
Check(!InZone(-.1f,.1f,.3f,.5f,.91f),"Wrong side accepted");
Check(!InZone(-.1f,.1f,-.1f,.1f,.5f),"Floor accepted");
Check(!InZone(-.1f,.1f,-.1f,.1f,1.1f),"Floating/stacked object accepted");
Check(!InZone(.1f,-.1f,-.1f,.1f,.91f),"Inverted bounds accepted");
Check(!InZone(float.NaN,.1f,-.1f,.1f,.91f),"NaN accepted");
Check(GermanLessonSpeech.IntroComplete(280).Contains("280"),"Wrong ending");
Console.WriteLine("PASS: 428 catalog tasks; stable-save migrations; 90 grammar graders and physical gates; 140 scenario schemas; invalid schemas; full-footprint/height/side placement rejection.");

foreach(var task in catalog.tasks.Where(t=>t.IsScenario))
{
    LessonScenarioRules.Validate(task);
    for(int i=-1;i<4;i++) Check(task.AcceptsChoice(i)==(i==task.answerIndex),"Bad scenario report grader");
}
Reject(c=>c.tasks.Single(t=>t.id=="CUR-291").steps[0].op="invented","Unknown operation accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-291").steps[0].zones[0]="absent","Unknown arrangement zone accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-291").zones[0].x=2,"Zone beyond tabletop accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-301").presets[0].key="absent","Unknown device accepted");
Check(!LessonScenarioRules.FreshState(4,4,false,false,true),"Stale preset accepted");
Check(!LessonScenarioRules.FreshState(5,4,false,false,false),"Moving door accepted");
Check(!LessonScenarioRules.FreshState(5,4,true,false,true),"Wrong state accepted");
Check(LessonScenarioRules.FreshState(5,4,false,false,true),"Fresh settled action rejected");
Check(!LessonScenarioRules.FreshPlacement(3,3,4,3,3,false),"Placement without fresh carry accepted");
Check(!LessonScenarioRules.FreshPlacement(4,3,3,3,4,false),"Cancelled carry accepted as placement");
Check(!LessonScenarioRules.FreshPlacement(4,3,4,3,3,false),"Old carry notification accepted");
Check(!LessonScenarioRules.FreshPlacement(4,3,4,3,4,true),"Held object accepted as placed");
Check(LessonScenarioRules.FreshPlacement(4,3,4,3,4,false),"Fresh placement rejected");
Console.WriteLine("PASS: all 140 report graders; fresh action, settled state, placement and cancelled/stale event rejection.");

var cases=JsonSerializer.Deserialize<LessonCaseCatalog>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(path)!,"CaseStudies.json")),new JsonSerializerOptions{IncludeFields=true})!;
cases.Validate();
Check(cases.Get("E").pages.Length==4 && cases.Get("M").pages.Length==4,"Case pages missing");
Check(string.Join(" ",cases.Get("E").pages).Contains("80 W")&&string.Join(" ",cases.Get("M").pages).Contains("21. November"),"Replaced source with answer key");
foreach(var t in catalog.tasks.Where(t=>t.IsScenario))
    foreach(var step in t.steps.Where(s=>s.op=="dialogue"))
        for(int i=-1;i<=3;i++)Check(step.AcceptsDialogue(i)==(i==step.answerIndex),"Dialogue choice guard failed");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-345").steps[0].answerIndex=3,"Invalid clarification answer accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-361").caseId="unknown","Unknown case accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-338").startHeld="absent","Unknown held preset accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-331").excludedItems=new[]{"absent"},"Unknown exclusion accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-339").scenarioItems[0].zone="absent","Unknown initial zone accepted");
Console.WriteLine("PASS: source packets E/M; dialogue graders; case IDs; initial-zone, exclusion and held-preset schemas.");

Check(LessonScenarioRules.BlockOpening(true,false,.6f),"Near opening was not blocked");
Check(!LessonScenarioRules.BlockOpening(false,false,.6f),"Legacy opening unexpectedly blocked");
Check(!LessonScenarioRules.BlockOpening(true,true,.6f),"Closing incorrectly blocked by lesson guard");
Check(!LessonScenarioRules.BlockOpening(true,false,.85f),"Clearance threshold is too strict");
Check(!LessonScenarioRules.ClearanceReached(false,1.2f),"Skipped required rejected attempt");
Check(!LessonScenarioRules.ClearanceReached(true,.94f)&&LessonScenarioRules.ClearanceReached(true,.95f),"Retreat threshold failed");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-335").unchangedDevices=Array.Empty<string>(),"Unguarded no-action task accepted");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-344").steps[0].key="living","Non-door clearance owner accepted");
Console.WriteLine("PASS: clearance boundaries, legacy guard isolation and protected no-action confirmation.");

Check(cases.cases.Length==4 && cases.Get("W").pages.Length==4 && cases.Get("H").pages.Length==4,"W/H sources missing");
foreach(var t in catalog.tasks.Where(t=>int.Parse(t.id.Substring(4))>=381))
    Check(t.level=="C1.2" && t.IsScenario && t.steps[0].op=="read","New stage missing source/gate");
Check(catalog.ResumeStage(catalog.tasks.Take(400).Select(t=>t.id).ToList())==400,"400 checkpoint became final ending");
Reject(c=>c.tasks.Single(t=>t.id=="CUR-381").caseId="unimplemented-case","Unknown new case accepted");
Console.WriteLine("PASS: W/H packets, 50 source-based stages, 380/400 save continuation and CUR-430 ending at display 428.");

Check(catalog.tasks.All(t => !string.IsNullOrWhiteSpace(t.englishInstruction)), "Missing English instruction");
Reject(c => c.tasks.Single(t=>t.id=="CUR-001").englishInstruction = " ", "Blank English instruction accepted");
Console.WriteLine("PASS: English instruction coverage and missing-translation rejection.");

Check(catalog.controlsPractice.Length == 8 && catalog.controlsPractice.All(s => !string.IsNullOrWhiteSpace(s.englishInstruction)),
    "Hands-on guide is missing translated controls instructions");
Reject(c => c.controlsPractice[0].englishInstruction = "", "Missing practice translation accepted");
Reject(c => c.controlsPractice[1].id = c.controlsPractice[0].id, "Duplicate practice ID accepted");
for (int step = 0; step < 7; step++)
{
    Check(!HandsOnTutorialRules.Ready(step, 0, 10, false, false, false, false, false), "Idle guide advanced " + step);
    for (int action = 0; action < 7; action++)
        Check(HandsOnTutorialRules.Ready(step, action == 0 ? .65f : 0, action == 1 ? .4f : 10,
            action == 2, action == 3, action == 4, action == 5, action == 6) == (action == step),
            "Wrong action advanced hands-on step " + step);
}
Check(!HandsOnTutorialRules.Ready(7,100,0,true,false,true,true,true),"Explore accepted old actions without a fresh direct name");
Check(HandsOnTutorialRules.Ready(7,0,10,false,true,false,false,false),"Explore rejected a fresh direct name");
Check(ObjectNameTapRules.Accepts(.1f,2,true),"Short object tap rejected");
Check(!ObjectNameTapRules.Accepts(.1f,19,true),"Look drag became a name tap");
Check(!ObjectNameTapRules.Accepts(.6f,0,true),"Long hold became a name tap");
Check(!ObjectNameTapRules.Accepts(.1f,0,false),"Cancelled touch became a name tap");
Check(!HandsOnTutorialRules.Ready(8, 100, 0, true, true, true, true, true), "Completed practice advanced again");
Check(!HandsOnTutorialRules.Ready(0, .64f, 0, true, true, true, true, true), "Movement threshold skipped");
Check(!HandsOnTutorialRules.Ready(1, 100, .41f, true, true, true, true, true), "Table distance threshold skipped");
bool[] cells = Enumerable.Repeat(true, 25).ToArray();
cells[2] = cells[7] = cells[12] = cells[17] = false;
var previousCells = new int[cells.Length]; var frontier = new Queue<int>(); var routeCells = new List<int>();
Check(TutorialRouteSearch.Find(cells, 5, 0, 4, previousCells, frontier, routeCells), "Route failed to go around obstacle wall");
Check(routeCells[0] == 0 && routeCells[^1] == 4 && routeCells.Contains(22), "Route cut through barrier");
for (int i = 0; i < routeCells.Count; i++)
{
    Check(cells[routeCells[i]], "Route crossed an occupied cell");
    if (i > 0) Check(Math.Abs(routeCells[i] % 5 - routeCells[i-1] % 5) + Math.Abs(routeCells[i] / 5 - routeCells[i-1] / 5) == 1,
        "Route jumped or cut a diagonal corner");
}
cells[22] = false;
Check(!TutorialRouteSearch.Find(cells, 5, 0, 4, previousCells, frontier, routeCells) && routeCells.Count == 0, "Blocked route left a misleading line");
Check(!TutorialRouteSearch.Find(cells, 5, -1, 4, previousCells, frontier, routeCells), "Invalid start accepted");
Check(TutorialRouteSearch.Find(cells, 5, 0, 0, previousCells, frontier, routeCells) && routeCells.SequenceEqual(new[] {0}), "Same-cell arrival failed");
Console.WriteLine("PASS: eight translated hands-on steps; real-action gates and distance boundaries; obstacle detours, cardinal adjacency, blocked/invalid/same-cell routes.");

Check(ManipulationMotion.Ease(-1)==0 && ManipulationMotion.Ease(1)==1 && ManipulationMotion.Ease(2)==1,"Manipulation easing endpoints/clamps failed");
float previousEase=0;
for(int sample=0;sample<=100;sample++)
{
    float t=sample/100f,ease=ManipulationMotion.Ease(t);
    Check(ease>=previousEase && ease>=0 && ease<=1,"Manipulation overshot or reversed");previousEase=ease;
    Check(ManipulationMotion.Lift(t,.04f)>=0 && ManipulationMotion.Lift(t,.04f)<=.040001f,"Release lift exceeded its arc");
}
Check(ManipulationMotion.Lift(0,.04f)==0 && ManipulationMotion.Lift(1,.04f)==0,"Manipulation moved its endpoints");
Check(ManipulationMotion.Lift(.001f,.04f)/.001f<.01f && ManipulationMotion.Lift(.999f,.04f)/.001f<.01f,"Manipulation arc has an abrupt endpoint velocity");
Check(ManipulationMotion.PickupSeconds>.2f && ManipulationMotion.PlaceSeconds>.2f,"Manipulation lost its visible transition");
Console.WriteLine("PASS: pickup/release motion endpoint stability, monotonic easing, bounded lift and visible duration; native timing/cancellation checks remain separate.");

Check(!GermanLessonSpeech.PickupGuidance(true).Contains("drücke",StringComparison.OrdinalIgnoreCase) &&
    !GermanLessonSpeech.IdentifyGuidance(true).Contains("drücke",StringComparison.OrdinalIgnoreCase),"Phone guidance mentions keyboard presses");
Check(GermanLessonSpeech.PickupGuidance(false).Contains("E") && GermanLessonSpeech.IdentifyGuidance(false).Contains("F"),"Desktop controls missing");
foreach(bool holding in new[]{false,true})foreach(bool placing in new[]{false,true})foreach(bool touch in new[]{false,true})
{
    Check(GermanLessonSpeech.StillRelevant(GermanLessonSpeech.ReleaseBeforeNext,holding,placing,touch)==(holding&&!placing),"Stale mandatory release warning");
    Check(GermanLessonSpeech.StillRelevant(GermanLessonSpeech.ReleaseOptional,holding,placing,touch)==(holding&&!placing),"Stale optional release warning");
    Check(GermanLessonSpeech.StillRelevant(GermanLessonSpeech.PickupHelpTouch,holding,placing,touch)==touch,"Wrong touch guidance");
    Check(GermanLessonSpeech.StillRelevant(GermanLessonSpeech.PickupHelpKeyboard,holding,placing,touch)==!touch,"Wrong keyboard guidance");
}
Console.WriteLine("PASS: mode-specific speech and queued/current release relevance across holding/placing/input states.");
Check(catalog.controlGuidance.Length==4 && catalog.controlGuidance.All(h=>!string.IsNullOrWhiteSpace(h.englishInstruction)),"Control guidance translation missing");
Reject(c=>c.controlGuidance[0].englishInstruction="","Untranslated control hint accepted");
