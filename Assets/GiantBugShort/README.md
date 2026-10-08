# Giant Bug Short

Open `Assets/Scenes/GiantBugShort.unity` and press Play. This scene uses your man and bug prefabs; the original SampleScene and model assets are preserved.

## Scene controls

Select **ShortDirector** in the Hierarchy. Its Inspector contains character references, timing, walking/running/chasing speeds, the giant multiplier, and sound references.

- **Milestone / Approach:** walk to ApproachPoint and stop.
- **Milestone / Growth:** walk, pause, and grow the bug.
- **Milestone / Full Sequence:** add reaction, turn, chase, and the final freeze.
- **Restart Short:** appears in Play Mode and restores the original positions and scale.

Move ApproachPoint and EscapePoint to change the path. The default approach gap is 2.7 meters to leave room for the giant bug's model. Default growth is 12 times the original scale over 0.8 seconds.

## Scripts

- `ShortDirector.cs`: automatic scene sequence and optional animation/audio cues.
- `PortraitCamera.cs`: follows and frames both subjects for 9:16, with optional growth shake.
- `Assets/Editor/GiantBugSceneBuilder.cs`: scene setup tools under the Giant Bug Short menu.
- `Assets/Editor/ShortDirectorEditor.cs`: Inspector controls and script/controller shortcuts.
- `Assets/Editor/ShortWorkbench.cs`: local validation and rendered preview tooling; not included in player builds.
- `Assets/Editor/MeshyAnimationSetup.cs`: connects the Meshy clips using matching bone paths and preserves destination bone lengths.

## Connecting animation clips

1. Import additional clips into `Assets/Characters/Animations`.
2. Open `Assets/Characters/man.controller` in the Animator window.
3. Select Idle, Walk, or Run and assign the matching clip to its **Motion** field.
4. The current Idle, Walk, and Run states use distinct Meshy clips at speed 1. Copies in `Assets/GiantBugShort` preserve the man's proportions. The original FBX imports are unchanged.
5. The script calls these named states directly. Animator transition arrows or keyboard parameters are not required.
6. The character's Animator is on the model child under PlayerCharacter. Keep Apply Root Motion off; ShortDirector moves the root.

Animation rigs must be compatible. A Humanoid clip needs a valid Humanoid avatar; Generic clips need matching bone paths and rig conventions. Do not assume that any imported FBX can be assigned without checking the animated result.

The bug controller is `Assets/Characters/bug.controller`. Its Idle uses the bug's Meshy idle motion; Move uses the adapted Meshy Running motion at speed 0.7 for the larger stride. Unconditional transitions are muted because ShortDirector controls the sequence.

## Camera and recording

Select Main Camera to adjust PortraitCamera offsets, framing margin, and shake strength. The camera uses a portrait viewport even when the Game panel is wide. For a clean recording, set Game view to 1080 × 1920 (9:16), then record a Play Mode run using your preferred recorder. No recording package is required just to preview.

The full sequence targets roughly 27 seconds. Actual duration changes with movement distances and Inspector values. At the ending, movement and animation freeze and chase audio stops. Exit and re-enter Play Mode or click Restart Short to replay.

Three synthesized placeholder sound cues are included. Replace the AudioSource clips under ShortDirector with your preferred growth, reaction, and chase sounds.

## Validation outputs

Play Mode reports are saved in the project-root `Validation` directory. Preview images are written to `Temp/ShortCaptures`; Unity may clear Temp when the project closes.
