public static class GermanLessonSpeech
{
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
