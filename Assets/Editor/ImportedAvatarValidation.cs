using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Run only in an isolated batch project: never open/replace the user's working scene.
[InitializeOnLoad]
public static class ImportedAvatarValidation
{
    private const string Active="DeutschRoom.ImportedAvatarSmoke.Active",Failed="DeutschRoom.ImportedAvatarSmoke.Failed",Checked="DeutschRoom.ImportedAvatarSmoke.Checked";
    static ImportedAvatarValidation()
    {
        EditorApplication.playModeStateChanged+=Changed;
        EditorApplication.update+=Poll;
        if(Application.isBatchMode && SessionState.GetBool(Active,false) && EditorApplication.isPlaying)EditorApplication.delayCall+=Check;
    }
    private static void Poll()
    {
        if(Application.isBatchMode && SessionState.GetBool(Active,false) && EditorApplication.isPlaying &&
            !SessionState.GetBool(Checked,false))Check();
    }
    public static void Run()
    {
        if(!Application.isBatchMode || !File.Exists(Path.Combine(Application.dataPath,"../.avatar-smoke-project")))
            throw new InvalidOperationException("Run the imported-avatar smoke test in an explicitly marked isolated batch project.");
        SessionState.SetBool(Active,true);SessionState.SetBool(Checked,false);SessionState.SetBool(Failed,false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorApplication.isPlaying=true;
    }
    private static void Changed(PlayModeStateChange change)
    {
        if(!Application.isBatchMode || !SessionState.GetBool(Active,false))return;
        if(change==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Check;
        if(change==PlayModeStateChange.EnteredEditMode)
        {SessionState.SetBool(Active,false);EditorApplication.Exit(SessionState.GetBool(Failed,true)?1:0);}
    }
    [Serializable] private sealed class NativeReport
    {
        public bool passed,rootMotion;
        public string source,shader;
        public string[] clips;
        public float height,visualScale;
        public int bones,fullTriangles,firstPersonTriangles,directionalClipsTested,gripCasesTested;
    }
    private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
    private static void Check()
    {
        if(!EditorApplication.isPlaying || SessionState.GetBool(Checked,false))return;
        SessionState.SetBool(Checked,true);
        GameObject root=null;
        try
        {
            root=new GameObject("Isolated imported avatar test");root.SetActive(false);
            var capsule=root.AddComponent<CharacterController>();capsule.height=1.76f;capsule.radius=.35f;capsule.center=new Vector3(0,.9f,0);
            var view=new GameObject("View",typeof(Camera));view.transform.SetParent(root.transform,false);view.transform.localPosition=new Vector3(0,1.62f,0);
            var movement=root.AddComponent<FirstPersonMovement>();movement.playerCamera=view.transform;movement.enabled=false;
            var interaction=root.AddComponent<PlayerInteraction>();interaction.enabled=false;
            var body=root.AddComponent<ImportedPlayerBody>();root.SetActive(true);
            // No learning/profile initialization in the isolated visual test.
            root.GetComponent<LearningProgress>().enabled=false;
            Require(body.enabled && body.VisualRoot!=null && body.CharacterAnimator!=null,"Imported visual failed to initialize.");
            Require(body.CharacterAnimator.isHuman && body.CharacterAnimator.avatar.isValid,"Invalid imported humanoid avatar.");
            Require(!body.CharacterAnimator.applyRootMotion,"Animation owns player displacement.");
            Require(body.VisualRoot.GetComponentsInChildren<Collider>().Length==0,"Visual introduced colliders.");
            Require(body.VisualRoot.GetComponentsInChildren<MonoBehaviour>().Length==0,"Vendor demo behaviour leaked into the player visual.");
            var full=Array.Find(body.VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(),r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly);
            var first=Array.Find(body.VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(),r=>r.name=="First-person body");
            Require(full!=null && first!=null,"Missing full/first-person renderer pair.");
            Require(first.sharedMesh.triangles.Length>0 && first.sharedMesh.triangles.Length<full.sharedMesh.triangles.Length,"Head triangle filter failed.");
            var clips=body.CharacterAnimator.runtimeAnimatorController.animationClips;
            foreach(string expected in new[]{"Idle","Walking","Running"})
            {
                Require(body.CharacterAnimator.HasState(0,Animator.StringToHash("Base Layer."+expected)),"Missing animator state "+expected);

            }
            Require(clips.Length==17 && clips.All(c=>c!=null && c.humanMotion && c.length>0 && c.isLooping),"Expected original idle and 16 looping Humanoid motion clips.");
            Require(clips.Count(c=>c.name.StartsWith("HumanM@"))==16,"Human Basic Motions clips are not bound.");
            int directionChecks=0;
            var animator=body.CharacterAnimator;
            foreach(string gait in new[]{"Walk","Run"})
            for(int direction=0;direction<8;direction++)
            {
                float angle=direction*Mathf.PI/4;
                animator.SetFloat("MoveX",Mathf.Sin(angle));animator.SetFloat("MoveZ",Mathf.Cos(angle));
                animator.speed=1;animator.Play("Base Layer."+(gait=="Walk"?"Walking":"Running"),0,0);animator.Update(0);
                string clipName="HumanM@"+gait+"01_"+PlayerLocomotionBuilder.Directions[direction];
                Require(animator.GetCurrentAnimatorClipInfo(0).Any(info=>info.clip.name==clipName && info.weight>.95f),"Incorrect directional blend: "+clipName);
                Vector3 originalRoot=root.transform.position;
                var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Vector3 start=foot.position;float motion=0;
                for(int sample=0;sample<12;sample++)
                {
                    animator.Update(1f/30);motion=Mathf.Max(motion,Vector3.Distance(start,foot.position));
                    foreach(var bone in first.bones)if(bone!=null)
                        Require(!float.IsNaN(bone.position.x) && !float.IsNaN(bone.rotation.w) && Vector3.Distance(bone.position,root.transform.position)<3,"Invalid retargeted pose: "+clipName);
                }
                Require(motion>.01f,"No retargeted foot motion: "+clipName);
                Require(Vector3.Distance(root.transform.position,originalRoot)<.0001f,"Clip displaced gameplay root: "+clipName);
                directionChecks++;
            }
            animator.speed=1;animator.Play("Base Layer.Idle",0,0);animator.Update(0);
            Require(full.sharedMaterial!=null && full.sharedMaterial.shader!=null && full.sharedMaterial.shader.name!="Hidden/InternalErrorShader","Imported material/shader is missing.");
            Require(full.sharedMaterial.GetTexture("_BaseMap")!=null,"Imported albedo texture is missing.");
            var fitted=new Mesh();full.BakeMesh(fitted,true);bool bounded=false;Bounds geometry=default;
            foreach(Vector3 vertex in fitted.vertices)
            {
                Vector3 point=root.transform.InverseTransformPoint(full.transform.TransformPoint(vertex));
                if(!bounded){geometry=new Bounds(point,Vector3.zero);bounded=true;}else geometry.Encapsulate(point);
            }
            UnityEngine.Object.Destroy(fitted);
            Require(Mathf.Abs(geometry.size.y-capsule.height)<.025f,"Avatar height "+geometry.size.y+" does not match capsule "+capsule.height+"; visual scale "+body.VisualRoot.localScale.x+"; human scale "+body.CharacterAnimator.humanScale);
            foreach(var bone in new[]{HumanBodyBones.Head,HumanBodyBones.Hips,HumanBodyBones.RightHand,HumanBodyBones.LeftHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot})
                Require(body.CharacterAnimator.GetBoneTransform(bone)!=null,"Missing bone "+bone);
            body.ShowHeadInMirror(true);Require(full.shadowCastingMode==ShadowCastingMode.On && first.forceRenderingOff,"Mirror view is incomplete or duplicated.");
            body.ShowHeadInMirror(false);Require(full.shadowCastingMode==ShadowCastingMode.ShadowsOnly && !first.forceRenderingOff,"Main-view masking was not restored.");
            int gripChecks=CheckCarryGrip(body,interaction,movement,capsule);
            var posture=typeof(FirstPersonMovement).GetProperty("Posture");
            foreach(var mode in new[]{FirstPersonMovement.BodyPosture.Standing,FirstPersonMovement.BodyPosture.Sitting,FirstPersonMovement.BodyPosture.Lying})
            {
                posture.SetValue(movement,mode);capsule.height=mode==FirstPersonMovement.BodyPosture.Standing?1.76f:mode==FirstPersonMovement.BodyPosture.Sitting?1.05f:.72f;
                for(int i=0;i<4;i++){body.SendMessage("Update");body.CharacterAnimator.Update(.016f);body.SendMessage("LateUpdate");}
                foreach(var bone in first.bones)if(bone!=null)Require(!float.IsNaN(bone.position.x) && !float.IsNaN(bone.rotation.w),"Non-finite pose for "+mode);
            }
            var curl=typeof(ImportedPlayerBody).GetMethod("CurlFingers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Transform finger=body.CharacterAnimator.GetBoneTransform(HumanBodyBones.RightIndexProximal);Quaternion before=finger.rotation;
            curl.Invoke(body,new object[]{1f});Require(Quaternion.Angle(before,finger.rotation)>.1f,"Imported fingers did not curl.");
            var report=new NativeReport { passed=true,source="Stylized Player Character (Free), URP",height=geometry.size.y,
                visualScale=body.VisualRoot.localScale.x,bones=first.bones.Length,fullTriangles=full.sharedMesh.triangles.Length/3,
                firstPersonTriangles=first.sharedMesh.triangles.Length/3,clips=clips.Select(c=>c.name).Distinct().ToArray(),shader=full.sharedMaterial.shader.name,
                rootMotion=body.CharacterAnimator.applyRootMotion,directionalClipsTested=directionChecks,gripCasesTested=gripChecks };
            File.WriteAllText(Path.Combine(Application.dataPath,"../AvatarNativeResult.json"),JsonUtility.ToJson(report,true));
            Debug.Log("IMPORTED_AVATAR_NATIVE_PASS: humanoid rig, readable mesh, head filtering, mirror restoration, no demo/colliders, standing/sitting/lying finite poses and finger articulation; 16 retargeted directional clips, correct blend selection and no gameplay-root displacement; settled palm contact across item sizes, rotations, look angles and postures.");
        }
        catch(Exception error){SessionState.SetBool(Failed,true);Debug.LogException(error);}
        finally{if(root!=null)UnityEngine.Object.Destroy(root);EditorApplication.isPlaying=false;}
    }
    private static int CheckCarryGrip(ImportedPlayerBody body,PlayerInteraction interaction,FirstPersonMovement movement,CharacterController capsule)
    {
        int cases=0;
        var held=typeof(PlayerInteraction).GetProperty("HeldItem");
        var posture=typeof(FirstPersonMovement).GetProperty("Posture");
        var yaw=typeof(PlayerInteraction).GetField("placementYaw",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var view=movement.playerCamera;
        foreach(Vector3 size in new[]{new Vector3(.025f,.015f,.21f),new Vector3(.12f,.14f,.12f),new Vector3(.28f,.08f,.28f)})
        {
            var root=new GameObject("Isolated grip object");root.SetActive(false);
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.name=size.z<.22f && size.x<.03f ? "SpoonHandle" : "Container";
            mesh.transform.SetParent(root.transform,false);mesh.transform.localScale=size;
            if(mesh.name=="SpoonHandle")
            {
                var bowl=GameObject.CreatePrimitive(PrimitiveType.Cube);bowl.name="SpoonBowl";
                bowl.transform.SetParent(root.transform,false);bowl.transform.localPosition=new Vector3(0,0,.13f);bowl.transform.localScale=new Vector3(.05f,.018f,.06f);
            }
            root.transform.localScale=new Vector3(1.1f,.9f,1.2f);
            var item=root.AddComponent<PickupItem>();root.SetActive(true);item.BeginCarry();held.SetValue(interaction,item);
            try
            {
                if(mesh.name=="SpoonHandle")Require(Vector3.Distance(item.LocalGripPoint,Vector3.zero)<.001f && Vector3.Distance(item.LocalGripPoint,item.LocalBounds.center)>.01f,"Spoon grip must use handle, not whole-object centre.");
                foreach(var mode in new[]{FirstPersonMovement.BodyPosture.Standing,FirstPersonMovement.BodyPosture.Sitting,FirstPersonMovement.BodyPosture.Lying})
                foreach(float pitch in new[]{-60f,0f,65f})
                foreach(float angle in new[]{0f,90f,180f,270f})
                foreach(string state in new[]{"Idle","Walking","Running"})
                {
                    posture.SetValue(movement,mode);capsule.height=mode==FirstPersonMovement.BodyPosture.Standing?1.76f:mode==FirstPersonMovement.BodyPosture.Sitting?1.05f:.72f;
                    view.localPosition=new Vector3(0,mode==FirstPersonMovement.BodyPosture.Standing?1.62f:mode==FirstPersonMovement.BodyPosture.Sitting?1f:.42f,0);
                    view.localRotation=Quaternion.Euler(pitch,0,0);yaw.SetValue(interaction,angle);
                    interaction.SendMessage("LateUpdate");body.SendMessage("Update");
                    body.CharacterAnimator.SetFloat("MoveX",state=="Walking"?1:0);body.CharacterAnimator.SetFloat("MoveZ",state=="Walking"?0:-1);
                    body.CharacterAnimator.Play("Base Layer."+state,0,.2f);body.CharacterAnimator.Update(0);body.SendMessage("LateUpdate");
                    Require(Vector3.Distance(interaction.HandPosition,body.PalmContact)<.002f,"Held item detached from imported palm: "+mode+" / "+pitch+" / "+angle+" / "+state);
                    Require(!float.IsNaN(item.transform.position.x),"Invalid carried item pose.");
                    // Re-applying the final pass must not accumulate drift.
                    Vector3 position=item.transform.position;
                    interaction.AlignSettledCarryToPalm(body.PalmContact);
                    Require(Vector3.Distance(position,item.transform.position)<.0001f,"Grip correction accumulates drift.");
                    var placing=typeof(PlayerInteraction).GetField("placing",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    placing.SetValue(interaction,true);
                    interaction.AlignSettledCarryToPalm(body.PalmContact+Vector3.one);
                    Require(Vector3.Distance(position,item.transform.position)<.0001f,"Carry anchor overwrote placement pose.");
                    placing.SetValue(interaction,false);
                    var elapsed=typeof(PlayerInteraction).GetField("pickupElapsed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                    elapsed.SetValue(interaction,.10f);interaction.AlignSettledCarryToPalm(body.PalmContact+Vector3.one);
                    Require(Vector3.Distance(position,item.transform.position)<.0001f,"Carry anchor skipped pickup animation.");
                    elapsed.SetValue(interaction,1f);
                    cases++;
                }
            }
            finally { interaction.CancelCarry();UnityEngine.Object.DestroyImmediate(root); }
        }
        posture.SetValue(movement,FirstPersonMovement.BodyPosture.Standing);capsule.height=1.76f;
        view.localPosition=new Vector3(0,1.62f,0);view.localRotation=Quaternion.identity;
        return cases;
    }

}
