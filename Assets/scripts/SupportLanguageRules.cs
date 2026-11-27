using System;

// Completion counts, not CUR identifiers: retired task IDs do not shift checkpoints.
public static class SupportLanguageRules
{
    public static int Checkpoint(int completed) => Math.Max(0, completed) / 50;
    public static bool ShouldAsk(int completed, int total, int acknowledged) =>
        completed > 0 && completed < total && Checkpoint(completed) > acknowledged;
}


// Reading time is separate from lesson progress and pauses during reading/other UI.
public static class TaskNoticeTiming
{
    public static float Duration(int characters) => Math.Max(9,Math.Min(30,Math.Max(0,characters)/22f));
    public static float Advance(float elapsed,float seconds,bool blocked) => blocked?elapsed:elapsed+Math.Max(0,seconds);
}
