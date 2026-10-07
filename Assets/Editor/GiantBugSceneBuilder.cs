using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GiantBugShort;

public static class GiantBugSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/GiantBugShort.unity";
    [MenuItem("Giant Bug Short/2 - Wire Sequence")]
    public static void Wire()
    {
        var host=GameObject.Find("ShortDirector");
        var director=host.GetComponent<ShortDirector>();
        if(!director) director=host.AddComponent<ShortDirector>();
        director.character=GameObject.Find("PlayerCharacter").transform;
        director.bug=GameObject.Find("Bug").transform;
        director.approachPoint=GameObject.Find("ApproachPoint").transform;
        director.escapePoint=GameObject.Find("EscapePoint").transform;
        director.characterAnimator=SetupAnimator(director.character.GetChild(0).gameObject,"man");
        director.bugAnimator=SetupAnimator(director.bug.GetChild(0).gameObject,"bug");
        Camera.main.transform.position=new Vector3(4,3.5f,-13);
        Camera.main.transform.LookAt(new Vector3(0,.8f,-4));
        Camera.main.aspect=9f/16;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        File.WriteAllText("Temp/ShortWorkbench.report","Sequence wired with imported animation clips.");
    }
    static Animator SetupAnimator(GameObject visual,string asset)
    {
        string path="Assets/GiantBugShort/"+asset+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)
        {
            var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Characters/"+asset+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            var clip=Object.Instantiate(source); clip.name=asset+"_Loop";
            var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true;
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,"Assets/GiantBugShort/"+asset+"_Loop.anim");
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm=controller.layers[0].stateMachine;
            foreach(var stateName in new[]{"Idle","Walk","Run","Move"})
            {
                var state=sm.AddState(stateName); state.motion=clip;
                state.speed=asset=="man"?(stateName=="Idle"?0:stateName=="Run"?2.2f:1):1;
                if(stateName=="Idle") sm.defaultState=state;
            }
        }
        var animator=visual.GetComponent<Animator>();
        if(!animator) animator=visual.AddComponent<Animator>();
        animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        return animator;
    }
    [MenuItem("Giant Bug Short/1 - Create Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive); return; }
        Directory.CreateDirectory("Assets/GiantBugShort");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var man = Model("man", "PlayerCharacter", 1.8f, new Vector3(0,0,-8));
        var bug = Model("bug", "Bug", .28f, Vector3.zero);
        bug.transform.rotation = Quaternion.Euler(0,180,0);
        Primitive("Ground", PrimitiveType.Plane, new Vector3(0,-.015f,-24), new Vector3(6,1,9), new Color(.20f,.27f,.31f));
        // A simple runway gives the chase a readable direction and visible motion.
        Primitive("Walkway", PrimitiveType.Cube, new Vector3(0,-.045f,-26), new Vector3(4,.08f,70), new Color(.47f,.51f,.48f));
        for (int i=0;i<15;i++)
            Primitive("Path seam " + i, PrimitiveType.Cube, new Vector3(0,.001f,6-i*5), new Vector3(4,.005f,.025f), new Color(.28f,.34f,.34f));
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity=1.25f; light.shadows=LightShadows.Soft;
        light.transform.rotation=Quaternion.Euler(40,-30,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.6f,.65f,.72f);
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag="MainCamera";
        camera.gameObject.AddComponent<AudioListener>();
        camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.075f,.12f,.17f);
        camera.fieldOfView=48; camera.nearClipPlane=.1f; camera.farClipPlane=150;
        camera.transform.position=new Vector3(8,6,-12);
        camera.transform.LookAt(new Vector3(0,.8f,-3.6f));
        camera.aspect=9f/16;
        new GameObject("ShortDirector");
        Marker("CharacterStart",man.transform.position);
        Marker("ApproachPoint",new Vector3(0,0,-2.7f));
        Marker("EscapePoint",new Vector3(0,0,-55));
        EditorSceneManager.SaveScene(scene,ScenePath);
        // Keep the source scene loaded but out of the cinematic camera/light/audio setup.
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var other=SceneManager.GetSceneAt(i);
            if(other!=scene && other.isLoaded && !other.isDirty) { EditorSceneManager.CloseScene(other,false); i--; }
        }
        Selection.activeGameObject=man;
        File.WriteAllText("Temp/ShortWorkbench.report","Step 1 complete: " + ScenePath + "\nSource prefabs preserved. Character height 1.8m; bug height 0.28m.");
        ShortWorkbench.Capture("step1");
    }
    static void Marker(string name, Vector3 position) { new GameObject(name).transform.position=position; }
    static GameObject Model(string asset, string name, float height, Vector3 position)
    {
        var root=new GameObject(name);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/"+asset+".prefab");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        visual.transform.SetParent(root.transform,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        Bounds bounds=VisualBounds(visual);
        visual.transform.localScale*=height/bounds.size.y;
        bounds=VisualBounds(visual);
        visual.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        root.transform.position=position;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen=true;
        return root;
    }
    public static Bounds VisualBounds(GameObject go)
    {
        bool first=true; var bounds=new Bounds();
        foreach(var renderer in go.GetComponentsInChildren<Renderer>())
        {
            if(first) { bounds=renderer.bounds; first=false; } else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
    public static void NormalizeModels()
    {
        foreach(var name in new[]{"PlayerCharacter","Bug"})
        {
            var root=GameObject.Find(name).transform;
            var visual=root.GetChild(0);
            var oldRotation=root.rotation; root.rotation=Quaternion.identity;
            visual.localPosition=Vector3.zero; visual.localScale=Vector3.one;
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen=false;
            var bounds=VisualBounds(visual.gameObject);
            visual.localScale*= (name=="Bug"?.28f:1.8f)/bounds.size.y;
            bounds=VisualBounds(visual.gameObject);
            visual.position+=new Vector3(root.position.x-bounds.center.x,root.position.y-bounds.min.y,root.position.z-bounds.center.z);
            root.rotation=oldRotation;
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        File.WriteAllText("Temp/ShortWorkbench.report","Models normalized using imported skin bounds.");
    }
    static void Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.position=position; go.transform.localScale=scale;
        string path="Assets/GiantBugShort/"+(name.StartsWith("Path seam")?"Seam":name)+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat) { mat=new Material(Shader.Find("Standard")); mat.color=color; mat.SetFloat("_Glossiness",.15f); AssetDatabase.CreateAsset(mat,path); }
        go.GetComponent<Renderer>().sharedMaterial=mat;
    }
}
