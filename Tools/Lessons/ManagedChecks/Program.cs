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
Check(catalog.tasks.Length == 430, "Wrong total");
Check(catalog.tasks.Count(t => t.kind == "P") == 106 && catalog.tasks.Count(t => t.kind == "I") == 94 &&
    catalog.tasks.Count(t => t.kind == "G") == 90 && catalog.tasks.Count(t=>t.IsScenario) == 140, "Wrong category counts");
Check(catalog.tasks.Count(t => t.IsGrammar && t.InteractionKind == "I") == 10 &&
    catalog.tasks.Count(t => t.RequiresPlacement) == 20, "New action modes missing");
foreach (int previous in new[] {17,30,53,80,130,133,150,180,200,230,240,250,260,270,280,290,300,310,320,330,340,350,360,361,370,380,390,400,410,420,430})
    Check(catalog.ResumeStage(catalog.tasks.Take(previous).Select(t=>t.id).ToList()) == previous, "Lost progress " + previous);
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
Reject(c=>c.tasks[230].answerIndex=4,"Bad answer index");
Reject(c=>c.tasks[240].choices[0]="", "Empty choice");
Reject(c=>c.tasks[240].action="P", "Identification prompt accepted as pickup");
Reject(c=>c.tasks[250].relation="unter", "Unsupported placement relation");
Reject(c=>c.tasks[250].referenceName=c.tasks[250].targetName,"Self reference");
Reject(c=>c.tasks[250].reviewOf="CUR-251","Future/self review");
Reject(c=>c.tasks[133].reviewOf=null,"Unmarked duplicate target");
Reject(c=>c.tasks[133].kind="I","Review action mismatch");
Reject(c=>c.tasks[1].id=c.tasks[0].id,"Duplicate ID");
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
Console.WriteLine("PASS: 430 catalog tasks; stable-save migrations; 90 grammar graders and physical gates; 140 scenario schemas; invalid schemas; full-footprint/height/side placement rejection.");

foreach(var task in catalog.tasks.Where(t=>t.IsScenario))
{
    LessonScenarioRules.Validate(task);
    for(int i=-1;i<4;i++) Check(task.AcceptsChoice(i)==(i==task.answerIndex),"Bad scenario report grader");
}
Reject(c=>c.tasks[290].steps[0].op="invented","Unknown operation accepted");
Reject(c=>c.tasks[290].steps[0].zones[0]="absent","Unknown arrangement zone accepted");
Reject(c=>c.tasks[290].zones[0].x=2,"Zone beyond tabletop accepted");
Reject(c=>c.tasks[300].presets[0].key="absent","Unknown device accepted");
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
Reject(c=>c.tasks[344].steps[0].answerIndex=3,"Invalid clarification answer accepted");
Reject(c=>c.tasks[360].caseId="unknown","Unknown case accepted");
Reject(c=>c.tasks[337].startHeld="absent","Unknown held preset accepted");
Reject(c=>c.tasks[330].excludedItems=new[]{"absent"},"Unknown exclusion accepted");
Reject(c=>c.tasks[338].scenarioItems[0].zone="absent","Unknown initial zone accepted");
Console.WriteLine("PASS: source packets E/M; dialogue graders; case IDs; initial-zone, exclusion and held-preset schemas.");

Check(LessonScenarioRules.BlockOpening(true,false,.6f),"Near opening was not blocked");
Check(!LessonScenarioRules.BlockOpening(false,false,.6f),"Legacy opening unexpectedly blocked");
Check(!LessonScenarioRules.BlockOpening(true,true,.6f),"Closing incorrectly blocked by lesson guard");
Check(!LessonScenarioRules.BlockOpening(true,false,.85f),"Clearance threshold is too strict");
Check(!LessonScenarioRules.ClearanceReached(false,1.2f),"Skipped required rejected attempt");
Check(!LessonScenarioRules.ClearanceReached(true,.94f)&&LessonScenarioRules.ClearanceReached(true,.95f),"Retreat threshold failed");
Reject(c=>c.tasks[334].unchangedDevices=Array.Empty<string>(),"Unguarded no-action task accepted");
Reject(c=>c.tasks[343].steps[0].key="living","Non-door clearance owner accepted");
Console.WriteLine("PASS: clearance boundaries, legacy guard isolation and protected no-action confirmation.");

Check(cases.cases.Length==4 && cases.Get("W").pages.Length==4 && cases.Get("H").pages.Length==4,"W/H sources missing");
foreach(var t in catalog.tasks.Skip(380))
    Check(t.level=="C1.2" && t.IsScenario && t.steps[0].op=="read","New stage missing source/gate");
Check(catalog.ResumeStage(catalog.tasks.Take(400).Select(t=>t.id).ToList())==400,"400 checkpoint became final ending");
Reject(c=>c.tasks[380].caseId="unimplemented-case","Unknown new case accepted");
Console.WriteLine("PASS: W/H packets, 50 source-based stages, 380/400 save continuation and 430 ending.");

Check(catalog.tasks.All(t => !string.IsNullOrWhiteSpace(t.englishInstruction)), "Missing English instruction");
Reject(c => c.tasks[0].englishInstruction = " ", "Blank English instruction accepted");
Console.WriteLine("PASS: English instruction coverage and missing-translation rejection.");
