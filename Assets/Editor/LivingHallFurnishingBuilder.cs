using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// F05-F08 are a separate, idempotently rebuilt layer over the existing house shell.
public static class LivingHallFurnishingBuilder
{
    public const int AddedDoors=2;
    [Serializable] public sealed class Word {public string id,de,fa,en,room;public int order;}
    [Serializable] public sealed class Catalog {public List<Word> fixtures=new List<Word>();public List<Word> vocabulary=new List<Word>();}
    static Catalog catalog;static Transform root;
    static Material oak,metal,upholstery,accent,linen,rug,black,ceramic,green,soil,mirror,art;
    [MenuItem("Tools/DeutschRoom/Rebuild Living Room and Hall F05-F08")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var old=GameObject.Find("LivingHallFurnishings");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        if(!AssetDatabase.IsValidFolder("Assets/Art/LivingHall"))AssetDatabase.CreateFolder("Assets/Art","LivingHall");
        oak=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/Oak.mat");metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VisualPolish/BrushedSteel.mat");
        upholstery=Mat("SageUpholstery",new Color(.29f,.39f,.34f),.15f,true);
        accent=Mat("OchreCushion",new Color(.68f,.39f,.14f),.15f,true);linen=Mat("WarmLinen",new Color(.83f,.78f,.66f),.12f,true);
        rug=Mat("WovenRug",new Color(.55f,.49f,.39f),.1f,true);black=Mat("Charcoal",new Color(.025f,.032f,.037f),.38f);
        ceramic=Mat("PotCeramic",new Color(.65f,.36f,.24f),.32f);green=Mat("LeafGreen",new Color(.12f,.31f,.16f),.3f);
        soil=Mat("PottingSoil",new Color(.08f,.052f,.03f),.05f);mirror=Mat("Mirror",new Color(.72f,.82f,.85f),.98f);mirror.SetFloat("_Metallic",1);
        art=Mat("LandscapePrint",Color.white,.25f);art.SetTexture("_BaseMap",Landscape());art.SetTextureScale("_BaseMap",new Vector2(1,-1));art.SetTextureOffset("_BaseMap",new Vector2(0,1));
        linen.SetFloat("_Cull",0);linen.doubleSidedGI=true;
        root=new GameObject("LivingHallFurnishings").transform;catalog=new Catalog();
        Seating();TablesAndTV();Textiles();Lighting();PlantAndPicture();Hall();
        File.WriteAllText("Documentation/LivingHallContent.json",JsonUtility.ToJson(catalog,true)+"\n");Inventory();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("LIVING_HALL_BUILD_PASS: twenty F05-F08 targets at orders 30-49 and two working shoe-cabinet doors.");
    }
    public static void BuildAndCapture()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    public static void BuildReviewAndValidate()
    {Build();EditorApplication.delayCall+=()=>{try{VisualReviewCapture.Capture("after");InteractionValidation.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}};}
    static Material Mat(string name,Color color,float smooth,bool fabric=false)
    {
        string path="Assets/Art/LivingHall/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
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
        catalog.fixtures.Add(new Word{id=id,de=de,fa=fa,en=en,room=id.StartsWith("Hall_")?"Hallway":"LivingRoom",order=order});Record(de,fa,en);return t;
    }
    static void Part(Transform t,string de,string fa,string en){var s=new SerializedObject(t.gameObject.AddComponent<VocabularyPart>());s.FindProperty("displayName").stringValue=de;s.ApplyModifiedPropertiesWithoutUndo();Record(de,fa,en);}
    static Transform Box(Transform p,string name,Vector3 at,Vector3 size,Material m)
    {
        var t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider)).transform;t.SetParent(p,false);t.localPosition=at;t.localScale=size;
        t.GetComponent<MeshFilter>().sharedMesh=VisualPolishBuilder.FurnitureBox(size);t.GetComponent<Renderer>().sharedMaterial=m;return t;
    }
    // Normalized face UVs keep the single landscape image intact on screens/prints.
    static Transform Panel(Transform p,string name,Vector3 at,Vector3 size,Material m)
    {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;return o.transform;}
    static Transform Oval(Transform p,string name,Vector3 at,Vector3 size,Material m)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=at;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<SphereCollider>());var c=o.AddComponent<MeshCollider>();c.sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;c.convex=true;return o.transform;
    }
    static Transform Tube(Transform p,string name,Vector3 a,Vector3 b,float r,Material m)
    {var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=(a+b)/2;o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);o.transform.localScale=new Vector3(r*2,Vector3.Distance(a,b)/2,r*2);o.GetComponent<Renderer>().sharedMaterial=m;return o.transform;}
    static void Feet(Transform t,float width,float depth,float height)
    {foreach(float x in new[]{-width/2,width/2})foreach(float z in new[]{-depth/2,depth/2})Part(Box(t,"FurnitureLeg",new Vector3(x,height/2,z),new Vector3(.065f,height,.065f),oak),"der Möbelfuß","پایه مبلمان","furniture leg");}
    static void Seat(Transform t,float width)
    {
        Anchor(t,new Vector3(0,.54f,-.26f));
        Feet(t,width-.26f,.62f,.20f);Box(t,"SeatFrame",new Vector3(0,.28f,0),new Vector3(width,.20f,.86f),upholstery);
        for(int i=0;i<(width>1.5f?3:1);i++)
        {
            int n=width>1.5f?3:1;float w=(width-.25f)/n;float x=(i-(n-1)*.5f)*w;
            Part(Box(t,"SeatCushion",new Vector3(x,.44f,-.055f),new Vector3(w-.018f,.18f,.70f),upholstery),"das Sitzpolster","بالشتک نشیمن","seat cushion");
            var back=Box(t,"PaddedBack",new Vector3(x,.73f,.30f),new Vector3(w-.015f,.57f,.18f),upholstery);back.localRotation=Quaternion.Euler(-7,0,0);Part(back,"die Rückenlehne","پشتی صندلی","backrest");
        }
        foreach(float x in new[]{-width/2,width/2})Part(Box(t,"PaddedArm",new Vector3(x,.58f,0),new Vector3(.16f,.32f,.87f),upholstery),"die Armlehne","دسته صندلی","armrest");
    }
    static void Seating()
    {
        Seat(Item("Living_Sofa","das Sofa","مبل","sofa",30,new Vector3(-1.75f,0,-5.68f)),2.35f);
        Seat(Item("Living_Armchair","der Sessel","مبل تک‌نفره","armchair",31,new Vector3(-4.08f,0,-9.05f),-90),.86f);
    }
    static void TablesAndTV()
    {
        var t=Item("Living_CoffeeTable","der Couchtisch","میز جلو مبلی","coffee table",32,new Vector3(-1.7f,0,-7.85f));
        Part(Box(t,"Tabletop",new Vector3(0,.43f,0),new Vector3(1.35f,.06f,.70f),oak),"die Tischplatte","صفحه میز","tabletop");
        foreach(float x in new[]{-.55f,.55f})foreach(float z in new[]{-.24f,.24f})Part(Box(t,"TableLeg",new Vector3(x,.20f,z),new Vector3(.045f,.40f,.045f),metal),"das Tischbein","پایه میز","table leg");
        Anchor(t,new Vector3(0,.465f,0));
        t=Item("Living_TVStand","der Fernsehtisch","میز تلویزیون","TV stand",33,new Vector3(-1.65f,0,-10.02f),180);
        Feet(t,1.55f,.30f,.12f);foreach(float y in new[]{.16f,.52f})Part(Box(t,"StandShelf",new Vector3(0,y,0),new Vector3(1.9f,.05f,.43f),oak),"der Regalboden","طبقه قفسه","shelf board");
        foreach(float x in new[]{-.925f,0,.925f})Box(t,"StandDivider",new Vector3(x,.34f,0),new Vector3(.04f,.34f,.43f),oak);
        Box(t,"StandBack",new Vector3(0,.34f,.20f),new Vector3(1.85f,.34f,.025f),oak);
        Anchor(t,new Vector3(.7f,.55f,-.14f));
        t=Item("Living_TV","der Fernseher","تلویزیون","television",34,new Vector3(-1.65f,.55f,-10.02f),180);
        Anchor(t,new Vector3(0,.58f,-.04f));
        foreach(float x in new[]{-.45f,.45f}){Tube(t,"TVFoot",new Vector3(x,0,-.12f),new Vector3(x,.13f,0),.012f,black);Tube(t,"TVFoot",new Vector3(x,0,.12f),new Vector3(x,.13f,0),.012f,black);}
        Box(t,"TVFrame",new Vector3(0,.57f,0),new Vector3(1.55f,.88f,.055f),black);
        Part(Panel(t,"Screen",new Vector3(0,.58f,-.032f),new Vector3(1.47f,.79f,.008f),art),"der Bildschirm","صفحه‌نمایش","screen");
        Oval(t,"StandbyLight",new Vector3(.68f,.15f,-.034f),new Vector3(.008f,.008f,.006f),ceramic);
        t=Item("Living_Remote","die Fernbedienung","کنترل تلویزیون","remote control",35,new Vector3(-1.38f,.476f,-7.85f),-15);
        Box(t,"RemoteBody",Vector3.zero,new Vector3(.052f,.022f,.18f),black);
        for(int row=0;row<5;row++)for(int col=0;col<3;col++)Part(Box(t,"RemoteButton",new Vector3((col-1)*.012f,.013f,.05f-row*.023f),new Vector3(.008f,.004f,.011f),row==0?ceramic:metal),"die Taste","دکمه","button");
    }
    static void Textiles()
    {
        var t=Item("Living_Cushion","das Sofakissen","کوسن مبل","sofa cushion",36,new Vector3(-2.5f,.67f,-5.77f));
        var pillow=Oval(t,"SoftCushion",Vector3.zero,new Vector3(.40f,.39f,.17f),accent);pillow.localRotation=Quaternion.Euler(-15,0,-12);
        t=Item("Living_Throw","die Wohndecke","پتوی مبل","throw blanket",37,new Vector3(-.94f,.55f,-5.88f));
        Drapery(t,"FoldedThrow",.56f,.77f,false,linen);
        t=Item("Living_Rug","der Teppich","فرش","rug",38,new Vector3(-1.7f,.007f,-7.75f));
        Box(t,"RugBase",Vector3.zero,new Vector3(3.15f,.014f,2.55f),rug);
        foreach(float x in new[]{-1.48f,1.48f})Box(t,"RugBorder",new Vector3(x,.008f,0),new Vector3(.04f,.002f,2.4f),linen);
        foreach(float z in new[]{-1.18f,1.18f})Box(t,"RugBorder",new Vector3(0,.008f,z),new Vector3(2.96f,.002f,.04f),linen);
        // The marker sits on exposed fabric rather than beneath the coffee table.
        Anchor(t,new Vector3(.95f,.01f,.65f));
        t=Item("Living_Curtain","der Vorhang","پرده","curtain",39,new Vector3(-4.77f,1.5f,-7.8f),90);
        foreach(float side in new[]{-1f,1f}){var panel=new GameObject("CurtainPanel").transform;panel.SetParent(t,false);panel.localPosition=new Vector3(side*1.08f,1.17f,0);Drapery(panel,"PleatedCurtain",.47f,2.48f,true,linen);}
        Part(Tube(t,"CurtainRod",new Vector3(-1.4f,1.24f,0),new Vector3(1.4f,1.24f,0),.018f,metal),"die Gardinenstange","میله پرده","curtain rod");
        Anchor(t,new Vector3(-1.08f,0,.04f));
    }
    static void Anchor(Transform t,Vector3 at){var point=new GameObject("IdentificationPoint").transform;point.SetParent(t,false);point.localPosition=at;var s=new SerializedObject(t.GetComponent<KitchenInteractable>());s.FindProperty("identificationPoint").objectReferenceValue=point;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Drapery(Transform p,string name,float width,float length,bool hanging,Material m)
    {
        string path="Assets/Art/LivingHall/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool create=mesh==null;if(create)mesh=new Mesh{name=name};
        if(!hanging)
        {
            var draped=FurnishingSoftness.ThrowMesh();
            EditorUtility.CopySerialized(draped,mesh);UnityEngine.Object.DestroyImmediate(draped);
            if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
        }
        else
        {
            const int nx=30,ny=28;var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++)
            {float u=(float)i/nx,w=(float)j/ny,x=(u-.5f)*width;float fold=Mathf.Sin(u*Mathf.PI*10)*.028f;
                v.Add(hanging?new Vector3(x,-w*length,fold):new Vector3(x,-Mathf.Max(0,w-.65f)*.72f+fold*.2f,-(w-.5f)*length));uv.Add(new Vector2(u,w));}
            for(int j=0;j<ny;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i;triangles.AddRange(hanging?new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2}:new[]{a,a+1,a+nx+1,a+1,a+nx+2,a+nx+1});}
            mesh.Clear();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
        }
        var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));o.transform.SetParent(p,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=m;o.GetComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void Lighting()
    {
        var t=Item("Living_FloorLamp","die Stehlampe","چراغ ایستاده","floor lamp",40,new Vector3(-3.6f,0,-5.65f));
        Anchor(t,new Vector3(.1f,1.55f,0));
        Tube(t,"LampBase",Vector3.zero,new Vector3(0,.04f,0),.18f,black);Tube(t,"LampStem",Vector3.up*.04f,Vector3.up*1.6f,.014f,metal);
        Shade(t,new Vector3(0,1.44f,0),.32f,.35f);Light(t,new Vector3(0,1.5f,0),2.6f,.5f);
        t=Item("Living_CeilingLight","die Deckenleuchte","چراغ سقفی","ceiling light",41,new Vector3(-1.7f,2.83f,-7.8f));
        Tube(t,"CeilingMount",Vector3.up*.12f,Vector3.up*.17f,.09f,metal);Shade(t,Vector3.zero,.30f,.12f);Light(t,new Vector3(0,-.03f,0),4,.5f);
    }
    static void Shade(Transform t,Vector3 at,float r,float height)
    {var shade=new GameObject("LampShade",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider)).transform;shade.SetParent(t,false);shade.localPosition=at;shade.localScale=new Vector3(r/.125f,height/.16f,r/.125f);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Bedrooms/Lampshade.asset");shade.GetComponent<MeshFilter>().sharedMesh=mesh;shade.GetComponent<Renderer>().sharedMaterial=linen;shade.GetComponent<MeshCollider>().sharedMesh=mesh;Part(shade,"der Lampenschirm","کلاهک چراغ","lampshade");}
    static void Light(Transform t,Vector3 at,float range,float intensity){var o=new GameObject("WarmLight",typeof(Light));o.transform.SetParent(t,false);o.transform.localPosition=at;var l=o.GetComponent<Light>();l.color=new Color(1,.82f,.61f);l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;}
    static void PlantAndPicture()
    {
        var t=Item("Living_Plant","die Zimmerpflanze","گیاه آپارتمانی","houseplant",42,new Vector3(-4.0f,.48f,-6.45f));
        Tube(t,"PlantStem",Vector3.zero,new Vector3(0,.95f,0),.015f,green);
        for(int i=0;i<11;i++){float a=i*2.4f,y=.15f+i*.07f;Vector3 tip=new Vector3(Mathf.Cos(a)*.28f,y+.13f,Mathf.Sin(a)*.28f);Tube(t,"LeafStalk",new Vector3(0,y,0),tip,.006f,green);var leaf=Oval(t,"Leaf",tip,new Vector3(.15f,.035f,.31f),green);leaf.localRotation=Quaternion.Euler(25,-a*Mathf.Rad2Deg,25);Part(leaf,"das Blatt","برگ","leaf");}
        t=Item("Living_Pot","der Blumentopf","گلدان گیاه","plant pot",43,new Vector3(-4.0f,0,-6.45f));
        Tube(t,"PotBody",new Vector3(0,.04f,0),new Vector3(0,.45f,0),.23f,ceramic);Tube(t,"PotRim",new Vector3(0,.43f,0),new Vector3(0,.48f,0),.245f,ceramic);Tube(t,"Soil",new Vector3(0,.479f,0),new Vector3(0,.482f,0),.217f,soil);Anchor(t,new Vector3(.24f,.25f,0));
        t=Item("Living_Picture","das Bild","تابلو","picture",44,new Vector3(-1.7f,1.95f,-5.12f));
        Box(t,"ArtBacking",Vector3.zero,new Vector3(1.27f,.69f,.025f),oak);
        Panel(t,"Landscape",new Vector3(0,0,-.016f),new Vector3(1.14f,.56f,.008f),art);
        foreach(float x in new[]{-.62f,.62f})Part(Box(t,"PictureFrame",new Vector3(x,0,-.027f),new Vector3(.035f,.69f,.04f),oak),"der Bilderrahmen","قاب تابلو","picture frame");
        foreach(float y in new[]{-.325f,.325f})Box(t,"PictureFrame",new Vector3(0,y,-.027f),new Vector3(1.27f,.035f,.04f),oak);
    }
    static void Hall()
    {
        var t=Item("Hall_ShoeCabinet","der Schuhschrank","جاکفشی","shoe cabinet",45,new Vector3(3.49f,0,-10.05f),90);
        Anchor(t,new Vector3(0,.60f,-.21f));
        foreach(float x in new[]{-.47f,.47f})Box(t,"CabinetSide",new Vector3(x,.52f,0),new Vector3(.035f,1.04f,.34f),oak);
        Box(t,"CabinetBack",new Vector3(0,.52f,.15f),new Vector3(.91f,1.04f,.03f),oak);
        foreach(float y in new[]{.05f,.5f,1.04f})Part(Box(t,"ShoeShelf",new Vector3(0,y,0),new Vector3(.92f,.035f,.32f),oak),"der Regalboden","طبقه قفسه","shelf board");
        foreach(int side in new[]{-1,1})
        {
            var hinge=new GameObject("ShoeCabinetHinge").transform;hinge.SetParent(t,false);hinge.localPosition=new Vector3(side*.465f,.54f,-.185f);
            var door=hinge.gameObject.AddComponent<Openable>();Name(door,"der Schuhschrank");var s=new SerializedObject(door);s.FindProperty("openAngle").floatValue=-side*95;s.ApplyModifiedPropertiesWithoutUndo();
            Part(Box(hinge,"CabinetDoor",new Vector3(-side*.23f,0,0),new Vector3(.45f,.96f,.03f),oak),"die Schranktür","در کابینت","cabinet door");
            Part(Box(hinge,"CabinetHandle",new Vector3(-side*.40f,0,-.035f),new Vector3(.025f,.15f,.035f),metal),"der Schrankgriff","دستگیره کابینت","cabinet handle");
        }
        t=Item("Hall_CoatRack","die Garderobe","رخت‌آویز ورودی","coat rack",46,new Vector3(1.97f,1.65f,-10.0f),-90);
        Box(t,"CoatRackBack",Vector3.zero,new Vector3(.95f,.22f,.045f),oak);
        for(int i=0;i<4;i++){float x=-.33f+i*.22f;Part(Tube(t,"CoatHook",new Vector3(x,-.025f,-.03f),new Vector3(x,-.025f,-.12f),.012f,metal),"der Haken","قلاب","hook");Tube(t,"HookTip",new Vector3(x,-.025f,-.12f),new Vector3(x,.035f,-.12f),.012f,metal);}
        t=Item("Hall_Mat","die Fußmatte","پادری","doormat",47,new Vector3(2.8f,.012f,-5.8f));Box(t,"Doormat",Vector3.zero,new Vector3(.95f,.024f,.58f),rug);
        for(int i=0;i<12;i++)Box(t,"MatRib",new Vector3(-.43f+i*.078f,.014f,0),new Vector3(.018f,.004f,.51f),black);
        t=Item("Hall_Mirror","der Spiegel","آینه","mirror",48,new Vector3(3.67f,1.5f,-11.65f),90);
        Box(t,"MirrorBacking",Vector3.zero,new Vector3(.62f,1.16f,.035f),oak);Part(Box(t,"MirrorSurface",new Vector3(0,0,-.022f),new Vector3(.54f,1.08f,.008f),mirror),"die Spiegelfläche","سطح آینه","mirror surface");
        t=Item("Hall_Bench","die Sitzbank","نیمکت ورودی","entrance bench",49,new Vector3(3.44f,0,-6.05f),90);
        Feet(t,1.05f,.24f,.38f);Part(Box(t,"BenchSeat",new Vector3(0,.42f,0),new Vector3(1.25f,.08f,.37f),oak),"die Sitzfläche","سطح نشیمن","seat surface");
        Box(t,"BenchPad",new Vector3(0,.48f,0),new Vector3(1.14f,.05f,.34f),upholstery);
    }
    static Texture2D Landscape()
    {
        const string path="Assets/Art/LivingHall/Landscape.png";var texture=new Texture2D(512,256,TextureFormat.RGB24,false);
        for(int y=0;y<256;y++)for(int x=0;x<512;x++)
        {float u=x/511f,v=y/255f;Color c=Color.Lerp(new Color(.86f,.77f,.57f),new Color(.27f,.48f,.55f),v);if(Vector2.Distance(new Vector2(u,v),new Vector2(.72f,.73f))<.10f)c=new Color(.97f,.85f,.57f);
            if(v<.43f+Mathf.Sin(u*8)*.12f)c=new Color(.32f,.46f,.43f);if(v<.28f+Mathf.Sin(u*11+2)*.08f)c=new Color(.18f,.32f,.30f);if(v<.12f+Mathf.Sin(u*7)*.05f)c=new Color(.12f,.23f,.23f);texture.SetPixel(x,y,c);}
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void Inventory()
    {
        string path="Documentation/tools.txt",text=File.ReadAllText(path);int a=text.IndexOf("گروه F05",StringComparison.Ordinal),b=text.IndexOf("گروه F09",StringComparison.Ordinal);
        if(a<0||b<a)throw new InvalidOperationException("Missing planned inventory groups F05-F08.");text=text.Substring(0,a)+text.Substring(a,b-a).Replace("[ ] ","")+text.Substring(b);
        const string heading="مرحله ۱۷ — وسایل ساخته‌شدهٔ پذیرایی و راهرو";int start=text.IndexOf(heading,StringComparison.Ordinal);string later="";
        if(start>=0){int next=text.IndexOf("\nمرحله ",start+heading.Length,StringComparison.Ordinal);if(next>=0)later="\n"+text.Substring(next);text=text.Substring(0,start).TrimEnd();}
        text+="\n\n"+heading+"\nDeutsch | فارسی | English\n";foreach(var w in catalog.vocabulary)text+=w.de+" | "+w.fa+" | "+w.en+"\n";
        int pending=System.Text.RegularExpressions.Regex.Matches(text,@"(?m)^\[ \] (der|die|das) ").Count;
        text+="\n۲۰ مورد F05–F08 ساخته شدند؛ تعداد موارد در انتظار در کل فهرست: "+pending+". این وسایل هدف شناسایی ثابت‌اند؛ دو در جاکفشی باز می‌شوند. تلویزیون تصویر ثابت دارد، چراغ‌ها روشن‌اند و آینه سطح فلزی براق دارد.\n";
        File.WriteAllText(path,text+later);
    }
}
