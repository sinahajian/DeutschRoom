using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LessonDeviceRegistry
{
    private readonly Dictionary<string, KitchenInteractable> devices = new Dictionary<string, KitchenInteractable>();
    public IEnumerable<string> Keys => devices.Keys;
    public KitchenInteractable Get(string key) => devices.TryGetValue(key, out var value) ? value : throw new InvalidOperationException("Unknown lesson device " + key);
    public bool State(string key)
    {
        var d = Get(key);
        if (d is Openable o) return o.IsOpen;
        if (d is Faucet f) return f.IsRunning;
        if (d is RoomLightSwitch l) return l.IsOn;
        return ((LessonDevice)d).IsOn;
    }
    public bool Settled(string key) => Get(key) is Openable o ? !o.IsMoving : !(Get(key) is LessonDevice d) || !d.IsBusy;
    public uint Sequence(string key)
    {
        var d = Get(key);
        if (d is Openable o) return o.InteractionSequence;
        if (d is Faucet f) return f.InteractionSequence;
        if (d is RoomLightSwitch l) return l.InteractionSequence;
        return ((LessonDevice)d).InteractionSequence;
    }
    public uint Cycles(string key) => Get(key) is LessonDevice d ? d.CompletedCycles : 0;
    public void SetState(string key, bool state)
    {
        var d = Get(key);
        if (d is Openable o) o.SetLessonState(state);
        else if (d is Faucet f) f.SetLessonState(state);
        else if (d is RoomLightSwitch l) l.SetLessonState(state);
        else ((LessonDevice)d).SetLessonState(state);
    }
    public Vector3 Point(string key)
    { var d=Get(key); var c=d.GetComponentInChildren<Collider>(); return c != null ? c.bounds.center : d.transform.position; }
    private static GameObject Required(string path) => GameObject.Find(path) ?? throw new InvalidOperationException("Missing lesson binding " + path);
    private static LessonDevice Add(GameObject part, LessonDevice.DeviceMode mode, string name, Light light = null)
    {
        if (part.GetComponent<KitchenInteractable>() != null && part.GetComponent<LessonDevice>() == null)
        {
            var existing = part.transform.Find("LessonControl");
            if (existing != null) part = existing.gameObject;
            else
            {
                var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
                button.name = "LessonControl"; button.hideFlags = HideFlags.DontSave;
                button.transform.SetParent(part.transform, false);
                button.transform.localPosition = new Vector3(0,0,-.515f);
                button.transform.localScale = new Vector3(.08f,.22f,.035f);
                button.GetComponent<Renderer>().sharedMaterial = Resources.Load<Material>("Lessons/PlacementZone");
                part = button;
            }
        }
        var d = part.GetComponent<LessonDevice>();
        if (d == null) { d = part.AddComponent<LessonDevice>(); d.Configure(mode,name,light); }
        return d;
    }
    public static LessonDeviceRegistry Create()
    {
        var r = new LessonDeviceRegistry();
        r.devices.Add("living", UnityEngine.Object.FindAnyObjectByType<RoomLightSwitch>() ?? throw new InvalidOperationException("Missing hallway light switch."));
        r.devices.Add("kitchenTap", Required("Faucet_Interactive").GetComponent<Faucet>());
        foreach (var pair in new[] { ("cupboard","Schrank_Cabinet/Hinge_der_Unterschrank_Left"), ("drawer","Schrank_Cabinet/Schublade_Drawer"),
            ("fridge","Kitchen_Stage2/Hinge_der_Kuehlschrank"), ("washer","HouseAccessories/Bath_Washer/WasherDoorHinge"), ("detergent","HouseAccessories/Bath_Washer/DetergentDrawer"), ("shoe","LivingHallFurnishings/Hall_ShoeCabinet/ShoeCabinetHinge") })
            r.devices.Add(pair.Item1, Required(pair.Item2).GetComponent<Openable>());
        var hoodRoot=Required("Dunstabzug_Hood"); var hoodPart=hoodRoot.GetComponentInChildren<Collider>().gameObject;
        var hood=Add(hoodPart,LessonDevice.DeviceMode.Toggle,"die Dunstabzugshaube");
        if (hood.transform.Find("LessonDeviceEffect") == null)
        { var c=hoodRoot.GetComponentInChildren<Collider>().bounds; hood.AddFlow(c.center+Vector3.down*.05f,c.center+Vector3.down*.30f,true); }
        r.devices.Add("hood",hood);
        var basin=Add(Required("BathroomFurnishings/Bath_BasinTap/TapLever"),LessonDevice.DeviceMode.Toggle,"die Armatur");
        if (basin.transform.Find("LessonDeviceEffect") == null) basin.AddFlow(new Vector3(6.15f,1.12f,-11.49f),new Vector3(6.15f,.88f,-11.49f));
        r.devices.Add("basin",basin);
        var showerRoot=Required("Bath_Shower"); var shower=Add(showerRoot.GetComponentInChildren<Collider>().gameObject,LessonDevice.DeviceMode.Toggle,"die Dusche");
        if (shower.transform.Find("LessonDeviceEffect") == null) shower.AddFlow(new Vector3(8.65f,2.02f,-15.47f),new Vector3(8.65f,.13f,-15.47f));
        r.devices.Add("shower",shower);
        var lampRoot=Required("Bedroom1_NightLamp");r.devices.Add("lamp",Add(lampRoot.transform.Find("LampBase").gameObject,LessonDevice.DeviceMode.Toggle,"die Nachttischlampe",lampRoot.GetComponentInChildren<Light>()));
        var deskRoot=Required("Bedroom2_DeskLamp");
        r.devices.Add("desk",Add(deskRoot.transform.Find("LampBase").gameObject,LessonDevice.DeviceMode.Toggle,"die Schreibtischlampe",deskRoot.GetComponentInChildren<Light>()));
        var flush=Add(Required("BathroomFurnishings/Bath_Cistern/FlushButton"),LessonDevice.DeviceMode.Flush,"der Spülkasten");
        if (flush.transform.Find("LessonDeviceEffect") == null) flush.AddFlow(new Vector3(4.95f,.48f,-15.25f),new Vector3(4.95f,.35f,-15.2f));
        r.devices.Add("flush",flush);
        r.devices.Add("lid",Add(Required("BathroomFurnishings/Bath_Toilet/ToiletLid"),LessonDevice.DeviceMode.Lid,"die Toilette"));
        foreach (string key in r.Keys) if (r.Get(key)==null) throw new InvalidOperationException("Missing action owner " + key);
        return r;
    }
}
