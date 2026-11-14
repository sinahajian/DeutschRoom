using UnityEngine;

[System.Flags]
public enum PlayerAction
{
    None = 0,
    Interact = 1,
    Place = 2,
    Rotate = 4
}

public struct TouchControlLayout
{
    public Rect SafeArea;
    public Vector2 JoystickCenter;
    public float JoystickRadius;
    public Rect InteractButton;
    public Rect PlaceButton;
    public Rect RotateButton;
}

// Each finger keeps its role until release, so a thumb cannot turn into a look gesture
// or press a button just by sliding across it.
public sealed class TouchInputState
{
    private int? moveFinger;
    private int? lookFinger;
    private Vector2 moveCenter;
    private Vector2 previousLook;
    private float moveRadius;

    public Vector2 Movement { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public PlayerAction Actions { get; private set; }

    public void BeginFrame()
    {
        LookDelta = Vector2.zero;
        Actions = PlayerAction.None;
    }

    public void Press(int finger, Vector2 position, TouchControlLayout layout, bool canInteract, bool isHolding)
    {
        if (moveFinger == finger || lookFinger == finger || !layout.SafeArea.Contains(position))
            return;
        if (layout.InteractButton.Contains(position))
        {
            if (canInteract) Actions |= PlayerAction.Interact;
            return;
        }
        if (layout.PlaceButton.Contains(position))
        {
            if (isHolding) Actions |= PlayerAction.Place;
            return;
        }
        if (layout.RotateButton.Contains(position))
        {
            if (isHolding) Actions |= PlayerAction.Rotate;
            return;
        }
        if (!moveFinger.HasValue && Vector2.Distance(position, layout.JoystickCenter) <= layout.JoystickRadius * 1.4f)
        {
            moveFinger = finger;
            moveCenter = layout.JoystickCenter;
            moveRadius = Mathf.Max(1f, layout.JoystickRadius);
            Drag(finger, position);
        }
        else if (!lookFinger.HasValue && position.x >= layout.SafeArea.center.x)
        {
            lookFinger = finger;
            previousLook = position;
        }
    }

    public void Drag(int finger, Vector2 position)
    {
        if (moveFinger == finger)
        {
            Vector2 offset = (position - moveCenter) / moveRadius;
            float strength = Mathf.Clamp01((offset.magnitude - .12f) / .88f);
            Movement = offset.normalized * strength;
        }
        if (lookFinger == finger)
        {
            LookDelta += position - previousLook;
            previousLook = position;
        }
    }

    public void Release(int finger)
    {
        if (moveFinger == finger)
        {
            moveFinger = null;
            Movement = Vector2.zero;
        }
        if (lookFinger == finger)
            lookFinger = null;
    }

    public void Reset()
    {
        moveFinger = null;
        lookFinger = null;
        Movement = Vector2.zero;
        BeginFrame();
    }
}
