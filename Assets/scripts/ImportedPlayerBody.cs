using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The imported character is visual only. Movement, camera, collision and lesson
// interaction remain owned by the existing DeutschRoom player components.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(FirstPersonMovement),typeof(CharacterController))]
public sealed class ImportedPlayerBody : MonoBehaviour
{
    internal static readonly List<ImportedPlayerBody> ActiveBodies=new List<ImportedPlayerBody>();
    public const string VisualResource="Characters/PlayerVisual";
    public const string LocomotionResource="Characters/PlayerLocomotion";
    private static readonly int MoveX=Animator.StringToHash("MoveX"),MoveZ=Animator.StringToHash("MoveZ");
    public Transform VisualRoot { get; private set; }
    public Animator CharacterAnimator { get; private set; }
    public bool HeadHidden => !mirrorVisible;
    private FirstPersonMovement movement;
    private PlayerControls controls;
    private PlayerInteraction interaction;
    private CharacterController controller;
    private float standingHeight,feet;
    private Vector3 previousPosition,basePosition;
    private bool mirrorVisible;
    private int state=-1;
    private readonly List<Mesh> ownedMeshes=new List<Mesh>();
    private readonly List<SkinnedMeshRenderer> fullRenderers=new List<SkinnedMeshRenderer>();
    private readonly List<SkinnedMeshRenderer> viewRenderers=new List<SkinnedMeshRenderer>();
    private readonly Dictionary<Transform,BonePose> overrides=new Dictionary<Transform,BonePose>();
    private Transform hips,head,rightUpper,rightLower,rightHand,leftUpper,leftLower,leftHand;
    private Transform leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot;
    private readonly Transform[,] fingers=new Transform[5,3];
    private struct BonePose { public Vector3 position;public Quaternion rotation; }
    private static readonly int[] States={Animator.StringToHash("Base Layer.Idle"),Animator.StringToHash("Base Layer.Walking"),Animator.StringToHash("Base Layer.Running")};
    private void OnEnable(){if(!ActiveBodies.Contains(this))ActiveBodies.Add(this);}
    private void OnDisable(){ActiveBodies.Remove(this);RestoreBones();}
    private Transform Bone(HumanBodyBones bone)=>CharacterAnimator.GetBoneTransform(bone);

