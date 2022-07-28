# 1.0.3

- do not override parameter type to float

# 1.0.2

- fix crashes

# 1.0.1

- ignore no visemes detected

# 1.0.0

- renamed to "vrc3cvr" to match github repo
- updated with latest VRCSDK and CCK
- improved UI
- fixed null reference error ([issue 9](https://github.com/imagitama/vrc3cvr/issues/9))
- clones original avatar to preserve
- added message about converting PhysBones

# 0.0.12

- added extra logging for github issue #8

# 0.0.11

- changed time parameter and blend trees to use `GestureLeft`/`GestureRight` instead of `GestureLeftWeight`/`GestureRightWeight`
- fixed crash when no blink blendshapes

# 0.0.10

- output if the left or right toe bones are not set

# 0.0.9

- added checkbox to decide if to delete the `LeftHand` and `RightHand` layers provided by CVR

# 0.0.8

- show a toggle instead of a dropdown if only 1 dropdown item

# 0.0.7

- fixed resting gesture showing open-hand/surprised gesture

# 0.0.6

- fixed `NotEqual` int conditions not properly converting to floats

# 0.0.5

- do not render dropdown if no conditions use the int VRC param

# 0.0.4

- dropdowns for int VRC params

# 0.0.3

- use toggles (Game Object Toggles) for boolean params

# 0.0.2

- fix animator controller not working because of duplicate layer names
- changed back to sliders
- changed `NotEqual` condition to `LessThan` the float value

# 0.0.1

Initial release.
