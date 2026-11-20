using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FinalFurnishingValidation
{
    static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    public static void Run(PlayerInteraction player)
    {
        var root=GameObject.Find("FinalFurnishings")?.transform;Check(root!=null,"Missing final furnishing root.");
        var targets=root.GetComponentsInChildren<FixedLearningTarget>().OrderBy(t=>t.Order).ToArray();
        Check(targets.Length==10&&targets.Select(t=>t.Order).SequenceEqual(Enumerable.Range(70,10)),"Last ten targets must append at orders 70-79.");
        Check(player.Learning.SearchCount==133&&player.Learning.TotalStages==136,"Expected 53 pickups, eighty fixed targets and three article tasks.");
        foreach(var target in targets)
        {
            string id=target.name;Rect room=id.StartsWith("Hall_")?new Rect(1.8f,-16,2,11):id.StartsWith("Living_")?new Rect(-5,-10.5f,6.8f,5.5f):id.StartsWith("Kitchen_")?new Rect(-5,-5,10,10):new Rect(3.8f,-16,5.7f,5);
            Check(room.Contains(new Vector2(target.transform.position.x,target.transform.position.z)),"Final object outside its room: "+id);
            Check(HouseAccessoryValidation.VisibleFromWalkableSpot(player,target,room),"Final target cannot be reached/identified: "+id);
            foreach(string phrase in new[]{target.Item.DisplayName,GermanLessonSpeech.Find(target.Item.DisplayName),GermanLessonSpeech.FixedFound(target.Item.DisplayName)})
                Check(Resources.Load<AudioClip>("Voice/"+GermanVoice.Key(phrase))!=null,"Missing final-target voice: "+phrase);
        }
        foreach(var part in root.GetComponentsInChildren<VocabularyPart>())
        {
            Check(part.Owner!=null&&part.GetComponent<KitchenInteractable>()==null,"Final part overrides functional owner.");
            Check(Resources.Load<AudioClip>("Voice/"+GermanVoice.Key(part.DisplayName))!=null,"Missing final part voice: "+part.DisplayName);
        }
        var roll=GameObject.Find("Kitchen_PaperRoll");var rack=GameObject.Find("Abtropfgestell_DishRack");
        Check(!RenderBounds(roll).Intersects(RenderBounds(rack)),"Kitchen paper roll overlaps the existing dish rack.");
        var radiator=GameObject.Find("Living_Radiator").transform;
        Check(radiator.Find("WallBracket").position.x<radiator.position.x,"Radiator brackets face into the room instead of the wall.");
        var action=root.GetComponentInChildren<RoomLightSwitch>();Check(action!=null,"No functional light switch.");
        var s=new SerializedObject(action);var lights=s.FindProperty("lights");Check(lights.arraySize==3,"Switch must control exactly three living-room lights.");
        Check(action.IsOn&&action.GetAction(player)=="Ausschalten","Switch initial action mismatch.");
        try
        {
            action.Interact(player);Check(!action.IsOn&&action.GetAction(player)=="Einschalten","Switch did not change action.");
            for(int i=0;i<lights.arraySize;i++)Check(!(lights.GetArrayElementAtIndex(i).objectReferenceValue as Light).enabled,"Living-room lamp did not switch off.");
            action.Interact(player);Check(action.IsOn,"Switch did not restore lights.");
            for(int i=0;i<lights.arraySize;i++)Check((lights.GetArrayElementAtIndex(i).objectReferenceValue as Light).enabled,"Living-room lamp did not restore.");
        }
        finally{if(!action.IsOn)action.Interact(player);}
        Check(LearningProgress.RestoreStage(123,53,53,70,80)==133&&LearningProgress.RestoreStage(125,53,53,70,80)==135&&LearningProgress.RestoreStage(126,53,53,70,80)==136,"Final insertion lost saved article/completion phase.");
        Check(LearningProgress.RestoreStage(112,53,53,70,80)==112,"Existing accessory progress changed.");
        Debug.Log("FINAL_FURNISHING_VALIDATION_PASS: final ten targets and parts have reachable geometry/voice; living-room switch toggles and restores three lights; previous saved phases preserved.");
    }
    static Bounds RenderBounds(GameObject obj)
    {
        var renderers=obj.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
        foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds;
    }

}
