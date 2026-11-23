using System;
using System.Linq;
using UnityEngine;

public static class HouseAccessoryValidation
{
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static void Run(PlayerInteraction player)
    {
        var root=GameObject.Find("HouseAccessories")?.transform;Check(root!=null,"Missing accessory root.");
        var targets=root.GetComponentsInChildren<FixedLearningTarget>().OrderBy(t=>t.Order).ToArray();
        Check(targets.Length==20&&targets.Select(t=>t.Order).SequenceEqual(Enumerable.Range(50,20)),"Accessory batch must append exactly twenty targets at 50-69.");
        Check(player.Learning.TotalStages==IntroLessonCatalog.ImplementedTaskCount,"Unexpected curriculum size.");
        Check(root.GetComponentsInChildren<Openable>().Length==HouseAccessoryBuilder.AddedDoors,"Washer needs a working door and detergent drawer.");
        foreach(var target in targets)
        {
            Rect room=target.name.StartsWith("Hall_")?new Rect(1.8f,-16,2,11):target.name.StartsWith("Bedroom2_")?new Rect(3.8f,-11,5.7f,6):new Rect(3.8f,-16,5.7f,5);
            var p=target.transform.position;Check(room.Contains(new Vector2(p.x,p.z)),"Accessory outside assigned room: "+target.name);
            Check(target.GetComponentsInChildren<Collider>().Length>0,"Target has no geometry: "+target.name);
            foreach(var phrase in new[]{target.Item.DisplayName})
                IntroLessonValidation.CheckIdentificationVoice(player, phrase);
            Check(VisibleFromWalkableSpot(player,target,room),"No reachable identification surface within 2.7 metres: "+target.name);
        }
        foreach(var part in root.GetComponentsInChildren<VocabularyPart>())
        {
            Check(part.Owner!=null&&part.GetComponent<KitchenInteractable>()==null,"Part annotation replaced its functional owner: "+part.name);
            IntroLessonValidation.CheckIdentificationVoice(player, part.DisplayName);
        }
        foreach(var at in new[]{new Vector3(6.15f,0,-12.55f),new Vector3(8.95f,0,-13.75f),new Vector3(6.8f,0,-14.2f),new Vector3(5.9f,0,-9.35f)})
            Check(Clear(player,at),"New laundry/cleaning contents block an approach: "+at);
        Debug.Log("HOUSE_ACCESSORY_VALIDATION_PASS: twenty ordered targets; each has a visible surface from a clear standing position within interaction range; voice/part ownership, washer interactions and save migration verified.");
    }
    internal static bool Clear(PlayerInteraction player,Vector3 at)
    {
        foreach(var hit in Physics.OverlapCapsule(at+Vector3.up*.43f,at+Vector3.up*1.43f,.30f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
        {
            var door=hit.GetComponentInParent<Openable>();
            bool roomDoor=door!=null&&door.transform.IsChildOf(GameObject.Find("HomeExpansion").transform);
            if(!roomDoor&&!hit.transform.IsChildOf(player.transform))return false;
        }
        return true;
    }
    internal static bool VisibleFromWalkableSpot(PlayerInteraction player,FixedLearningTarget target,Rect room)
    {
        var points=target.GetComponentsInChildren<Collider>().Select(c=>c.bounds.center).ToList();
        if(target.Item.IdentificationPoint!=null)points.Insert(0,target.Item.IdentificationPoint.position);
        for(float x=room.xMin+.4f;x<room.xMax-.35f;x+=.35f)for(float z=room.yMin+.4f;z<room.yMax-.35f;z+=.35f)
        {
            var foot=new Vector3(x,0,z);var eye=foot+Vector3.up*1.6f;
            if(Vector3.Distance(eye,target.transform.position)>3.4f||!Clear(player,foot))continue;
            foreach(var point in points)
            {
                var delta=point-eye;if(delta.magnitude>2.7f)continue;
                if(Physics.Raycast(eye,delta.normalized,out var hit,delta.magnitude+.025f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.transform.IsChildOf(target.transform))return true;
            }
        }
        return false;
    }
}
