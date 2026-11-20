using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Last ten of the eighty planned furnishing instances; preserve all earlier layers.
public static class FinalFurnishingBuilder
{
    [Serializable] public sealed class Word {public string id,de,fa,en,room;public int order;}
    [Serializable] public sealed class Catalog {public List<Word> fixtures=new List<Word>();public List<Word> vocabulary=new List<Word>();}
    static Catalog catalog;static Transform root;
    static Material metal,white,dark,blue,paper,glass;
    [MenuItem("Tools/DeutschRoom/Rebuild Final Ten F14-F16")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var old=GameObject.Find("FinalFurnishings");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        if(!AssetDatabase.IsValidFolder("Assets/Art/FinalFurnishings"))AssetDatabase.CreateFolder("Assets/Art","FinalFurnishings");
        metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/BrushedSteel.mat");glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/Glass.mat");
        white=Mat("WarmEnamel",new Color(.88f,.88f,.81f),.55f);dark=Mat("Charcoal",new Color(.025f,.035f,.04f),.35f);
        blue=Mat("BlueAppliance",new Color(.16f,.28f,.33f),.55f);paper=Mat("KitchenPaper",new Color(.92f,.90f,.82f),.12f,true);
        root=new GameObject("FinalFurnishings").transform;catalog=new Catalog();
        BinBag();Fittings();Kitchen();
        File.WriteAllText("Documentation/FinalFurnishingContent.json",JsonUtility.ToJson(catalog,true)+"\n");Inventory();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("FINAL_FURNISHING_BUILD_PASS: last ten planned targets at orders 70-79; all eighty planned instances built.");
    }
    public static void BuildReviewAndValidate()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");InteractionValidation.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    static Material Mat(string name,Color color,float smooth,bool fabric=false)
    {
        string path="Assets/Art/FinalFurnishings/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
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
        catalog.fixtures.Add(new Word{id=id,de=de,fa=fa,en=en,room=id.StartsWith("Hall_")?"Hallway":id.StartsWith("Living_")?"LivingRoom":id.StartsWith("Kitchen_")?"Kitchen":"BathroomWC",order=order});Record(de,fa,en);return t;
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
        string path="Assets/Art/FinalFurnishings/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
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
    static void BinBag()
    {
        var t=Item("Bath_BinBag","die Mülltüte","کیسه زباله","bin bag",70,new Vector3(5.48f,0,-15.52f));
        Oval(t,"BagBody",new Vector3(0,.20f,0),new Vector3(.31f,.39f,.29f),dark);
        for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Tube(t,"BagGather",new Vector3(Mathf.Cos(a)*.085f,.34f,Mathf.Sin(a)*.085f),new Vector3(0,.41f,0),.009f,dark);}
        Part(Oval(t,"BagKnot",new Vector3(0,.41f,0),new Vector3(.055f,.04f,.055f),dark),"der Knoten","گره","knot");
        foreach(float x in new[]{-.02f,.02f}){var tie=Oval(t,"BagTie",new Vector3(x,.44f,0),new Vector3(.035f,.075f,.014f),dark);tie.localRotation=Quaternion.Euler(0,0,x<0?25:-25);}
    }
    static void Fittings()
    {
        var t=Item("Hall_LightSwitch","der Lichtschalter","کلید برق","light switch",71,new Vector3(1.97f,1.18f,-8.55f),-90);
        Box(t,"SwitchPlate",Vector3.zero,new Vector3(.10f,.10f,.015f),white);
        var mechanism=new GameObject("LightSwitchMechanism").transform;mechanism.SetParent(t,false);
        var switchAction=mechanism.gameObject.AddComponent<RoomLightSwitch>();Name(switchAction,"der Lichtschalter");
        var rocker=Box(mechanism,"SwitchRocker",new Vector3(0,0,-.015f),new Vector3(.065f,.075f,.018f),white);rocker.localRotation=Quaternion.Euler(-9,0,0);
        Part(rocker,"die Schalterwippe","صفحه کلید برق","switch rocker");
        var lights=new List<Light>();lights.Add(GameObject.Find("HomeExpansion/LivingRoom/AmbientRoomLight").GetComponent<Light>());
        lights.AddRange(GameObject.Find("Living_FloorLamp").GetComponentsInChildren<Light>());lights.AddRange(GameObject.Find("Living_CeilingLight").GetComponentsInChildren<Light>());
        var data=new SerializedObject(switchAction);data.FindProperty("lights").arraySize=lights.Count;for(int i=0;i<lights.Count;i++)data.FindProperty("lights").GetArrayElementAtIndex(i).objectReferenceValue=lights[i];data.FindProperty("rocker").objectReferenceValue=rocker;data.ApplyModifiedPropertiesWithoutUndo();
        t=Item("Hall_PowerSocket","die Steckdose","پریز","power socket",72,new Vector3(1.97f,.34f,-8.55f),-90);
        Box(t,"SocketPlate",Vector3.zero,new Vector3(.095f,.095f,.015f),white);
        Tube(t,"SocketRecess",new Vector3(0,0,-.009f),new Vector3(0,0,-.014f),.032f,dark);
        Tube(t,"SocketInsert",new Vector3(0,0,-.015f),new Vector3(0,0,-.017f),.028f,white);
        foreach(float x in new[]{-.013f,.013f})Tube(t,"SocketPinHole",new Vector3(x,0,-.019f),new Vector3(x,0,-.021f),.004f,dark);
        foreach(float y in new[]{-.025f,.025f})Box(t,"EarthContact",new Vector3(0,y,-.019f),new Vector3(.009f,.008f,.004f),metal);
        t=Item("Living_Radiator","der Heizkörper","رادیاتور","radiator",73,new Vector3(-4.70f,.22f,-7.8f),-90);
        foreach(float y in new[]{.03f,.47f})Tube(t,"RadiatorHeader",new Vector3(-.55f,y,0),new Vector3(.55f,y,0),.025f,white);
        for(int i=0;i<13;i++)Part(Box(t,"RadiatorFin",new Vector3(-.54f+i*.09f,.25f,0),new Vector3(.065f,.46f,.09f),white),"die Heizrippe","پره رادیاتور","radiator fin");
        foreach(float x in new[]{-.39f,.39f})Box(t,"WallBracket",new Vector3(x,.25f,.10f),new Vector3(.035f,.32f,.13f),metal);
        Anchor(t,new Vector3(0,.25f,-.055f));
        t=Item("Living_Thermostat","das Thermostat","ترموستات","thermostat",74,new Vector3(-4.60f,.66f,-8.43f),-90);
        Part(Tube(t,"RadiatorValve",new Vector3(0,-.16f,.10f),new Vector3(0,0,.10f),.018f,metal),"das Heizungsventil","شیر رادیاتور","radiator valve");
        Tube(t,"ThermostatNeck",new Vector3(0,0,.1f),Vector3.zero,.024f,metal);
        Part(Tube(t,"ThermostatDial",Vector3.zero,new Vector3(0,0,-.07f),.037f,white),"der Drehregler","پیچ تنظیم","control dial");
        for(int i=0;i<5;i++)Box(t,"DialTick",new Vector3(-.025f+i*.012f,.022f,-.074f),new Vector3(.003f,.012f,.002f),dark);
        Anchor(t,new Vector3(0,0,-.074f));
    }
    static void Kitchen()
    {
        var t=Item("Kitchen_Scissors","die Küchenschere","قیچی آشپزخانه","kitchen scissors",75,new Vector3(.77f,.963f,4.20f),-20);
        for(int side=-1;side<=1;side+=2)
        {
            var handle=Ring(t,"ScissorGrip",new Vector3(side*.025f,.006f,-.048f),.019f,.005f,blue,false);Part(handle,"der Scherengriff","دسته قیچی","scissor handle");
            var blade=Box(t,"ScissorBlade",new Vector3(-side*.012f,.007f,.035f),new Vector3(.011f,.006f,.095f),metal);blade.localRotation=Quaternion.Euler(0,side*10,0);Part(blade,"die Scherenklinge","تیغه قیچی","scissor blade");
        }
        Tube(t,"ScissorPivot",Vector3.up*.007f,Vector3.up*.016f,.006f,metal);
        t=Item("Kitchen_PaperRoll","die Küchenrolle","دستمال کاغذی رولی آشپزخانه","kitchen roll",76,new Vector3(-1.35f,.955f,4.70f));
        Part(Tube(t,"PaperRollStand",Vector3.zero,Vector3.up*.015f,.085f,metal),"der Küchenrollenhalter","پایه دستمال رولی","kitchen-roll holder");
        Lathe(t,"PaperRoll",new[]{new Vector2(.018f,.018f),new Vector2(.066f,.018f),new Vector2(.066f,.245f),new Vector2(.018f,.245f),new Vector2(.018f,.018f)},Vector3.zero,paper);
        Tube(t,"PaperStandRod",Vector3.zero,Vector3.up*.27f,.012f,metal);Box(t,"LoosePaper",new Vector3(.055f,.12f,-.055f),new Vector3(.10f,.21f,.002f),paper);Anchor(t,new Vector3(0,.12f,-.066f));
        t=Item("Kitchen_Toaster","der Toaster","توستر","toaster",77,new Vector3(-2.04f,.955f,4.45f));
        Box(t,"ToasterBase",new Vector3(0,.022f,0),new Vector3(.32f,.044f,.20f),dark);
        Box(t,"ToasterBody",new Vector3(0,.135f,0),new Vector3(.30f,.20f,.18f),metal);
        foreach(float z in new[]{-.043f,.043f}){Part(Box(t,"ToastSlot",new Vector3(0,.237f,z),new Vector3(.235f,.006f,.028f),dark),"der Toastschlitz","شیار نان توستر","toast slot");for(int i=0;i<9;i++)Box(t,"ToastGuide",new Vector3(-.10f+i*.025f,.241f,z),new Vector3(.004f,.005f,.025f),metal);}
        Part(Box(t,"ToasterLever",new Vector3(.17f,.13f,0),new Vector3(.045f,.025f,.05f),dark),"der Bedienhebel","اهرم کنترل","control lever");
        Part(Tube(t,"BrowningDial",new Vector3(.08f,.085f,-.095f),new Vector3(.08f,.085f,-.11f),.022f,dark),"der Bräunungsregler","پیچ تنظیم برشتگی","browning control");
        t=Item("Kitchen_CoffeeMachine","die Kaffeemaschine","قهوه‌ساز","coffee machine",78,new Vector3(-3.20f,.955f,4.48f));
        Box(t,"CoffeeBase",new Vector3(0,.025f,0),new Vector3(.30f,.05f,.27f),dark);
        Part(Box(t,"WaterTank",new Vector3(0,.22f,.09f),new Vector3(.27f,.37f,.085f),blue),"der Wassertank","مخزن آب","water tank");
        Box(t,"CoffeeTop",new Vector3(0,.41f,0),new Vector3(.29f,.045f,.24f),dark);
        Part(Box(t,"FilterHousing",new Vector3(0,.32f,-.035f),new Vector3(.23f,.14f,.19f),blue),"der Filterhalter","نگهدارنده فیلتر","filter holder");
        var carafe=Cup(t,"CoffeeCarafe",new Vector3(0,.06f,-.035f),.092f,.19f,glass);Part(carafe,"die Kaffeekanne","قوری قهوه‌ساز","coffee pot");
        Tube(t,"CarafeLid",new Vector3(0,.252f,-.035f),new Vector3(0,.265f,-.035f),.096f,dark);
        Part(Box(t,"CarafeHandle",new Vector3(.12f,.16f,-.035f),new Vector3(.025f,.14f,.035f),dark),"der Kannengriff","دسته قوری","pot handle");
        Box(t,"CoffeePowerButton",new Vector3(.105f,.035f,-.141f),new Vector3(.025f,.018f,.008f),blue);Anchor(t,new Vector3(0,.32f,-.14f));
        t=Item("Kitchen_Scale","die Küchenwaage","ترازوی آشپزخانه","kitchen scale",79,new Vector3(2.81f,.955f,4.44f));
        Box(t,"ScaleBase",new Vector3(0,.022f,0),new Vector3(.26f,.044f,.23f),blue);
        Part(Box(t,"WeighingPlate",new Vector3(0,.052f,.02f),new Vector3(.25f,.015f,.17f),metal),"die Wiegefläche","سطح توزین","weighing platform");
        Part(Box(t,"ScaleDisplay",new Vector3(0,.046f,-.081f),new Vector3(.09f,.004f,.028f),dark),"das Display","نمایشگر","display");
        foreach(float x in new[]{-.09f,.09f})Box(t,"ScaleButton",new Vector3(x,.047f,-.081f),new Vector3(.025f,.005f,.025f),white);
        Anchor(t,new Vector3(0,.063f,.02f));
    }
    static void Inventory()
    {
        string path="Documentation/tools.txt",text=File.ReadAllText(path);int a=text.IndexOf("گروه F14",StringComparison.Ordinal),b=text.IndexOf("مرحله ۱۵",a,StringComparison.Ordinal);
        if(a<0||b<a)throw new InvalidOperationException("Missing final inventory groups.");
        string section=text.Substring(a,b-a);foreach(var w in catalog.fixtures)section=section.Replace("[ ] "+w.de+" |",w.de+" |");text=text.Substring(0,a)+section+text.Substring(b);
        const string heading="مرحله ۱۹ — ده وسیلهٔ نهایی خانه و آشپزخانه";int start=text.IndexOf(heading,StringComparison.Ordinal);string later="";
        if(start>=0){int next=text.IndexOf("\nمرحله ",start+heading.Length,StringComparison.Ordinal);if(next>=0)later="\n"+text.Substring(next);text=text.Substring(0,start).TrimEnd();}
        text+="\n\n"+heading+"\nDeutsch | فارسی | English\n";foreach(var w in catalog.vocabulary)text+=w.de+" | "+w.fa+" | "+w.en+"\n";
        text+="\nهر ۸۰ نمونهٔ برنامه‌ریزی‌شده ساخته شده‌اند؛ هیچ ردیف وسیلهٔ [ ] باقی نمانده است. این ده مورد هدف شناسایی ثابت‌اند. کلید برق چراغ‌های پذیرایی را قطع و وصل می‌کند؛ گرمایش، کارکرد پریز، برش، توست‌کردن، دم‌کردن قهوه و وزن‌کشی شبیه‌سازی نشده‌اند.\n";
        File.WriteAllText(path,text+later);
    }
}
