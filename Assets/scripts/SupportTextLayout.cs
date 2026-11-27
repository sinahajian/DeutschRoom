using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Prepare visual lines once, then use private-use font aliases so native bidi
// cannot reorder the already arranged Arabic glyphs. Catalog text stays logical.
public static class SupportTextLayout
{
    private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
    {
        { '\u0621', new[] { '\ufe80', '\u0621', '\u0621', '\u0621' } },
        { '\u0622', new[] { '\ufe81', '\ufe82', '\u0622', '\u0622' } },
        { '\u0623', new[] { '\ufe83', '\ufe84', '\u0623', '\u0623' } },
        { '\u0624', new[] { '\ufe85', '\ufe86', '\u0624', '\u0624' } },
        { '\u0625', new[] { '\ufe87', '\ufe88', '\u0625', '\u0625' } },
        { '\u0626', new[] { '\ufe89', '\ufe8a', '\ufe8b', '\ufe8c' } },
        { '\u0627', new[] { '\ufe8d', '\ufe8e', '\u0627', '\u0627' } },
        { '\u0628', new[] { '\ufe8f', '\ufe90', '\ufe91', '\ufe92' } },
        { '\u0629', new[] { '\ufe93', '\ufe94', '\u0629', '\u0629' } },
        { '\u062a', new[] { '\ufe95', '\ufe96', '\ufe97', '\ufe98' } },
        { '\u062b', new[] { '\ufe99', '\ufe9a', '\ufe9b', '\ufe9c' } },
        { '\u062c', new[] { '\ufe9d', '\ufe9e', '\ufe9f', '\ufea0' } },
        { '\u062d', new[] { '\ufea1', '\ufea2', '\ufea3', '\ufea4' } },
        { '\u062e', new[] { '\ufea5', '\ufea6', '\ufea7', '\ufea8' } },
        { '\u062f', new[] { '\ufea9', '\ufeaa', '\u062f', '\u062f' } },
        { '\u0630', new[] { '\ufeab', '\ufeac', '\u0630', '\u0630' } },
        { '\u0631', new[] { '\ufead', '\ufeae', '\u0631', '\u0631' } },
        { '\u0632', new[] { '\ufeaf', '\ufeb0', '\u0632', '\u0632' } },
        { '\u0633', new[] { '\ufeb1', '\ufeb2', '\ufeb3', '\ufeb4' } },
        { '\u0634', new[] { '\ufeb5', '\ufeb6', '\ufeb7', '\ufeb8' } },
        { '\u0635', new[] { '\ufeb9', '\ufeba', '\ufebb', '\ufebc' } },
        { '\u0636', new[] { '\ufebd', '\ufebe', '\ufebf', '\ufec0' } },
        { '\u0637', new[] { '\ufec1', '\ufec2', '\ufec3', '\ufec4' } },
        { '\u0638', new[] { '\ufec5', '\ufec6', '\ufec7', '\ufec8' } },
        { '\u0639', new[] { '\ufec9', '\ufeca', '\ufecb', '\ufecc' } },
        { '\u063a', new[] { '\ufecd', '\ufece', '\ufecf', '\ufed0' } },
        { '\u0641', new[] { '\ufed1', '\ufed2', '\ufed3', '\ufed4' } },
        { '\u0642', new[] { '\ufed5', '\ufed6', '\ufed7', '\ufed8' } },
        { '\u0643', new[] { '\ufed9', '\ufeda', '\ufedb', '\ufedc' } },
        { '\u0644', new[] { '\ufedd', '\ufede', '\ufedf', '\ufee0' } },
        { '\u0645', new[] { '\ufee1', '\ufee2', '\ufee3', '\ufee4' } },
        { '\u0646', new[] { '\ufee5', '\ufee6', '\ufee7', '\ufee8' } },
        { '\u0647', new[] { '\ufee9', '\ufeea', '\ufeeb', '\ufeec' } },
        { '\u0648', new[] { '\ufeed', '\ufeee', '\u0648', '\u0648' } },
        { '\u0649', new[] { '\ufeef', '\ufef0', '\ufbe8', '\ufbe9' } },
        { '\u064a', new[] { '\ufef1', '\ufef2', '\ufef3', '\ufef4' } },
        { '\u0671', new[] { '\ufb50', '\ufb51', '\u0671', '\u0671' } },
        { '\u0677', new[] { '\ufbdd', '\u0677', '\u0677', '\u0677' } },
        { '\u0679', new[] { '\ufb66', '\ufb67', '\ufb68', '\ufb69' } },
        { '\u067a', new[] { '\ufb5e', '\ufb5f', '\ufb60', '\ufb61' } },
        { '\u067b', new[] { '\ufb52', '\ufb53', '\ufb54', '\ufb55' } },
        { '\u067e', new[] { '\ufb56', '\ufb57', '\ufb58', '\ufb59' } },
        { '\u067f', new[] { '\ufb62', '\ufb63', '\ufb64', '\ufb65' } },
        { '\u0680', new[] { '\ufb5a', '\ufb5b', '\ufb5c', '\ufb5d' } },
        { '\u0683', new[] { '\ufb76', '\ufb77', '\ufb78', '\ufb79' } },
        { '\u0684', new[] { '\ufb72', '\ufb73', '\ufb74', '\ufb75' } },
        { '\u0686', new[] { '\ufb7a', '\ufb7b', '\ufb7c', '\ufb7d' } },
        { '\u0687', new[] { '\ufb7e', '\ufb7f', '\ufb80', '\ufb81' } },
        { '\u0688', new[] { '\ufb88', '\ufb89', '\u0688', '\u0688' } },
        { '\u068c', new[] { '\ufb84', '\ufb85', '\u068c', '\u068c' } },
        { '\u068d', new[] { '\ufb82', '\ufb83', '\u068d', '\u068d' } },
        { '\u068e', new[] { '\ufb86', '\ufb87', '\u068e', '\u068e' } },
        { '\u0691', new[] { '\ufb8c', '\ufb8d', '\u0691', '\u0691' } },
        { '\u0698', new[] { '\ufb8a', '\ufb8b', '\u0698', '\u0698' } },
        { '\u06a4', new[] { '\ufb6a', '\ufb6b', '\ufb6c', '\ufb6d' } },
        { '\u06a6', new[] { '\ufb6e', '\ufb6f', '\ufb70', '\ufb71' } },
        { '\u06a9', new[] { '\ufb8e', '\ufb8f', '\ufb90', '\ufb91' } },
        { '\u06ad', new[] { '\ufbd3', '\ufbd4', '\ufbd5', '\ufbd6' } },
        { '\u06af', new[] { '\ufb92', '\ufb93', '\ufb94', '\ufb95' } },
        { '\u06b1', new[] { '\ufb9a', '\ufb9b', '\ufb9c', '\ufb9d' } },
        { '\u06b3', new[] { '\ufb96', '\ufb97', '\ufb98', '\ufb99' } },
        { '\u06ba', new[] { '\ufb9e', '\ufb9f', '\u06ba', '\u06ba' } },
        { '\u06bb', new[] { '\ufba0', '\ufba1', '\ufba2', '\ufba3' } },
        { '\u06be', new[] { '\ufbaa', '\ufbab', '\ufbac', '\ufbad' } },
        { '\u06c0', new[] { '\ufba4', '\ufba5', '\u06c0', '\u06c0' } },
        { '\u06c1', new[] { '\ufba6', '\ufba7', '\ufba8', '\ufba9' } },
        { '\u06c5', new[] { '\ufbe0', '\ufbe1', '\u06c5', '\u06c5' } },
        { '\u06c6', new[] { '\ufbd9', '\ufbda', '\u06c6', '\u06c6' } },
        { '\u06c7', new[] { '\ufbd7', '\ufbd8', '\u06c7', '\u06c7' } },
        { '\u06c8', new[] { '\ufbdb', '\ufbdc', '\u06c8', '\u06c8' } },
        { '\u06c9', new[] { '\ufbe2', '\ufbe3', '\u06c9', '\u06c9' } },
        { '\u06cb', new[] { '\ufbde', '\ufbdf', '\u06cb', '\u06cb' } },
        { '\u06cc', new[] { '\ufbfc', '\ufbfd', '\ufbfe', '\ufbff' } },
        { '\u06d0', new[] { '\ufbe4', '\ufbe5', '\ufbe6', '\ufbe7' } },
        { '\u06d2', new[] { '\ufbae', '\ufbaf', '\u06d2', '\u06d2' } },
        { '\u06d3', new[] { '\ufbb0', '\ufbb1', '\u06d3', '\u06d3' } },
    };
    private static readonly Dictionary<string,string> Cache = new Dictionary<string,string>();
    public static string Wrap(string text, GUIStyle style, float width, bool rtl)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (rtl) text=RemoveDirectionControls(text);
        width=Math.Max(16,width);
        string key=(style.font == null ? "default" : style.font.name)+":"+style.fontSize+":"+(int)width+":"+rtl+":"+text;
        if (Cache.TryGetValue(key,out string cached)) return cached;
        var result=new StringBuilder();
        foreach (string paragraph in text.Replace("\r","").Split('\n'))
        {
            string line="";
            foreach (string word in paragraph.Split(' '))
            {
                string next=line.Length==0?word:line+" "+word;
                if (line.Length>0 && style.CalcSize(new GUIContent(rtl?DisplayLine(next):next)).x>width)
                { Append(line); line=word; }
                else line=next;
            }
            Append(line);
        }
        if (result.Length>0) result.Length--;
        string value=result.ToString();
        if (Cache.Count>=192) Cache.Clear();
        Cache[key]=value;
        return value;
        void Append(string line) { result.Append(rtl?DisplayLine(line):line);result.Append('\n'); }
    }
    // These offsets are shared with Tools/Localization/build_font.py. Glyphs are
    // identical; only their Unicode bidi class changes to L at the drawing boundary.
    public static char DisplayGlyph(char value) => value>='\u0600' && value<='\u08ff'
        ? (char)('\ue000'+value-'\u0600')
        : value>='\ufb50' && value<='\ufeff' ? (char)('\ue400'+value-'\ufb50') : value;
    public static string DisplayLine(string logicalLine)
    {
        char[] visual=ShapeLine(RemoveDirectionControls(logicalLine)).ToCharArray();
        for(int i=0;i<visual.Length;i++) visual[i]=DisplayGlyph(visual[i]);
        return new string(visual);
    }
    private static string RemoveDirectionControls(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var clean=new StringBuilder(text.Length);
        foreach(char c in text)
            if(c!='\u061c' && c!='\u200e' && c!='\u200f' &&
                !(c>='\u202a' && c<='\u202e') && !(c>='\u2066' && c<='\u2069')) clean.Append(c);
        return clean.ToString();
    }
    private static bool Mark(char c) => CharUnicodeInfo.GetUnicodeCategory(c)==UnicodeCategory.NonSpacingMark;
    private static int Neighbour(string text,int index,int step)
    {
        index+=step;
        while(index>=0 && index<text.Length && Mark(text[index])) index+=step;
        return index;
    }
    public static string ShapeLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        bool hasArabic=false;
        foreach(char c in text) if (Forms.ContainsKey(c)) {hasArabic=true;break;}
        if (!hasArabic) return text;
        char[] shaped=text.ToCharArray();
        for(int i=0;i<text.Length;i++)
        {
            if(!Forms.TryGetValue(text[i],out var form)) continue;
            int previous=Neighbour(text,i,-1),next=Neighbour(text,i,1);
            bool joinsPrevious=previous>=0 && Forms.TryGetValue(text[previous],out var before) && before[2]!=text[previous] && form[1]!=text[i];
            bool joinsNext=next<text.Length && Forms.TryGetValue(text[next],out var after) && form[2]!=text[i] && after[1]!=text[next];
            shaped[i]=form[joinsPrevious?(joinsNext?3:1):(joinsNext?2:0)];
        }
        var clusters=new List<string>();
        var iterator=StringInfo.GetTextElementEnumerator(new string(shaped));
        while(iterator.MoveNext()) clusters.Add(iterator.GetTextElement());
        // Resolve neutral characters using their neighbours, then reverse the RTL
        // line and restore contiguous LTR runs (e.g. Shift + F and 0.65).
        var leftToRight=new bool[clusters.Count];
        for(int i=0;i<clusters.Count;i++)
        {
            char c=clusters[i][0];
            if (IsLatinOrNumber(c)) {leftToRight[i]=true;continue;}
            if (IsArabic(c) && !NumericSeparator(c)) continue;
            int previous=i-1,next=i+1;
            while(previous>=0 && !IsStrong(clusters[previous][0])) previous--;
            while(next<clusters.Count && !IsStrong(clusters[next][0])) next++;
            leftToRight[i]=previous>=0 && next<clusters.Count && IsLatinOrNumber(clusters[previous][0]) && IsLatinOrNumber(clusters[next][0]);
        }
        clusters.Reverse(); Array.Reverse(leftToRight);
        for(int i=0;i<clusters.Count;i++)
        {
            if(!leftToRight[i]) {clusters[i]=Mirror(clusters[i]);continue;}
            int start=i;
            while(i+1<clusters.Count && leftToRight[i+1]) i++;
            clusters.Reverse(start,i-start+1);
        }
        return string.Concat(clusters);
    }
    private static bool IsArabic(char c) => c>='\u0600' && c<='\u08ff' || c>='\ufb50' && c<='\ufeff';
    private static bool IsLatinOrNumber(char c) => char.IsDigit(c) || char.IsLetter(c) && !IsArabic(c);
    private static bool NumericSeparator(char c) => c=='\u066b' || c=='\u066c';
    private static bool IsStrong(char c) => IsArabic(c) && !NumericSeparator(c) || IsLatinOrNumber(c);
    private static string Mirror(string s) => s=="("?")":s==")"?"(":s=="["?"]":s=="]"?"[":s=="“"?"”":s=="”"?"“":s=="«"?"»":s=="»"?"«":s;
}
