using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GiantBugShort;

// Local editor workbench. Commands are explicit, project-local, and consumed once.
[InitializeOnLoad]
public static class ShortWorkbench
{
    const string Request = "Temp/ShortWorkbench.request";
    const string Report = "Temp/ShortWorkbench.report";
    static ShortWorkbench() { EditorApplication.update += Poll; EditorApplication.update += MonitorTest; Application.logMessageReceived += Log; }
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        string command = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try
        {
            if (command == "inspect") Inspect();
            else if (command == "build") GiantBugSceneBuilder.Build();
            else if (command == "capture") Capture("scene");
            else if (command == "normalize") GiantBugSceneBuilder.NormalizeModels();
            else if (command == "wire") GiantBugSceneBuilder.Wire();
            else if (command == "scripts")
            {
                Selection.activeObject=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/GiantBugShort/ShortDirector.cs");
                EditorGUIUtility.PingObject(Selection.activeObject);
                EditorUtility.FocusProjectWindow();
            }
            else if (command.StartsWith("test-")) BeginTest(command.Substring(5));
            else File.WriteAllText(Report, "Unknown command: " + command);
        }
        catch (Exception ex) { File.WriteAllText(Report, ex.ToString()); Debug.LogException(ex); }
    }
    static void Log(string message,string stack,LogType type)
    {
        if(SessionState.GetBool("ShortTest",false) && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))
            SessionState.SetString("ShortTestErrors",SessionState.GetString("ShortTestErrors","")+message+"\n");
    }
    static void BeginTest(string mode)
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before testing.");
        var d=UnityEngine.Object.FindObjectOfType<ShortDirector>();
        d.milestone=mode=="approach"?ShortMilestone.Approach:mode=="growth"?ShortMilestone.Growth:ShortMilestone.FullSequence;
        SessionState.SetString("ShortTestMode",mode);
        SessionState.SetString("ShortTestErrors",""); SessionState.SetString("ShortTestTrace","");
        SessionState.SetString("ShortTestPhase",""); SessionState.SetInt("ShortTestRuns",0);
        SessionState.SetBool("ShortTest",true);
        SessionState.SetFloat("ShortTestStart",(float)EditorApplication.timeSinceStartup);
        EditorApplication.isPlaying=true;
    }
    static void MonitorTest()
    {
        if(!SessionState.GetBool("ShortTest",false)) return;
        if(EditorApplication.timeSinceStartup-SessionState.GetFloat("ShortTestStart",0)>100)
        { FinishTest("FAIL: test timed out"); return; }
        if(!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        var d=UnityEngine.Object.FindObjectOfType<ShortDirector>(); if(!d || d.Phase==ShortPhase.Ready) return;
        string key=d.Phase.ToString();
        int run=SessionState.GetInt("ShortTestRuns",0);
        if(key!=SessionState.GetString("ShortTestPhase","") && (d.PhaseTime>.15f || d.Phase==ShortPhase.Complete))
        {
            SessionState.SetString("ShortTestPhase",key);
            SessionState.SetString("ShortTestTrace",SessionState.GetString("ShortTestTrace","")+"Run "+run+" "+key+" at "+d.Elapsed.ToString("F2")+"s\n");
            if(run==0) Capture(SessionState.GetString("ShortTestMode","")+"-"+key);
        }
        if(d.Phase!=ShortPhase.Complete) return;
        float multiplier=d.milestone==ShortMilestone.Approach?1:Mathf.Max(1,d.giantMultiplier);
        bool scaleOk=(d.bug.localScale-d.OriginalBugScale*multiplier).sqrMagnitude<.000001f;
        Vector3 expected=d.milestone==ShortMilestone.FullSequence?d.escapePoint.position:d.approachPoint.position;
        bool positionOk=(d.character.position-expected).sqrMagnitude<.000001f;
        if(!scaleOk || !positionOk) { FinishTest("FAIL: scale="+scaleOk+" position="+positionOk); return; }
        if(run==0)
        {
            SessionState.SetInt("ShortTestRuns",1);
            d.RestartSequence();
            if((d.bug.localScale-d.OriginalBugScale).sqrMagnitude>.000001f) { FinishTest("FAIL: reset scale"); return; }
            SessionState.SetString("ShortTestPhase","");
            return;
        }
        FinishTest("PASS: two consecutive runs, exact stop position, expected final scale, restart reset.\nFinal duration: "+d.Elapsed.ToString("F2")+"s");
    }
    static void FinishTest(string result)
    {
        string errors=SessionState.GetString("ShortTestErrors","");
        File.WriteAllText(Report,result+"\nErrors: "+(errors.Length==0?"none":errors)+"\n"+SessionState.GetString("ShortTestTrace",""));
        Directory.CreateDirectory("Validation");
        File.Copy(Report,"Validation/"+SessionState.GetString("ShortTestMode","")+".txt",true);
        SessionState.SetBool("ShortTest",false);
        EditorApplication.isPlaying=false;
    }
    public static void Capture(string name)
    {
        Directory.CreateDirectory("Temp/ShortCaptures");
        var camera = Camera.main;
        var previous = camera.targetTexture;
        var active = RenderTexture.active;
        var rt = new RenderTexture(540, 960, 24);
        var texture = new Texture2D(540, 960, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Temp/ShortCaptures/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(texture);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
    static void Inspect()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Active scene: " + EditorSceneManager.GetActiveScene().path);
        sb.AppendLine("Dirty: " + EditorSceneManager.GetActiveScene().isDirty);
        foreach (string name in new[] { "man", "bug" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/" + name + ".prefab");
            var go = UnityEngine.Object.Instantiate(prefab);
            try
            {
                sb.AppendLine("MODEL " + name);
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    sb.AppendLine("  " + t.name + " pos=" + t.localPosition + " rot=" + t.localEulerAngles);
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    sb.AppendLine("RENDERER " + r.name + " bounds=" + r.bounds + " shader=" + r.sharedMaterial.shader.name);
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/" + name + ".fbx").OfType<AnimationClip>())
                    sb.AppendLine("CLIP " + clip.name + " duration=" + clip.length);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        File.WriteAllText(Report, sb.ToString());
    }
}
