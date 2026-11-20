using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Runs against the real pickup/placement callbacks; the local profile is never written.
public static class LessonGuidanceValidation
{
    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    public static void Run(PlayerInteraction player)
    {
        LearningProgress learning = player.Learning;
        int searchCount = learning.SearchCount;
        PickupItem[] items = UnityEngine.Object.FindObjectsByType<PickupItem>();
        Vector3[] positions = items.Select(item => item.transform.position).ToArray();
        Quaternion[] rotations = items.Select(item => item.transform.rotation).ToArray();
        Transform[] parents = items.Select(item => item.transform.parent).ToArray();
        FieldInfo state = typeof(LearningProgress).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic);
        object previousState = state.GetValue(learning);
        bool persistence = learning.PersistenceEnabled;
        learning.PersistenceEnabled = false;
        try
        {
            Transform garden = GameObject.Find("WindowGarden")?.transform;
            Check(garden != null && garden.GetComponentsInChildren<Renderer>().Length >= 70, "Window garden is missing.");
            Check(garden.GetComponentsInChildren<Collider>().Length == 0, "Decorative garden added unnecessary physics.");
            foreach (Renderer renderer in garden.GetComponentsInChildren<Renderer>())
                Check(renderer.bounds.max.x < -5.1f, "Garden geometry entered the kitchen.");
            for (int i = 0; i < items.Length; i++) items[i].transform.position = new Vector3(-3 + i % 8 * .3f, 0, -2 + i / 8 * .3f);
            Physics.SyncTransforms();
            PickupItem spoon = items.First(item => item.DisplayName == "der Löffel");
            SetStage(state, learning, 0, items.Length, false);
            Check(learning.TryGetMapTarget(out Vector3 searchTarget, out LessonMinimap.TargetKind searchKind) && searchTarget == spoon.transform.position && searchKind == LessonMinimap.TargetKind.Find,
                "Minimap does not target the current vocabulary object.");
            Check(player.TryPickUp(spoon), "Cannot test spoken vocabulary completion.");
            Check(learning.TryGetMapTarget(out Vector3 nextTarget, out _) && nextTarget != spoon.transform.position, "Minimap did not switch to next stage.");
            Check(player.Voice.LastRequest[0] == GermanLessonSpeech.Found(spoon.DisplayName), "Found object was not named with accusative/pronoun feedback.");
            Check(player.Voice.LastRequest.Any(line => line.StartsWith("Finde als Nächstes ")), "Next search target was not spoken.");
            PlaceAway(player);

            FixedLearningTarget[] fixedGoals = UnityEngine.Object.FindObjectsByType<FixedLearningTarget>().OrderBy(goal => goal.Order).ToArray();
            SetStage(state, learning, items.Length, items.Length, false);
            for (int i = 0; i < fixedGoals.Length; i++)
            {
                Check(learning.TryGetMapTarget(out Vector3 fixedPosition, out _) && fixedPosition == fixedGoals[i].transform.position, "Minimap does not follow fixed fixture target.");
                player.Identify(fixedGoals[i].Item);
                Check(learning.Stage == items.Length + i + 1 && player.Voice.LastRequest.Contains(GermanLessonSpeech.FixedFound(fixedGoals[i].Item.DisplayName)),
                    "Identifying the fixture did not complete exactly one fixed-object stage.");
                if (i + 1 < fixedGoals.Length) { player.Identify(fixedGoals[i].Item); Check(learning.Stage == items.Length + i + 1, "Repeated identification skipped a stage."); }
            }
            PickupItem[] feminine = items.Where(item => item.DisplayName.StartsWith("die ")).OrderBy(item => item.LocalBounds.size.sqrMagnitude).Take(10).ToArray();
            Collider table = GameObject.Find("TableTop").GetComponent<Collider>();
            PutOnTable(feminine[0], table, 0);
            PutOnTable(spoon, table, 1);
            SetStage(state, learning, searchCount + 1, items.Length, false);
            learning.RepeatTask();
            Check(learning.TryGetMapTarget(out _, out LessonMinimap.TargetKind cleanupKind) && cleanupKind == LessonMinimap.TargetKind.Remove, "Minimap does not mark old table items for removal.");
            Check(learning.IsClearingTable && player.Voice.LastRequest.Contains(GermanLessonSpeech.ClearTable) &&
                player.Voice.LastRequest.Any(line => line.StartsWith("Nimm ")), "Pre-existing items were not named for table cleanup.");
            learning.Placed(feminine[0]);
            Check(learning.IsClearingTable, "Pre-existing matching item bypassed table cleanup.");
            Check(player.TryPickUp(feminine[0]), "Cannot clear matching object.");
            PlaceAway(player);
            Check(player.TryPickUp(spoon), "Cannot clear old spoon.");
            Check(learning.IsClearingTable, "Cleanup completed before the held object was put away.");
            Check(learning.TryGetMapTarget(out Vector3 putAway, out LessonMinimap.TargetKind awayKind) && awayKind == LessonMinimap.TargetKind.PutAway &&
                !table.bounds.Contains(putAway), "Minimap does not show an outside-table placement location.");
            PlaceAway(player);
            Check(!learning.IsClearingTable && player.Voice.LastRequest.Contains(GermanLessonSpeech.TableReady), "Empty table did not begin sorting with spoken confirmation.");

            Check(learning.TryGetMapTarget(out Vector3 matchingTarget, out LessonMinimap.TargetKind matchKind) && matchKind == LessonMinimap.TargetKind.Find, "Minimap has no matching article target.");
            Check(player.TryPickUp(feminine[0]), "Cannot test held matching map target.");
            Check(learning.TryGetMapTarget(out Vector3 tableTarget, out LessonMinimap.TargetKind tableKind) && tableKind == LessonMinimap.TargetKind.Table && tableTarget == table.bounds.center, "Minimap does not direct the held matching item to the table.");
            PlaceAway(player);
            PutOnTable(spoon, table, 0);
            learning.Placed(spoon);
            Check(learning.TryGetMapTarget(out Vector3 wrongTarget, out LessonMinimap.TargetKind wrongKind) && wrongKind == LessonMinimap.TargetKind.Remove && wrongTarget == spoon.transform.position,
                "Minimap does not direct removal of the wrong article.");
            Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.Placed(spoon.DisplayName)) &&
                player.Voice.LastRequest.Contains(GermanLessonSpeech.Remove(spoon.DisplayName)), "Wrong article placement was not named and corrected.");
            for (int i = 0; i < feminine.Length; i++)
            {
                PutOnTable(feminine[i], table, i);
                learning.Placed(feminine[i]);
                Check(player.Voice.LastRequest.Contains(GermanLessonSpeech.Placed(feminine[i].DisplayName)), "Placed item was not announced.");
                Check(learning.Stage == searchCount + 1, "Sorting completed with a wrong-article object still on the table.");
                if (i == 0)
                {
                    Check(player.TryPickUp(feminine[0]), "Cannot remove counted item.");
                    Check(learning.TableCount == 0, "Removing a matching object retained its count.");
                    PlaceAway(player);
                    PutOnTable(feminine[0], table, 0); learning.Placed(feminine[0]);
                    Check(learning.TableCount == 1, "Replacing same item double-counted it.");
                }
            }
            Check(player.TryPickUp(spoon), "Cannot remove wrong article object.");
            PlaceAway(player);
            Check(learning.Stage == searchCount + 2 && learning.IsClearingTable &&
                player.Voice.LastRequest.Contains(GermanLessonSpeech.Finished) && player.Voice.LastRequest.Contains(GermanLessonSpeech.ClearTable),
                "Next article did not require clearing the previous article's objects.");
            SetStage(state, learning, searchCount + 3, items.Length, true);
            Check(!learning.TryGetMapTarget(out _, out _), "Completed curriculum retains a stale minimap target.");
            foreach (string line in player.Voice.LastRequest)
                Check(Resources.Load<AudioClip>("Voice/" + GermanVoice.Key(line)) != null, "Missing guidance clip: " + line);
            Debug.Log("LESSON_GUIDANCE_VALIDATION_PASS: named find/place feedback, old-table cleanup, incorrect article removal, live counts, no duplicate credit, next-article cleanup and garden bounds.");
        }
        finally
        {
            if (player.HeldItem != null) { player.enabled = false; player.enabled = true; }
            for (int i = 0; i < items.Length; i++) { items[i].transform.SetParent(parents[i], true); items[i].transform.SetPositionAndRotation(positions[i], rotations[i]); }
            state.SetValue(learning, previousState);
            learning.PersistenceEnabled = persistence;
            player.Voice.Stop();
            Physics.SyncTransforms();
        }
    }
    private static void SetStage(FieldInfo field, LearningProgress learning, int stage, int count, bool prepared)
    {
        string json = "{\"started\":true,\"stage\":" + stage + ",\"pickupCount\":" + count + ",\"tablePrepared\":" + (prepared ? "true" : "false") + ",\"poses\":[]}";
        field.SetValue(learning, JsonUtility.FromJson(json, field.FieldType));
    }
    private static void PlaceAway(PlayerInteraction player)
    {
        Collider floor = GameObject.Find("Floor").GetComponent<Collider>();
        Check(floor.Raycast(new Ray(new Vector3(3, 3, -2), Vector3.down), out RaycastHit hit, 5) && player.TryPlaceHeld(hit), "Cannot put cleared object outside the table.");
    }
    private static void PutOnTable(PickupItem item, Collider table, int slot)
    {
        Quaternion rotation = item.PlacementRotation(0);
        Bounds bounds = item.BoundsAt(Vector3.zero, rotation);
        Vector3 point = new Vector3(table.bounds.min.x + .24f + slot % 5 * .37f, table.bounds.max.y + .01f, table.bounds.min.z + .25f + slot / 5 * .42f);
        item.transform.SetPositionAndRotation(point - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), rotation);
        Physics.SyncTransforms();
    }
}
