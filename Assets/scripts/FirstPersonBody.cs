using UnityEngine;
using UnityEngine.Rendering;

// A lightweight first-person avatar. The CharacterController remains the only body collider.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(FirstPersonMovement), typeof(CharacterController))]
public sealed class FirstPersonBody : MonoBehaviour
{
    private FirstPersonMovement movement;
    private PlayerControls controls;
    private PlayerInteraction interaction;
    private CharacterController controller;
    private Transform root, torso, waist, head, hair;
    private readonly Transform[] upperArms = new Transform[2], forearms = new Transform[2], hands = new Transform[2];
    private readonly Transform[] thighs = new Transform[2], shins = new Transform[2], shoes = new Transform[2];
    private readonly Transform[] knees = new Transform[2], elbows = new Transform[2];
    private readonly Material[] materials = new Material[5];
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
        materials[0] = Surface("Avatar teal sweatshirt", new Color(.12f, .39f, .38f), .28f);
        materials[1] = Surface("Avatar navy trousers", new Color(.075f, .12f, .19f), .18f);
        materials[2] = Surface("Avatar skin", new Color(.64f, .39f, .25f), .3f);
        materials[3] = Surface("Avatar sneakers", new Color(.79f, .82f, .79f), .28f);
        materials[4] = Surface("Avatar hair", new Color(.065f, .045f, .035f), .15f);
        root = new GameObject("Player Avatar").transform; root.SetParent(transform, false);
        torso = Part("Sweatshirt", PrimitiveType.Capsule, root, materials[0]);
        waist = Part("Waist", PrimitiveType.Sphere, root, materials[1]);
        head = Part("Head", PrimitiveType.Sphere, root, materials[2]);
        hair = Part("Hair", PrimitiveType.Sphere, head, materials[4]);
        hair.localPosition = new Vector3(0, .34f, -.1f); hair.localScale = new Vector3(1.03f, .45f, 1.03f);
        for (int i = 0; i < 2; i++)
        {
            Transform eye = Part(i == 0 ? "Left eye" : "Right eye", PrimitiveType.Sphere, head, materials[3]);
            eye.localPosition = new Vector3(i == 0 ? -.21f : .21f, .10f, .43f);
            eye.localScale = new Vector3(.19f, .16f, .12f);
            Transform pupil = Part("Pupil", PrimitiveType.Sphere, eye, materials[4]);
            pupil.localPosition = new Vector3(0, 0, .42f); pupil.localScale = new Vector3(.5f,.65f,.45f);
        }
        Transform nose = Part("Nose", PrimitiveType.Sphere, head, materials[2]);
        nose.localPosition = new Vector3(0, -.07f, .47f); nose.localScale = new Vector3(.15f,.18f,.16f);
        Transform neck = Part("Neck", PrimitiveType.Sphere, head, materials[2]);
        neck.localPosition = new Vector3(0, -.55f, -.05f); neck.localScale = new Vector3(.5f, .65f, .5f);
        // Own head cannot block the camera, including at the lowest posture and maximum pitch.
        foreach (Renderer renderer in head.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        for (int i = 0; i < 2; i++)
        {
            string side = i == 0 ? "Left " : "Right ";
            upperArms[i] = Part(side + "sleeve", PrimitiveType.Capsule, root, materials[0]);
            forearms[i] = Part(side + "forearm", PrimitiveType.Capsule, root, materials[0]);
            hands[i] = Part(side + "hand", PrimitiveType.Sphere, root, materials[2]);
            Transform thumb = Part(side + "thumb", PrimitiveType.Sphere, hands[i], materials[2]);
            thumb.localPosition = new Vector3(i == 0 ? .38f : -.38f, -.06f, .15f);
            thumb.localScale = new Vector3(.36f, .56f, .55f);
            thighs[i] = Part(side + "trouser thigh", PrimitiveType.Capsule, root, materials[1]);
            shins[i] = Part(side + "trouser shin", PrimitiveType.Capsule, root, materials[1]);
            knees[i] = Part(side + "knee", PrimitiveType.Sphere, root, materials[1]);
            elbows[i] = Part(side + "elbow", PrimitiveType.Sphere, root, materials[0]);
            shoes[i] = Part(side + "sneaker", PrimitiveType.Capsule, root, materials[3]);
        }
        Pose(0, 0);
    }

    private static Material Surface(string name, Color color, float smoothness)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
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
        Segment(torso, hip + Vector3.up * .10f, shoulder, .36f, .24f);
        waist.localPosition = hip; waist.localScale = new Vector3(.32f, .22f, .23f);
        head.localPosition = new Vector3(0, eye - .015f, -.12f);
        head.localScale = new Vector3(.22f, .28f, .23f);
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
            shoes[i].localScale = new Vector3(.125f, .13f, .08f);
            Vector3 armShoulder = shoulder + new Vector3(side * .215f, -.025f, 0);
            Vector3 elbow = new Vector3(side * .265f, eye - .49f, .14f - swing * .035f);
            Vector3 wrist = new Vector3(side * .245f, eye - .28f - swing * .025f, .48f + swing * .035f);
            elbow = Vector3.Lerp(elbow, new Vector3(side * .26f, hipY + .18f, -.04f), lookDown);
            wrist = Vector3.Lerp(wrist, new Vector3(side * .24f, hipY - .07f, .05f), lookDown);
            if (interaction != null && interaction.HeldItem != null && i == 1 && movement.playerCamera != null)
                wrist = transform.InverseTransformPoint(movement.playerCamera.TransformPoint(new Vector3(.20f, -.29f, .58f)));
            if (lie > 0) { elbow = Vector3.Lerp(elbow, new Vector3(side * .25f, .17f + feet, .26f), lie); wrist.y = Mathf.Lerp(wrist.y, .15f + feet, lie); }
            Segment(upperArms[i], armShoulder, elbow, .115f, .12f);
            Segment(forearms[i], elbow, wrist, .09f, .095f);
            elbows[i].localPosition = elbow; elbows[i].localScale = Vector3.one * .105f;
            hands[i].localPosition = wrist + new Vector3(0, -.005f, .025f);
            hands[i].localRotation = Quaternion.Euler(15, -side * 12, 0);
            hands[i].localScale = new Vector3(.075f, .07f, .13f);
        }
    }

    private static void Segment(Transform part, Vector3 from, Vector3 to, float width, float depth)
    {
        part.localPosition = (from + to) / 2;
        part.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
        part.localScale = new Vector3(width, Mathf.Max(width, Vector3.Distance(from, to) + width * .25f) / 2, depth);
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
        foreach (Material material in materials) if (material != null) Destroy(material);
    }
}
