using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BedroomFurnishingBuilder
{
    public const int AddedDoors = 8;
    [Serializable] public sealed class Word { public string id,de,fa,en,room;public int order; }
    [Serializable] public sealed class Catalog { public List<Word> fixtures=new List<Word>();public List<Word> vocabulary=new List<Word>(); }
    private static Catalog catalog;
    private static Transform root;
    private static Material oak,metal,linen,blue,dark,cream,paper,cover;
    [MenuItem("Tools/DeutschRoom/Rebuild Bedrooms F01-F04")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        GameObject old=GameObject.Find("BedroomFurnishings");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        if(!AssetDatabase.IsValidFolder("Assets/Art/Bedrooms"))AssetDatabase.CreateFolder("Assets/Art","Bedrooms");
        oak=Load("Oak");metal=Load("BrushedSteel");
        linen=Cloth("Linen",new Color(.91f,.89f,.81f));blue=Cloth("BlueDuvet",new Color(.28f,.42f,.48f));dark=Cloth("ChairFabric",new Color(.12f,.18f,.21f));
        cream=Material("LampShade",new Color(.91f,.85f,.69f),.2f);paper=Material("Paper",new Color(.93f,.91f,.82f),.15f);cover=Material("BookCover",new Color(.16f,.32f,.34f),.25f);
        root=new GameObject("BedroomFurnishings").transform;catalog=new Catalog();
        Bed("Bedroom1",new Vector3(-3.15f,0,-14.1f),0,10,true);
        Wardrobe();Dresser();Nightstand("Bedroom1",new Vector3(-1.90f,0,-13.4f),17);
        Lamp("Bedroom1_NightLamp","die Nachttischlampe","چراغ پاتختی","bedside lamp","Bedroom1",18,new Vector3(-1.90f,.58f,-13.4f),false);
        Hanger();
        Bed("Bedroom2",new Vector3(8.2f,0,-8.65f),180,20,false);
        Nightstand("Bedroom2",new Vector3(6.95f,0,-9.3f),24);
        Desk();OfficeChair();
        Lamp("Bedroom2_DeskLamp","die Schreibtischlampe","چراغ مطالعه","desk lamp","Bedroom2",27,new Vector3(5.55f,.81f,-5.5f),true);
        Bookcase();Book();
        File.WriteAllText("Documentation/BedroomContent.json",JsonUtility.ToJson(catalog,true)+"\n");UpdateInventory();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("BEDROOM_BUILD_PASS: 20 fixed learning instances F01-F04, beds/bedding, storage, working doors/drawers and study furniture.");
    }
    public static void BuildAndCapture()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    public static void BuildAndValidate(){Build();InteractionValidation.Run();}
    public static void BuildReviewAndValidate()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");InteractionValidation.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    private static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/"+name+".mat");
    private static Material Material(string name,Color tint,float smooth)
    {
        string path="Assets/Art/Bedrooms/"+name+".mat";Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);return m;
    }
    private static Material Cloth(string name,Color tint)
    {
        Material m=Material(name,tint,.16f);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VisualPolish/cloth-albedo.png"));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VisualPolish/cloth-normal.png"));m.SetFloat("_BumpScale",.2f);m.EnableKeyword("_NORMALMAP");return m;
    }
    private static Transform Item(string id,string de,string fa,string en,string room,int order,Vector3 at,float yaw=0)
    {
        Transform t=new GameObject(id).transform;t.SetParent(root,false);t.position=at;t.rotation=Quaternion.Euler(0,yaw,0);
        VocabularyObject label=t.gameObject.AddComponent<VocabularyObject>();Name(label,de);
        FixedLearningTarget target=t.gameObject.AddComponent<FixedLearningTarget>();SerializedObject data=new SerializedObject(target);data.FindProperty("order").intValue=order;data.ApplyModifiedPropertiesWithoutUndo();
        catalog.fixtures.Add(new Word{id=id,de=de,fa=fa,en=en,room=room,order=order});Record(de,fa,en);return t;
    }
    private static void Name(KitchenInteractable label,string name){SerializedObject data=new SerializedObject(label);data.FindProperty("displayName").stringValue=name;data.ApplyModifiedPropertiesWithoutUndo();}
    private static void Record(string de,string fa,string en){if(!catalog.vocabulary.Exists(w=>w.de==de))catalog.vocabulary.Add(new Word{de=de,fa=fa,en=en});}
    private static void Part(Transform t,string de,string fa,string en)
    {VocabularyPart p=t.GetComponent<VocabularyPart>();if(p==null)p=t.gameObject.AddComponent<VocabularyPart>();SerializedObject data=new SerializedObject(p);data.FindProperty("displayName").stringValue=de;data.ApplyModifiedPropertiesWithoutUndo();Record(de,fa,en);}
    private static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material m)
    {
        GameObject obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;
        obj.GetComponent<MeshFilter>().sharedMesh=VisualPolishBuilder.FurnitureBox(size);obj.GetComponent<Renderer>().sharedMaterial=m;return obj.transform;
    }
    private static Transform Ellipsoid(Transform parent,string name,Vector3 at,Vector3 size,Material m)
    {GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(obj.GetComponent<SphereCollider>());MeshCollider collider=obj.AddComponent<MeshCollider>();collider.sharedMesh=obj.GetComponent<MeshFilter>().sharedMesh;collider.convex=true;return obj.transform;}
    private static Transform Tube(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material m)
    {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Cylinder);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=(a+b)/2;
        obj.transform.localScale=new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2);obj.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);obj.GetComponent<Renderer>().sharedMaterial=m;return obj.transform;
    }
    private static void Bed(string room,Vector3 at,float yaw,int first,bool sheet)
    {
        Quaternion q=Quaternion.Euler(0,yaw,0);
        Transform bed=Item(room+"_Bed","das Bett","تخت","bed",room,first,at,yaw);
        Part(Box(bed,"Headboard",new Vector3(0,.66f,1.07f),new Vector3(1.68f,.94f,.075f),oak),"das Kopfteil","تاج تخت","headboard");
        Part(Box(bed,"BedFrame",new Vector3(0,.28f,0),new Vector3(1.65f,.22f,2.14f),oak),"der Bettrahmen","چارچوب تخت","bed frame");
        foreach(float x in new[]{-.68f,.68f})foreach(float z in new[]{-.9f,.9f})Part(Box(bed,"BedFoot",new Vector3(x,.13f,z),new Vector3(.09f,.26f,.09f),oak),"der Bettfuß","پایه تخت","bed leg");
        Transform mattress=Item(room+"_Mattress","die Matratze","تشک","mattress",room,first+1,at+Vector3.up*.39f,yaw);
        Box(mattress,"MattressBody",new Vector3(0,.085f,0),new Vector3(1.52f,.17f,2.02f),linen);
        Transform pillow=Item(room+"_Pillow","das Kopfkissen","بالش","pillow",room,first+2,at+q*new Vector3(0,.615f,.69f),yaw);
        Ellipsoid(pillow,"PillowBody",Vector3.zero,new Vector3(.73f,.19f,.42f),linen);
        Transform duvet=Item(room+"_Duvet","die Bettdecke","لحاف","duvet",room,first+3,at+q*new Vector3(0,.605f,-.35f),yaw);
        duvet.position-=Vector3.up*.03f;
        ClothDuvet(duvet);
        if(sheet)
        {
            Transform linenSheet=Item(room+"_Sheet","das Bettlaken","ملحفه","bedsheet",room,first+4,at+q*new Vector3(0,.567f,0),yaw);
            Box(linenSheet,"VisibleSheetBand",new Vector3(0,-.004f,.54f),new Vector3(1.53f,.003f,.87f),linen);
            Transform point=new GameObject("IdentificationPoint").transform;point.SetParent(linenSheet,false);point.localPosition=new Vector3(.60f,0,.70f);
            SerializedObject label=new SerializedObject(linenSheet.GetComponent<KitchenInteractable>());label.FindProperty("identificationPoint").objectReferenceValue=point;label.ApplyModifiedPropertiesWithoutUndo();
            foreach(float x in new[]{-.77f,.77f})Box(linenSheet,"SheetEdge",new Vector3(x,-.055f,0),new Vector3(.012f,.12f,2.01f),linen);
        }
    }
    private static void Wardrobe()
    {
        Transform t=Item("Bedroom1_Wardrobe","der Kleiderschrank","کمد لباس","wardrobe","Bedroom1",15,new Vector3(-3.25f,0,-11.03f));
        foreach(float x in new[]{-.69f,.69f})Box(t,"WardrobeSide",new Vector3(x,1.1f,0),new Vector3(.04f,2.2f,.64f),oak);
        Box(t,"WardrobeBack",new Vector3(0,1.1f,.30f),new Vector3(1.36f,2.2f,.03f),oak);
        foreach(float y in new[]{.08f,2.18f})Box(t,"WardrobeShelf",new Vector3(0,y,0),new Vector3(1.36f,.04f,.62f),oak);
        Part(Tube(t,"ClothesRail",new Vector3(-.64f,1.76f,0),new Vector3(.64f,1.76f,0),.012f,metal),"die Kleiderstange","میله آویز لباس","clothes rail");
        foreach(int side in new[]{-1,1})
        {
            Transform hinge=new GameObject("BedroomWardrobeHinge").transform;hinge.SetParent(t,false);hinge.localPosition=new Vector3(side*.68f,1.12f,-.335f);
            Openable door=hinge.gameObject.AddComponent<Openable>();Name(door,"der Kleiderschrank");SerializedObject d=new SerializedObject(door);d.FindProperty("openAngle").floatValue=-side*100;d.ApplyModifiedPropertiesWithoutUndo();
            Part(Box(hinge,"WardrobeDoor",new Vector3(-side*.335f,0,0),new Vector3(.66f,2.08f,.04f),oak),"die Schranktür","در کابینت","cabinet door");
            Part(Box(hinge,"WardrobeHandle",new Vector3(-side*.58f,-.05f,-.045f),new Vector3(.025f,.22f,.04f),metal),"der Schrankgriff","دستگیره کابینت","cabinet handle");
        }
    }
    private static void CabinetShell(Transform t,float width,float height,float depth)
    {
        foreach(float x in new[]{-width/2,width/2})Box(t,"CabinetSide",new Vector3(x,height/2,0),new Vector3(.035f,height,depth),oak);
        Box(t,"CabinetBack",new Vector3(0,height/2,depth/2-.02f),new Vector3(width,height,.035f),oak);
        foreach(float y in new[]{.035f,height})Box(t,"CabinetHorizontal",new Vector3(0,y,0),new Vector3(width,.04f,depth),oak);
    }
    private static void Drawer(Transform owner,string de,float y,float width,float height,float depth)
    {
        Transform drawer=new GameObject("BedroomDrawer").transform;drawer.SetParent(owner,false);drawer.localPosition=new Vector3(0,y,-depth/2);
        Openable slide=drawer.gameObject.AddComponent<Openable>();Name(slide,de);SerializedObject data=new SerializedObject(slide);data.FindProperty("sliding").boolValue=true;data.FindProperty("slideOffset").vector3Value=new Vector3(0,0,-.32f);data.ApplyModifiedPropertiesWithoutUndo();
        Part(Box(drawer,"DrawerFront",Vector3.zero,new Vector3(width,height,.035f),oak),"die Schublade","کشو","drawer");
        Part(Box(drawer,"DrawerHandle",new Vector3(0,0,-.035f),new Vector3(.20f,.025f,.035f),metal),"der Schubladengriff","دستگیره کشو","drawer handle");
        Box(drawer,"DrawerTray",new Vector3(0,-height/2+.025f,depth*.43f),new Vector3(width-.06f,.025f,depth*.84f),oak);
        foreach(float x in new[]{-width/2+.03f,width/2-.03f})Box(drawer,"DrawerTraySide",new Vector3(x,0,depth*.43f),new Vector3(.025f,height*.7f,depth*.84f),oak);
    }
    private static void Dresser()
    {
        Transform t=Item("Bedroom1_Dresser","die Kommode","دراور","chest of drawers","Bedroom1",16,new Vector3(-.6f,0,-11.05f));
        CabinetShell(t,1.10f,.85f,.52f);
        foreach(float y in new[]{.19f,.44f,.69f})Drawer(t,"die Kommode",y,1.05f,.22f,.52f);
    }
    private static void Nightstand(string room,Vector3 at,int order)
    {
        Transform t=Item(room+"_Nightstand","der Nachttisch","پاتختی","bedside table",room,order,at);
        CabinetShell(t,.46f,.55f,.43f);Drawer(t,"der Nachttisch",.37f,.41f,.21f,.43f);
        Box(t,"LowerShelf",new Vector3(0,.10f,0),new Vector3(.43f,.03f,.4f),oak);
    }
    private static void Hanger()
    {
        Transform t=Item("Bedroom1_Hanger","der Kleiderbügel","چوب‌لباسی","clothes hanger","Bedroom1",19,new Vector3(-3.25f,1.63f,-11.03f));
        Tube(t,"HangerShoulder",new Vector3(-.22f,-.11f,0),new Vector3(0,.005f,0),.012f,oak);
        Tube(t,"HangerShoulder",new Vector3(0,.005f,0),new Vector3(.22f,-.11f,0),.012f,oak);
        Tube(t,"HangerBar",new Vector3(-.22f,-.11f,0),new Vector3(.22f,-.11f,0),.009f,oak);
        Vector3 last=new Vector3(0,.005f,0);
        for(int i=0;i<=12;i++){float a=-Mathf.PI/2+i*Mathf.PI*1.6f/12;Vector3 next=new Vector3(.025f+Mathf.Cos(a)*.028f,.10f+Mathf.Sin(a)*.028f,0);Tube(t,"HangerHook",last,next,.004f,metal);last=next;}
    }
    private static void Desk()
    {
        Transform t=Item("Bedroom2_Desk","der Schreibtisch","میز تحریر","desk","Bedroom2",25,new Vector3(6.1f,0,-5.65f));
        Part(Box(t,"Desktop",new Vector3(0,.77f,0),new Vector3(1.6f,.06f,.72f),oak),"die Tischplatte","صفحه میز","tabletop");
        foreach(float x in new[]{-.70f,.70f})foreach(float z in new[]{-.27f,.27f})Part(Box(t,"DeskLeg",new Vector3(x,.375f,z),new Vector3(.055f,.75f,.055f),metal),"das Tischbein","پایه میز","table leg");
        Drawer(t,"der Schreibtisch",.65f,.75f,.13f,.66f);
    }
    private static void OfficeChair()
    {
        Transform t=Item("Bedroom2_OfficeChair","der Bürostuhl","صندلی کار","desk chair","Bedroom2",26,new Vector3(6.1f,0,-6.85f));
        Part(Box(t,"ChairSeat",new Vector3(0,.49f,0),new Vector3(.48f,.08f,.45f),dark),"die Sitzfläche","سطح نشیمن","seat surface");
        Transform back=Box(t,"ChairBack",new Vector3(0,.79f,-.23f),new Vector3(.45f,.53f,.075f),dark);back.localRotation=Quaternion.Euler(-8,0,0);Part(back,"die Rückenlehne","پشتی صندلی","backrest");
        Tube(t,"ChairPiston",new Vector3(0,.1f,0),new Vector3(0,.45f,0),.032f,metal);
        for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Vector3 end=new Vector3(Mathf.Cos(a)*.30f,.08f,Mathf.Sin(a)*.30f);Tube(t,"ChairFoot",new Vector3(0,.12f,0),end,.018f,metal);Part(Ellipsoid(t,"Caster",end-Vector3.up*.035f,new Vector3(.07f,.065f,.07f),dark),"die Rolle","چرخ","caster");}
        foreach(float x in new[]{-.27f,.27f}){Tube(t,"ArmSupport",new Vector3(x,.5f,-.12f),new Vector3(x,.7f,-.12f),.012f,metal);Part(Box(t,"Armrest",new Vector3(x,.7f,-.02f),new Vector3(.06f,.045f,.27f),dark),"die Armlehne","دسته صندلی","armrest");}
    }
    private static void Lamp(string id,string de,string fa,string en,string room,int order,Vector3 at,bool desk)
    {
        Transform t=Item(id,de,fa,en,room,order,at);
        Ellipsoid(t,"LampBase",new Vector3(0,.015f,0),new Vector3(.18f,.03f,.18f),metal);
        Tube(t,"LampStem",new Vector3(0,.02f,0),new Vector3(0,.28f,0),.008f,metal);
        if(desk)Tube(t,"LampArm",new Vector3(0,.28f,0),new Vector3(.10f,.34f,0),.009f,metal);
        Transform shade=LampShade(t,new Vector3(desk?.10f:0,.27f,0));Part(shade,"der Lampenschirm","کلاهک چراغ","lampshade");
        var glow=new GameObject("WarmLampLight",typeof(Light));glow.transform.SetParent(t,false);glow.transform.localPosition=new Vector3(0,.25f,0);Light light=glow.GetComponent<Light>();light.type=LightType.Point;light.range=1.6f;light.intensity=.25f;light.color=new Color(1,.82f,.58f);light.shadows=LightShadows.None;
    }
    private static void Bookcase()
    {
        Transform t=Item("Bedroom2_Bookcase","das Bücherregal","قفسه کتاب","bookcase","Bedroom2",28,new Vector3(4.6f,0,-10.55f));
        foreach(float x in new[]{-.54f,.54f})Box(t,"BookcaseSide",new Vector3(x,.9f,0),new Vector3(.04f,1.8f,.34f),oak);
        Box(t,"BookcaseBack",new Vector3(0,.9f,-.15f),new Vector3(1.05f,1.8f,.03f),oak);
        foreach(float y in new[]{.08f,.52f,.96f,1.4f,1.78f})Part(Box(t,"Shelf",new Vector3(0,y,0),new Vector3(1.05f,.03f,.34f),oak),"der Regalboden","طبقه قفسه","shelf board");
    }
    private static void Book()
    {
        Transform t=Item("Bedroom2_Book","das Buch","کتاب","book","Bedroom2",29,new Vector3(6.5f,.82f,-5.66f),-12);
        Part(Box(t,"Pages",Vector3.zero,new Vector3(.20f,.035f,.28f),paper),"die Buchseiten","صفحه‌های کتاب","book pages");
        foreach(float y in new[]{-.021f,.021f})Part(Box(t,"Cover",new Vector3(0,y,0),new Vector3(.215f,.008f,.295f),cover),"der Buchdeckel","جلد کتاب","book cover");
        Box(t,"BookSpine",new Vector3(-.106f,0,0),new Vector3(.009f,.05f,.295f),cover);
        Box(t,"CoverDecoration",new Vector3(0,.027f,0),new Vector3(.14f,.002f,.025f),paper);
    }
    private static void ClothDuvet(Transform parent)
    {
        const int nx=36,nz=28;string path="Assets/Art/Bedrooms/DrapedDuvet.asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            var points=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {
                float x=Mathf.Lerp(-.83f,.83f,(float)i/nx),z=Mathf.Lerp(-.72f,.62f,(float)j/nz);
                float side=.18f*Mathf.Pow(Mathf.Clamp01((Mathf.Abs(x)-.73f)/.10f),.7f),foot=.16f*Mathf.Pow(Mathf.Clamp01((-.56f-z)/.16f),.7f);
                float y=-Mathf.Max(side,foot)+Mathf.Sin(x*13+z*4)*.007f+Mathf.Sin(z*19)*.003f;
                points.Add(new Vector3(x,y,z));uv.Add(new Vector2(x,z));
            }
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i;tris.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});}
            mesh=new Mesh{name="DrapedDuvet"};mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        GameObject obj=new GameObject("DuvetCloth",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshCollider>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=blue;
    }
    private static Transform LampShade(Transform parent,Vector3 at)
    {
        string path="Assets/Art/Bedrooms/Lampshade.asset";Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null)
        {
            Vector2[] profile={new Vector2(.125f,0),new Vector2(.08f,.16f),new Vector2(.075f,.16f),new Vector2(.12f,.004f),new Vector2(.125f,0)};
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int segments=48;
            for(int j=0;j<profile.Length;j++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));uv.Add(new Vector2((float)i/segments,(float)j/(profile.Length-1)));}
            for(int j=0;j<profile.Length-1;j++)for(int i=0;i<segments;i++){int a=j*(segments+1)+i,b=a+segments+1;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            mesh=new Mesh{name="Lampshade"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
        }
        GameObject obj=new GameObject("LampShade",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));obj.transform.SetParent(parent,false);obj.transform.localPosition=at;obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshCollider>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=cream;return obj.transform;
    }
    private static void UpdateInventory()
    {
        string path="Documentation/tools.txt",text=File.ReadAllText(path);int start=text.IndexOf("گروه F01",StringComparison.Ordinal),end=text.IndexOf("گروه F05",StringComparison.Ordinal);
        if(start>=0&&end>start)text=text.Substring(0,start)+text.Substring(start,end-start).Replace("[ ] ","")+text.Substring(end);
        const string section="مرحله ۱۶ — وسایل ساخته‌شدهٔ دو اتاق خواب";int previous=text.IndexOf(section,StringComparison.Ordinal);string later="";
        if(previous>=0){int next=text.IndexOf("\nمرحله ",previous+section.Length,StringComparison.Ordinal);if(next>=0)later="\n"+text.Substring(next);text=text.Substring(0,previous).TrimEnd();}
        text+="\n\n"+section+"\nDeutsch | فارسی | English\n";
        foreach(var w in catalog.vocabulary)text+=w.de+" | "+w.fa+" | "+w.en+"\n";
        int pending=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^\[ \] (der|die|das) ").Count;
        text+="\nگروه‌های F01 تا F04 با ۲۰ نمونهٔ مخصوص دو اتاق ساخته شدند. تعداد موارد هنوز اضافه‌نشده در فهرست: "+pending+". درها و کشوهای این گروه باز می‌شوند؛ اشیا در این نسخه هدف شناسایی ثابت‌اند و با نقطهٔ نام یا F شناخته می‌شوند.\n";
        File.WriteAllText(path,text+later);
    }
}
