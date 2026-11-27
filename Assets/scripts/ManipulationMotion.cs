// Shared pickup/place timing; independent of any character model.
public static class ManipulationMotion
{
    public const float PickupSeconds=.28f, PlaceSeconds=.24f;
    public static float Ease(float time)
    { float t=System.Math.Max(0,System.Math.Min(1,time));return t*t*(3-2*t); }
    public static float Lift(float time,float height)
    {float t=System.Math.Max(0,System.Math.Min(1,time));return 16*t*t*(1-t)*(1-t)*height;}
}
