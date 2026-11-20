using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Authored geometry is saved into SampleScene; nothing is spawned on every launch.
public static class KitchenContentBuilder
{
    private static readonly List<string> rows = new List<string>();
    private static Transform root, drawerRoot;
    private static Material ceramic, steel, blue, red, green, cream, brown, orange, yellow, white;
    private const string Section = "مرحله ۱۰ — محتویات کابینت‌ها، یخچال و اجزای زیر سینک";
    public const int AddedPickups = 36;

    [MenuItem("Tools/DeutschRoom/Rebuild Cabinet Contents")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach (string name in new[] { "KitchenContents_Stage10", "DrawerContents_Stage10" })
        { GameObject previous = GameObject.Find(name); if (previous != null) UnityEngine.Object.DestroyImmediate(previous); }
        Folder("Assets/Materials/Contents"); Folder("Assets/Models/KitchenContents");
        ceramic = Material("Ceramic", new Color(.87f, .88f, .8f));
        steel = Material("Steel", new Color(.56f, .62f, .64f), .7f);
        blue = Material("Blue", new Color(.13f, .42f, .61f));
        red = Material("Red", new Color(.72f, .16f, .12f));
        green = Material("Green", new Color(.24f, .48f, .18f));
        cream = Material("Cream", new Color(.92f, .83f, .61f));
        brown = Material("Wood", new Color(.42f, .24f, .12f));
        orange = Material("Orange", new Color(.95f, .4f, .08f));
        yellow = Material("Yellow", new Color(.94f, .72f, .16f));
        white = Material("White", new Color(.93f, .95f, .93f));
        root = new GameObject("KitchenContents_Stage10").transform;
        drawerRoot = new GameObject("DrawerContents_Stage10").transform;
        drawerRoot.SetParent(GameObject.Find("Schublade_Drawer").transform, false);
        rows.Clear();
        WallCupboards(); BaseCupboards(); Drawer(); UnderSink(); Fridge(); Furniture();
        if (UnityEngine.Object.FindObjectsByType<PickupItem>().Length != 17 + AddedPickups)
            throw new InvalidOperationException("Unexpected pickup count; update the authored content count.");
        string path = "Documentation/tools.txt";
        string existing = File.ReadAllText(path);
        int start = existing.IndexOf(Section, StringComparison.Ordinal);
        string laterSections = "";
        if (start >= 0)
        {
            int next = existing.IndexOf("\nمرحله ", start + Section.Length, StringComparison.Ordinal);
            if (next >= 0) laterSections = "\n" + existing.Substring(next);
            existing = existing.Substring(0, start).TrimEnd();
        }
        File.WriteAllText(path, existing.TrimEnd() + "\n\n" + Section + "\nDeutsch | فارسی | English\n" +
            string.Join("\n", rows) + "\n\nهمهٔ وسایل جدید نام و صدای آلمانی دارند. وسایل داخل کشو همراه آن حرکت می‌کنند. ظروف و خوراکی‌ها قابل برداشتن‌اند؛ لوله‌ها و مبلمان ثابت‌اند.\n" +
            "کنترل وضعیت بدن: در بخش آزاد سمت راست، به پایین بکشید و ۰٫۶ ثانیه ثابت نگه دارید تا بنشینید؛ رها کنید و دوباره همین حرکت را انجام دهید تا دراز بکشید. کشیدن به بالا و نگه‌داشتن، یک حالت بالاتر می‌برد. روی کامپیوتر C پایین و X بالا. زیر مانع نمی‌توان بلند شد.\n" + laterSections, new UTF8Encoding(false));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("KITCHEN_CONTENT_BUILD_PASS: " + AddedPickups + " new pickup objects, labeled plumbing/furniture, tools.txt updated.");
    }

    public static void BuildAndValidate()
    {
        Build();
        InteractionValidation.Run();
    }

