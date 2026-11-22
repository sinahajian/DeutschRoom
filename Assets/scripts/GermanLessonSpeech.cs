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
    public static string Stage(int number) => "Aufgabe " + number + ".";
    public static string Accusative(string name) => name.StartsWith("der ", System.StringComparison.Ordinal) ? "den " + name.Substring(4) : name;
    public static string Pronoun(string name) => name.StartsWith("der ", System.StringComparison.Ordinal) ? "ihn" : name.StartsWith("die ", System.StringComparison.Ordinal) ? "sie" : "es";
    public static string Find(string name) => "Finde als Nächstes " + Accusative(name) + ".";
    public static string Found(string name) => "Du hast " + Accusative(name) + " gefunden. Jetzt kannst du " + Pronoun(name) + " ablegen.";
    public static string FixedFound(string name) => "Richtig! Du hast " + Accusative(name) + " gefunden.";
    public static string Placed(string name) => "Du hast " + Accusative(name) + " auf den Tisch gelegt.";
    public static string Remove(string name) => "Nimm " + Accusative(name) + " vom Tisch und lege " + Pronoun(name) + " woanders ab.";
    public static string Count(int count) => count == 1 ? "Auf dem Tisch liegt jetzt ein passender Gegenstand." :
        "Auf dem Tisch liegen jetzt " + count + " passende Gegenstände.";
    public const string ClearTable = "Bevor wir anfangen, räume den Tisch leer.";
    public const string PutElsewhere = "Lege den Gegenstand außerhalb des Tisches ab.";
    public const string TableReady = "Der Tisch ist jetzt leer. Los geht's!";
    public const string Finished = "Gut gemacht! Diese Aufgabe ist geschafft.";
}
