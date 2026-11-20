using UnityEngine;

// One vertical swipe, followed by a stationary hold, changes one posture only.
// Releasing and swiping again is required before the next change.
public sealed class PostureGesture
{
    private Vector2 origin, holdPosition;
    private float holdSince;
    private int direction;
    private bool active, consumed;
    public void Begin(Vector2 position) { origin = holdPosition = position; direction = 0; active = true; consumed = false; }
    public int Sample(Vector2 position, float now, float threshold)
    {
        if (!active || consumed) return 0;
        Vector2 delta = position - origin;
        if (direction == 0)
        {
            if (Mathf.Abs(delta.y) < threshold || Mathf.Abs(delta.y) < Mathf.Abs(delta.x) * 1.5f) return 0;
            direction = delta.y < 0 ? -1 : 1;
            holdPosition = position;
            holdSince = now;
        }
        if (Vector2.Distance(position, holdPosition) > threshold * .18f)
        { holdPosition = position; holdSince = now; }
        if (now - holdSince < .6f) return 0;
        consumed = true;
        return direction;
    }
    public void Reset() { active = false; consumed = false; direction = 0; }
}