    private static void WallCupboards()
    {
        Item("Cup", "die Tasse", "فنجان", "cup", new Vector3(-3.13f, 1.786f, 4.59f), "cup", ceramic);
        Item("Mug", "der Becher", "ماگ", "mug", new Vector3(-2.76f, 1.786f, 4.59f), "cup", blue);
        Item("Saucer", "die Untertasse", "نعلبکی", "saucer", new Vector3(-3.10f, 2.134f, 4.59f), "plate", ceramic);
        Item("WineGlass", "das Weinglas", "جام", "wine glass", new Vector3(-2.76f, 2.134f, 4.59f), "wineglass", blue);
        Item("Teapot", "die Teekanne", "قوری", "teapot", new Vector3(-2.19f, 1.786f, 4.59f), "teapot", ceramic);
        Item("SugarBowl", "die Zuckerdose", "قندان", "sugar bowl", new Vector3(-1.86f, 1.786f, 4.59f), "jar", cream);
        Item("Flour", "das Mehl", "آرد", "flour", new Vector3(-1.34f, 1.786f, 4.59f), "carton", cream);
        Item("Rice", "der Reis", "برنج", "rice", new Vector3(-.98f, 1.786f, 4.59f), "jar", cream);
        Item("Pasta", "die Nudeln", "پاستا", "pasta", new Vector3(-1.16f, 2.134f, 4.59f), "jar", yellow);
        Item("Coffee", "der Kaffee", "قهوه", "coffee", new Vector3(.94f, 1.786f, 4.59f), "jar", brown);
        Item("Tea", "der Tee", "چای", "tea", new Vector3(1.29f, 1.786f, 4.59f), "carton", green);
        Item("Cereal", "das Müsli", "موسلی", "muesli", new Vector3(1.11f, 2.134f, 4.59f), "carton", orange);
        foreach (GameObject cabinet in UnityEngine.Object.FindObjectsByType<GameObject>())
            if (cabinet.name.StartsWith("Haengeschrank_", StringComparison.Ordinal))
                foreach (Transform child in cabinet.transform)
                    if (child.name == "InteriorShelf") Label(child.gameObject, "der Einlegeboden", "طبقه داخلی کابینت", "cabinet shelf");
    }

    private static void BaseCupboards()
    {
        Item("Colander", "das Sieb", "آبکش", "colander", new Vector3(-3.04f, .128f, 4.43f), "colander", steel);
        Item("MeasuringJug", "der Messbecher", "پارچ اندازه‌گیری", "measuring jug", new Vector3(-2.58f, .128f, 4.43f), "jug", blue);
        Item("Casserole", "die Auflaufform", "ظرف گراتن", "casserole dish", new Vector3(-2.10f, .128f, 4.43f), "dish", ceramic);
        Item("StorageBox", "die Vorratsdose", "ظرف نگهداری مواد غذایی", "food storage container", new Vector3(.91f, .224f, 4.30f), "box", blue);
        Item("Grater", "die Reibe", "رنده", "grater", new Vector3(1.33f, .224f, 4.30f), "grater", steel);
        Item("BakingTin", "die Backform", "قالب کیک", "baking tin", new Vector3(1.12f, .224f, 4.64f), "colander", brown);
    }

    private static void Drawer()
    {
        Transform d = drawerRoot.parent;
        Item("Spatula", "der Pfannenwender", "کفگیر", "spatula", d.TransformPoint(new Vector3(-.55f, -.069f, -.04f)), "spatula", brown, drawerRoot);
        Item("CanOpener", "der Dosenöffner", "دربازکن قوطی", "can opener", d.TransformPoint(new Vector3(-.18f, -.069f, -.04f)), "opener", red, drawerRoot);
        Item("RollingPin", "das Nudelholz", "وردنه", "rolling pin", d.TransformPoint(new Vector3(.37f, -.069f, .10f)), "rollingpin", brown, drawerRoot);
        Item("Peeler", "der Schäler", "پوست‌کن", "peeler", d.TransformPoint(new Vector3(.25f, -.069f, -.18f)), "peeler", green, drawerRoot);
    }

