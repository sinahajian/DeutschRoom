using System;
using UnityEngine;

public static class HomePresentationValidation
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    public static void CheckHome(PlayerInteraction player)
    {
        LessonMinimap minimap = player.GetComponent<LessonMinimap>();
        Check(minimap != null && minimap.WorldArea.Contains(new Vector2(0, 4)) && minimap.WorldArea.Contains(new Vector2(9, -15)), "Minimap does not cover kitchen and new rooms.");
        Rect world = new Rect(-5, -16, 14.5f, 21), screen = new Rect(0, 0, 145, 210);
        Check(Vector2.Distance(LessonMinimap.Project(new Vector3(-5, 0, 5), world, screen), Vector2.zero) < .001f &&
              Vector2.Distance(LessonMinimap.Project(new Vector3(9.5f, 0, -16), world, screen), new Vector2(145, 210)) < .001f,
              "Minimap axes are mirrored or north is not up.");
        foreach (float width in new[] { 430f, 650f, 960f })
        {
            Rect map = LessonMinimap.Layout(width);
            Rect menu = new Rect(148, 18, 48, 48);
            Rect task = new Rect(width - 174, 18, 156, 58);
            Check(!map.Overlaps(menu) && !map.Overlaps(task) && !menu.Overlaps(task) && map.xMin >= 0 && map.xMax <= width,
                "Minimap overlaps toolbar at width " + width);
        }
        Transform home = GameObject.Find("HomeExpansion")?.transform;
        Check(home != null, "Home expansion missing.");
        foreach (string name in new[] { "Bedroom1", "Bedroom2", "LivingRoom", "BathroomWC", "Hallway" })
        {
            Transform room = home.Find(name);
            Check(room != null && room.Find(name + "_Floor") != null && room.Find(name + "_Ceiling") != null, "Incomplete empty room: " + name);
            Check(room.GetComponentsInChildren<PickupItem>().Length == 0 && room.childCount == 3, "The architectural room shell changed unexpectedly.");
        }
        // Trace the center of every doorway with a full standing capsule after opening doors.
        Vector3[] starts = { new Vector3(2.8f, 0, -4), new Vector3(2.8f, 0, -7.6f), new Vector3(2.8f, 0, -13.1f), new Vector3(2.8f, 0, -8), new Vector3(2.8f, 0, -13.4f) };
        Vector3[] ends = { new Vector3(2.8f, 0, -6), new Vector3(.5f, 0, -7.6f), new Vector3(.5f, 0, -13.1f), new Vector3(5, 0, -8), new Vector3(5, 0, -13.4f) };
        for (int i = 0; i < starts.Length; i++)
        {
            Vector3 direction = ends[i] - starts[i];
            foreach (RaycastHit hit in Physics.CapsuleCastAll(starts[i] + Vector3.up * .42f, starts[i] + Vector3.up * 1.42f,
                .30f, direction.normalized, direction.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                Check(hit.collider.transform.IsChildOf(player.transform), "Blocked standing doorway route: " + i + " / " + hit.collider.name);
            Check(Physics.Raycast(ends[i] + Vector3.up, Vector3.down, out RaycastHit ground, 2) && ground.normal.y > .9f,
                "Floor missing across room doorway " + i);
        }
    }

    public static void CheckPresentation(PlayerInteraction player)
    {
        LessonPresentation settings = player.GetComponent<LessonPresentation>();
        LessonPresentation.OutputMode mode = settings.Mode;
        float volume = settings.VoiceVolume;
        bool reported = settings.ReportedNoSound;
        bool persistence = settings.PersistenceEnabled;
        settings.PersistenceEnabled = false;
        try
        {
            settings.SetVoiceVolume(1);
            settings.ReportNoSound(false);
            settings.SendMessage("OnDeviceMute", false);
            settings.SetMode(LessonPresentation.OutputMode.Text);
            player.Voice.Speak("Richtig!");
            Check(settings.ShowText && !settings.PlayVoice && player.Voice.CurrentCaption == "Richtig!" && !player.Voice.GetComponent<AudioSource>().isPlaying,
                "Text-only mode did not show caption or silence narration.");
            settings.SetMode(LessonPresentation.OutputMode.Voice);
            Check(!settings.ShowText && settings.PlayVoice, "Voice-only mode has wrong output flags.");
            settings.SetMode(LessonPresentation.OutputMode.Both);
            Check(settings.ShowText && settings.PlayVoice, "Combined mode did not enable both outputs.");
            settings.SetMode(LessonPresentation.OutputMode.Voice);
            settings.SendMessage("OnDeviceMute", true);
            Check(settings.ShowAudioWarning && settings.ShowText && !settings.PlayVoice, "Device-mute event did not enable readable fallback.");
            settings.SendMessage("OnDeviceMute", false);
            Check(!settings.ShowAudioWarning && !settings.ShowText, "Volume recovery did not restore the selected voice-only mode.");
            settings.ReportNoSound(true);
            Check(settings.ShowText && settings.ShowAudioWarning, "Manual no-sound report did not enable text.");
            settings.ReportNoSound(false);
            settings.SetVoiceVolume(0);
            Check(settings.ShowAudioWarning && settings.ShowText && !settings.PlayVoice, "App voice-volume zero left the learner without feedback.");
            for (int stage = 1; stage <= player.Learning.TotalStages; stage++)
                Check(Resources.Load<AudioClip>("VoiceIntroA11/" + GermanVoice.Key(GermanLessonSpeech.Stage(stage))) != null, "Missing new stage voice: " + stage);
            Debug.Log("HOME_PRESENTATION_VALIDATION_PASS: empty connected rooms, standing doorway clearance; text/voice/both, system mute and recovery, manual no-sound fallback, app volume and all stage announcements.");
        }
        finally
        {
            player.Voice.Stop(); settings.SetMode(mode); settings.SetVoiceVolume(volume); settings.ReportNoSound(reported);
            settings.SendMessage("OnDeviceMute", false); settings.RefreshDeviceAudio(); settings.PersistenceEnabled = persistence;
        }
    }
}
