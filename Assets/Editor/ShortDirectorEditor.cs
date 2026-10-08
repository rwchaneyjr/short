using GiantBugShort;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShortDirector))]
public sealed class ShortDirectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var director=(ShortDirector)target;
        EditorGUILayout.HelpBox("Automatic short: approach → grow → react → chase. Use Milestone to test one section. Animation states live in the assigned Animator controllers.",MessageType.Info);
        if(Application.isPlaying)
        {
            EditorGUILayout.LabelField("Now playing",director.Phase+" — "+director.Elapsed.ToString("F1")+" s");
            if(GUILayout.Button("Restart Short")) director.RestartSequence();
            Repaint();
        }
        DrawDefaultInspector();
        if(GUILayout.Button("Show Sequence Script")) EditorGUIUtility.PingObject(MonoScript.FromMonoBehaviour(director));
        if(director.characterAnimator && GUILayout.Button("Show Character Animator Controller"))
            Selection.activeObject=director.characterAnimator.runtimeAnimatorController;
    }
}
