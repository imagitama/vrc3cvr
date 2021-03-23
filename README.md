Convert a VRChat SDK3 avatar to ChilloutVR with this Unity script.

Tested with:
- VRChat Avatar SDK3 2021.02.23
- ChilloutVR CCK 2.3
- Unity 2019.4.13f1

Tested using avatar [Canis Woof by Rezillo Ryker](https://www.vrcarena.com/assets/fnADyoq3IE5b4zIZGanA):

<img src="assets/screenshot_caniswoof_fat.png" width="300" />

<img src="assets/screenshot_sliders.png" width="300" />

[Watch video](assets/recording_caniswoof.mp4?raw=true)

## Usage

Go to [Releases](https://github.com/imagitama/vrc3cvr/releases/latest) and expand "Assets" and download the `.unitypackage`.

1. Copy your VRChat avatar Unity project and open it in Unity 2019
2. Install the ChilloutVR CCK (FAQ channel in their [Discord](https://discord.gg/ABI))
3. Import the vrc3cvr `.unitypackage`
4. Click PeanutTools -> VRC Chillout Converter.
5. Select the VRC avatar you want to convert.
6. Click Convert.

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
  - ChilloutVR only supports float parameters so booleans and ints have been converted

## Important Notes

VRC has 2 float params called `GestureLeftWeight` and `GestureRightWeight` which is the percentage the user is holding down the trigger. CVR instead provides it in the `GestureLeft` and `GestureRight` params where `0.5` would be 50% of the trigger when clenching your fist.

Mapping of VRC gestures to CVR:

| Gesture | VRC | CVR |
| --- | --- | --- | 
| Nothing | 0 | 0 |
| Fist | 1 | 1 |
| Open Hand | 2 | -1 |
| Point | 3 | 4 |
| Peace | 4 | 5 |
| Rock'n'Roll | 5 | 6 |
| Gun | 6 | 3 |
| Thumbs Up | 7 | 2 |

## Avatar-specific notes

### Awtter

- you must add a missing parameter to the `Actions` controller named `AFK` (`boolean`)
- remove the default animation in state `WaitForActionOrAFK` in `Actions` controller and enable "Write Defaults"

## Ideas for future

- support jaw flap blendshape
- automatically detect jaw/mouth and move voice position
- use Object Toggle advanced avatar setting for true/false
- GestureLeftWeight/GestureRightWeight
- show list of parameters/layers/states/transitions/conditions that might need to be fixed up