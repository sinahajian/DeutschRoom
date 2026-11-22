using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LessonCaseCatalog
{
    public int version;
    public Case[] cases;
    [Serializable] public sealed class Case { public string id,title,source; public string[] pages; }
    public void Validate()
    {
        if(version!=1||cases==null||cases.Length!=4)throw new InvalidOperationException("Expected case packets E, M, W and H.");
        var ids=new HashSet<string>();
        foreach(var c in cases)
            if(c==null||!ids.Add(c.id)||(c.id!="E"&&c.id!="M"&&c.id!="W"&&c.id!="H")||string.IsNullOrWhiteSpace(c.title)||c.pages==null||c.pages.Length==0||Array.Exists(c.pages,string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Invalid case source.");
    }
    public Case Get(string id) => Array.Find(cases,c=>c.id==id) ?? throw new InvalidOperationException("Missing case " + id);
    public static LessonCaseCatalog Load()
    {
        var asset=Resources.Load<TextAsset>("Lessons/CaseStudies");
        if(asset==null)throw new InvalidOperationException("Missing case sources.");
        var catalog=JsonUtility.FromJson<LessonCaseCatalog>(asset.text);catalog.Validate();return catalog;
    }
}