    private static void UnderSink()
    {
        Label(GameObject.Find("Spuele_BasinBottom"), "das Spülbecken", "لگن سینک", "sink basin");
        Label(GameObject.Find("Spuele_Rim"), "die Spüle", "سینک ظرف‌شویی", "kitchen sink");
        Label(GameObject.Find("Spuele_Drain"), "der Abfluss", "خروجی آب سینک", "sink drain");
        Transform drain = Fixed("DrainPipe", "das Abflussrohr", "لوله خروجی سینک", "sink drainpipe");
        Tube(drain, new Vector3(0, .705f, 4.45f), new Vector3(0, .48f, 4.45f), .032f, steel);
        Transform trap = Fixed("Trap", "der Siphon", "سیفون", "P-trap");
        Vector3 last = new Vector3(0, .48f, 4.45f);
        for (int i = 1; i <= 12; i++)
        {
            float angle = Mathf.PI + i / 12f * Mathf.PI;
            Vector3 next = new Vector3(.11f + .11f * Mathf.Cos(angle), .48f + .11f * Mathf.Sin(angle), 4.45f);
            Tube(trap, last, next, .034f, steel); last = next;
        }
        Transform waste = Fixed("WastePipe", "das Abwasserrohr", "لوله فاضلاب", "waste pipe");
        Tube(waste, last, new Vector3(.22f, .48f, 4.96f), .032f, steel);
        Transform hotValve = Fixed("HotValve", "das Eckventil", "شیر پیسوار", "angle valve");
        Valve(hotValve, -.28f, red);
        Transform coldValve = Fixed("ColdValve", "das Eckventil", "شیر پیسوار", "angle valve");
        Valve(coldValve, .30f, blue);
        Transform hot = Fixed("HotWaterHose", "der Warmwasserschlauch", "شلنگ آب گرم", "hot water hose");
        Tube(hot, new Vector3(-.28f, .57f, 4.72f), new Vector3(-.22f, .67f, 4.83f), .012f, red);
        Tube(hot, new Vector3(-.22f, .67f, 4.83f), new Vector3(.24f, .85f, 4.83f), .012f, red);
        Tube(hot, new Vector3(.24f, .85f, 4.83f), new Vector3(.25f, .965f, 4.78f), .012f, red);
        Transform cold = Fixed("ColdWaterHose", "der Kaltwasserschlauch", "شلنگ آب سرد", "cold water hose");
        Tube(cold, new Vector3(.30f, .57f, 4.72f), new Vector3(.34f, .72f, 4.83f), .012f, blue);
        Tube(cold, new Vector3(.34f, .72f, 4.83f), new Vector3(.30f, .965f, 4.78f), .012f, blue);
        Item("Bucket", "der Eimer", "سطل", "bucket", new Vector3(-.39f, .224f, 4.38f), "bucket", blue);
        Item("Cleaner", "das Reinigungsmittel", "شوینده", "cleaning product", new Vector3(.40f, .224f, 4.29f), "bottle", green);
        Item("DishBrush", "die Spülbürste", "برس ظرف‌شویی", "dish brush", new Vector3(.39f, .224f, 4.60f), "brush", cream);
    }

    private static void Fridge()
    {
        Item("Milk", "die Milch", "شیر", "milk", new Vector3(3.25f, 1.444f, 4.28f), "carton", white);
        Item("Yogurt", "der Joghurt", "ماست", "yogurt", new Vector3(3.53f, 1.444f, 4.28f), "yogurt", blue);
        Item("Butter", "die Butter", "کره", "butter", new Vector3(3.79f, 1.444f, 4.28f), "butter", yellow);
        Item("Cheese", "der Käse", "پنیر", "cheese", new Vector3(3.25f, 1.024f, 4.28f), "cheese", yellow);
        Item("Egg", "das Ei", "تخم‌مرغ", "egg", new Vector3(3.54f, 1.024f, 4.28f), "egg", cream);
        Item("Jam", "die Marmelade", "مربا", "jam", new Vector3(3.79f, 1.024f, 4.28f), "jar", red);
        Item("Apple", "der Apfel", "سیب", "apple", new Vector3(3.25f, .684f, 4.27f), "apple", red);
        Item("Tomato", "die Tomate", "گوجه‌فرنگی", "tomato", new Vector3(3.52f, .684f, 4.27f), "tomato", red);
        Item("Cucumber", "die Gurke", "خیار", "cucumber", new Vector3(3.78f, .684f, 4.36f), "cucumber", green);
        Item("Juice", "der Saft", "آب‌میوه", "juice", new Vector3(3.40f, 1.794f, 4.35f), "carton", orange);
        Item("IceCream", "das Eis", "بستنی", "ice cream", new Vector3(3.52f, .334f, 4.30f), "box", blue);
        foreach (GameObject obj in UnityEngine.Object.FindObjectsByType<GameObject>())
            if (obj.name == "Fridge_InteriorShelf") Label(obj, "der Kühlschrankboden", "طبقه یخچال", "refrigerator shelf");
    }

