using UnityEngine;
using UnityEngine.Rendering;

// A lightweight first-person avatar. The CharacterController remains the only body collider.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(FirstPersonMovement), typeof(CharacterController))]
public sealed class FirstPersonBody : MonoBehaviour
{
    internal static readonly System.Collections.Generic.List<FirstPersonBody> ActiveBodies = new System.Collections.Generic.List<FirstPersonBody>();
    private Renderer[] headRenderers;
    private void OnEnable() => ActiveBodies.Add(this);
    private void OnDisable() => ActiveBodies.Remove(this);

    private FirstPersonMovement movement;
    private PlayerControls controls;
    private PlayerInteraction interaction;
    private CharacterController controller;
    private Transform root, torso, waist, head, hair;
    private readonly Transform[] upperArms = new Transform[2], forearms = new Transform[2], hands = new Transform[2];
    private readonly Transform[] thighs = new Transform[2], shins = new Transform[2], shoes = new Transform[2];
    private readonly Transform[] knees = new Transform[2], elbows = new Transform[2];
    private readonly Material[] materials = new Material[8];
    private readonly System.Collections.Generic.List<Mesh> ownedMeshes = new System.Collections.Generic.List<Mesh>();
    private readonly AvatarAnatomy.Hand[] handRigs = new AvatarAnatomy.Hand[2];
    private readonly Transform[] eyes = new Transform[2];
    private readonly float[] grip = new float[2];
    private float standingHeight, feet, phase, stride;
    private Vector3 previousPosition;

    public Transform VisualRoot => root;

