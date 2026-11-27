using System;
using System.Linq;
using UnityEngine;

public static class BedroomVocabularyValidation
{
    private static void Check(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
    public static void Run(PlayerInteraction player)
    {
        var all=UnityEngine.Object.FindObjectsByType<FixedLearningTarget>().OrderBy(item=>item.Order).ToArray();
        Check(all.Length==79 && all.Select(item=>item.Order).SequenceEqual(Enumerable.Range(0,80).Where(order=>order!=48)),"Fixed target order must preserve bathroom then append twenty bedroom instances.");
        Transform bedrooms=GameObject.Find("BedroomFurnishings")?.transform;
        Check(bedrooms!=null && bedrooms.GetComponentsInChildren<FixedLearningTarget>().Length==20,"Bedroom batch must contain exactly twenty primary targets.");
        Check(bedrooms.GetComponentsInChildren<Openable>().Length==BedroomFurnishingBuilder.AddedDoors,"Expected wardrobe doors and bedroom drawers.");
        foreach(var target in all.Where(item=>item.Order>=10&&item.Order<30))
        {
            Vector3 p=target.transform.position;
            bool room1=target.name.StartsWith("Bedroom1_");
            Check(room1?(p.x>-4.92f&&p.x<1.72f&&p.z>-15.92f&&p.z< -10.58f):(p.x>3.88f&&p.x<9.42f&&p.z> -10.92f&&p.z< -5.08f),"Furniture target outside its bedroom: "+target.name);
            Check(target.GetComponentInChildren<Collider>()!=null,"Furniture has no identification collider: "+target.name);
            IntroLessonValidation.CheckIdentificationVoice(player, target.Item.DisplayName);
        }
        foreach(var part in bedrooms.GetComponentsInChildren<VocabularyPart>())
            Check(part.Owner!=null&&part.GetComponent<KitchenInteractable>()==null,"Part metadata broke furniture ownership.");
        var sheet=GameObject.Find("Bedroom1_Sheet").GetComponent<KitchenInteractable>();
        Check(sheet.IdentificationPoint!=null,"Sheet marker has no exposed anchor.");
        Vector3 eye=new Vector3(-1.1f,1.6f,-13.1f),delta=sheet.IdentificationPoint.position-eye;
        Check(Physics.Raycast(eye,delta.normalized,out RaycastHit sheetHit,delta.magnitude+.02f)&&sheetHit.collider.GetComponentInParent<KitchenInteractable>()==sheet,"Pillow/duvet obscures the sheet identification point.");
        var wardrobe=GameObject.Find("Bedroom1_Wardrobe").transform;
        Check(wardrobe.GetComponentsInChildren<Openable>().Length==2,"Wardrobe is not openable.");
        var hanger=GameObject.Find("Bedroom1_Hanger").GetComponent<FixedLearningTarget>();
        Check(hanger.Order==19,"Hanger must retain its own instance target.");
        foreach(Vector3 at in new[]{new Vector3(-1.1f,0,-13.1f),new Vector3(-2.1f,0,-12.2f),new Vector3(5.1f,0,-8.0f),new Vector3(7.0f,0,-7.0f)})
            foreach(Collider hit in Physics.OverlapCapsule(at+Vector3.up*.43f,at+Vector3.up*1.43f,.30f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                Check(hit.transform.IsChildOf(player.transform),"Bedroom walking route blocked: "+hit.name);
        Debug.Log("BEDROOM_VOCABULARY_VALIDATION_PASS: twenty appended targets, model placement, voices, component ownership, storage interactions, saved stages and clear walking routes.");
    }
}