    private static void Furniture()
    {
        foreach (string name in new[] { "Stuhl_Chair_Front_Left", "Stuhl_Chair_Front_Right", "Stuhl_Chair_Back_Left", "Stuhl_Chair_Back_Right" })
            Label(GameObject.Find(name), "der Stuhl", "صندلی", "chair");
        Label(GameObject.Find("Table"), "der Tisch", "میز", "table");
    }

    private static void Record(string de, string fa, string en)
    { string row = de + " | " + fa + " | " + en; if (!rows.Contains(row)) rows.Add(row); }
    private static void SetName(KitchenInteractable item, string de)
    { SerializedObject serialized = new SerializedObject(item); serialized.FindProperty("displayName").stringValue = de; serialized.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Label(GameObject obj, string de, string fa, string en)
    {
        if (obj == null) throw new InvalidOperationException("Missing scene object for " + de);
        VocabularyObject label = obj.GetComponent<VocabularyObject>();
        if (label == null) label = obj.AddComponent<VocabularyObject>();
        SetName(label, de); Record(de, fa, en);
    }
    private static Transform Fixed(string id, string de, string fa, string en)
    {
        Transform obj = new GameObject("Detail_" + id).transform;
        obj.SetParent(root, false); Label(obj.gameObject, de, fa, en); return obj;
    }
    private static void Valve(Transform parent, float x, Material cap)
    {
        Tube(parent, new Vector3(x, .57f, 4.96f), new Vector3(x, .57f, 4.69f), .021f, steel);
        Part(parent, PrimitiveType.Cylinder, new Vector3(x, .57f, 4.95f), new Vector3(.08f, .009f, .08f), steel).localRotation = Quaternion.Euler(90, 0, 0);
        Part(parent, PrimitiveType.Cylinder, new Vector3(x, .60f, 4.71f), new Vector3(.065f, .013f, .065f), cap);
    }
    private static void Item(string id, string de, string fa, string en, Vector3 position, string kind, Material color, Transform parent = null)
    {
        Transform obj = new GameObject("Contents_" + id).transform;
        obj.SetParent(parent != null ? parent : root, true);
        obj.position = position;
        Shape(obj, kind, color);
        PickupItem pickup = obj.gameObject.AddComponent<PickupItem>();
        SetName(pickup, de);
        SerializedObject serialized = new SerializedObject(pickup);
        serialized.FindProperty("lessonOrder").intValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo();
        Record(de, fa, en);
    }

    private static void Shape(Transform obj, string kind, Material color)
    {
        switch (kind)
        {
            case "cup": case "jug":
                Vessel(obj, kind, kind == "jug" ? .077f : .06f, kind == "jug" ? .19f : .13f, color);
                Handle(obj, .064f, .075f, .048f, color); break;
            case "wineglass":
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .008f, 0), new Vector3(.11f, .008f, .11f), ceramic);
                Tube(obj, new Vector3(0, .01f, 0), new Vector3(0, .09f, 0), .008f, ceramic);
                Transform bowl = new GameObject("Bowl").transform; bowl.SetParent(obj, false); bowl.localPosition = Vector3.up * .09f;
                Vessel(bowl, "WineBowl", .054f, .095f, ceramic); break;
            case "plate": Vessel(obj, "Saucer", .12f, .022f, color); break;
            case "teapot":
                Part(obj, PrimitiveType.Sphere, new Vector3(0, .085f, 0), new Vector3(.19f, .16f, .16f), color);
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .17f, 0), new Vector3(.09f, .012f, .09f), color);
                Part(obj, PrimitiveType.Sphere, new Vector3(0, .191f, 0), Vector3.one * .025f, brown);
                Tube(obj, new Vector3(-.06f, .09f, 0), new Vector3(-.145f, .16f, 0), .019f, color);
                Handle(obj, .087f, .085f, .053f, color); break;
            case "jar":
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .09f, 0), new Vector3(.13f, .09f, .13f), color);
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .187f, 0), new Vector3(.14f, .009f, .14f), steel);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .09f, -.066f), new Vector3(.085f, .066f, .002f), cream); break;
            case "carton":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .105f, 0), new Vector3(.125f, .21f, .12f), color);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .10f, -.061f), new Vector3(.1f, .065f, .002f), blue);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .225f, 0), new Vector3(.125f, .03f, .055f), cream); break;
            case "colander":
                Vessel(obj, "Colander", .13f, .12f, color);
                Tube(obj, new Vector3(-.12f, .10f, 0), new Vector3(-.18f, .10f, 0), .012f, steel);
                Tube(obj, new Vector3(.12f, .10f, 0), new Vector3(.18f, .10f, 0), .012f, steel); break;
            case "dish":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .01f, 0), new Vector3(.30f, .02f, .22f), color);
                foreach (float x in new[] { -.145f, .145f }) Part(obj, PrimitiveType.Cube, new Vector3(x, .05f, 0), new Vector3(.015f, .08f, .22f), color);
                foreach (float z in new[] { -.105f, .105f }) Part(obj, PrimitiveType.Cube, new Vector3(0, .05f, z), new Vector3(.28f, .08f, .015f), color); break;
            case "box": case "butter":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .045f, 0), new Vector3(.21f, .09f, .15f), color);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .096f, 0), new Vector3(.22f, .012f, .16f), cream); break;
            case "grater":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .11f, 0), new Vector3(.12f, .22f, .11f), steel);
                for (int y = 0; y < 4; y++) for (int x = 0; x < 3; x++)
                    Part(obj, PrimitiveType.Cube, new Vector3(-.035f + .035f * x, .04f + .04f * y, -.056f), new Vector3(.018f, .014f, .003f), brown);
                Tube(obj, new Vector3(-.05f, .25f, 0), new Vector3(.05f, .25f, 0), .015f, color); break;
            case "spatula":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .012f, -.05f), new Vector3(.035f, .024f, .19f), color);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .014f, .09f), new Vector3(.11f, .028f, .11f), steel); break;
            case "opener":
                Part(obj, PrimitiveType.Cube, new Vector3(-.02f, .012f, -.04f), new Vector3(.025f, .024f, .16f), color);
                Part(obj, PrimitiveType.Cube, new Vector3(.02f, .012f, -.04f), new Vector3(.025f, .024f, .16f), color);
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .025f, .07f), new Vector3(.075f, .018f, .075f), steel); break;
            case "rollingpin":
                Tube(obj, new Vector3(-.12f, .035f, 0), new Vector3(.12f, .035f, 0), .035f, color);
                Tube(obj, new Vector3(-.18f, .035f, 0), new Vector3(.18f, .035f, 0), .015f, color); break;
            case "peeler":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .012f, -.04f), new Vector3(.03f, .024f, .14f), color);
                Tube(obj, new Vector3(-.04f, .013f, .05f), new Vector3(.04f, .013f, .05f), .008f, steel);
                Tube(obj, new Vector3(-.04f, .013f, .05f), new Vector3(0, .013f, .02f), .006f, steel);
                Tube(obj, new Vector3(.04f, .013f, .05f), new Vector3(0, .013f, .02f), .006f, steel); break;
            case "bucket":
                Vessel(obj, "Bucket", .11f, .25f, color);
                Vector3 handleStart = new Vector3(.11f, .25f, 0);
                for (int i = 1; i <= 12; i++)
                {
                    float angle = Mathf.PI * i / 12;
                    Vector3 next = new Vector3(.11f * Mathf.Cos(angle), .25f + .11f * Mathf.Sin(angle), 0);
                    Tube(obj, handleStart, next, .007f, steel); handleStart = next;
                }
                break;
            case "bottle":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .115f, 0), new Vector3(.10f, .23f, .075f), color);
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .265f, 0), new Vector3(.04f, .035f, .04f), red);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .12f, -.039f), new Vector3(.075f, .08f, .002f), cream); break;
            case "brush":
                Tube(obj, new Vector3(0, .015f, 0), new Vector3(0, .19f, 0), .012f, brown);
                Part(obj, PrimitiveType.Cube, new Vector3(0, .23f, 0), new Vector3(.065f, .08f, .055f), color); break;
            case "yogurt":
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .055f, 0), new Vector3(.105f, .055f, .105f), color);
                Part(obj, PrimitiveType.Cylinder, new Vector3(0, .114f, 0), new Vector3(.115f, .004f, .115f), white); break;
            case "cheese":
                Part(obj, PrimitiveType.Cube, new Vector3(0, .035f, 0), new Vector3(.17f, .07f, .13f), color);
                for (int i = 0; i < 3; i++) Part(obj, PrimitiveType.Sphere, new Vector3(-.05f + i * .05f, .07f, 0), new Vector3(.022f, .003f, .022f), orange); break;
            case "egg": Part(obj, PrimitiveType.Sphere, new Vector3(0, .043f, 0), new Vector3(.057f, .086f, .057f), color); break;
            case "apple": case "tomato":
                Part(obj, PrimitiveType.Sphere, new Vector3(0, .065f, 0), new Vector3(.13f, .12f, .13f), color);
                Tube(obj, new Vector3(0, .12f, 0), new Vector3(.008f, .15f, 0), .005f, brown);
                Part(obj, PrimitiveType.Sphere, new Vector3(.02f, .132f, 0), new Vector3(.05f, .008f, .022f), green); break;
            case "cucumber":
                Part(obj, PrimitiveType.Capsule, new Vector3(0, .033f, 0), new Vector3(.06f, .13f, .06f), color).localRotation = Quaternion.Euler(90, 0, 0); break;
            default: throw new InvalidOperationException("Unknown content shape " + kind);
        }
    }

    private static Transform Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material; return obj.transform;
    }
    private static void Tube(Transform parent, Vector3 a, Vector3 b, float radius, Material material)
    {
        Transform cylinder = Part(parent, PrimitiveType.Cylinder, (a + b) / 2, new Vector3(radius * 2, Vector3.Distance(a, b) / 2, radius * 2), material);
        cylinder.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
    }
    private static void Handle(Transform parent, float x, float y, float radius, Material material)
    {
        Vector3 last = new Vector3(x, y - radius, 0);
        for (int i = 1; i <= 8; i++)
        {
            float a = -Mathf.PI / 2 + i * Mathf.PI / 8;
            Vector3 next = new Vector3(x + Mathf.Cos(a) * radius, y + Mathf.Sin(a) * radius, 0);
            Tube(parent, last, next, .008f, material); last = next;
        }
    }
    private static void Vessel(Transform parent, string name, float radius, float height, Material material)
    {
        string path = "Assets/Models/KitchenContents/" + name + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            Vector2[] profile = { new Vector2(0, 0), new Vector2(radius * .72f, 0), new Vector2(radius, height),
                new Vector2(radius - .007f, height), new Vector2(radius * .72f - .007f, .012f), new Vector2(0, .012f) };
            const int segments = 32;
            List<Vector3> vertices = new List<Vector3>(); List<int> triangles = new List<int>();
            for (int j = 0; j < profile.Length; j++)
                for (int i = 0; i <= segments; i++)
                { float a = i * Mathf.PI * 2 / segments; vertices.Add(new Vector3(Mathf.Cos(a) * profile[j].x, profile[j].y, Mathf.Sin(a) * profile[j].x)); }
            for (int j = 0; j < profile.Length - 1; j++)
                for (int i = 0; i < segments; i++)
                { int a = j * (segments + 1) + i, b = a + segments + 1; triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
        }
        GameObject obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
        obj.transform.SetParent(parent, false); obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        MeshCollider collider = obj.GetComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true;
    }
    private static Material Material(string name, Color color, float metallic = 0)
    {
        string path = "Assets/Materials/Contents/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", metallic > 0 ? .6f : .28f);
        return material;
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); Folder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
