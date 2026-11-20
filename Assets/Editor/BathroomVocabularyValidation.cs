using System;
using System.Linq;
using UnityEngine;

public static class BathroomVocabularyValidation
{
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    public static void Run(PlayerInteraction player)
    {
        var fixtures=UnityEngine.Object.FindObjectsByType<FixedLearningTarget>().Where(item=>item.Order<10).OrderBy(item=>item.Order).ToArray();
        Check(fixtures.Length==10 && fixtures.Select(item=>item.Order).SequenceEqual(Enumerable.Range(0,10)),"Bathroom needs exactly 10 ordered fixture targets.");
        Check(player.Learning.SearchCount==133 && player.Learning.TotalStages==136,"Expanded curriculum count mismatch.");
        foreach(var fixture in fixtures)
        {
            Check(fixture.GetComponentInChildren<Collider>()!=null,"Unreachable fixture without collider: "+fixture.name);
            Check(Resources.Load<AudioClip>("Voice/"+GermanVoice.Key(fixture.Item.DisplayName))!=null,"Missing fixture name voice.");
        }
        foreach(var part in UnityEngine.Object.FindObjectsByType<VocabularyPart>())
        {
            Check(!string.IsNullOrEmpty(part.DisplayName),"Unnamed component: "+part.name);
            Check(Resources.Load<AudioClip>("Voice/"+GermanVoice.Key(part.DisplayName))!=null,"Missing component voice: "+part.DisplayName);
        }
        var hood=GameObject.Find("Dunstabzug_Hood").GetComponent<KitchenInteractable>();
        Check(hood!=null && hood.DisplayName=="die Dunstabzugshaube","Hood remains unidentified.");
        var leg=GameObject.Find("Leg_1").GetComponent<VocabularyPart>();
        Check(leg!=null && leg.DisplayName=="das Tischbein" && leg.Owner.DisplayName=="der Tisch","Table leg is not identified separately from its owner.");
        var handle=GameObject.Find("Cabinet_LeftHandle").GetComponent<VocabularyPart>();
        Check(handle!=null && handle.Owner is Openable && handle.GetComponent<KitchenInteractable>()==null,"Component label replaced cabinet opening interaction.");
        int stageBeforeDetail = player.Learning.Stage;
        player.IdentifyPart(handle);
        Check(player.IdentifiedName == handle.DisplayName,"Detail button mixed the part and owner names.");
        Check(player.Voice.LastRequest.Length == 1 && player.Voice.LastRequest[0] == handle.DisplayName,
            "Detail button should speak only the precise part name.");
        Check(player.Learning.Stage == stageBeforeDetail,"Detail lookup unexpectedly advanced the whole-object lesson.");
        Check(LearningProgress.RestoreStage(54,53,53,0,10)==64 && LearningProgress.RestoreStage(56,53,53,0,10)==66 &&
              LearningProgress.RestoreStage(57,53,53,10,10)==57,"Bathroom insertion damaged existing article/completion/fixed-stage saves.");
        // Standing positions in front of the fixtures and shower opening remain free.
        foreach(Vector3 at in new[]{new Vector3(4.95f,0,-14.25f),new Vector3(6.15f,0,-12.55f),new Vector3(8.95f,0,-13.75f)})
            foreach(Collider obstacle in Physics.OverlapCapsule(at+Vector3.up*.43f,at+Vector3.up*1.43f,.30f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                Check(obstacle.transform.IsChildOf(player.transform),"Bathroom approach blocked by "+obstacle.name);
        Debug.Log("BATHROOM_VOCABULARY_VALIDATION_PASS: ten accessible fixed targets, hood and component voices/context, preserved cabinet interaction and saved-stage migration.");
    }
}
