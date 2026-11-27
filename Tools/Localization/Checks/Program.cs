using System.Text.Json;
using UnityEngine;
void Check(bool value,string why) {if(!value)throw new Exception(why);}
Check(!SupportLanguages.HasSelection && SupportLanguages.Bilingual,"Fresh profile must choose a language with translation on");
PlayerPrefs.SetInt("ExistingCourseSentinel",428);
SupportLanguages.Select(SupportLanguage.Persian,0);
Check(SupportLanguages.HasSelection && SupportLanguages.Selected==SupportLanguage.Persian && SupportLanguages.IsRightToLeft,"Persian selection");
for(int n=0;n<=428;n++)
{
    int last=n/50;
    Check(!SupportLanguageRules.ShouldAsk(n,428,last),"Repeated acknowledged prompt: "+n);
    Check(SupportLanguageRules.ShouldAsk(n,428,Math.Max(0,last-1))==(n>=50 && n<428),"Boundary eligibility: "+n);
}
Check(!SupportLanguageRules.ShouldAsk(49,428,0) && SupportLanguageRules.ShouldAsk(50,428,0),"First checkpoint must follow fifty completions");
SupportLanguages.Acknowledge(50,false);
Check(!SupportLanguages.Bilingual && SupportLanguages.AcknowledgedCheckpoint==1,"No choice persists");
SupportLanguages.Select(SupportLanguage.Ukrainian,90);
Check(!SupportLanguages.Bilingual && SupportLanguages.AcknowledgedCheckpoint==1 && !SupportLanguages.IsRightToLeft,"Settings change must preserve answer/checkpoint");
Check(SupportLanguageRules.ShouldAsk(100,428,SupportLanguages.AcknowledgedCheckpoint),"Ask again after another fifty even when translation is off");
SupportLanguages.Acknowledge(100,true);
Check(SupportLanguages.Bilingual && SupportLanguages.AcknowledgedCheckpoint==2,"Yes choice persists");
SupportLanguages.ResetCheckpoints();
Check(SupportLanguages.AcknowledgedCheckpoint==0 && SupportLanguages.Bilingual && PlayerPrefs.GetInt("ExistingCourseSentinel")==428,"Reset checkpoints must not erase language, toggle or course profile");
PlayerPrefs.SetInt("DeutschRoom.SupportLanguage.v1",99);
Check(SupportLanguages.Selected==SupportLanguage.Ukrainian,"Invalid saved index must be bounded");
using var catalog=JsonDocument.Parse(File.ReadAllText("Assets/Resources/Localization/SupportLanguages.json"));
var entries=catalog.RootElement.GetProperty("entries").EnumerateArray().ToDictionary(e=>e.GetProperty("source").GetString()!);
using var lessons=JsonDocument.Parse(File.ReadAllText("Assets/Resources/Lessons/IntroA11.json"));
foreach(var task in lessons.RootElement.GetProperty("tasks").EnumerateArray())
{
    string source=task.GetProperty("englishInstruction").GetString()!;
    Check(entries.ContainsKey(source),"Task translation missing: "+task.GetProperty("id").GetString());
    foreach(SupportLanguage lang in Enum.GetValues<SupportLanguage>())
    {
        string value=SupportLanguages.Text(source,lang);
        Check(!string.IsNullOrWhiteSpace(value),"Empty translation");
        if(lang!=SupportLanguage.English) Check(value!=source,"Silent English fallback: "+source);
    }
}
Check(SupportLanguages.Text("Pick up the spoon.",SupportLanguage.Persian)=="قاشق را بردارید.","Exact Persian lookup");
Check(SupportTextLayout.ShapeLine("سلام")=="\ufee1\ufe8e\ufee0\ufeb3","Arabic joins and direction");
Check(SupportTextLayout.ShapeLine("بِ")=="\ufe8fِ","Diacritics must stay with their base glyph");
Check(SupportTextLayout.ShapeLine("دکمه Anhören و Shift + F را بزنید").Contains("Shift + F"),"Latin key combination reversed");
Check(SupportTextLayout.ShapeLine("دکمه Anhören را بزنید").Contains("Anhören"),"German button label reversed");
Check(SupportTextLayout.ShapeLine("مسافة 0.65 متر").Contains("0.65"),"Decimal reversed");
Check(SupportTextLayout.ShapeLine("Українська") == "Українська","Ukrainian reversed");
Check(SupportTextLayout.ShapeLine("پس از ۵۰ مرحله").Contains("۵۰"),"Persian checkpoint number reversed");
Check(SupportTextLayout.ShapeLine("مسافت ۰٫۶۵ متر").Contains("۰٫۶۵"),"Persian decimal reversed");
// Regression for the supplied language-picker screenshot: old visual Arabic was
// reordered again by native bidi. Rendered aliases must carry no RTL codepoints.
foreach(string name in new[] {"فارسی","العربية"})
{
    string visual=SupportTextLayout.ShapeLine(name);
    string display=SupportTextLayout.DisplayLine(name);
    Check(display==string.Concat(visual.Select(SupportTextLayout.DisplayGlyph)),"Picker glyph order changed");
    Check(display.All(c=>c>='\ue000' && c<='\ue7af'),"Picker still contains native RTL codepoints");
}
Check(SupportTextLayout.DisplayLine("فارسی Anhören ۵۰").Contains("Anhören"),"Display boundary changed German label");
Check(SupportTextLayout.DisplayLine("\u200fفارسی\u202c")==SupportTextLayout.DisplayLine("فارسی"),"Direction controls reached renderer");
Check(SupportTextLayout.DisplayLine("English")=="English" && SupportTextLayout.DisplayLine("Українська")=="Українська","LTR language changed");
// A player can read at their own pace without advancing or restarting a lesson.
Check(TaskNoticeTiming.Duration(0)==9 && TaskNoticeTiming.Duration(10000)==30,"Notice reading bounds");
float readingTime=8.5f;
for(int i=0;i<120;i++)readingTime=TaskNoticeTiming.Advance(readingTime,1,true);
Check(readingTime==8.5f,"Holding/another modal consumed reading time");
Check(TaskNoticeTiming.Advance(readingTime,.5f,false)==9,"Release failed to resume the fade timer");
Check(TaskNoticeTiming.Advance(readingTime,-1,false)==readingTime,"Negative frame time rewound the notice");
var style=new GUIStyle();
string wrapped=SupportTextLayout.Wrap("قاشق را بردارید و روی میز بگذارید",style,100,true);
Check(wrapped.Contains('\n'),"Long RTL text must wrap before reordering");
Check(wrapped==SupportTextLayout.Wrap("قاشق را بردارید و روی میز بگذارید",style,100,true),"Cached layout differs");
Console.WriteLine("PASS: 50-task checkpoints, opt-out/opt-in, preferences/profile isolation, 428 × 4 lookups, Arabic joining/diacritics, mixed German/keys/numbers RTL wrapping and paused notice timing.");