    private void Awake()
    {
        movement = GetComponent<FirstPersonMovement>();
        controls = GetComponent<PlayerControls>();
        interaction = GetComponent<PlayerInteraction>();
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        feet = controller.center.y - controller.height / 2;
        previousPosition = transform.position;
        materials[0] = Surface("Avatar teal sweatshirt", new Color(.17f, .48f, .46f), .28f);
        materials[1] = Surface("Avatar navy trousers", new Color(.11f, .17f, .25f), .18f);
        materials[2] = Surface("Avatar skin", new Color(.73f, .52f, .39f), .22f);
        materials[3] = Surface("Avatar sneakers", new Color(.79f, .82f, .79f), .28f);
        materials[4] = Surface("Avatar hair", new Color(.10f, .067f, .045f), .15f);
        materials[5] = Surface("Warm eye whites", new Color(.86f,.84f,.78f), .5f);
        materials[6] = Surface("Brown irises",new Color(.15f,.08f,.035f),.48f);
        materials[7] = Surface("Muted lips",new Color(.49f,.25f,.21f),.26f);
        root = new GameObject("Player Avatar").transform; root.SetParent(transform, false);
        Mesh shirt = AvatarAnatomy.Shirt(); ownedMeshes.Add(shirt);
        torso = MeshPart("Sweatshirt", root, shirt, materials[0]);
        waist = Part("Waist", PrimitiveType.Sphere, root, materials[1]);
        head = new GameObject("Head").transform; head.SetParent(root,false);
        Transform face = new GameObject("Face and hair").transform; face.SetParent(head,false);
        Mesh skull = AvatarAnatomy.Head(false), haircut = AvatarAnatomy.Head(true);
        MeshPart("Shaped face",face,skull,materials[2]); hair=MeshPart("Hair",face,haircut,materials[4]);
        for (int i = 0; i < 2; i++)
        {
            float side=i==0?-1:1;
            eyes[i]=new GameObject(i==0?"Left eye":"Right eye").transform;eyes[i].SetParent(head,false);
            eyes[i].localPosition=new Vector3(side*.185f,.085f,.4f);
            FacialPart("Eye white",eyes[i],Vector3.zero,new Vector3(.145f,.070f,.038f),materials[5]);
            FacialPart("Iris",eyes[i],new Vector3(0,0,.022f),new Vector3(.055f,.058f,.016f),materials[6]);
            FacialPart("Pupil",eyes[i],new Vector3(0,0,.031f),new Vector3(.028f,.035f,.008f),materials[4]);
            FacialPart("Eye catchlight",eyes[i],new Vector3(-.009f,.011f,.037f),new Vector3(.009f,.009f,.004f),materials[5]);
            Contour("Upper eyelid",eyes[i],new Vector3(0,.022f,.023f),.145f,.018f,.012f,.008f,materials[2]);
            Contour("Lower eyelid",eyes[i],new Vector3(0,-.023f,.023f),.135f,.012f,-.007f,.006f,materials[2]);
            var brow=Contour("Eyebrow",face,new Vector3(side*.185f,.17f,.399f),.17f,.019f,.018f,.011f,materials[4]);
            brow.localRotation=Quaternion.Euler(0,0,-side*6);
            FacialPart("Ear",face,new Vector3(side*.47f,-.015f,-.015f),new Vector3(.12f,.22f,.10f),materials[2]);
            FacialPart("Ear contour",face,new Vector3(side*.485f,-.015f,.033f),new Vector3(.055f,.13f,.025f),materials[7]);
        }
        for(int i=0;i<2;i++)
            FacialPart("Nostril",face,new Vector3((i==0?-1:1)*.037f,-.114f,.472f),new Vector3(.027f,.013f,.012f),materials[7]);
        Contour("Upper lip",face,new Vector3(0,-.202f,.403f),.18f,.014f,.008f,.012f,materials[7]);
        Contour("Lower lip",face,new Vector3(0,-.22f,.403f),.17f,.018f,-.004f,.014f,materials[7]);
        Transform neck = Part("Neck", PrimitiveType.Sphere, face, materials[2]);
        neck.localPosition = new Vector3(0, -.55f, -.05f); neck.localScale = new Vector3(.5f, .65f, .5f);
        AvatarAnatomy.CombineStatic(face,ownedMeshes); Destroy(skull); Destroy(haircut);
        // Own head cannot block the camera, including at the lowest posture and maximum pitch.
        var visibleHead = new System.Collections.Generic.List<Renderer>();
        foreach(var renderer in head.GetComponentsInChildren<Renderer>()) if(renderer.enabled)visibleHead.Add(renderer);
        headRenderers = visibleHead.ToArray();
        foreach (Renderer renderer in headRenderers) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left " : "Right ";
            upperArms[i] = Part(side + "sleeve", PrimitiveType.Capsule, root, materials[0]);
            forearms[i] = Part(side + "forearm", PrimitiveType.Capsule, root, materials[0]);
            hands[i] = new GameObject(side+"hand").transform; hands[i].SetParent(root,false);
            handRigs[i]=new AvatarAnatomy.Hand(hands[i],materials[2],i==0); ownedMeshes.Add(handRigs[i].mesh);
            thighs[i] = Part(side + "trouser thigh", PrimitiveType.Capsule, root, materials[1]);
            shins[i] = Part(side + "trouser shin", PrimitiveType.Capsule, root, materials[1]);
            knees[i] = Part(side + "knee", PrimitiveType.Sphere, root, materials[1]);
            elbows[i] = Part(side + "elbow", PrimitiveType.Sphere, root, materials[0]);
            shoes[i] = Part(side + "sneaker", PrimitiveType.Capsule, root, materials[3]);
        }
        Pose(0, 0);
    }

    // Called synchronously around the offscreen mirror render; the main view keeps its head hidden.
    public void ShowHeadInMirror(bool visible)
    {
        if (head == null) return;
        foreach (var renderer in headRenderers)
            if(renderer!=null)renderer.shadowCastingMode = visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
    }

    private Transform Contour(string name,Transform parent,Vector3 position,float width,float height,float arch,float depth,Material material)
    {
        Mesh mesh=AvatarAnatomy.Feature(width,height,arch,depth);ownedMeshes.Add(mesh);
        Transform part=MeshPart(name,parent,mesh,material);part.localPosition=position;return part;
    }

    private static Transform MeshPart(string name,Transform parent,Mesh mesh,Material material)
    {
        var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent,false);
        part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=material;return part.transform;
    }
    private static Transform FacialPart(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {var part=Part(name,PrimitiveType.Sphere,parent,material);part.localPosition=position;part.localScale=scale;return part;}

    private static Material Surface(string name, Color color, float smoothness)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
        // A small material-only fill preserves facial/clothing separation in dark rooms.
        // It does not add a scene light or flatten the Lit shader's directional shading.
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * .075f);
        return material;
    }

    private static Transform Part(string name, PrimitiveType shape, Transform parent, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape); part.name = name;
        part.transform.SetParent(parent, false);
        Collider collider = part.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part.transform;
    }

    private void LateUpdate()
    {
        Vector3 delta = transform.position - previousPosition; delta.y = 0;
        previousPosition = transform.position;
        // Animation follows actual movement, so pushing against a wall does not walk in place.
        bool active = controls != null && controls.GameplayActive && Time.deltaTime > 0;
        float speed = active ? delta.magnitude / Mathf.Max(.001f, Time.deltaTime) : 0;
        stride = Mathf.MoveTowards(stride, Mathf.Clamp01(speed / 2), Time.deltaTime * 6);
        if (active) phase += Mathf.Min(delta.magnitude, .15f) * 9;
        float sit = Mathf.InverseLerp(standingHeight, Mathf.Max(controller.radius * 2, 1.05f), controller.height);
        float lie = Mathf.InverseLerp(Mathf.Max(controller.radius * 2, 1.05f), controller.radius * 2 + .02f, controller.height);
        Pose(sit, lie);
    }

    private void Pose(float sit, float lie)
    {
        float eye = movement.playerCamera != null ? movement.playerCamera.localPosition.y : standingHeight - .15f;
        float lookDown = movement.playerCamera != null ? Mathf.InverseLerp(15, 65,
            Mathf.DeltaAngle(0, movement.playerCamera.localEulerAngles.x)) : 0;
        float hipY = Mathf.Lerp(Mathf.Lerp(.94f, .48f, sit), .16f, lie) + feet;
        float hipZ = Mathf.Lerp(-.14f - .02f * sit, .30f, lie);
        Vector3 hip = new Vector3(0, hipY, hipZ);
        Vector3 shoulder = new Vector3(0, eye - .24f, -.16f);
        Vector3 shirtBottom = hip + Vector3.up * .035f;
        torso.localPosition = (shirtBottom + shoulder) * .5f;
        torso.localRotation = Quaternion.FromToRotation(Vector3.up, shoulder-shirtBottom);
        torso.localScale = new Vector3(.78f, Vector3.Distance(shirtBottom,shoulder), .57f);
        waist.localPosition = hip; waist.localScale = new Vector3(.32f, .22f, .23f);
        head.localPosition = new Vector3(0, eye - .015f, -.12f);
        head.localScale = new Vector3(.22f, .28f, .23f);
        float pitch=movement.playerCamera!=null?Mathf.DeltaAngle(0,movement.playerCamera.localEulerAngles.x):0;
        head.localRotation=Quaternion.Euler(Mathf.Clamp(pitch*.65f,-35,45),0,0);
        float blinkTime=Mathf.Repeat(Time.time,4.7f);
        float blink=blinkTime<.16f?1-Mathf.Sin(blinkTime/.16f*Mathf.PI)*.965f:1;
        foreach(var eyeRoot in eyes)if(eyeRoot.localScale.y!=blink)eyeRoot.localScale=new Vector3(1,blink,1);
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1 : 1;
            float swing = Mathf.Sin(phase + i * Mathf.PI) * stride * (1 - lie);
            Vector3 upper = hip + new Vector3(side * .105f, -.015f, 0);
            Vector3 knee = new Vector3(side * .12f, Mathf.Lerp(.51f - .18f * sit, .14f, lie) + feet,
                Mathf.Lerp(.04f + .22f * sit, .59f, lie) + swing * .11f);
            Vector3 ankle = new Vector3(side * .12f, feet + .11f + Mathf.Max(0, swing) * .045f,
                Mathf.Lerp(.10f + .16f * sit, .88f, lie) + swing * .18f);
            Segment(thighs[i], upper, knee, .15f, .16f);
            Segment(shins[i], knee, ankle, .115f, .13f);
            knees[i].localPosition = knee; knees[i].localScale = new Vector3(.125f,.125f,.13f);
            shoes[i].localPosition = ankle + new Vector3(0, -.045f, .065f);
            shoes[i].localRotation = Quaternion.Euler(90, 0, 0);
            shoes[i].localScale = new Vector3(.105f, .145f, .075f);
            Vector3 armShoulder = shoulder + new Vector3(side * .215f, -.025f, 0);
            Vector3 elbow = new Vector3(side * .265f, eye - .48f, -.035f - swing * .05f);
            Vector3 wrist = new Vector3(side * .245f, eye - .74f - swing * .025f, -.015f + swing * .06f);
            elbow = Vector3.Lerp(elbow, new Vector3(side * .26f, hipY + .18f, -.04f), lookDown);
            wrist = Vector3.Lerp(wrist, new Vector3(side * .24f, hipY - .07f, .05f), lookDown);
            if (lie > 0) { elbow = Vector3.Lerp(elbow, new Vector3(side * .25f, .17f + feet, .26f), lie); wrist.y = Mathf.Lerp(wrist.y, .15f + feet, lie); }
            Quaternion handRotation=Quaternion.Euler(80,-side*12,0);
            bool reaching=i==1 && interaction!=null && interaction.HandEngaged;
            if(reaching)
            {
                float reach=interaction.HandBlend;
                Vector3 target=transform.InverseTransformPoint(interaction.HandPosition);
                Vector3 direction=(target-armShoulder).normalized;
                Vector3 reachedWrist=armShoulder+Vector3.ClampMagnitude(target-direction*.075f-armShoulder,.61f);
                Vector3 bend=Vector3.Cross(Vector3.up,direction)*side;
                float distance=Vector3.Distance(armShoulder,reachedWrist);
                Vector3 reachedElbow=(armShoulder+reachedWrist)*.5f+bend.normalized*Mathf.Sqrt(Mathf.Max(.002f,.31f*.31f-distance*distance*.25f));
                wrist=Vector3.Lerp(wrist,reachedWrist,reach);elbow=Vector3.Lerp(elbow,reachedElbow,reach);
                handRotation=Quaternion.Slerp(handRotation,Quaternion.LookRotation(direction,Vector3.up),reach);
            }
            Segment(upperArms[i], armShoulder, elbow, .115f, .12f);
            Segment(forearms[i], elbow, wrist, .09f, .095f);
            elbows[i].localPosition = elbow; elbows[i].localScale = Vector3.one * .105f;
            if(!hands[i].localPosition.Equals(wrist))hands[i].localPosition = wrist;
            if(!hands[i].localRotation.Equals(handRotation))hands[i].localRotation = handRotation;
            grip[i]=Mathf.MoveTowards(grip[i],reaching?interaction.HandClosure:.12f,Time.deltaTime*7);
            handRigs[i].Pose(grip[i]);
        }
    }

    private static void Segment(Transform part, Vector3 from, Vector3 to, float width, float depth)
    {
        Vector3 position=(from+to)/2;
        Quaternion rotation=Quaternion.FromToRotation(Vector3.up,to-from);
        Vector3 scale=new Vector3(width,Mathf.Max(width,Vector3.Distance(from,to)+width*.25f)/2,depth);
        if(!part.localPosition.Equals(position))part.localPosition=position;
        if(!part.localRotation.Equals(rotation))part.localRotation=rotation;
        if(!part.localScale.Equals(scale))part.localScale=scale;
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
        foreach (Material material in materials) if (material != null) Destroy(material);
        foreach (Mesh mesh in ownedMeshes) if(mesh!=null)Destroy(mesh);
    }
}
