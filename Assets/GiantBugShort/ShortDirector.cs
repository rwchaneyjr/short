using UnityEngine;

namespace GiantBugShort
{
    public enum ShortPhase { Ready, Establish, Walk, Look, Grow, React, Turn, Chase, Hold, Complete }
    public enum ShortMilestone { Approach, Growth, FullSequence }

    public sealed class ShortDirector : MonoBehaviour
    {
        [Header("Scene references")]
        public Transform character;
        public Transform bug;
        public Transform approachPoint;
        public Transform escapePoint;
        public Animator characterAnimator;
        public Animator bugAnimator;
        [Header("Playback")]
        public bool playOnStart = true;
        public ShortMilestone milestone = ShortMilestone.Growth;
        [Min(0)] public float establishDuration = 3;
        [Min(.01f)] public float walkSpeed = 1.06f;
        [Min(0)] public float lookDuration = 1;
        [Min(0)] public float growthDuration = .8f;
        [Min(1)] public float giantMultiplier = 12;
        public AudioSource growthSound;
        [Header("Optional Animator state names")]
        public string idleState = "Idle";
        public string walkState = "Walk";
        public string runState = "Run";
        public string bugMoveState = "Move";
        [SerializeField] ShortPhase phase = ShortPhase.Ready;
        public ShortPhase Phase => phase;
        public float Elapsed { get; private set; }
        public float PhaseTime { get; private set; }
        public Vector3 OriginalBugScale => bugScale;
        Vector3 characterStart, bugStart, bugScale;
        Quaternion characterRotation, bugRotation;
        bool initialized;

        void Start() { if (playOnStart) RestartSequence(); }
        void Update() { Advance(Time.deltaTime); }
        bool Initialize()
        {
            if (!character || !bug || !approachPoint)
            { Debug.LogError("ShortDirector needs Character, Bug, and Approach Point references.", this); return false; }
            if (!initialized)
            {
                characterStart=character.position; characterRotation=character.rotation;
                bugStart=bug.position; bugRotation=bug.rotation; bugScale=bug.localScale;
                initialized=true;
            }
            if(characterAnimator) characterAnimator.applyRootMotion=false;
            if(bugAnimator) bugAnimator.applyRootMotion=false;
            return true;
        }
        [ContextMenu("Restart Sequence")]
        public void RestartSequence()
        {
            if(!Application.isPlaying || !Initialize()) return;
            character.SetPositionAndRotation(characterStart,characterRotation);
            bug.SetPositionAndRotation(bugStart,bugRotation); bug.localScale=bugScale;
            if(growthSound) growthSound.Stop();
            Elapsed=0; Play(characterAnimator,idleState); Play(bugAnimator,idleState);
            SetPhase(ShortPhase.Establish);
        }
        public void Advance(float delta)
        {
            if(!initialized || phase==ShortPhase.Complete || phase==ShortPhase.Ready || delta<=0) return;
            Elapsed+=delta; PhaseTime+=delta;
            switch(phase)
            {
                case ShortPhase.Establish:
                    if(PhaseTime>=Mathf.Max(0,establishDuration)) { Play(characterAnimator,walkState); SetPhase(ShortPhase.Walk); }
                    break;
                case ShortPhase.Walk:
                    Vector3 target=approachPoint.position; target.y=characterStart.y;
                    Vector3 heading=target-character.position;
                    if(heading.sqrMagnitude>.00001f) character.rotation=Quaternion.LookRotation(heading);
                    character.position=Vector3.MoveTowards(character.position,target,Mathf.Max(.01f,walkSpeed)*delta);
                    if((character.position-target).sqrMagnitude<.000001f)
                    {
                        Play(characterAnimator,idleState);
                        SetPhase(milestone==ShortMilestone.Approach?ShortPhase.Complete:ShortPhase.Look);
                    }
                    break;
                case ShortPhase.Look:
                    if(PhaseTime>=Mathf.Max(0,lookDuration)) { if(growthSound) growthSound.Play(); SetPhase(ShortPhase.Grow); }
                    break;
                case ShortPhase.Grow:
                    float t=growthDuration<=0?1:Mathf.Clamp01(PhaseTime/growthDuration);
                    bug.localScale=bugScale*Mathf.Lerp(1,Mathf.Max(1,giantMultiplier),Mathf.SmoothStep(0,1,t));
                    if(t>=1) SetPhase(ShortPhase.Complete);
                    break;
            }
        }
        void SetPhase(ShortPhase next) { phase=next; PhaseTime=0; }
        static void Play(Animator animator,string state)
        {
            if(!animator || !animator.runtimeAnimatorController || string.IsNullOrEmpty(state)) return;
            int hash=Animator.StringToHash(state);
            if(animator.HasState(0,hash)) { animator.speed=1; animator.CrossFadeInFixedTime(hash,.12f); }
        }
        void OnDisable() { if(growthSound) growthSound.Stop(); }
    }
}
