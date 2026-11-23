using System;
using System.Linq;
using UnityEngine;

public static class LivingHallVocabularyValidation
{
    static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
    public static void Run(PlayerInteraction player)
    {
        Transform root=GameObject.Find("LivingHallFurnishings")?.transform;
        Check(root!=null,"Missing living/hall furnishing root.");
        var targets=root.GetComponentsInChildren<FixedLearningTarget>().OrderBy(t=>t.Order).ToArray();
        Check(targets.Length==20&&targets.Select(t=>t.Order).SequenceEqual(Enumerable.Range(30,20)),"F05-F08 must append twenty stable targets at 30-49.");
        Check(player.Learning.TotalStages==IntroLessonCatalog.ImplementedTaskCount,"Unexpected curriculum size.");
        foreach(var target in targets)
        {
            var p=target.transform.position;bool hall=target.name.StartsWith("Hall_");
            Check(hall?(p.x>1.88f&&p.x<3.72f&&p.z>-15.92f&&p.z< -5.08f):(p.x> -4.92f&&p.x<1.72f&&p.z> -10.42f&&p.z< -5.08f),"Target outside assigned room: "+target.name);
            Check(target.GetComponentInChildren<Collider>()!=null,"No identification surface: "+target.name);
            foreach(string phrase in new[]{target.Item.DisplayName})
                IntroLessonValidation.CheckIdentificationVoice(player, phrase);
        }
        foreach(var part in root.GetComponentsInChildren<VocabularyPart>())
        {
            Check(part.Owner!=null&&part.GetComponent<KitchenInteractable>()==null,"Part annotation overrides functional owner: "+part.name);
            IntroLessonValidation.CheckIdentificationVoice(player, part.DisplayName);
        }
        Check(root.GetComponentsInChildren<Openable>().Length==2,"Shoe cabinet requires two opening doors.");
        // A continuous standing route down the corridor, through both occupied-room entrances,
        // and through the living room remains clear with storage closed.
        for(float z=-5.5f;z>=-15.3f;z-=.2f)Clear(player,new Vector3(2.8f,0,z));
        for(float x=2.8f;x>=-.1f;x-=.2f)Clear(player,new Vector3(x,0,-7.6f));
        foreach(Vector3 p in new[]{new Vector3(0,0,-6),new Vector3(-.25f,0,-9),new Vector3(-3.6f,0,-7.5f)})Clear(player,p);
        // Verify that layered items have independently reachable surfaces, not only labels.
        Ray("Living_Remote",new Vector3(-.55f,1.6f,-7.5f),new Vector3(-1.38f,.49f,-7.85f));
        Ray("Living_Cushion",new Vector3(-2.5f,1.6f,-6.8f),new Vector3(-2.5f,.7f,-5.83f));
        var rug=GameObject.Find("Living_Rug").GetComponent<KitchenInteractable>();
        Ray("Living_Rug",new Vector3(-.1f,1.6f,-7.1f),rug.IdentificationPoint.position);
        Debug.Log("LIVING_HALL_VOCABULARY_VALIDATION_PASS: twenty appended targets, German voice, functional storage, saved phases, clear corridor/room access and exposed textile/remote surfaces.");
    }
    static void Clear(PlayerInteraction player,Vector3 at)
    {
        foreach(var hit in Physics.OverlapCapsule(at+Vector3.up*.43f,at+Vector3.up*1.43f,.30f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                // Existing room-door motion is validated by HomePresentationValidation after opening.
                var door=hit.GetComponentInParent<Openable>();
                bool roomDoor=door!=null&&door.transform.IsChildOf(GameObject.Find("HomeExpansion").transform);
                Check(roomDoor||hit.transform.IsChildOf(player.transform),"Living/hall walking route blocked at "+at+" by "+hit.name);
            }
    }
    static void Ray(string name,Vector3 eye,Vector3 point)
    {
        var delta=point-eye;
        Check(Physics.Raycast(eye,delta.normalized,out var hit,delta.magnitude+.03f)&&hit.collider.GetComponentInParent<KitchenInteractable>()==GameObject.Find(name).GetComponent<KitchenInteractable>(),"Layered target cannot be identified: "+name);
    }
}
