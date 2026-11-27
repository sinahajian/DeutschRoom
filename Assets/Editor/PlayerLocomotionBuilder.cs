using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Retarget the imported in-place Humanoid clips; never import the vendor demo player or controls.
public static class PlayerLocomotionBuilder
{
    public const string Output = "Assets/Resources/Characters/PlayerLocomotion.controller";
    private const string Source = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/";
    public static readonly string[] Directions = { "Forward", "ForwardRight", "Right", "BackwardRight", "Backward", "BackwardLeft", "Left", "ForwardLeft" };
    private static readonly Vector2[] Positions = { new Vector2(0,1), new Vector2(.7071068f,.7071068f), new Vector2(1,0), new Vector2(.7071068f,-.7071068f), new Vector2(0,-1), new Vector2(-.7071068f,-.7071068f), new Vector2(-1,0), new Vector2(-.7071068f,.7071068f) };

    [MenuItem("Tools/DeutschRoom/Build Human Basic Motions Locomotion")]
    public static void Build()
    {
        // Validate dependencies before replacing generated content.
        var walks = LoadClips("Walk"); var runs = LoadClips("Run");
        var source = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Animations/Animations Controllers/AC_Player.controller");
        var idle = source.layers[0].stateMachine.states.First(s => s.state.name == "Idle").state.motion;
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Output);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(Output);
        else
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Output))
                if (asset != controller) UnityEngine.Object.DestroyImmediate(asset, true);
            controller.layers = new AnimatorControllerLayer[0];
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddLayer("Base Layer");
        }
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveZ", AnimatorControllerParameterType.Float);
        var machine = controller.layers[0].stateMachine;
        var standing = machine.AddState("Idle"); standing.motion = idle;
        machine.defaultState = standing;
        AddMovement(controller, machine, "Walking", walks);
        AddMovement(controller, machine, "Running", runs);
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        Debug.Log("PLAYER_LOCOMOTION_BUILD_PASS: original idle plus 16 in-place Humanoid clips in two directional blend trees; source assets preserved.");
    }
    private static AnimationClip[] LoadClips(string gait)
    {
        return Directions.Select(direction => {
            string path = Source + gait + "/HumanM@" + gait + "01_" + direction + ".fbx";
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null || !clip.humanMotion || !clip.isLooping) throw new InvalidOperationException("Missing looping Humanoid clip: " + path);
            return clip;
        }).ToArray();
    }
    private static void AddMovement(AnimatorController controller, AnimatorStateMachine machine, string name, AnimationClip[] clips)
    {
        var tree = new BlendTree { name = name + " 8 Directions", blendType = BlendTreeType.SimpleDirectional2D, blendParameter = "MoveX", blendParameterY = "MoveZ", useAutomaticThresholds = false };
        AssetDatabase.AddObjectToAsset(tree, controller);
        for (int i = 0; i < clips.Length; i++) tree.AddChild(clips[i], Positions[i]);
        var state = machine.AddState(name); state.motion = tree;
    }
}
