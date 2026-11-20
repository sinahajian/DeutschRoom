using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Next twenty pending instances: F09, F12, F13 and the first four F14 rows.
public static class HouseAccessoryBuilder
{
    public const int AddedDoors=2;
    [Serializable] public sealed class Word {public string id,de,fa,en,room;public int order;}
    [Serializable] public sealed class Catalog {public List<Word> fixtures=new List<Word>();public List<Word> vocabulary=new List<Word>();}
    static Catalog catalog;static Transform root;
    static Material oak,metal,white,dark,blue,fabric,soap,basket,bristles,mirror;
    [MenuItem("Tools/DeutschRoom/Rebuild Accessories F09 F12-F14")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var old=GameObject.Find("HouseAccessories");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        if(!AssetDatabase.IsValidFolder("Assets/Art/HouseAccessories"))AssetDatabase.CreateFolder("Assets/Art","HouseAccessories");
        oak=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/Oak.mat");metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/BrushedSteel.mat");
        white=Mat("ApplianceEnamel",new Color(.87f,.89f,.86f),.62f);dark=Mat("Rubber",new Color(.025f,.035f,.04f),.23f);
        blue=Mat("DustyBlue",new Color(.16f,.34f,.43f),.4f);fabric=Mat("TowelFabric",new Color(.55f,.72f,.68f),.12f,true);fabric.SetFloat("_Cull",0);
        soap=Mat("Soap",new Color(.87f,.70f,.45f),.48f);basket=Mat("BasketWillow",new Color(.54f,.38f,.23f),.18f);bristles=Mat("Bristles",new Color(.72f,.67f,.50f),.1f);
        mirror=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/LivingHall/Mirror.mat");
        root=new GameObject("HouseAccessories").transform;catalog=new Catalog();
        Hall();Bathroom();Laundry();Cleaning();
        File.WriteAllText("Documentation/HouseAccessoryContent.json",JsonUtility.ToJson(catalog,true)+"\n");Inventory();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("HOUSE_ACCESSORIES_BUILD_PASS: twenty new targets at orders 50-69; washer door and detergent drawer open.");
    }
    public static void BuildAndCapture()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    public static void BuildReviewAndValidate()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");InteractionValidation.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    static Material Mat(string name,Color color,float smooth,bool fabric=false)
    {
        string path="Assets/Art/HouseAccessories/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
        if(fabric){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VisualPolish/cloth-albedo.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VisualPolish/cloth-normal.png"));m.SetFloat("_BumpScale",.22f);m.EnableKeyword("_NORMALMAP");}
        return m;
    }
    static void Name(KitchenInteractable item,string de){var s=new SerializedObject(item);s.FindProperty("displayName").stringValue=de;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Record(string de,string fa,string en){if(!catalog.vocabulary.Exists(w=>w.de==de))catalog.vocabulary.Add(new Word{de=de,fa=fa,en=en});}
    static Transform Item(string id,string de,string fa,string en,int order,Vector3 at,float yaw=0)
    {
        var t=new GameObject(id).transform;t.SetParent(root,false);t.position=at;t.rotation=Quaternion.Euler(0,yaw,0);
        Name(t.gameObject.AddComponent<VocabularyObject>(),de);var s=new SerializedObject(t.gameObject.AddComponent<FixedLearningTarget>());s.FindProperty("order").intValue=order;s.ApplyModifiedPropertiesWithoutUndo();
        catalog.fixtures.Add(new Word{id=id,de=de,fa=fa,en=en,room=id.StartsWith("Hall_")?"Hallway":id.StartsWith("Bedroom2_")?"Bedroom2":"BathroomWC",order=order});Record(de,fa,en);return t;
    }
    static void Part(Transform t,string de,string fa,string en){var s=new SerializedObject(t.gameObject.AddComponent<VocabularyPart>());s.FindProperty("displayName").stringValue=de;s.ApplyModifiedPropertiesWithoutUndo();Record(de,fa,en);}
    static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material m)
    {
        var t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider)).transform;t.SetParent(p,false);t.localPosition=at;t.localScale=size;
        t.GetComponent<MeshFilter>().sharedMesh=VisualPolishBuilder.FurnitureBox(size);t.GetComponent<Renderer>().sharedMaterial=m;return t;
    }
    static Transform Oval(Transform p,string name,Vector3 at,Vector3 size,Material m)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<SphereCollider>());var c=o.AddComponent<MeshCollider>();c.sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;c.convex=true;return o.transform;
    }
    static Transform Tube(Transform p,string name,Vector3 a,Vector3 b,float r,Material m)
    {var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=(a+b)/2;o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);o.transform.localScale=new Vector3(r*2,Vector3.Distance(a,b)/2,r*2);o.GetComponent<Renderer>().sharedMaterial=m;return o.transform;}
    static void Anchor(Transform t,Vector3 at){var point=new GameObject("IdentificationPoint").transform;point.SetParent(t,false);point.localPosition=at;var s=new SerializedObject(t.GetComponent<KitchenInteractable>());s.FindProperty("identificationPoint").objectReferenceValue=point;s.ApplyModifiedPropertiesWithoutUndo();}
    // Profile is in metres: hollow containers and rings have a real inner surface.
    static Transform Lathe(Transform p,string name,Vector2[] profile,Vector3 at,Material material)
    {
        string path="Assets/Art/HouseAccessories/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            const int n=48;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int j=0;j<profile.Length;j++)for(int i=0;i<=n;i++){float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));uv.Add(new Vector2((float)i/n,(float)j/(profile.Length-1)));}
            for(int j=0;j<profile.Length-1;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i,b=a+n+1;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        var t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider)).transform;t.SetParent(p,false);t.localPosition=at;t.GetComponent<MeshFilter>().sharedMesh=mesh;t.GetComponent<MeshCollider>().sharedMesh=mesh;t.GetComponent<Renderer>().sharedMaterial=material;return t;
    }
    static Transform Ring(Transform p,string name,Vector3 at,float radius,float thickness,Material m,bool upright=true)
    {
        var profile=new Vector2[17];for(int i=0;i<profile.Length;i++){float a=i*Mathf.PI*2/(profile.Length-1);profile[i]=new Vector2(radius+Mathf.Cos(a)*thickness,Mathf.Sin(a)*thickness);}
        var t=Lathe(p,name,profile,at,m);if(upright)t.localRotation=Quaternion.Euler(90,0,0);return t;
    }
    static Transform Cup(Transform p,string name,Vector3 at,float radius,float height,Material m)
    {return Lathe(p,name,new[]{new Vector2(0,0),new Vector2(radius*.8f,0),new Vector2(radius,height),new Vector2(radius-.012f,height),new Vector2(radius*.8f-.012f,.025f),new Vector2(0,.025f)},at,m);}
    static void Hall()
    {
        var t=Item("Hall_Key","der Schlüssel","کلید","key",50,new Vector3(2.10f,1.43f,-6.12f),-90);
        Ring(t,"KeyBow",Vector3.up*.025f,.025f,.006f,metal);
        Part(Box(t,"KeyShaft",new Vector3(0,-.035f,0),new Vector3(.012f,.085f,.008f),metal),"der Schlüsselschaft","ساقه کلید","key shaft");
        for(int i=0;i<3;i++)Part(Box(t,"KeyTooth",new Vector3(.012f,-.045f-i*.016f,0),new Vector3(.018f,.009f,.008f),metal),"der Schlüsselbart","دندانه کلید","key bit");
        t=Item("Hall_KeyRack","das Schlüsselbrett","آویز کلید","key rack",51,new Vector3(1.97f,1.51f,-6.12f),-90);
        Box(t,"KeyRackBoard",Vector3.zero,new Vector3(.46f,.20f,.035f),oak);
        for(int i=0;i<3;i++){float x=-.14f+i*.14f;Part(Tube(t,"KeyHook",new Vector3(x,-.02f,-.02f),new Vector3(x,-.04f,-.10f),.006f,metal),"der Haken","قلاب","hook");Tube(t,"HookEnd",new Vector3(x,-.04f,-.10f),new Vector3(x,-.01f,-.10f),.006f,metal);}
        Anchor(t,new Vector3(-.20f,.07f,-.03f));
        t=Item("Hall_Umbrella","der Regenschirm","چتر","umbrella",52,new Vector3(2.10f,.12f,-11.75f));
        Tube(t,"UmbrellaShaft",Vector3.zero,new Vector3(0,.94f,0),.007f,metal);
        Part(Oval(t,"FoldedCanopy",new Vector3(0,.42f,0),new Vector3(.115f,.68f,.115f),blue),"das Schirmdach","پارچه چتر","umbrella canopy");
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Tube(t,"UmbrellaFold",new Vector3(Mathf.Cos(a)*.018f,.12f,Mathf.Sin(a)*.018f),new Vector3(Mathf.Cos(a)*.05f,.70f,Mathf.Sin(a)*.05f),.002f,dark);}
        Vector3 last=new Vector3(0,.94f,0);for(int i=1;i<=20;i++){float a=i*Mathf.PI*1.15f/20;Vector3 next=new Vector3(.047f-Mathf.Cos(a)*.047f,.94f+Mathf.Sin(a)*.047f,0);Tube(t,"UmbrellaHandle",last,next,.012f,oak);last=next;}
        Anchor(t,new Vector3(0,.67f,0));
        t=Item("Hall_UmbrellaStand","der Schirmständer","جاچتری","umbrella stand",53,new Vector3(2.10f,0,-11.75f));
        Cup(t,"UmbrellaStandCup",Vector3.zero,.16f,.43f,blue);Anchor(t,new Vector3(.16f,.23f,0));
        t=Item("Hall_Clock","die Wanduhr","ساعت دیواری","wall clock",54,new Vector3(1.96f,2.12f,-9.0f),-90);
        var face=Tube(t,"ClockFace",new Vector3(0,0,-.015f),new Vector3(0,0,.015f),.25f,white);
        Ring(t,"ClockRim",new Vector3(0,0,-.022f),.25f,.017f,oak);
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=Box(t,"HourMark",new Vector3(Mathf.Sin(a)*.213f,Mathf.Cos(a)*.213f,-.033f),new Vector3(.012f,i%3==0?.035f:.022f,.004f),dark);tick.localRotation=Quaternion.Euler(0,0,-i*30);}
        Part(Tube(t,"HourHand",new Vector3(0,0,-.04f),new Vector3(-.085f,.07f,-.04f),.008f,dark),"der Stundenzeiger","عقربه ساعت‌شمار","hour hand");
        Part(Tube(t,"MinuteHand",new Vector3(0,0,-.045f),new Vector3(.17f,.035f,-.045f),.005f,dark),"der Minutenzeiger","عقربه دقیقه‌شمار","minute hand");
        Oval(t,"ClockPin",new Vector3(0,0,-.05f),new Vector3(.025f,.025f,.012f),metal);
    }
    static void Bathroom()
    {
        var t=Item("Bath_Mirror","der Spiegel","آینه","mirror",55,new Vector3(6.15f,1.72f,-11.12f));
        Box(t,"MirrorBacking",Vector3.zero,new Vector3(1.15f,.77f,.035f),oak);
        Part(Box(t,"MirrorSurface",new Vector3(0,0,-.023f),new Vector3(1.07f,.69f,.008f),mirror),"die Spiegelfläche","سطح آینه","mirror surface");
        t=Item("Bath_Towel","das Handtuch","حوله","towel",56,new Vector3(7.48f,1.13f,-11.30f));
        Cloth(t,"HangingTowel",.52f,.68f,fabric);Anchor(t,new Vector3(0,-.34f,-.024f));
        t=Item("Bath_TowelRail","der Handtuchhalter","آویز حوله","towel rail",57,new Vector3(7.48f,1.13f,-11.16f));
        foreach(float x in new[]{-.35f,.35f})Tube(t,"RailMount",new Vector3(x,0,0),new Vector3(x,0,-.14f),.018f,metal);
        Tube(t,"TowelBar",new Vector3(-.35f,0,-.14f),new Vector3(.35f,0,-.14f),.014f,metal);Anchor(t,new Vector3(.33f,0,-.14f));
        t=Item("Bath_Soap","die Seife","صابون","soap",58,new Vector3(6.65f,.86f,-11.57f),-12);
        Oval(t,"SoapBar",new Vector3(0,.015f,0),new Vector3(.105f,.04f,.065f),soap);
        Part(Oval(t,"SoapDish",new Vector3(0,-.016f,0),new Vector3(.14f,.022f,.10f),white),"die Seifenschale","جاصابونی","soap dish");
        t=Item("Bath_ToiletBrush","die Toilettenbürste","برس توالت","toilet brush",59,new Vector3(4.35f,0,-15.45f));
        Cup(t,"ToiletBrushHolder",Vector3.zero,.075f,.22f,white);
        Tube(t,"BrushStem",Vector3.up*.12f,Vector3.up*.47f,.008f,metal);Part(Oval(t,"BrushGrip",Vector3.up*.46f,new Vector3(.035f,.12f,.035f),dark),"der Griff","دستگیره","handle");
        Oval(t,"BrushHead",Vector3.up*.16f,new Vector3(.11f,.12f,.11f),bristles);Anchor(t,Vector3.up*.43f);
        t=Item("Bath_PaperHolder","der Toilettenpapierhalter","نگهدارنده دستمال توالت","toilet roll holder",60,new Vector3(3.97f,.73f,-15.20f),-90);
        Box(t,"RollWallMount",Vector3.zero,new Vector3(.22f,.075f,.025f),metal);
        foreach(float x in new[]{-.12f,.12f})Tube(t,"RollSupport",new Vector3(x,0,0),new Vector3(x,0,-.13f),.012f,metal);
        Tube(t,"RollSpindle",new Vector3(-.13f,0,-.13f),new Vector3(.13f,0,-.13f),.008f,metal);
        var roll=Lathe(t,"PaperRoll",new[]{new Vector2(.027f,-.05f),new Vector2(.075f,-.05f),new Vector2(.075f,.05f),new Vector2(.027f,.05f),new Vector2(.027f,-.05f)},new Vector3(0,0,-.13f),white);roll.localRotation=Quaternion.Euler(0,0,90);Part(roll,"das Toilettenpapier","دستمال توالت","toilet paper");
        Box(t,"PaperTail",new Vector3(0,-.09f,-.19f),new Vector3(.10f,.15f,.002f),white);Anchor(t,new Vector3(.12f,0,-.13f));
    }
    static void Cloth(Transform p,string name,float width,float height,Material m)
    {
        string path="Assets/Art/HouseAccessories/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            const int nx=24,ny=24;var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++){float u=(float)i/nx,w=(float)j/ny;v.Add(new Vector3((u-.5f)*width,-w*height,-.018f*Mathf.Sin(u*Mathf.PI*8)*w));uv.Add(new Vector2(u,w));}
            for(int j=0;j<ny;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i;tri.AddRange(new[]{a,a+1,a+nx+1,a+1,a+nx+2,a+nx+1});}
            mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        var t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider)).transform;t.SetParent(p,false);t.GetComponent<MeshFilter>().sharedMesh=mesh;t.GetComponent<Renderer>().sharedMaterial=m;t.GetComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Laundry()
    {
        var t=Item("Bath_Washer","die Waschmaschine","ماشین لباس‌شویی","washing machine",61,new Vector3(8.65f,0,-11.50f));
        // Hollow front-load casing leaves the opening free when the door swings.
        foreach(float x in new[]{-.31f,.31f})Box(t,"WasherSide",new Vector3(x,.45f,0),new Vector3(.035f,.86f,.60f),white);
        foreach(float y in new[]{.05f,.87f})Box(t,"WasherHorizontal",new Vector3(0,y,0),new Vector3(.64f,.045f,.60f),white);
        Box(t,"WasherBack",new Vector3(0,.45f,.28f),new Vector3(.60f,.80f,.025f),white);
        Box(t,"ControlPanel",new Vector3(0,.76f,-.30f),new Vector3(.60f,.15f,.025f),white);
        Box(t,"LowerFront",new Vector3(0,.12f,-.30f),new Vector3(.60f,.13f,.025f),white);
        foreach(float x in new[]{-.285f,.285f})Box(t,"FrontJamb",new Vector3(x,.42f,-.30f),new Vector3(.055f,.49f,.025f),white);
        var drum=Cup(t,"WasherDrum",new Vector3(0,.42f,.10f),.24f,.35f,metal);drum.localRotation=Quaternion.Euler(-90,0,0);
        // Perforations on the visible drum back are named with their drum owner.
        Part(drum,"die Waschtrommel","دیگ لباس‌شویی","washing drum");
        var hinge=new GameObject("WasherDoorHinge").transform;hinge.SetParent(t,false);hinge.localPosition=new Vector3(-.245f,.42f,-.33f);
        var open=hinge.gameObject.AddComponent<Openable>();Name(open,"die Waschmaschine");var s=new SerializedObject(open);s.FindProperty("openAngle").floatValue=-105;s.ApplyModifiedPropertiesWithoutUndo();
        Part(Ring(hinge,"WasherDoorRim",new Vector3(.245f,0,0),.225f,.025f,white),"die Waschmaschinentür","در ماشین لباس‌شویی","washing-machine door");
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/Glass.mat");Oval(hinge,"DoorWindow",new Vector3(.245f,0,0),new Vector3(.405f,.405f,.022f),glass);
        Part(Box(hinge,"WasherHandle",new Vector3(.43f,0,-.045f),new Vector3(.035f,.12f,.045f),dark),"der Türgriff","دستگیره در","door handle");
        var drawer=new GameObject("DetergentDrawer").transform;drawer.SetParent(t,false);drawer.localPosition=new Vector3(-.17f,.77f,-.33f);
        var slide=drawer.gameObject.AddComponent<Openable>();Name(slide,"die Waschmaschine");s=new SerializedObject(slide);s.FindProperty("sliding").boolValue=true;s.FindProperty("slideOffset").vector3Value=new Vector3(0,0,-.18f);s.ApplyModifiedPropertiesWithoutUndo();
        Part(Box(drawer,"DetergentDrawerFront",Vector3.zero,new Vector3(.24f,.10f,.025f),white),"das Waschmittelfach","محفظه مواد شوینده","detergent compartment");
        Box(drawer,"DetergentTray",new Vector3(0,-.045f,.12f),new Vector3(.22f,.025f,.24f),white);
        var knob=Tube(t,"ProgramKnob",new Vector3(.07f,.77f,-.33f),new Vector3(.07f,.77f,-.36f),.035f,metal);Part(knob,"der Programmwähler","پیچ انتخاب برنامه","program selector");
        Part(Box(t,"WasherDisplay",new Vector3(.21f,.77f,-.32f),new Vector3(.09f,.045f,.008f),dark),"das Display","نمایشگر","display");Anchor(t,new Vector3(.27f,.61f,-.32f));
        t=Item("Bath_LaundryBasket","der Wäschekorb","سبد لباس","laundry basket",62,new Vector3(8.9f,0,-12.55f));
        Basket(t);Anchor(t,new Vector3(-.23f,.24f,0));
        t=Item("Bath_Detergent","das Waschmittel","مایع یا پودر لباس‌شویی","laundry detergent",63,new Vector3(8.80f,.90f,-11.50f));
        Box(t,"DetergentBottle",new Vector3(0,.12f,0),new Vector3(.13f,.24f,.09f),blue);
        Tube(t,"BottleNeck",new Vector3(0,.24f,0),new Vector3(0,.27f,0),.027f,blue);
        Part(Tube(t,"BottleCap",new Vector3(0,.27f,0),new Vector3(0,.30f,0),.032f,white),"der Verschluss","درپوش","cap");Box(t,"BottleLabel",new Vector3(0,.12f,-.046f),new Vector3(.09f,.10f,.002f),white);
        t=Item("Bedroom2_DryingRack","der Wäscheständer","بند رخت تاشو","drying rack",64,new Vector3(5.9f,0,-10.28f));
        foreach(float x in new[]{-.48f,.48f}){Tube(t,"RackLeg",new Vector3(x,.03f,-.31f),new Vector3(x,1.02f,.26f),.012f,metal);Tube(t,"RackLeg",new Vector3(x,.03f,.31f),new Vector3(x,1.02f,-.26f),.012f,metal);}
        foreach(float z in new[]{-.28f,.28f})Tube(t,"RackFrame",new Vector3(-.55f,1.02f,z),new Vector3(.55f,1.02f,z),.012f,white);
        for(int i=0;i<9;i++){float x=-.5f+i*.125f;Tube(t,"DryingBar",new Vector3(x,1.02f,-.28f),new Vector3(x,1.02f,.28f),.004f,white);}
        Anchor(t,new Vector3(.45f,1.02f,-.28f));
        t=Item("Bedroom2_ClothesPeg","die Wäscheklammer","گیره لباس","clothes peg",65,new Vector3(5.9f,1.04f,-10.28f));
        foreach(float x in new[]{-.009f,.009f}){var jaw=Box(t,"PegJaw",new Vector3(x,0,0),new Vector3(.012f,.018f,.075f),oak);jaw.localRotation=Quaternion.Euler(0,0,x<0?-8:8);}
        Part(Ring(t,"PegSpring",new Vector3(0,.01f,0),.009f,.002f,metal),"die Feder","فنر","spring");
    }
    static void Basket(Transform t)
    {
        Box(t,"BasketBase",new Vector3(0,.025f,0),new Vector3(.44f,.05f,.34f),basket);
        for(int i=0;i<8;i++){float y=.06f+i*.043f;foreach(float z in new[]{-.18f,.18f})Box(t,"WovenRail",new Vector3(0,y,z),new Vector3(.46f,.021f,.018f),basket);foreach(float x in new[]{-.23f,.23f})Box(t,"WovenRail",new Vector3(x,y,0),new Vector3(.018f,.021f,.36f),basket);}
        for(int i=0;i<10;i++){float x=-.21f+i*.047f;foreach(float z in new[]{-.18f,.18f})Box(t,"BasketUpright",new Vector3(x,.20f,z),new Vector3(.012f,.37f,.018f),basket);}
        foreach(float x in new[]{-.23f,.23f})for(int i=0;i<7;i++)Box(t,"BasketUpright",new Vector3(x,.2f,-.15f+i*.05f),new Vector3(.018f,.37f,.012f),basket);
        foreach(float z in new[]{-.18f,.18f})Box(t,"BasketRim",new Vector3(0,.40f,z),new Vector3(.48f,.035f,.026f),basket);
        foreach(float x in new[]{-.23f,.23f}){Box(t,"BasketRim",new Vector3(x,.40f,0),new Vector3(.026f,.035f,.36f),basket);var handle=Ring(t,"BasketHandle",new Vector3(x,.44f,0),.06f,.009f,basket);handle.localRotation=Quaternion.Euler(0,0,90);Part(handle,"der Griff","دستگیره","handle");}
    }
    static void Cleaning()
    {
        var t=Item("Bath_Vacuum","der Staubsauger","جاروبرقی","vacuum cleaner",66,new Vector3(6.1f,0,-15.45f));
        Oval(t,"VacuumHousing",new Vector3(0,.19f,0),new Vector3(.42f,.31f,.52f),blue);
        foreach(float x in new[]{-.22f,.22f}){var wheel=Tube(t,"VacuumWheel",new Vector3(x-.025f,.13f,.10f),new Vector3(x+.025f,.13f,.10f),.10f,dark);Part(wheel,"das Rad","چرخ","wheel");}
        Part(Box(t,"VacuumHandle",new Vector3(0,.36f,.04f),new Vector3(.18f,.035f,.045f),dark),"der Griff","دستگیره","handle");
        var hose=new GameObject("VacuumHose").transform;hose.SetParent(t,false);Vector3 last=new Vector3(0,.23f,-.23f);
        for(int i=1;i<=32;i++){float u=i/32f;Vector3 next=new Vector3(.5f*u,.23f+Mathf.Sin(u*Mathf.PI)*.63f,-.23f-.14f*Mathf.Sin(u*Mathf.PI));var piece=Tube(hose,"HoseSegment",last,next,.022f,dark);if(i==16)Part(piece,"der Saugschlauch","شلنگ مکش","vacuum hose");last=next;}
        Part(Tube(t,"VacuumWand",new Vector3(.5f,.15f,-.23f),new Vector3(.5f,1.15f,.08f),.018f,metal),"das Saugrohr","لوله مکش","vacuum tube");
        Part(Box(t,"VacuumFloorHead",new Vector3(.5f,.055f,-.23f),new Vector3(.30f,.07f,.16f),dark),"die Bodendüse","سری کف جاروبرقی","floor nozzle");Anchor(t,new Vector3(0,.32f,-.1f));
        t=Item("Bath_Broom","der Besen","جارو","broom",67,new Vector3(6.95f,0,-15.77f));
        Part(Tube(t,"BroomHandle",new Vector3(0,.15f,0),new Vector3(0,1.35f,0),.015f,oak),"der Stiel","دسته بلند","long handle");
        Box(t,"BroomHead",new Vector3(0,.16f,0),new Vector3(.32f,.06f,.075f),blue);
        for(int i=0;i<25;i++)Tube(t,"BroomBristle",new Vector3(-.15f+i*.0125f,.13f,0),new Vector3(-.16f+i*.0133f,.015f,0),.006f,bristles);
        Anchor(t,new Vector3(0,.75f,0));
        t=Item("Bath_Dustpan","das Kehrblech","خاک‌انداز","dustpan",68,new Vector3(7.0f,.015f,-15.20f),20);
        Box(t,"DustpanTray",new Vector3(0,.012f,0),new Vector3(.26f,.025f,.23f),blue);
        foreach(float x in new[]{-.125f,.125f})Box(t,"DustpanRim",new Vector3(x,.045f,.01f),new Vector3(.012f,.065f,.21f),blue);
        Box(t,"DustpanBack",new Vector3(0,.045f,.11f),new Vector3(.26f,.065f,.015f),blue);
        Part(Tube(t,"DustpanHandle",new Vector3(0,.055f,.11f),new Vector3(0,.12f,.30f),.018f,blue),"der Griff","دستگیره","handle");
        t=Item("Bath_Mop","der Wischmopp","تی","mop",69,new Vector3(7.45f,0,-15.55f));
        Part(Tube(t,"MopHandle",new Vector3(0,.10f,0),new Vector3(0,1.42f,0),.014f,metal),"der Stiel","دسته بلند","long handle");
        Box(t,"MopHead",new Vector3(0,.07f,0),new Vector3(.29f,.06f,.12f),blue);
        for(int i=0;i<17;i++)Tube(t,"MopStrand",new Vector3(-.13f+i*.016f,.05f,0),new Vector3(-.14f+i*.017f,.015f,(i%2==0?1:-1)*.10f),.009f,fabric);
        Anchor(t,new Vector3(0,.75f,0));
    }
    static void Inventory()
    {
        string path="Documentation/tools.txt",text=File.ReadAllText(path);
        foreach(string group in new[]{"F09","F12","F13","F14"})
        {
            int a=text.IndexOf("گروه "+group,StringComparison.Ordinal),b=text.IndexOf("\nگروه ",a+8,StringComparison.Ordinal);if(a<0||b<a)throw new InvalidOperationException("Missing inventory group "+group);
            string section=text.Substring(a,b-a);foreach(var w in catalog.fixtures)section=section.Replace("[ ] "+w.de+" |",w.de+" |");text=text.Substring(0,a)+section+text.Substring(b);
        }
        const string heading="مرحله ۱۸ — لوازم راهرو، حمام، رخت‌شویی و نظافت";int start=text.IndexOf(heading,StringComparison.Ordinal);string later="";
        if(start>=0){int next=text.IndexOf("\nمرحله ",start+heading.Length,StringComparison.Ordinal);if(next>=0)later="\n"+text.Substring(next);text=text.Substring(0,start).TrimEnd();}
        text+="\n\n"+heading+"\nDeutsch | فارسی | English\n";foreach(var w in catalog.vocabulary)text+=w.de+" | "+w.fa+" | "+w.en+"\n";
        int pending=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^\[ \] (der|die|das) ").Count;
        text+="\n۲۰ مورد F09، F12، F13 و چهار ردیف اول F14 ساخته شدند؛ تعداد موارد در انتظار: "+pending+". اشیا هدف شناسایی ثابت‌اند. در و کشوی لباس‌شویی باز می‌شوند؛ چرخهٔ شست‌وشو، نظافت، ساعت زنده و بازتاب زندهٔ آینه شبیه‌سازی نشده‌اند.\n";
        File.WriteAllText(path,text+later);
    }
}
