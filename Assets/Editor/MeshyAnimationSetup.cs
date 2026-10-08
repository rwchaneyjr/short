using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MeshyAnimationSetup
{
    [MenuItem("Giant Bug Short/Connect Meshy Animations")]
    public static void Connect()
    {
        var paths=AssetDatabase.FindAssets("t:Model",new[]{"Assets/Characters"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        string runPath=paths.First(p=>p.Contains("Animation_Running"));
        foreach(string targetName in new[]{"man","bug"})
        {
            var targetModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+targetName+".fbx");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Characters/"+targetName+".controller");
            if(!controller) throw new System.InvalidOperationException("Missing controller for "+targetName);
            var machine=controller.layers[0].stateMachine;
            foreach(string stateName in new[]{"Idle","Walk","Run","Move"})
            {
                string sourcePath=stateName=="Idle"?"Assets/Characters/bug.fbx":stateName=="Walk"?"Assets/Characters/man.fbx":runPath;
                var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                var source=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).First();
                string clipPath="Assets/GiantBugShort/"+targetName+"_"+(stateName=="Move"?"Run":stateName)+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if(!clip)
                {
                    clip=Object.Instantiate(source); clip.name=targetName+"_"+(stateName=="Move"?"Run":stateName);
                    // Meshy exports share bone paths but not bone lengths. Keep each
                    // destination skeleton's proportions while transferring rotations.
                    var sourceHips=sourceModel.transform.Find("target_character/mixamorig:Hips");
                    var targetHips=targetModel.transform.Find("target_character/mixamorig:Hips");
                    float ratio=targetHips.localPosition.z/sourceHips.localPosition.z;
                    foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        var destination=targetModel.transform.Find(binding.path);
                        var origin=sourceModel.transform.Find(binding.path);
                        if(!destination || !origin) throw new System.InvalidOperationException("Unmatched bone: "+binding.path);
                        if(binding.propertyName.StartsWith("m_LocalPosition."))
                        {
                            int axis=binding.propertyName.EndsWith("x")?0:binding.propertyName.EndsWith("y")?1:2;
                            AnimationCurve curve;
                            if(destination==targetHips)
                            {
                                curve=AnimationUtility.GetEditorCurve(clip,binding);
                                var keys=curve.keys;
                                for(int i=0;i<keys.Length;i++)
                                {
                                    keys[i].value=destination.localPosition[axis]+(keys[i].value-origin.localPosition[axis])*ratio;
                                    keys[i].inTangent*=ratio; keys[i].outTangent*=ratio;
                                }
                                curve.keys=keys;
                            }
                            else curve=AnimationCurve.Constant(0,source.length,destination.localPosition[axis]);
                            AnimationUtility.SetEditorCurve(clip,binding,curve);
                        }
                        else if(binding.propertyName.StartsWith("m_LocalScale."))
                        {
                            int axis=binding.propertyName.EndsWith("x")?0:binding.propertyName.EndsWith("y")?1:2;
                            AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,source.length,destination.localScale[axis]));
                        }
                    }
                    var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true;
                    AnimationUtility.SetAnimationClipSettings(clip,settings);
                    AssetDatabase.CreateAsset(clip,clipPath);
                }
                var state=machine.states.Select(c=>c.state).FirstOrDefault(s=>s.name==stateName);
                if(!state) state=machine.AddState(stateName);
                state.motion=clip; state.speed=targetName=="bug" && (stateName=="Run" || stateName=="Move")?.7f:1;
                if(stateName=="Idle") machine.defaultState=state;
            }
            EditorUtility.SetDirty(controller);
        }
        AssetDatabase.SaveAssets();
        GiantBugSceneBuilder.FullSetup();
        File.WriteAllText("Temp/ShortWorkbench.report","Connected Meshy Idle, Walk, and full Running clips to both rigs; original imports preserved. Full scene saved.");
    }
    public static void Audit()
    {
        var report=new StringBuilder();
        foreach(string path in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Characters"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!model) continue;
            report.AppendLine("MODEL "+path);
            foreach(var t in model.GetComponentsInChildren<Transform>())
                if(t.name.Contains("Hips") || t.name.Contains("Leg") || t.name.Contains("Spine") || t.name=="target_character")
                    report.AppendLine(AnimationUtility.CalculateTransformPath(t,model.transform)+" pos="+t.localPosition.ToString("F6")+" scale="+t.localScale.ToString("F6"));
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
            {
                report.AppendLine("CLIP "+clip.name+" length="+clip.length);
                var bindings=AnimationUtility.GetCurveBindings(clip);
                report.AppendLine("CURVES "+bindings.Length);
                foreach(var binding in bindings.Take(16)) report.AppendLine(binding.path+" : "+binding.propertyName);
            }
        }
        File.WriteAllText("Temp/ShortWorkbench.report",report.ToString());
    }
}