    private void Awake()
    {
        movement=GetComponent<FirstPersonMovement>();controls=GetComponent<PlayerControls>();
        interaction=GetComponent<PlayerInteraction>();controller=GetComponent<CharacterController>();
        standingHeight=controller.height;feet=controller.center.y-standingHeight*.5f;
        previousPosition=transform.position;
        GameObject prefab=Resources.Load<GameObject>(VisualResource);
        if(prefab==null){Debug.LogError("Imported player visual is missing: "+VisualResource,this);enabled=false;return;}
        VisualRoot=Instantiate(prefab,transform,false).transform;VisualRoot.name="Imported Player Visual";
        // The resource prefab excludes the vendor's animation-gallery viewer.
        foreach(var collider in VisualRoot.GetComponentsInChildren<Collider>(true)){collider.enabled=false;Destroy(collider);}
        CharacterAnimator=VisualRoot.GetComponentInChildren<Animator>();
        if(CharacterAnimator==null || CharacterAnimator.avatar==null || !CharacterAnimator.avatar.isValid || !CharacterAnimator.isHuman)
        {Debug.LogError("Imported player requires a valid humanoid avatar.",this);enabled=false;return;}
        CharacterAnimator.applyRootMotion=false;CharacterAnimator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        CharacterAnimator.Rebind();CharacterAnimator.Play(States[0]);CharacterAnimator.Update(0);
        hips=Bone(HumanBodyBones.Hips);head=Bone(HumanBodyBones.Head);
        rightUpper=Bone(HumanBodyBones.RightUpperArm);rightLower=Bone(HumanBodyBones.RightLowerArm);rightHand=Bone(HumanBodyBones.RightHand);
        leftUpper=Bone(HumanBodyBones.LeftUpperArm);leftLower=Bone(HumanBodyBones.LeftLowerArm);leftHand=Bone(HumanBodyBones.LeftHand);
        leftThigh=Bone(HumanBodyBones.LeftUpperLeg);leftShin=Bone(HumanBodyBones.LeftLowerLeg);leftFoot=Bone(HumanBodyBones.LeftFoot);
        rightThigh=Bone(HumanBodyBones.RightUpperLeg);rightShin=Bone(HumanBodyBones.RightLowerLeg);rightFoot=Bone(HumanBodyBones.RightFoot);
        HumanBodyBones[,] mapping={
            {HumanBodyBones.RightThumbProximal,HumanBodyBones.RightThumbIntermediate,HumanBodyBones.RightThumbDistal},
            {HumanBodyBones.RightIndexProximal,HumanBodyBones.RightIndexIntermediate,HumanBodyBones.RightIndexDistal},
            {HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightMiddleIntermediate,HumanBodyBones.RightMiddleDistal},
            {HumanBodyBones.RightRingProximal,HumanBodyBones.RightRingIntermediate,HumanBodyBones.RightRingDistal},
            {HumanBodyBones.RightLittleProximal,HumanBodyBones.RightLittleIntermediate,HumanBodyBones.RightLittleDistal}};
        for(int digit=0;digit<5;digit++)for(int joint=0;joint<3;joint++)fingers[digit,joint]=Bone(mapping[digit,joint]);
        NormalizeAndPrepareMeshes();
        var locomotion=Resources.Load<RuntimeAnimatorController>(LocomotionResource);
        if(locomotion==null){Debug.LogError("Directional locomotion controller is missing: "+LocomotionResource,this);enabled=false;return;}
        CharacterAnimator.runtimeAnimatorController=locomotion;
        CharacterAnimator.Rebind();CharacterAnimator.Play(States[0]);CharacterAnimator.Update(0);
        basePosition=VisualRoot.localPosition;
        ShowHeadInMirror(false);
    }
    private void NormalizeAndPrepareMeshes()
    {
        var skins=VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        bool hasBounds=false;Bounds bounds=default;
        var baked=new Mesh {name="Imported avatar fitting sample"};
        try
        {
            foreach(var skin in skins)
            {
                // Compensate renderer scale before TransformPoint applies it once.
                skin.BakeMesh(baked,true);
                foreach(Vector3 vertex in baked.vertices)
                {
                    Vector3 p=transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                    if(!hasBounds){bounds=new Bounds(p,Vector3.zero);hasBounds=true;}else bounds.Encapsulate(p);
                }
            }
        }
        finally{Destroy(baked);}
        if(!hasBounds || bounds.size.y<.1f)throw new InvalidOperationException("Imported player has no usable skinned geometry.");
        float scale=standingHeight/bounds.size.y;
        VisualRoot.localScale=Vector3.one*scale;
        VisualRoot.localPosition=new Vector3(-bounds.center.x*scale,feet-bounds.min.y*scale,-.10f);
        foreach(var skin in skins)
        {
            if(skin.sharedMesh==null || !skin.sharedMesh.isReadable)throw new InvalidOperationException("Player mesh needs Read/Write enabled for first-person head masking.");
            Mesh body=Instantiate(skin.sharedMesh);body.name="Imported player first-person body";ownedMeshes.Add(body);
            var weights=body.boneWeights;var headBones=new bool[skin.bones.Length];
            for(int i=0;i<headBones.Length;i++)headBones[i]=skin.bones[i]!=null && (skin.bones[i]==head || skin.bones[i].IsChildOf(head));
            for(int sub=0;sub<body.subMeshCount;sub++)
            {
                var kept=new List<int>();int[] triangles=body.GetTriangles(sub);
                for(int i=0;i<triangles.Length;i+=3)
                    if(!IsHead(weights[triangles[i]],headBones) && !IsHead(weights[triangles[i+1]],headBones) && !IsHead(weights[triangles[i+2]],headBones))
                    {kept.Add(triangles[i]);kept.Add(triangles[i+1]);kept.Add(triangles[i+2]);}
                body.SetTriangles(kept,sub,false);
            }
            var visual=new GameObject("First-person body",typeof(SkinnedMeshRenderer));visual.transform.SetParent(skin.transform,false);
            var view=visual.GetComponent<SkinnedMeshRenderer>();
            view.sharedMesh=body;view.sharedMaterials=skin.sharedMaterials;view.bones=skin.bones;view.rootBone=skin.rootBone;
            view.localBounds=skin.localBounds;view.quality=skin.quality;view.updateWhenOffscreen=true;
            view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=skin.receiveShadows;
            skin.updateWhenOffscreen=true;
            fullRenderers.Add(skin);viewRenderers.Add(view);
        }
    }
    private static bool IsHead(BoneWeight w,bool[] headBones)
    {
        float weight=0;
        if(w.boneIndex0<headBones.Length && headBones[w.boneIndex0])weight+=w.weight0;
        if(w.boneIndex1<headBones.Length && headBones[w.boneIndex1])weight+=w.weight1;
        if(w.boneIndex2<headBones.Length && headBones[w.boneIndex2])weight+=w.weight2;
        if(w.boneIndex3<headBones.Length && headBones[w.boneIndex3])weight+=w.weight3;
        return weight>=.45f;
    }
    public void ShowHeadInMirror(bool visible)
    {
        mirrorVisible=visible;
        foreach(var renderer in fullRenderers)if(renderer!=null)renderer.shadowCastingMode=visible?ShadowCastingMode.On:ShadowCastingMode.ShadowsOnly;
        foreach(var renderer in viewRenderers)if(renderer!=null)renderer.forceRenderingOff=visible;
    }
    private void RestoreBones()
    {
        foreach(var pair in overrides)if(pair.Key!=null){pair.Key.localPosition=pair.Value.position;pair.Key.localRotation=pair.Value.rotation;}
        overrides.Clear();
    }
    private void Record(Transform bone)
    {
        if(bone!=null && !overrides.ContainsKey(bone))overrides.Add(bone,new BonePose {position=bone.localPosition,rotation=bone.localRotation});
    }
    private void Rotate(Transform bone,Quaternion worldRotation){if(bone==null)return;Record(bone);bone.rotation=worldRotation;}
    private void Update()
    {
        if(CharacterAnimator==null || VisualRoot==null)return;
        RestoreBones();VisualRoot.localPosition=basePosition;VisualRoot.localRotation=Quaternion.identity;
        Vector3 displacement=Vector3.ProjectOnPlane(transform.position-previousPosition,Vector3.up);
        float distance=displacement.magnitude;
        previousPosition=transform.position;
        if(Time.deltaTime<=0)return;
        float speed=controls!=null && controls.GameplayActive?distance/Mathf.Max(.001f,Time.deltaTime):0;
        Vector3 direction=transform.InverseTransformDirection(displacement.normalized);
        if(speed>.08f)
        {
            CharacterAnimator.SetFloat(MoveX,direction.x,.10f,Time.deltaTime);
            CharacterAnimator.SetFloat(MoveZ,direction.z,.10f,Time.deltaTime);
        }
        int next=movement.Posture!=FirstPersonMovement.BodyPosture.Standing || speed<.08f?0:speed<2.5f?1:2;
        if(next!=state){CharacterAnimator.CrossFadeInFixedTime(States[next],.16f);state=next;}
        CharacterAnimator.speed=next==0?1:Mathf.Clamp(speed/(next==1?1.5f:4),.65f,1.35f);
    }
    private void LateUpdate()
    {
        if(CharacterAnimator==null || VisualRoot==null || hips==null)return;
        float sit=Mathf.InverseLerp(standingHeight,Mathf.Max(controller.radius*2,1.05f),controller.height);
        float lie=Mathf.InverseLerp(Mathf.Max(controller.radius*2,1.05f),controller.radius*2+.02f,controller.height);
        if(lie>.001f)
        {
            VisualRoot.localRotation=Quaternion.Euler(-90*lie,0,0);
            VisualRoot.localPosition=Vector3.Lerp(basePosition,new Vector3(basePosition.x,feet+.17f,standingHeight-.10f),lie);
        }
        if(sit>.001f && lie<.99f)
        {
            Record(hips);Vector3 p=transform.InverseTransformPoint(hips.position);
            p.y=Mathf.Lerp(p.y,feet+.48f,sit*(1-lie));hips.position=transform.TransformPoint(p);
            SeatLeg(leftThigh,leftShin,leftFoot,-1,sit*(1-lie));SeatLeg(rightThigh,rightShin,rightFoot,1,sit*(1-lie));
        }
        if(head!=null && movement.playerCamera!=null)
        {
            float pitch=Mathf.Clamp(Mathf.DeltaAngle(0,movement.playerCamera.localEulerAngles.x)*.55f,-35,45);
            Rotate(head,Quaternion.AngleAxis(pitch,transform.right)*head.rotation);
        }
        if(lie>.01f)
        {
            RestArm(leftUpper,leftLower,leftHand,-1,lie);
            if(interaction==null || !interaction.HandEngaged)RestArm(rightUpper,rightLower,rightHand,1,lie);
        }
        if(interaction!=null && interaction.HandEngaged && rightHand!=null)
        {
            float blend=interaction.HandBlend;
            // Build a hand frame from the imported knuckles, rather than assuming
            // that its wrist axes or palm length match the old procedural model.
            Vector3 forward=fingers[2,0]!=null ? fingers[2,0].position-rightHand.position : rightHand.forward*.08f;
            Vector3 across=fingers[4,0]!=null && fingers[1,0]!=null ? fingers[4,0].position-fingers[1,0].position : rightHand.right;
            Vector3 normal=Vector3.Cross(forward,across).normalized;
            if(normal.sqrMagnitude<.01f)normal=rightHand.up;
            Quaternion frame=Quaternion.LookRotation(forward,normal);
            Quaternion desired=Quaternion.LookRotation(interaction.HandRotation*Vector3.right,interaction.HandRotation*Vector3.up)*Quaternion.Inverse(frame)*rightHand.rotation;
            Quaternion rotation=Quaternion.Slerp(rightHand.rotation,desired,blend);
            Vector3 localContact=Quaternion.Inverse(rightHand.rotation)*(PalmContact-rightHand.position);
            Vector3 wrist=interaction.HandPosition-rotation*localContact;
            Vector3 target=Vector3.Lerp(rightHand.position,wrist,blend);
            SolveLimb(rightUpper,rightLower,rightHand,target,transform.TransformPoint(new Vector3(.45f,movement.playerCamera.localPosition.y-.4f,-.1f)));
            Rotate(rightHand,rotation);
            CurlFingers(interaction.HandClosure*blend);
            interaction.AlignSettledCarryToPalm(PalmContact);
        }
    }
    public Vector3 PalmContact
    {
        get
        {
            if(rightHand==null)return transform.position;
            Vector3 forward=fingers[2,0]!=null ? fingers[2,0].position-rightHand.position : rightHand.forward*.08f;
            Vector3 across=fingers[4,0]!=null && fingers[1,0]!=null ? fingers[4,0].position-fingers[1,0].position : rightHand.right;
            Vector3 normal=Vector3.Cross(forward,across).normalized;
            return rightHand.position+forward*.65f+normal*.012f;
        }
    }
    private void SeatLeg(Transform upper,Transform lower,Transform foot,float side,float blend)
    {
        if(foot==null)return;Quaternion rotation=foot.rotation;
        Vector3 target=transform.TransformPoint(new Vector3(side*.12f,feet+.10f,.42f));
        SolveLimb(upper,lower,foot,Vector3.Lerp(foot.position,target,blend),transform.TransformPoint(new Vector3(side*.12f,feet+.45f,.28f)));
        Rotate(foot,rotation);
    }
    private void RestArm(Transform upper,Transform lower,Transform hand,float side,float blend)
    {
        if(hand==null)return;Quaternion rotation=hand.rotation;
        SolveLimb(upper,lower,hand,Vector3.Lerp(hand.position,transform.TransformPoint(new Vector3(side*.26f,feet+.15f,.5f)),blend),
            transform.TransformPoint(new Vector3(side*.4f,feet+.18f,.22f)));
        Rotate(hand,rotation);
    }
    private void SolveLimb(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole)
    {
        if(upper==null || lower==null || end==null)return;
        Vector3 start=upper.position;float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,end.position);
        Vector3 direction=target-start;if(direction.sqrMagnitude<.000001f || a<.001f || b<.001f)return;
        float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);direction.Normalize();
        Vector3 bend=Vector3.ProjectOnPlane(pole-start,direction).normalized;
        if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(direction,transform.right).normalized;
        float x=(a*a-b*b+distance*distance)/(2*distance);
        Vector3 middle=start+direction*x+bend*Mathf.Sqrt(Mathf.Max(0,a*a-x*x));
        Rotate(upper,Quaternion.FromToRotation(lower.position-start,middle-start)*upper.rotation);
        Vector3 destination=start+direction*distance;
        Rotate(lower,Quaternion.FromToRotation(end.position-lower.position,destination-lower.position)*lower.rotation);
    }
    private void CurlFingers(float closure)
    {
        if(closure<=.001f)return;
        for(int digit=0;digit<5;digit++)for(int joint=0;joint<3;joint++)
        {
            Transform bone=fingers[digit,joint];if(bone==null)continue;
            Vector3 direction=bone.childCount>0?bone.GetChild(0).position-bone.position:bone.forward;
            Vector3 axis=Vector3.Cross(direction,rightHand.position-bone.position).normalized;
            if(axis.sqrMagnitude<.01f)axis=rightHand.right;
            Rotate(bone,Quaternion.AngleAxis(closure*(digit==0?25:joint==0?40:65),axis)*bone.rotation);
        }
    }
    private void OnDestroy()
    {
        ActiveBodies.Remove(this);
        foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);
        if(VisualRoot!=null)Destroy(VisualRoot.gameObject);
    }
}
