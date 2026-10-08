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
        [Header("Reaction and chase")]
        [Min(0)] public float reactionDuration = .4f;
        [Min(0)] public float turnDuration = .8f;
        [Min(.01f)] public float runSpeed = 3.8f;
        [Min(0)] public float chaseDelay = .5f;
        [Min(0)] public float chaseSpeed = 3.5f;
        [Min(0)] public float lateChaseSpeed = 4.5f;
        [Min(0)] public float accelerationTime = 6;
        [Min(0)] public float minimumSeparation = 2.7f;
        [Min(0)] public float endingHold = 2.5f;
        public AudioSource reactionSound;
        public AudioSource chaseSound;
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
        Quaternion turnFrom, turnTo;
        bool chaseStarted;
        bool initialized;

        void Start() { if (playOnStart) RestartSequence(); }
        void Update() { Advance(Time.deltaTime); }
        bool Initialize()
        {
            if (!character || !bug || !approachPoint)
            { Debug.LogError("ShortDirector needs Character, Bug, and Approach Point references.", this); return false; }
            if(milestone==ShortMilestone.FullSequence && !escapePoint)
            { Debug.LogError("Full sequence needs an Escape Point.",this); return false; }
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
            if(reactionSound) reactionSound.Stop();
            if(chaseSound) chaseSound.Stop();
            chaseStarted=false;
            Elapsed=0; Play(characterAnimator,idleState); Play(bugAnimator,idleState);
            SetPhase(ShortPhase.Establish);
            var camera=Camera.main;
            if(camera && camera.TryGetComponent<PortraitCamera>(out var rig)) rig.Snap();
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
                    if(t>=1)
                    {
                        if(milestone!=ShortMilestone.FullSequence) SetPhase(ShortPhase.Complete);
                        else { if(reactionSound) reactionSound.Play(); SetPhase(ShortPhase.React); }
                    }
                    break;
                case ShortPhase.React:
                    if(PhaseTime>=Mathf.Max(0,reactionDuration))
                    {
                        turnFrom=character.rotation;
                        Vector3 away=escapePoint.position-character.position; away.y=0;
                        turnTo=away.sqrMagnitude>.0001f?Quaternion.LookRotation(away):character.rotation;
                        SetPhase(ShortPhase.Turn);
                    }
                    break;
                case ShortPhase.Turn:
                    float turnT=turnDuration<=0?1:Mathf.Clamp01(PhaseTime/turnDuration);
                    character.rotation=Quaternion.Slerp(turnFrom,turnTo,Mathf.SmoothStep(0,1,turnT));
                    if(turnT>=1) { Play(characterAnimator,runState); SetPhase(ShortPhase.Chase); }
                    break;
                case ShortPhase.Chase:
                    Vector3 escape=escapePoint.position; escape.y=characterStart.y;
                    character.position=Vector3.MoveTowards(character.position,escape,Mathf.Max(.01f,runSpeed)*delta);
                    if(PhaseTime>=Mathf.Max(0,chaseDelay))
                    {
                        if(!chaseStarted) { chaseStarted=true; Play(bugAnimator,bugMoveState); if(chaseSound) chaseSound.Play(); }
                        Vector3 direction=character.position-bug.position; direction.y=0;
                        float distance=direction.magnitude;
                        if(distance>.0001f)
                        {
                            bug.rotation=Quaternion.LookRotation(direction);
                            float speed=Mathf.Lerp(chaseSpeed,lateChaseSpeed,Mathf.Clamp01((PhaseTime-accelerationTime)/2));
                            float travel=Mathf.Min(Mathf.Max(0,speed)*delta,Mathf.Max(0,distance-minimumSeparation));
                            bug.position+=direction/distance*travel;
                        }
                    }
                    if((character.position-escape).sqrMagnitude<.000001f)
                    {
                        if(chaseSound) chaseSound.Stop();
                        if(characterAnimator) characterAnimator.speed=0;
                        if(bugAnimator) bugAnimator.speed=0;
                        SetPhase(ShortPhase.Hold);
                    }
                    break;
                case ShortPhase.Hold:
                    if(PhaseTime>=Mathf.Max(0,endingHold)) SetPhase(ShortPhase.Complete);
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
        void OnDisable()
        {
            if(growthSound) growthSound.Stop();
            if(reactionSound) reactionSound.Stop();
            if(chaseSound) chaseSound.Stop();
        }
    }
}
