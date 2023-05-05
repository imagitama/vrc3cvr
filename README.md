**This tool is in "low maintenance" mode. I will not be making many updates to it (I don't play ChilloutVR much). I welcome any PR.**

Alternative tool: https://fluffs.gumroad.com/l/sdk3-to-cck

Convert a VRChat SDK3 avatar to ChilloutVR with this Unity script.

Tested with:

- VRChat Avatar SDK3 2022.06.03.00.04
- ChilloutVR CCK 3.3
- Unity 2019.4.31f1

Tested using avatar [Canis Woof by Rezillo Ryker](https://www.vrcarena.com/assets/fnADyoq3IE5b4zIZGanA) (CanineRez_UnityVRC_V011 from July 2022 with PhysBones):

<img src="assets/screenshot_caniswoof_fat.png" />

<img src="assets/screenshot_sliders.png" />

## Video

[Watch video](assets/recording_caniswoof.mp4?raw=true)

## Usage

Go to [Releases](https://github.com/imagitama/vrc3cvr/releases/latest) and expand "Assets" and download the `.unitypackage`.

1. Install the ChilloutVR CCK (FAQ channel in their [Discord](https://discord.gg/ABI))
2. Download and import the vrc3cvr `.unitypackage`
3. Click PeanutTools -> VRC3CVR
4. Select the VRC avatar you want to convert (ensure you have the VRC SDK in the project)
5. Click Convert

Want to convert your PhysBones to DynamicBones? Use these tools:

- https://booth.pm/ja/items/4032295
- https://github.com/Dreadrith/PhysBone-Converter

You don't need to buy DynamicBones! [Use this instead.](https://web.archive.org/web/20220204083651if_/https://objects.githubusercontent.com/github-production-release-asset-2e65be/412817921/ea75cc97-f679-4430-9d4e-29145b0143e0?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAIWNJYAX4CSVEH53A%2F20220204%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20220204T083509Z&X-Amz-Expires=300&X-Amz-Signature=b2b485dc8b05964635b634adadf39d9ada42f9c6843710acdd0c824dbcaf8ee8&X-Amz-SignedHeaders=host&actor_id=0&key_id=0&repo_id=412817921&response-content-disposition=attachment%3B%20filename%3DDynamic.Bone.Container.v1.3.0.unitypackage&response-content-type=application%2Foctet-stream) A free alternative to the Dynamic Bone package that can interpret bone information and works for uploading to VRChat.

## What does it do?

- adds a ChilloutVR avatar component (if missing)
- sets the face mesh
- sets the visemes
- sets the blink blendshapes
- sets the viewpoint and voice position to the VRChat avatar viewpoint
- adds an advanced avatar setting for each VRChat parameter
  - sliders for all float params
  - toggle for all boolean params
  - dropdown for all int params (toggle if only 1 int found)
- converts each animator controller (gestures, FX, etc.) to support ChilloutVR's gesture system
  - references to `GestureLeftWeight`/`GestureRightWeight` are converted to `GestureLeft`/`GestureRight` (check your Fist animation!)

## Mapping gestures

Mapping of VRC gestures to CVR:

| Gesture     | VRC | CVR |
| ----------- | --- | --- |
| Nothing     | 0   | 0   |
| Fist        | 1   | 1   |
| Open Hand   | 2   | -1  |
| Point       | 3   | 4   |
| Peace       | 4   | 5   |
| Rock'n'Roll | 5   | 6   |
| Gun         | 6   | 3   |
| Thumbs Up   | 7   | 2   |

### Trigger weight

VRC has two parameters `GestureLeftWeight` and `GestureRightWeight`. They do not exist in CVR and instead check `GestureLeft` amount where 0.5 is 50% of the trigger for the fist animation.

## Avatar compatibility

These avatars have been tested and verified to work in ChilloutVR using the tool. Some have notes for manual steps.

- [x] Canis Woof (Rezillo Ryker)
- [x] Rexouium (Rezillo Ryker)
  - manual step: add missing parameters `ToeMoveH` and `ToeMoveV`
- [x] Awtter (Shade the Bat)
  - manual step: add missing parameter `AFK`
  - fix locomotion: remove the motion in state `WaitForActionOrAFK` in `Actions` controller and enable "Write Defaults"
- [x] Shiba Inu (Alucard/Pikapetey)
- [x] Wickerbeast (Jin A)

Please message via the Discord if you have used the tool on your avatar.

## Ideas for future

- support jaw flap blendshape
- automatically detect jaw/mouth and move voice position
- GestureLeftWeight/GestureRightWeight

## Troubleshooting

### "VRCExpressionParameters.Parameter does not contain a definition for defaultValue" or another VRChat error

Update to a more recent version. Tested with at least VRChat Avatar SDK3 2021.02.23.

### When performing a gesture my hands do not animate

Uncheck "My avatar has custom hand animations" and convert.

### "The type or namespace 'VRC' could not be found"

You need the VRC SDK in your project.
