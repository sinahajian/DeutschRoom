using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BathroomFurnishingBuilder
{
    public const int AddedDoors = 2;
    [Serializable] public sealed class Word { public string id, de, fa, en; public int order = -1; }
    [Serializable] public sealed class Catalog { public List<Word> fixtures = new List<Word>(); public List<Word> vocabulary = new List<Word>(); }
    private static Catalog catalog;
    private static Transform root;
    private static Material ceramic, steel, glass, oak, stone, dark;
    private const string MeshPath = "Assets/Art/Bathroom";
    [MenuItem("Tools/DeutschRoom/Rebuild Bathroom & Vocabulary")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        GameObject previous = GameObject.Find("BathroomFurnishings"); if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        if (!AssetDatabase.IsValidFolder(MeshPath)) AssetDatabase.CreateFolder("Assets/Art", "Bathroom");
        ceramic = Load("Ceramic"); steel = Load("BrushedSteel"); glass = Load("Glass"); oak = Load("Oak"); stone = Load("Quartz");
        dark = AssetDatabase.LoadAssetAtPath<Material>(MeshPath + "/Dark.mat");
        if (dark == null) { dark = new Material(Shader.Find("Universal Render Pipeline/Lit")); dark.SetColor("_BaseColor", new Color(.04f, .06f, .06f)); AssetDatabase.CreateAsset(dark, MeshPath + "/Dark.mat"); }
        root = new GameObject("BathroomFurnishings").transform; catalog = new Catalog();
        Toilet(); Vanity(); Shower(); AnnotateExisting();
        File.WriteAllText("Documentation/BathroomContent.json", JsonUtility.ToJson(catalog, true) + "\n");
        UpdateInventory();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        Debug.Log("BATHROOM_BUILD_PASS: 10 fixed learning fixtures; existing whole-object and component names, without replacing interaction owners.");
    }
    public static void BuildAndValidate() { Build(); InteractionValidation.Run(); }
    public static void BuildAndCapture()
    {
        Build();
        EditorApplication.delayCall += () => { try { VisualReviewCapture.Capture("after"); EditorApplication.Exit(0); } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); } };
    }
    private static Material Load(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/" + name + ".mat");
        if (material == null) throw new InvalidOperationException("Apply visual polish first: " + name);
        return material;
    }
    private static Transform Fixture(string id, string de, string fa, string en, int order, Vector3 at)
    {
        Transform obj = new GameObject(id).transform; obj.SetParent(root, false); obj.position = at;
        VocabularyObject label = obj.gameObject.AddComponent<VocabularyObject>(); Name(label, de);
        FixedLearningTarget target = obj.gameObject.AddComponent<FixedLearningTarget>();
        SerializedObject data = new SerializedObject(target); data.FindProperty("order").intValue = order; data.ApplyModifiedPropertiesWithoutUndo();
        catalog.fixtures.Add(new Word { id = id, de = de, fa = fa, en = en, order = order }); Record(de, fa, en);
        return obj;
    }
    private static void Toilet()
    {
        Transform toilet = Fixture("Bath_Toilet", "die Toilette", "توالت؛ کاسه و نشیمن", "toilet fixture", 0, new Vector3(4.95f, 0, -15.2f));
        Lathe(toilet, "ToiletPedestal", new[] { V(0,0), V(.8f,0), V(.7f,.6f), V(1,1), V(0,1) }, new Vector3(.16f,.32f,.23f), new Vector3(0,0,0), ceramic);
        Transform bowl = Lathe(toilet, "ToiletBowl", new[] { V(0,0),V(.55f,0),V(.82f,.35f),V(1,.9f),V(1,1),V(.86f,1),V(.7f,.5f),V(.4f,.28f),V(0,.25f) }, new Vector3(.24f,.20f,.33f), new Vector3(0,.28f,0), ceramic);
        Part(bowl.gameObject, "die Toilettenschüssel", "کاسه توالت", "toilet bowl");
        Transform seat = Ring(toilet, "ToiletSeat", new Vector3(.25f,.018f,.34f), new Vector3(0,.49f,0), ceramic);
        Part(seat.gameObject, "der Toilettensitz", "نشیمن توالت", "toilet seat");
        Transform lid = Primitive(toilet, "ToiletLid", PrimitiveType.Sphere, new Vector3(0,.69f,-.29f), new Vector3(.48f,.48f,.035f), ceramic);
        Part(lid.gameObject, "der Toilettendeckel", "درپوش توالت", "toilet lid");
        Transform cistern = Fixture("Bath_Cistern", "der Spülkasten", "فلاش‌تانک", "toilet cistern", 1, new Vector3(4.95f,.52f,-15.66f));
        Box(cistern,"CisternBody",new Vector3(0,.22f,0),new Vector3(.49f,.44f,.23f),ceramic);
        Box(cistern,"CisternLid",new Vector3(0,.45f,0),new Vector3(.51f,.035f,.25f),ceramic);
        Transform flush = Primitive(cistern,"FlushButton",PrimitiveType.Cylinder,new Vector3(0,.478f,0),new Vector3(.085f,.009f,.06f),steel);
        Part(flush.gameObject,"die Spültaste","دکمه فلاش","flush button");
    }
    private static void Vanity()
    {
        Transform cabinet = Fixture("Bath_Vanity", "der Waschbeckenunterschrank", "کابینت زیر روشویی", "vanity cabinet", 9, new Vector3(6.15f,0,-11.48f));
        foreach(float x in new[]{-.555f,.555f}) Box(cabinet,"VanitySide",new Vector3(x,.45f,0),new Vector3(.04f,.64f,.54f),oak);
        Box(cabinet,"VanityBottom",new Vector3(0,.14f,0),new Vector3(1.11f,.04f,.54f),oak);
        Box(cabinet,"VanityBack",new Vector3(0,.45f,.25f),new Vector3(1.11f,.64f,.035f),oak);
        Box(cabinet,"VanityCounter",new Vector3(0,.80f,0),new Vector3(1.22f,.06f,.61f),stone);
        foreach (int side in new[] { -1,1 })
        {
            Transform hinge = new GameObject("VanityDoorHinge").transform; hinge.SetParent(cabinet,false); hinge.localPosition=new Vector3(side*.56f,.45f,-.285f);
            Openable openable=hinge.gameObject.AddComponent<Openable>();Name(openable,"der Waschbeckenunterschrank");
            SerializedObject data=new SerializedObject(openable);data.FindProperty("openAngle").floatValue=-side*100;data.ApplyModifiedPropertiesWithoutUndo();
            Transform panel=Box(hinge,"VanityDoor",new Vector3(-side*.275f,0,0),new Vector3(.55f,.62f,.035f),oak);
            Part(panel.gameObject,"die Schranktür","در کابینت","cabinet door");
            Transform handle=Box(hinge,"VanityHandle",new Vector3(-side*.47f,.2f,-.035f),new Vector3(.10f,.025f,.035f),steel);
            Part(handle.gameObject,"der Schrankgriff","دستگیره کابینت","cabinet handle");
        }
        foreach (float x in new[] { -.47f,.47f }) foreach (float z in new[] { -.18f,.18f }) Box(cabinet,"VanityFoot",new Vector3(x,.06f,z),new Vector3(.045f,.12f,.045f),steel);
        Transform basin = Fixture("Bath_Washbasin","das Waschbecken","روشویی","washbasin",2,new Vector3(6.15f,.82f,-11.52f));
        Lathe(basin,"WashbasinBody",new[] { V(0,0),V(.50f,0),V(.80f,.25f),V(1,.9f),V(1,1),V(.94f,1),V(.73f,.30f),V(.38f,.10f),V(0,.10f) },new Vector3(.38f,.145f,.245f),Vector3.zero,ceramic);
        Transform drain = Primitive(basin,"BasinDrain",PrimitiveType.Cylinder,new Vector3(0,.025f,0),new Vector3(.065f,.004f,.065f),steel);
        Part(drain.gameObject,"der Abfluss","خروجی آب","drain");
        Transform tap = Fixture("Bath_BasinTap","die Armatur","شیر روشویی","basin tap",3,new Vector3(6.15f,.84f,-11.20f));
        Tube(tap,new Vector3(6.15f,.84f,-11.20f),new Vector3(6.15f,1.14f,-11.20f),.025f,steel);
        Transform spout = Tube(tap,new Vector3(6.15f,1.14f,-11.20f),new Vector3(6.15f,1.14f,-11.49f),.021f,steel);
        Part(spout.gameObject,"der Auslauf","آبریز شیر","tap spout");
        Transform lever = Box(tap,"TapLever",new Vector3(0,.34f,-.045f),new Vector3(.04f,.025f,.14f),steel);
        Part(lever.gameObject,"der Bedienhebel","اهرم کنترل","control lever");
    }
    private static void Shower()
    {
        Transform shower = Fixture("Bath_Shower","die Dusche","دوش","shower",4,new Vector3(8.65f,1.1f,-15.82f));
        Transform mixer = Tube(shower,new Vector3(8.47f,1.1f,-15.82f),new Vector3(8.83f,1.1f,-15.82f),.04f,steel);
        Part(mixer.gameObject,"die Duscharmatur","شیر دوش","shower mixer");
        Tube(shower,new Vector3(8.65f,1.15f,-15.80f),new Vector3(8.65f,2.10f,-15.80f),.014f,steel);
        Tube(shower,new Vector3(8.65f,2.10f,-15.80f),new Vector3(8.65f,2.10f,-15.47f),.014f,steel);
        Transform enclosure = Fixture("Bath_Enclosure","die Duschkabine","کابین دوش","shower enclosure",5,new Vector3(8.60f,0,-15.0f));
        Transform tray = Box(enclosure,"ShowerTray",new Vector3(0,.05f,0),new Vector3(1.55f,.10f,1.78f),ceramic);
        Part(tray.gameObject,"die Duschwanne","سینی زیر دوش","shower tray");
        Transform left = Box(enclosure,"ShowerGlassLeft",new Vector3(-.775f,1.10f,0),new Vector3(.012f,1.95f,1.78f),glass);
        Part(left.gameObject,"die Glasscheibe","پنل شیشه‌ای","glass panel");
        Transform front = Box(enclosure,"ShowerGlassFront",new Vector3(-.43f,1.10f,.89f),new Vector3(.69f,1.95f,.012f),glass);
        Part(front.gameObject,"die Glasscheibe","پنل شیشه‌ای","glass panel");
        foreach (float z in new[] { -.89f,.89f }) Box(enclosure,"ShowerFrame",new Vector3(-.775f,1.1f,z),new Vector3(.025f,2.0f,.025f),steel);
        // Leave an 86 cm entrance opening on the front-right side.
        Transform head = Fixture("Bath_ShowerHead","der Duschkopf","سردوش","shower head",6,new Vector3(8.65f,2.045f,-15.47f));
        Primitive(head,"ShowerHeadBody",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.22f,.018f,.22f),steel);
        for (int i = 0; i < 12; i++)
        { float a=i*Mathf.PI*2/12; Primitive(head,"Nozzle",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.075f,-.021f,Mathf.Sin(a)*.075f),Vector3.one*.009f,dark); }
        Transform hose = Fixture("Bath_ShowerHose","der Duschschlauch","شلنگ دوش","shower hose",7,new Vector3(8.80f,1.1f,-15.70f));
        Vector3 a0 = new Vector3(8.80f,1.1f,-15.8f), b0 = new Vector3(9.05f,.15f,-15.65f), c0 = new Vector3(8.68f,2.04f,-15.5f), last=a0;
        for (int i=1;i<=28;i++) { float t=i/28f; Vector3 next=(1-t)*(1-t)*a0+2*(1-t)*t*b0+t*t*c0; Tube(hose,last,next,.012f,steel); last=next; }
        Transform floorDrain = Fixture("Bath_FloorDrain","der Bodenablauf","کف‌شور","floor drain",8,new Vector3(8.65f,.107f,-15.05f));
        Box(floorDrain,"DrainCover",Vector3.zero,new Vector3(.17f,.009f,.17f),steel);
        for (int i=0;i<5;i++) Box(floorDrain,"DrainSlot",new Vector3(-.056f+i*.028f,.005f,0),new Vector3(.01f,.002f,.12f),dark);
    }
    private static void AnnotateExisting()
    {
        Whole("Dunstabzug_Hood","die Dunstabzugshaube","هود آشپزخانه","range hood");
        Whole("Herd_OvenBody","der Gasherd","اجاق گاز","gas stove"); Whole("Herd_Hob","das Kochfeld","صفحه اجاق","cooktop");
        Whole("Fenster_Window","das Fenster","پنجره","window"); Whole("Vorhaenge_Curtains","der Vorhang","پرده","curtain");
        Whole("CeilingLamp_Diffuser","die Deckenleuchte","چراغ سقفی","ceiling light");
        Whole("Plane","der Boden","کف","floor"); Whole("Floor","der Boden","کف","floor"); Whole("Ceiling","die Decke","سقف","ceiling");
        foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>())
        {
            string n=collider.name;
            if (n.EndsWith("_Floor")) WholeObject(collider.gameObject,"der Boden","کف","floor");
            else if (n.EndsWith("_Ceiling")) WholeObject(collider.gameObject,"die Decke","سقف","ceiling");
            else if (n.EndsWith("DoorFrame")||n.EndsWith("DoorFrameTop")) Part(collider.gameObject,"der Türrahmen","چارچوب در","door frame");
            else if (n.EndsWith("WindowFrame")) Part(collider.gameObject,"der Fensterrahmen","قاب پنجره","window frame");
            else if (n.EndsWith("WindowGlass")) Part(collider.gameObject,"die Fensterscheibe","شیشه پنجره","window pane");
            else if (n.StartsWith("Leg_")) Part(collider.gameObject,"das Tischbein","پایه میز","table leg");
            else if (n=="TableTop") Part(collider.gameObject,"die Tischplatte","صفحه میز","tabletop");
            else if (n=="Chair_Leg") Part(collider.gameObject,"das Stuhlbein","پایه صندلی","chair leg");
            else if (n=="Chair_Seat") Part(collider.gameObject,"die Sitzfläche","سطح نشیمن","seat surface");
            else if (n=="Chair_SeatCushion") Part(collider.gameObject,"das Sitzkissen","بالشتک نشیمن","seat cushion");
            else if (n=="Chair_BackSlat") Part(collider.gameObject,"die Rückenlehne","پشتی صندلی","backrest");
            else if (n=="Cabinet_LeftDoor"||n=="Cabinet_RightDoor"||n=="LeftDoor"||n=="RightDoor"||n=="Sink_Door"||n=="Prep_Door") Part(collider.gameObject,"die Schranktür","در کابینت","cabinet door");
            else if (n=="Cabinet_LeftHandle"||n=="Cabinet_RightHandle"||n=="LeftHandle"||n=="RightHandle"||n=="Sink_Handle"||n=="Prep_Handle") Part(collider.gameObject,"der Schrankgriff","دستگیره کابینت","cabinet handle");
            else if (n=="Drawer_Handle") Part(collider.gameObject,"der Schubladengriff","دستگیره کشو","drawer handle");
            else if (n=="Drawer_Front") Part(collider.gameObject,"die Schubladenfront","جلوی کشو","drawer front");
            else if (n=="Window_Handle") Part(collider.gameObject,"der Fenstergriff","دستگیره پنجره","window handle");
            else if (n.StartsWith("Window_Frame")) Part(collider.gameObject,"der Fensterrahmen","قاب پنجره","window frame");
            else if (n=="Window_Glass") Part(collider.gameObject,"die Fensterscheibe","شیشه پنجره","window pane");
            else if (n=="Window_Sill") Part(collider.gameObject,"die Fensterbank","لبه زیر پنجره","window sill");
            else if (n=="DoorHandle"||n=="Door_LeverHandle") Part(collider.gameObject,"der Türgriff","دستگیره در","door handle");
            else if (n=="DoorLeaf"||n=="Door_Leaf") Part(collider.gameObject,"das Türblatt","لنگه در","door leaf");
            else if (n=="Curtain_Rod") Part(collider.gameObject,"die Gardinenstange","میله پرده","curtain rod");
            else if (n=="Pot_Lid") Part(collider.gameObject,"der Topfdeckel","در قابلمه","pot lid");
            else if (n=="Pot_LidKnob"||n=="Kettle_LidKnob") Part(collider.gameObject,"der Deckelknopf","دستگیره درپوش","lid knob");
            else if (n=="Herd_Burner"||n=="Gasherd_BurnerCap") Part(collider.gameObject,"der Gasbrenner","شعله اجاق","gas burner");
            else if (n=="Gasherd_PanSupport") Part(collider.gameObject,"der Topfträger","شبکه نگهدارنده قابلمه","pan support");
            else if (n=="Herd_Control"||n.Contains("ControlKnob")) Part(collider.gameObject,"der Bedienknopf","پیچ کنترل","control knob");
            else if (n.Contains("Display")) Part(collider.gameObject,"das Display","نمایشگر","display");
            else if (n=="Kuehlschrank_Handle"||n=="Backofen_Handle"||n=="Mikrowelle_Handle") Part(collider.gameObject,"der Griff","دستگیره","handle");
            else if (n.StartsWith("WindowWall_")||n.StartsWith("DoorWall_")||n.StartsWith("Wall_")||
                (collider.transform.parent?.name=="HomeExpansion" && collider.bounds.size.y>.5f)) WholeObject(collider.gameObject,"die Wand","دیوار","wall");
        }
    }
    private static void Whole(string objectName,string de,string fa,string en)
    {
        GameObject obj=GameObject.Find(objectName); if(obj==null)return;
        WholeObject(obj,de,fa,en);
    }
    private static void WholeObject(GameObject obj,string de,string fa,string en)
    {
        KitchenInteractable label=obj.GetComponent<KitchenInteractable>(); if(label==null)label=obj.AddComponent<VocabularyObject>();
        Name(label,de); Record(de,fa,en);
    }
    private static void Name(KitchenInteractable label,string de)
    { SerializedObject data=new SerializedObject(label);data.FindProperty("displayName").stringValue=de;data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Part(GameObject obj,string de,string fa,string en)
    {
        VocabularyPart part=obj.GetComponent<VocabularyPart>();if(part==null)part=obj.AddComponent<VocabularyPart>();
        SerializedObject data=new SerializedObject(part);data.FindProperty("displayName").stringValue=de;data.ApplyModifiedPropertiesWithoutUndo();Record(de,fa,en);
    }
    private static void Record(string de,string fa,string en)
    { if(!catalog.vocabulary.Exists(v=>v.de==de))catalog.vocabulary.Add(new Word{de=de,fa=fa,en=en}); }
    private static Vector2 V(float r,float y)=>new Vector2(r,y);
    private static Transform Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        GameObject obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;
        obj.GetComponent<MeshFilter>().sharedMesh=VisualPolishBuilder.FurnitureBox(size);obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform;
    }
    private static Transform Primitive(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 size,Material material)
    { GameObject obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform; }
    private static Transform Tube(Transform parent,Vector3 a,Vector3 b,float radius,Material material)
    {
        Transform t=Primitive(parent,"Pipe",PrimitiveType.Cylinder,parent.InverseTransformPoint((a+b)/2),new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),material);
        t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);return t;
    }
    private static Transform Ring(Transform parent,string name,Vector3 size,Vector3 at,Material material)
    {
        List<Vector2> p=new List<Vector2>();for(int i=0;i<=12;i++){float a=i*Mathf.PI*2/12;p.Add(V(.91f+.09f*Mathf.Cos(a),.5f+.5f*Mathf.Sin(a)));}
        return Lathe(parent,name,p.ToArray(),new Vector3(size.x,size.y*2,size.z),at,material);
    }
    private static Transform Lathe(Transform parent,string name,Vector2[] profile,Vector3 size,Vector3 at,Material material)
    {
        string path=MeshPath+"/"+name+".asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            const int segments=64;List<Vector3> vertices=new List<Vector3>();List<Vector2> uv=new List<Vector2>();List<int> triangles=new List<int>();
            for(int j=0;j<profile.Length;j++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));uv.Add(new Vector2((float)i/segments,(float)j/(profile.Length-1)));}
            for(int j=0;j<profile.Length-1;j++)for(int i=0;i<segments;i++){int a=j*(segments+1)+i,b=a+segments+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);
        }
        GameObject obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshCollider>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;return obj.transform;
    }
    private static void UpdateInventory()
    {
        string path="Documentation/tools.txt",text=File.ReadAllText(path);int start=text.IndexOf("گروه F10",StringComparison.Ordinal),end=text.IndexOf("گروه F12",StringComparison.Ordinal);
        if(start>=0&&end>start)text=text.Substring(0,start)+text.Substring(start,end-start).Replace("[ ] ","")+text.Substring(end);
        const string section="مرحله ۱۵ — وسایل ساخته‌شدهٔ حمام و نام اجزای وسایل";int previous=text.IndexOf(section,StringComparison.Ordinal);string later="";
        if(previous>=0){int next=text.IndexOf("\nمرحله ",previous+section.Length,StringComparison.Ordinal);if(next>=0)later="\n"+text.Substring(next);text=text.Substring(0,previous).TrimEnd();}
        text+="\n\n"+section+"\nDeutsch | فارسی | English\n";
        foreach(Word word in catalog.vocabulary)text+=word.de+" | "+word.fa+" | "+word.en+"\n";
        int pending=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^\[ \] (der|die|das) ").Count;
        text+="\nگروه‌های F10 و F11 اکنون ساخته شده‌اند. تعداد موارد هنوز اضافه‌نشده در فهرست: "+pending+". نام‌های اجزا به‌صورت جداگانه قابل شناسایی‌اند و عمل اصلی باز کردن، برداشتن یا استفاده از صاحب جزء را عوض نمی‌کنند. وسایل حمام ثابت‌اند؛ این نسخه شست‌وشو یا تخلیهٔ واقعی را شبیه‌سازی نمی‌کند.\n";
        File.WriteAllText(path,text+later);
    }
}
