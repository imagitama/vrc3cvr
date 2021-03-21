# VRC Chillout Converter

Convert a VRChat SDK3 avatar to ChilloutVR with this Unity script.

Tested with:
- VRChat Avatar SDK3 2021.02.23
- ChilloutVR CCK 2.3
- Unity 2019.4.13f1

Tested using avatar [Canis Woof by Rezillo Ryker](https://www.vrcarena.com/assets/fnADyoq3IE5b4zIZGanA).

## Usage

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
  - using Sliders for each parameter (for booleans 50%-100% is true)
- converts each animator controller (gestures, FX, etc.) to support ChilloutVR's gesture system
  - ChilloutVR only supports float parameters so booleans and ints have been converted

## Important Notes

VRC has 2 params called `GestureLeftWeight` and `GestureRightWeight` which is the amount of trigger the user is using. In CVR that is the float value of the gesture.

Mapping of VRC gestures to CVR:

| Gesture | VRC | CVR |
| --- | --- | --- | 
| Nothing | 0 | ? |
| Fist | 1 | 1 |
| Open Hand | 2 | 0 |
| Point | 3 | 4 |
| Peace | 4 | 5 |
| Rock'n'Roll | 5 | 6 |
| Gun | 6 | 3 |
| Thumbs Up | 7 | ? |

## Ideas for future

- use Object Toggle advanced avatar setting for true/false
- GestureLeftWeight/GestureRightWeight
- Blendtrees