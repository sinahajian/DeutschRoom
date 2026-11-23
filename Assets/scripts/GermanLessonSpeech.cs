public static class GermanLessonSpeech
{
    public const string ReleaseBeforeNext = "Lege zuerst den Gegenstand ab. Danach kannst du den nächsten nehmen.";
    public const string WrongPickup = "Das ist noch nicht der gesuchte Gegenstand. Versuch es noch einmal.";
    public static string IntroComplete(int count) => "Geschafft! Du hast alle " + count + " Aufgaben geschafft. Du kannst jetzt frei weiterüben.";
    public const string IntroWelcome = "Willkommen! Starte den Lernpfad und entdecke die Gegenstände.";
    public const string ReviewHint = "Die Karte zeigt dir jetzt das gesuchte Ziel.";
    public const string ReleaseOptional = "Du kannst den getragenen Gegenstand jetzt ablegen.";
    public const string IdentifyHelp = "Feste Gegenstände bleiben an ihrem Platz. Schau das Ziel an und tippe auf Anhören oder drücke F.";
    public const string PickupHelp = "Gehe zum Gegenstand und schau ihn an. Tippe auf Aufheben oder drücke E.";
    public const string PickupHelpTouch = "Gehe zum Gegenstand und schau ihn an. Tippe auf Aufheben.";
    public const string PickupHelpKeyboard = "Gehe zum Gegenstand und schau ihn an. Drücke E zum Aufheben.";
    public const string IdentifyHelpTouch = "Feste Gegenstände bleiben an ihrem Platz. Schau das Ziel an und tippe auf Anhören.";
    public const string IdentifyHelpKeyboard = "Feste Gegenstände bleiben an ihrem Platz. Schau das Ziel an und drücke F.";
    public static string PickupGuidance(bool touch) => touch ? PickupHelpTouch : PickupHelpKeyboard;
    public static string IdentifyGuidance(bool touch) => touch ? IdentifyHelpTouch : IdentifyHelpKeyboard;
    public static bool StillRelevant(string line, bool holding, bool placing, bool touch)
    {
        if(line==ReleaseBeforeNext || line==ReleaseOptional)return holding && !placing;
        if(line==PickupHelpTouch || line==IdentifyHelpTouch)return touch;
        if(line==PickupHelpKeyboard || line==IdentifyHelpKeyboard)return !touch;
        return true;
    }
    public static string Stage(int number) => "Aufgabe " + number + ".";
    public static string Accusative(string name) => name.StartsWith("der ", System.StringComparison.Ordinal) ? "den " + name.Substring(4) : name;
}
