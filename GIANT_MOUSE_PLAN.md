# Giant Mouse Short — staged Codex plan

## Project inspection

- Unity version: 2022.3.62f2.
- Existing scene: `Assets/Scenes/SampleScene.unity`.
- Existing scene objects: Main Camera and Directional Light.
- No character, mouse, animation, audio, or custom script assets currently exist in Assets.
- Timeline and Unity Test Framework are installed; Cinemachine is not listed in the package manifest.

## Working rules

Complete one step at a time. Preserve existing assets and create a separate GiantMouseShort scene. Use primitive placeholders until models are supplied. Keep movement deterministic and automatic. Expose timing, references, speeds, and scale multiplier in the Inspector. Report what was actually tested and any manual assignments after each step.

## Step 1 — Scene structure

Prompt for Codex:

> Prepare only the scene structure for GiantMouseShort in this Unity 2022.3 project. Preserve SampleScene. Create PlayerCharacter, Mouse, Ground, Main Camera, Directional Light, and an empty ShortDirector. Use a capsule character and a recognizable mouse assembled from primitives because no models are available. Place both under ground-level root transforms, with visual geometry offset above the ground so future scaling keeps the mouse grounded. Add clearly named CharacterStart, ApproachPoint, and EscapePoint markers. Arrange a static camera so the placeholders are visible. Do not add movement or growth yet. Report the scene path and missing production assets.

Acceptance: scene opens without errors; all objects are identifiable; mouse is small; both subjects sit on the ground. Play Mode remains static.

## Step 2 — Approach and stop

> Add a ShortDirector C# component. Automatically walk PlayerCharacter toward a configurable ApproachPoint, initially about 1.5 meters from Mouse. Expose walk speed. Rotate toward travel direction and stop exactly at the target without overshooting. Support optional Animator Idle and Walk state names, and handle missing Animator or animation states gracefully. Keep root motion from competing with scripted movement. Add an initial delay and a restart action that restores the starting transform. Do not implement growth or chase.

Acceptance: character walks to the marker and stays there at different frame rates. Restart produces the same result. Missing required references produce a clear diagnostic and safely stop the sequence.

## Step 3 — Growth: first milestone

> After the character stops, wait 1 second and grow the mouse smoothly to 12 times its original local scale over 0.8 seconds. Expose delay, multiplier, and duration. Preserve the original nonuniform scale; do not repeatedly multiply the current scale. Support an optional growth AudioSource. Keep the feet/base at ground level through the ground-level root. Finish in an explicit Complete state. Restart restores mouse scale and character position. Do not add running or chasing.

Acceptance: walk, stop, wait, grow, complete. Mouse remains grounded. Repeat twice without accumulated scale. Test absent audio and zero growth duration. Check that the giant mouse does not intersect the character; adjust ApproachPoint or the visual geometry before continuing.

**Milestone gate:** visually verify Steps 1–3 in Unity before implementing Step 4.

## Step 4 — React, turn, and escape

> After growth, hold for 0.4 seconds, turn the character away, and run toward EscapePoint. Expose reaction delay, turn duration, and run speed. Support an optional Run animation. Stop exactly at EscapePoint. Keep the original approach and growth behavior working. Do not implement the mouse chase yet.

Acceptance: a readable pause and turn precede running; character reaches the escape marker and stops. Restart restores the full sequence.

## Step 5 — Predictable chase and ending

> Start the mouse chase 0.5 seconds after the character starts running. Expose chase speed, turn speed, and minimum separation. Face the character on the horizontal plane and support an optional movement animation. Start slightly slower than the character. For a near-catch ending, use a configurable late acceleration or authored chase target: a consistently slower mouse cannot close the gap. Account for giant model bounds when choosing separation. Stop both subjects cleanly at the ending cue, before overlap. Reset all chase state on restart.

Acceptance: no randomness, collisions, or visible overlap; the near-catch is repeatable; the mouse stops when the sequence ends.

## Step 6 — Portrait camera

> Add a simple camera controller with Inspector offsets for approach, growth, and chase. Use the current project dependencies. Frame both subjects during growth, and keep the giant mouse visible behind the running character during chase. Smoothly blend shots. Compose and verify in a 9:16 Game view; account for subject bounds and the narrow horizontal field of view. Avoid adding packages unless necessary.

Acceptance: test at 1080 by 1920; heads, ears, and feet remain intentionally framed; camera does not clip into subjects or scenery.

## Step 7 — Polish and recording

> Add optional reaction and chase audio, bounded growth camera shake, and configurable sequence timing. Ensure all audio and shake stop or reset cleanly. Target 20–30 seconds, measured from playback start to the final hold. End with a near-catch freeze or cut-ready hold. Document how to restart and record the Game view. Check existing recording tools before proposing an installation.

Suggested 27-second pacing:

| Time | Action |
| --- | --- |
| 0–3 s | Establish character and tiny mouse |
| 3–8 s | Walk toward mouse |
| 8–9 s | Look down / pause |
| 9–9.8 s | Mouse grows |
| 9.8–10.2 s | Reaction hold |
| 10.2–11 s | Turn and begin escape |
| 11–24 s | Chase with increasing tension |
| 24–27 s | Near-catch ending and final hold |

Set movement distances and speeds together to hit this timing. Do not assume delays alone determine total duration.

## Production assets to assign later

- Character model with compatible Idle, Walk, and Run clips/controller.
- Mouse model, optionally with locomotion animation; use a ground-level wrapper root.
- Growth boom, reaction, and chase sounds that you have permission to use.
- Optional environment art and a chosen ending prop, such as a door.

Primitive placeholders are sufficient to verify the sequence, but do not provide final character animation or production visuals.
