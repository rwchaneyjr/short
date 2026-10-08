using UnityEngine;

namespace GiantBugShort
{
    [RequireComponent(typeof(Camera))]
    public sealed class PortraitCamera : MonoBehaviour
    {
        public ShortDirector director;
        [Tooltip("Direction from the subjects toward the camera.")]
        public Vector3 approachOffset = new Vector3(.28f,.22f,-1);
        public Vector3 growthOffset = new Vector3(.48f,.2f,-1);
        public Vector3 chaseOffset = new Vector3(.22f,.16f,-1);
        [Min(1)] public float framingMargin = 1.3f;
        [Min(1)] public float minimumDistance = 5;
        [Min(.1f)] public float blendSpeed = 4;
        public bool portraitLetterbox = true;
        public bool growthShake = true;
        [Range(0,.3f)] public float shakeStrength = .07f;
        Camera view;
        Renderer[] subjects;
        Vector3 smoothedCenter, smoothedDirection;
        float distance;
        bool ready;
        void Awake() { view=GetComponent<Camera>(); }
        public void Snap() { ready=false; Frame(0); }
        void LateUpdate() { Frame(Time.deltaTime); }
        void Frame(float dt)
        {
            if(!director || !director.character || !director.bug) return;
            if(!view) view=GetComponent<Camera>();
            if(portraitLetterbox)
            {
                float screenAspect=(float)Screen.width/Mathf.Max(1,Screen.height);
                float target=9f/16;
                view.rect=screenAspect>target?new Rect((1-target/screenAspect)/2,0,target/screenAspect,1):new Rect(0,(1-screenAspect/target)/2,1,screenAspect/target);
                view.aspect=target;
            }
            if(subjects==null)
            {
                var first=director.character.GetComponentsInChildren<Renderer>();
                var second=director.bug.GetComponentsInChildren<Renderer>();
                subjects=new Renderer[first.Length+second.Length]; first.CopyTo(subjects,0); second.CopyTo(subjects,first.Length);
            }
            bool haveBounds=false; var bounds=new Bounds();
            foreach(var renderer in subjects)
            {
                if(!renderer || !renderer.enabled) continue;
                if(!haveBounds) { bounds=renderer.bounds; haveBounds=true; } else bounds.Encapsulate(renderer.bounds);
            }
            if(!haveBounds) return;
            Vector3 direction=director.Phase>=ShortPhase.Chase?chaseOffset:director.Phase>=ShortPhase.Look?growthOffset:approachOffset;
            if(direction.sqrMagnitude<.001f) direction=new Vector3(.3f,.2f,-1);
            float blend=ready?1-Mathf.Exp(-blendSpeed*dt):1;
            smoothedCenter=Vector3.Lerp(smoothedCenter,bounds.center,blend);
            smoothedDirection=Vector3.Lerp(smoothedDirection,direction.normalized,blend).normalized;
            Quaternion rotation=Quaternion.LookRotation(-smoothedDirection,Vector3.up);
            Quaternion inverse=Quaternion.Inverse(rotation);
            float tanY=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad), tanX=tanY*view.aspect;
            float required=minimumDistance;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2) for(int z=-1;z<=1;z+=2)
            {
                Vector3 point=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                Vector3 local=inverse*(point-smoothedCenter);
                required=Mathf.Max(required,Mathf.Abs(local.x)*framingMargin/tanX-local.z,Mathf.Abs(local.y)*framingMargin/tanY-local.z);
            }
            distance=ready?Mathf.Max(required,Mathf.Lerp(distance,required,blend)):required;
            Vector3 position=smoothedCenter+smoothedDirection*distance;
            if(growthShake && director.Phase==ShortPhase.Grow)
            {
                float envelope=1-Mathf.Clamp01(director.PhaseTime/Mathf.Max(.001f,director.growthDuration));
                position+=rotation*new Vector3(Mathf.Sin(director.PhaseTime*77),Mathf.Sin(director.PhaseTime*93),0)*shakeStrength*envelope;
            }
            transform.SetPositionAndRotation(position,rotation); ready=true;
        }
    }
}
