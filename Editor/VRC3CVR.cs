using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRCExpressionParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;
using VRC.SDK3.Avatars.Components;
using ABI.CCK.Components;
using ABI.CCK.Scripts;
using ABI.CCK.Scripts.Editor;
using PeanutTools_VRC3CVR;

public class VRC3CVR : EditorWindow
{
    Animator animator;
    bool isConverting = false;
    VRCAvatarDescriptor vrcAvatarDescriptor;
    CVRAvatar cvrAvatar;
    SkinnedMeshRenderer bodySkinnedMeshRenderer;
    Vector3 vrcViewPosition;
    string[] vrcVisemeBlendShapes;
    string blinkBlendshapeName;
    AnimatorController chilloutAnimatorController;
    AnimatorController[] vrcAnimatorControllers;
    string outputDirName = "VRC3CVR_Output";
    bool shouldDeleteCvrHandLayers = true;
    Vector2 scrollPosition;
    GameObject chilloutAvatarGameObject;
    bool shouldCloneAvatar = true;
    bool shouldDeletePhysBones = true;
    bool inteligentParameterNames = false;

    [MenuItem("PeanutTools/VRC3CVR")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRC3CVR>();
        window.titleContent = new GUIContent("VRC3CVR");
        window.minSize = new Vector2(250, 50);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        CustomGUI.BoldLabel("VRC3CVR");
        CustomGUI.ItalicLabel("Convert your VRChat avatar to ChilloutVR");

        CustomGUI.LineGap();

        CustomGUI.HorizontalRule();

        CustomGUI.LineGap();

        CustomGUI.BoldLabel("Step 1: Select your avatar");

        CustomGUI.SmallLineGap();

        vrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", vrcAvatarDescriptor, typeof(VRCAvatarDescriptor));

        CustomGUI.SmallLineGap();

        CustomGUI.BoldLabel("Step 2: Configure settings");

        CustomGUI.SmallLineGap();

        shouldDeleteCvrHandLayers = GUILayout.Toggle(shouldDeleteCvrHandLayers, "My avatar has custom hand animations");
        CustomGUI.ItalicLabel("If your avatar overwrites the default finger animations when performing expressions");

        CustomGUI.SmallLineGap();

        GUILayout.Label("Need to convert your PhysBones to DynamicBones? Use this tool: https://booth.pm/ja/items/4032295");

        CustomGUI.SmallLineGap();

        shouldCloneAvatar = GUILayout.Toggle(shouldCloneAvatar, "Clone avatar");

        CustomGUI.SmallLineGap();

        shouldDeletePhysBones = GUILayout.Toggle(shouldDeletePhysBones, "Delete PhysBones and colliders");
        CustomGUI.ItalicLabel("Always deletes contact receivers and senders");

        CustomGUI.SmallLineGap();

        inteligentParameterNames = GUILayout.Toggle(inteligentParameterNames, "Inteligent Parameter names");

        CustomGUI.SmallLineGap();

        CustomGUI.BoldLabel("Step 3: Convert");

        CustomGUI.SmallLineGap();

        EditorGUI.BeginDisabledGroup(GetIsReadyForConvert() == false);
        if (GUILayout.Button("Convert"))
        {
            Convert();
        }
        EditorGUI.EndDisabledGroup();
        CustomGUI.ItalicLabel("Clones your original avatar to preserve it");

        if (animator != null) {
            Transform leftToesTransform = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            Transform righToesTransform = animator.GetBoneTransform(HumanBodyBones.RightToes);

            if (leftToesTransform == null || righToesTransform == null) {
                CustomGUI.SmallLineGap();

                CustomGUI.RenderErrorMessage("You do not have a " + (leftToesTransform == null ? "left" : "right") + " toe bone configured");
                CustomGUI.RenderWarningMessage("You must configure this before you upload your avatar");
            }
        }

        CustomGUI.SmallLineGap();

        CustomGUI.MyLinks("vrc3cvr");

        EditorGUILayout.EndScrollView();
    }

    bool GetAreToeBonesSet() {
        return true;
    }

    bool GetIsReadyForConvert()
    {
        return vrcAvatarDescriptor != null;
    }

    void SetAnimator() {
        // this is not necessary for VRC or CVR but it helps people test their controller
        // and lets us query for Toe bones for our GUI
        animator = chilloutAvatarGameObject.GetComponent<Animator>();
        animator.runtimeAnimatorController = chilloutAnimatorController;
    }

    void CreateChilloutAvatar() {
        if (shouldCloneAvatar) {
            chilloutAvatarGameObject = Instantiate(vrcAvatarDescriptor.gameObject);
            chilloutAvatarGameObject.name = vrcAvatarDescriptor.gameObject.name.Trim() + " (ChilloutVR)";
            chilloutAvatarGameObject.SetActive(true);
        } else {
            chilloutAvatarGameObject = vrcAvatarDescriptor.gameObject;
        }
    }

    void HideOriginalAvatar() {
        vrcAvatarDescriptor.gameObject.SetActive(false);
    }

    void Convert()
    {
        if (isConverting == true)
        {
            Debug.Log("Cannot convert - already in progress");
        }

        isConverting = true;

        Debug.Log("Starting to convert...");

        CreateChilloutAvatar();
        GetValuesFromVrcAvatar();
        CreateChilloutComponentIfNeeded();
        PopulateChilloutComponent();
        CreateEmptyChilloutAnimator();
        MergeVrcAnimatorsIntoChilloutAnimator();
        SetAnimator();
        ConvertVrcParametersToChillout();
        InsertChilloutOverride();
        DeleteVrcComponents();

        if (shouldCloneAvatar) {
            HideOriginalAvatar();
        }

        Debug.Log("Conversion complete!");

        isConverting = false;
    }

    void InsertChilloutOverride() {
        Debug.Log("Inserting chillout override controller...");

        AnimatorOverrideController overrideController = new AnimatorOverrideController(chilloutAnimatorController);

        AssetDatabase.CreateAsset(overrideController, "Assets/" + outputDirName + "/ChilloutVR Overrides.overrideController");

        cvrAvatar.overrides = overrideController;

        EditorUtility.SetDirty(cvrAvatar);
        Repaint();

        Debug.Log("Inserted!");
    }

    void BuildChilloutAnimatorWithParams()
    {
        Debug.Log("Building chillout animator with params...");


        Debug.Log("Settings" + cvrAvatar.avatarSettings);

        foreach (UnityEditor.Editor go in Resources.FindObjectsOfTypeAll(typeof(UnityEditor.Editor)))
        {
            // This method is private in CCK
            MethodInfo privateMethod = go.GetType().GetMethod("CreateAnimator", BindingFlags.NonPublic | BindingFlags.Instance);

            if (privateMethod != null)
            {
                MethodInfo onInspectorGUIMethod = go.GetType().GetMethod("OnInspectorGUI");
                onInspectorGUIMethod.Invoke(go, new object[] { });

                privateMethod.Invoke(go, new object[] { });
            }
        }

        cvrAvatar.overrides = cvrAvatar.avatarSettings.overrides;

        Debug.Log("Chillout animator with params built");
    }

    void DeleteVrcComponents()
    {
        Debug.Log("Deleting VRC components...");

        DestroyImmediate(chilloutAvatarGameObject.GetComponent(typeof(VRC.Core.PipelineManager)));

        var vrcComponents = chilloutAvatarGameObject.GetComponentsInChildren(typeof(Component), true).ToList().Where(c => c.GetType().Name.StartsWith("VRC")).ToList();

        if (vrcComponents.Count > 0) {
            Debug.Log("Found " + vrcComponents.Count + " VRC components");

            foreach (var component in vrcComponents) {
                string componentName = component.GetType().Name;

                if (!shouldDeletePhysBones && componentName.Contains("PhysBone")) {
                    continue;
                }

                Debug.Log(component.name + "." + componentName);

                DestroyImmediate(component);
            }
        }

        Debug.Log("VRC components deleted");
    }

    List<int> GetAllIntOptionsForParamFromAnimatorController(string paramName, AnimatorController animatorController)
    {
        List<int> results = new List<int>();

        foreach (AnimatorControllerLayer layer in animatorController.layers) {
            foreach (ChildAnimatorState state in layer.stateMachine.states) {
                foreach (AnimatorStateTransition transition in state.state.transitions.Concat(layer.stateMachine.anyStateTransitions)) {
                    foreach (AnimatorCondition condition in transition.conditions) {
                        if (condition.parameter == paramName && results.Contains((int)condition.threshold) == false) {
                            Debug.Log("Adding " + condition.threshold + " as option for param " + paramName);
                            results.Add((int)condition.threshold);
                        }
                    }
                }
            }
        }

        return results;
    }

    List<int> GetAllIntOptionsForParam(string paramName) {
        List<int> results = new List<int>();

        Debug.Log("Getting all int options for param \"" + paramName + "\"...");

        for (int i = 0; i < vrcAnimatorControllers.Length; i++)
        {
            // if the user has not selected anything
            if (vrcAnimatorControllers[i] == null)
            {
                continue;
            }

            List<int> newResults = GetAllIntOptionsForParamFromAnimatorController(paramName, vrcAnimatorControllers[i]);

            foreach (int newResult in newResults) {
                if (results.Contains(newResult) == false) {
                    results.Add(newResult);
                }
            }
        }

        Debug.Log("Found " + results.Count + " int options: " + string.Join(", ", results.ToArray()));

        if (results.Count == 0) {
            Debug.Log("Found 0 int options for param " + paramName + " - this is probably not what you want!");
        }

        return results;
    }

    List<CVRAdvancedSettingsDropDownEntry> ConvertIntToGameObjectDropdownOptions(List<int> ints, VRCExpressionParameter param = null) {
        List<CVRAdvancedSettingsDropDownEntry> entries = new List<CVRAdvancedSettingsDropDownEntry>();

        ints.Sort();

        foreach (int value in ints) {
            entries.Add(new CVRAdvancedSettingsDropDownEntry() {
                name = param == null ? value.ToString() : ParameterValueFriendlyName(param, value)
            });
        }

        return entries;
    }

    VRCExpressionsMenu[] GetExpressionSubMenus(VRCExpressionsMenu m)
    {
        List<VRCExpressionsMenu> menus = new List<VRCExpressionsMenu>();
        menus.Add(m);
        foreach(VRCExpressionsMenu.Control c in m.controls)
        {
            if(c.type == VRCExpressionsMenu.Control.ControlType.SubMenu)
            {
                if(c.subMenu == null)
                {
                    Debug.LogWarning($"Null submenu in VRCExpressionsMenu {m.ToString()} control {c.ToString()}");
                    continue;
                }
                menus.AddRange(GetExpressionSubMenus(c.subMenu));
            }
        }
        return menus.ToArray();
    }

    VRCExpressionsMenu[] GetExpressionMenus()
    {
        VRCExpressionsMenu basemenu = vrcAvatarDescriptor.expressionsMenu;

        if (basemenu == null) return new VRCExpressionsMenu[0];

        return GetExpressionSubMenus(basemenu);
    }
    
    bool IsControlFor(VRCExpressionsMenu.Control c, VRCExpressionParameter param)
    {
        switch (c.type)
        {
            case VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet:
            case VRCExpressionsMenu.Control.ControlType.FourAxisPuppet:
            case VRCExpressionsMenu.Control.ControlType.RadialPuppet:
                foreach (var subparam in c.subParameters)
                {
                    if (subparam.name == param.name) return true;
                }
                break;
            case VRCExpressionsMenu.Control.ControlType.Button:
            case VRCExpressionsMenu.Control.ControlType.Toggle:
            case VRCExpressionsMenu.Control.ControlType.SubMenu:
            default:
                break;
        }
        return c.parameter.name == param.name;
    }

    string MenuFriendlyName(VRCExpressionsMenu tm)
    {
        foreach (VRCExpressionsMenu m in GetExpressionMenus())
        {
            foreach (VRCExpressionsMenu.Control c in m.controls)
            {
                if (c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.subMenu == tm) return c.name;
            }
        }
        return tm.name;
    }

    string ParameterFriendlyName(VRCExpressionParameter param, bool multi = false)
    {
        if (inteligentParameterNames)
        {
            // Attempts to detect a friendly name for an expression parameter, falling back to the name otherwise
            foreach (VRCExpressionsMenu m in GetExpressionMenus())
            {
                foreach (VRCExpressionsMenu.Control c in m.controls)
                {
                    if (IsControlFor(c, param))
                    {
                        if (multi)
                        {
                            return MenuFriendlyName(m);
                        }
                        else
                        {
                            return c.name;
                        }
                    }
                }
            }
        }
        return param.name;
    }

    string ParameterValueFriendlyName(VRCExpressionParameter param, int value)
    {
        if (inteligentParameterNames)
        {
            // Attempts to detect a friendly name for an expression parameter value, falling back to the name otherwise
            foreach (VRCExpressionsMenu m in GetExpressionMenus())
            {
                foreach (VRCExpressionsMenu.Control c in m.controls)
                {
                    if (IsControlFor(c, param))
                    {
                        if (((int)c.value) == value)
                        {
                            return c.name;
                        }
                    }
                }
            }
        }
        return value.ToString();
    }

    void ConvertVrcParametersToChillout()
    {
        Debug.Log("Converting vrc parameters to chillout...");

        VRCExpressionParameters vrcParams = vrcAvatarDescriptor.expressionParameters;

        List<CVRAdvancedSettingsEntry> newParams = new List<CVRAdvancedSettingsEntry>();

        for (int i = 0; i < vrcParams?.parameters?.Length; i++)
        {
            VRCExpressionParameter vrcParam = vrcParams.parameters[i];

            Debug.Log("Param \"" + vrcParam.name + "\" type \"" + vrcParam.valueType + "\" default \"" + vrcParam.defaultValue + "\"");

            CVRAdvancedSettingsEntry newParam = null;

            switch (vrcParam.valueType)
            {
                case VRCExpressionParameters.ValueType.Int:
                    List<CVRAdvancedSettingsDropDownEntry> dropdownOptions = ConvertIntToGameObjectDropdownOptions(GetAllIntOptionsForParam(vrcParam.name), vrcParam);

                    if (dropdownOptions.Count > 1) {
                        newParam = new CVRAdvancedSettingsEntry() {
                            name = ParameterFriendlyName(vrcParam, true),
                            machineName = vrcParam.name,
                            type = CVRAdvancedSettingsEntry.SettingsType.GameObjectDropdown,
                            setting = new CVRAdvancesAvatarSettingGameObjectDropdown() {
                                usedType = CVRAdvancesAvatarSettingBase.ParameterType.GenerateInt,
                                defaultValue = (int)vrcParam.defaultValue,
                                options = dropdownOptions
                            }
                        };
                    } else {
                        Debug.Log("Param has less than 2 options so we are making a toggle instead");

                        newParam = new CVRAdvancedSettingsEntry() {
                            name = vrcParam.name,
                            machineName = vrcParam.name,
                            setting = new CVRAdvancesAvatarSettingGameObjectToggle() {
                                defaultValue = vrcParam.defaultValue == 1 ? true : false
                            }
                        };
                    }
                    break;

                case VRCExpressionParameters.ValueType.Float:
                    newParam = new CVRAdvancedSettingsEntry() {
                        name = ParameterFriendlyName(vrcParam),
                        machineName = vrcParam.name,
                        type = CVRAdvancedSettingsEntry.SettingsType.Slider,
                        setting = new CVRAdvancesAvatarSettingSlider() {
                            usedType = CVRAdvancesAvatarSettingBase.ParameterType.GenerateFloat,
                            defaultValue = vrcParam.defaultValue
                        }
                    };
                    break;

                case VRCExpressionParameters.ValueType.Bool:
                    newParam = new CVRAdvancedSettingsEntry() {
                        name = ParameterFriendlyName(vrcParam),
                        machineName = vrcParam.name,
                        setting = new CVRAdvancesAvatarSettingGameObjectToggle() {
                            usedType = CVRAdvancesAvatarSettingBase.ParameterType.GenerateBool,
                            defaultValue = vrcParam.defaultValue != 0 ? true : false
                        }
                    };
                    break;

                default:
                    throw new Exception("Cannot convert vrc parameter to chillout: unknown type \"" + vrcParam.valueType + "\"");
            }

            if (newParam != null) {
                newParams.Add(newParam);
            }
        }

        cvrAvatar.avatarSettings.settings = newParams;

        Debug.Log("Finished converting vrc params");
    }

    void MergeVrcAnimatorsIntoChilloutAnimator()
    {
        Debug.Log("Merging " + vrcAnimatorControllers.Length + " vrc animators into chillout animator...");

        for (int i = 0; i < vrcAnimatorControllers.Length; i++)
        {
            // if the user has not selected anything
            if (vrcAnimatorControllers[i] == null)
            {
                continue;
            }

            MergeVrcAnimatorIntoChilloutAnimator(vrcAnimatorControllers[i]);
        }

        Debug.Log("Finished merging all animators");
    }

    float GetChilloutGestureNumberForVrchatGestureNumber(float vrchatGestureNumber)
    {
        switch (vrchatGestureNumber)
        {
            // no gesture
            case 0:
                return 0;
            // fist
            case 1:
                return 1;
            // open hand
            case 2:
                return -1;
            // point
            case 3:
                return 4;
            // peace
            case 4:
                return 5;
            // rock n roll
            case 5:
                return 6;
            // gun
            case 6:
                return 3;
            // thumbs up
            case 7:
                return 2;
            default:
                throw new Exception("Cannot get chillout gesture number for vrchat gesture number: " + vrchatGestureNumber);
        }
    }

    AnimatorControllerParameter[] GetParametersWithoutDupes(AnimatorControllerParameter[] newParams, AnimatorControllerParameter[] existingParams)
    {
        List<AnimatorControllerParameter> finalParams = new List<AnimatorControllerParameter>(existingParams);

        for (int x = 0; x < newParams.Length; x++)
        {
            bool doesAlreadyExist = false;

            for (int y = 0; y < existingParams.Length; y++)
            {
                if (existingParams[y].name == newParams[x].name)
                {
                    doesAlreadyExist = true;
                }
            }

            //  Debug.Log("WITHOUT DUPE: " + newParams[x].name + " yes? " + (doesAlreadyExist == true ? "EXISTS" : " NO EXISTS"));

            if (doesAlreadyExist == false)
            {
                finalParams.Add(newParams[x]);
            }
        }

        return finalParams.ToArray();
    }

    AnimatorTransition[] ProcessTransitions(AnimatorTransition[] transitions)
    {
        return ProcessTransitions<AnimatorTransition>(transitions);
    }

    AnimatorStateTransition[] ProcessTransitions(AnimatorStateTransition[] transitions)
    {
        return ProcessTransitions<AnimatorStateTransition>(transitions);
    }

    AnimatorTranstitionType[] ProcessTransitions<AnimatorTranstitionType>(AnimatorTranstitionType[] transitions) where AnimatorTranstitionType : AnimatorTransitionBase, new()
    {
        List<AnimatorTranstitionType> transitionsToAdd = new List<AnimatorTranstitionType>();

        for (int t = 0; t < transitions.Length; t++)
        {
            List<AnimatorCondition> conditionsToAdd = new List<AnimatorCondition>();
            AnimatorTranstitionType transition = transitions[t];

            // Debug.Log(transitions[t].conditions.Length + " conditions");

            ProcessTransition(transition, transitionsToAdd, conditionsToAdd);
        }

        AnimatorTranstitionType[] newTransitions = new AnimatorTranstitionType[transitions.Length + transitionsToAdd.Count];

        transitions.CopyTo(newTransitions, 0);
        transitionsToAdd.ToArray().CopyTo(newTransitions, transitions.Length);

        return newTransitions;
    }

    void ProcessTransition<AnimatorTranstitionType>(AnimatorTranstitionType transition, List<AnimatorTranstitionType> transitionsToAdd, List<AnimatorCondition> conditionsToAdd, bool isDuplicate = false) where AnimatorTranstitionType : AnimatorTransitionBase, new()
    {
        // Convert GestureLeft/GestureRight to ChilloutVR
        for (int c = 0; c < transition.conditions.Length; c++)
        {
            AnimatorCondition condition = transition.conditions[c];

            if (condition.parameter == "GestureLeft" || condition.parameter == "GestureRight")
            {
                float chilloutGestureNumber = GetChilloutGestureNumberForVrchatGestureNumber(condition.threshold);

                if (condition.mode == AnimatorConditionMode.Equals)
                {
                    float thresholdLow = (float)(chilloutGestureNumber - 0.1);
                    float thresholdHigh = (float)(chilloutGestureNumber + 0.1);

                    // Look for GestureWeight and adjust threshold
                    if (chilloutGestureNumber == 1f) // Fist only
                    {
                        thresholdLow = 0.01f;

                        for (int w = 0; w < transition.conditions.Length; w++)
                        {
                            AnimatorCondition conditionW = transition.conditions[w];
                            if (
                                (condition.parameter == "GestureLeft" && conditionW.parameter == "GestureLeftWeight") ||
                                (condition.parameter == "GestureRight" && conditionW.parameter == "GestureRightWeight")
                            ) {
                                if (conditionW.mode == AnimatorConditionMode.Less)
                                {
                                    thresholdHigh = conditionW.threshold;
                                }
                                else
                                {
                                    thresholdLow = conditionW.threshold;
                                }
                            }
                        }
                    }

                    // Create replace conditions for ChilloutVR
                    AnimatorCondition newConditionLessThan = new AnimatorCondition();
                    newConditionLessThan.parameter = condition.parameter;
                    newConditionLessThan.mode = AnimatorConditionMode.Less;
                    newConditionLessThan.threshold = thresholdHigh;

                    conditionsToAdd.Add(newConditionLessThan);

                    AnimatorCondition newConditionGreaterThan = new AnimatorCondition();
                    newConditionGreaterThan.parameter = condition.parameter;
                    newConditionGreaterThan.mode = AnimatorConditionMode.Greater;
                    newConditionGreaterThan.threshold = thresholdLow;

                    conditionsToAdd.Add(newConditionGreaterThan);
                }
                else if (condition.mode == AnimatorConditionMode.NotEqual)
                {
                    float thresholdLow = (float)(chilloutGestureNumber - 0.1);
                    float thresholdHigh = (float)(chilloutGestureNumber + 0.1);

                    if (chilloutGestureNumber == 1f) // Fist only
                    {
                        thresholdLow = 0.01f;
                    }

                    if (isDuplicate) {
                        // Add greater than transition to duplicate
                        AnimatorCondition newConditionGreaterThan = new AnimatorCondition();
                        newConditionGreaterThan.parameter = condition.parameter;
                        newConditionGreaterThan.mode = AnimatorConditionMode.Greater;
                        newConditionGreaterThan.threshold = thresholdHigh;

                        conditionsToAdd.Add(newConditionGreaterThan);

                    } else {
                        // Change transition to use less than
                        AnimatorCondition newConditionLessThan = new AnimatorCondition();
                        newConditionLessThan.parameter = condition.parameter;
                        newConditionLessThan.mode = AnimatorConditionMode.Less;
                        newConditionLessThan.threshold = thresholdLow;

                        conditionsToAdd.Add(newConditionLessThan);

                        // Duplicate transition to create the "or greater than" transition
                        AnimatorTranstitionType newTransition = new AnimatorTranstitionType();
                        if (newTransition is AnimatorStateTransition) {
                            AnimatorStateTransition newTransitionTyped = newTransition as AnimatorStateTransition;
                            AnimatorStateTransition transitionTyped = transition as AnimatorStateTransition;
                            newTransitionTyped.duration = transitionTyped.duration;
                            newTransitionTyped.canTransitionToSelf = transitionTyped.canTransitionToSelf;
                            newTransitionTyped.exitTime = transitionTyped.exitTime;
                            newTransitionTyped.hasExitTime = transitionTyped.hasExitTime;
                            newTransitionTyped.hasFixedDuration = transitionTyped.hasFixedDuration;
                            newTransitionTyped.interruptionSource = transitionTyped.interruptionSource;
                            newTransitionTyped.offset = transitionTyped.offset;
                            newTransitionTyped.orderedInterruption = transitionTyped.orderedInterruption;
                        }

                        newTransition.name = transition.name;
                        newTransition.destinationState = transition.destinationState;
                        newTransition.destinationStateMachine = transition.destinationStateMachine;
                        newTransition.hideFlags = transition.hideFlags;
                        newTransition.isExit = transition.isExit;
                        newTransition.solo = transition.solo;
                        newTransition.mute = transition.mute;

                        for (int c2 = 0; c2 < transition.conditions.Length; c2++)
                        {
                            newTransition.AddCondition(transition.conditions[c2].mode, transition.conditions[c2].threshold, transition.conditions[c2].parameter);
                        }

                        List<AnimatorTranstitionType> transitionsToAdd2 = new List<AnimatorTranstitionType>();
                        List<AnimatorCondition> conditionsToAdd2 = new List<AnimatorCondition>();

                        ProcessTransition(newTransition, transitionsToAdd2, conditionsToAdd2, true);
                        newTransition.conditions = conditionsToAdd2.ToArray();

                        transitionsToAdd.Add(newTransition);
                    }
                }
            }
            else if (condition.parameter == "GestureLeftWeight" || condition.parameter == "GestureRightWeight")
            {
                // Look for fist gesture and create condition if needed
                bool gestureFound = false;

                for (int w = 0; w < transition.conditions.Length; w++)
                {
                    AnimatorCondition conditionW = transition.conditions[w];
                    if (
                        (condition.parameter == "GestureLeftWeight" && conditionW.parameter == "GestureLeft") ||
                        (condition.parameter == "GestureRightWeight" && conditionW.parameter == "GestureRight")
                    ) {
                        if (conditionW.threshold == 1f) {
                            gestureFound = true;
                            break;
                        }
                    }
                }

                // Create condition if gesture weight is used by itself
                if (!gestureFound)
                {
                    float thresholdLow = -0.1f;
                    float thresholdHigh = 1.1f;

                    if (condition.mode == AnimatorConditionMode.Less)
                    {
                        thresholdHigh = condition.threshold;
                    }
                    else
                    {
                        thresholdLow = condition.threshold;
                    }

                    string parameterName = condition.parameter == "GestureLeftWeight" ? "GestureLeft" : "GestureRight";

                    // Create replace conditions for ChilloutVR
                    AnimatorCondition newConditionLessThan = new AnimatorCondition();
                    newConditionLessThan.parameter = parameterName;
                    newConditionLessThan.mode = AnimatorConditionMode.Less;
                    newConditionLessThan.threshold = thresholdHigh;

                    conditionsToAdd.Add(newConditionLessThan);

                    AnimatorCondition newConditionGreaterThan = new AnimatorCondition();
                    newConditionGreaterThan.parameter = parameterName;
                    newConditionGreaterThan.mode = AnimatorConditionMode.Greater;
                    newConditionGreaterThan.threshold = thresholdLow;

                    conditionsToAdd.Add(newConditionGreaterThan);
                }
            }
            else
            {
                conditionsToAdd.Add(condition);
            }
        }

        transition.conditions = conditionsToAdd.ToArray();
    }

    void ProcessStateMachine(AnimatorStateMachine stateMachine)
    {
        for (int s = 0; s < stateMachine.states.Length; s++)
        {
            // Debug.Log(stateMachine.states[s].state.transitions.Length + " transitions");

            AnimatorState state = stateMachine.states[s].state;

            // assuming they only ever check weight for the Fist animation
            if (state.timeParameter == "GestureLeftWeight") {
                state.timeParameter = "GestureLeft";
            } else if (state.timeParameter == "GestureRightWeight") {
                state.timeParameter = "GestureRight";
            }

            if (state.motion is BlendTree) {
                BlendTree blendTree = (BlendTree)state.motion;

                if (blendTree.blendParameter == "GestureLeftWeight") {
                    blendTree.blendParameter = "GestureLeft";
                } else if (blendTree.blendParameter == "GestureRightWeight") {
                    blendTree.blendParameter = "GestureRight";
                }
            }

            AnimatorStateTransition[] newTransitions = ProcessTransitions(state.transitions);
            state.transitions = newTransitions;
        }

        stateMachine.anyStateTransitions = ProcessTransitions(stateMachine.anyStateTransitions);
        stateMachine.entryTransitions = ProcessTransitions(stateMachine.entryTransitions);

        if (stateMachine.stateMachines.Length > 0)
        {
            // Debug.Log("Found " + stateMachine.stateMachines.Length + " child state machines");
        }

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            ProcessStateMachine(childStateMachine.stateMachine);
        }
    }

    AnimatorController CopyVrcAnimatorForMerge(AnimatorController animator)
    {
        string animatorPath = AssetDatabase.GetAssetPath(animator);

        if (string.IsNullOrEmpty(animatorPath))
        {
            throw new Exception("Cannot copy vrc animator \"" + animator.name + "\": does not seem to exist! " + animatorPath);
        }

        string filename = Path.GetFileName(animatorPath);
        string pathToCopiedFile = "Assets/" + outputDirName + "/" + filename;

        Debug.Log("Copy " + animatorPath + " -> " + pathToCopiedFile);

        // ReplaceFile() doesn't actually replace for some reason so make sure there is none already there
        FileUtil.DeleteFileOrDirectory(pathToCopiedFile);

        AssetDatabase.Refresh();

        FileUtil.CopyFileOrDirectory(animatorPath, pathToCopiedFile);

        AssetDatabase.Refresh();

        AnimatorController newAnimatorController = (AnimatorController)AssetDatabase.LoadAssetAtPath(pathToCopiedFile, typeof(AnimatorController));

        if (newAnimatorController == null)
        {
            throw new Exception("Failed to load the created animator!");
        }

        return newAnimatorController;
    }

    void PurgeAnimator(AnimatorController animatorToPurge)
    {
        Destroy(animatorToPurge);
        string animatorPath = AssetDatabase.GetAssetPath(animatorToPurge);
        Debug.Log("Purge " + animatorPath);
        FileUtil.DeleteFileOrDirectory(animatorPath);
        AssetDatabase.Refresh();
    }

    void MergeVrcAnimatorIntoChilloutAnimator(AnimatorController originalAnimatorController)
    {
        Debug.Log("Merging vrc animator \"" + originalAnimatorController.name + "\"...");

        // we modify everything in place so we don't want to mutate the original
        AnimatorController animatorToMerge = CopyVrcAnimatorForMerge(originalAnimatorController);

        AnimatorControllerParameter[] existingParams = chilloutAnimatorController.parameters;
        AnimatorControllerParameter[] newParams = animatorToMerge.parameters;

        Debug.Log("Found " + newParams.Length + " parameters in this animator");

        chilloutAnimatorController.parameters = GetParametersWithoutDupes(newParams, existingParams);

        AnimatorControllerLayer[] existingLayers = chilloutAnimatorController.layers;

        AnimatorControllerLayer[] layersToMerge = animatorToMerge.layers;

        Debug.Log("Found " + layersToMerge.Length + " layers to merge");

        // CVR breaks if any layer names are the same
        layersToMerge = FixDuplicateLayerNames(layersToMerge, existingLayers);

        AnimatorControllerLayer[] newLayers = new AnimatorControllerLayer[existingLayers.Length + layersToMerge.Length];

        int newLayersIdx = 0;

        for (int i = 0; i < existingLayers.Length; i++)
        {
            if (existingLayers[i].stateMachine.states.Length > 0) { // Do not copy empty layers
                newLayers[newLayersIdx] = existingLayers[i];
                newLayersIdx++;
            }
        }

        for (int i = 0; i < layersToMerge.Length; i++)
        {
            AnimatorControllerLayer layer = layersToMerge[i];

            if (layer.stateMachine.states.Length > 0) { // Do not copy empty layers
                Debug.Log("Layer \"" + layer.name + "\" with " + layer.stateMachine.states.Length + " states");

                ProcessStateMachine(layer.stateMachine);

                newLayers[newLayersIdx] = layer;
                newLayersIdx++;
            }
        }

        Array.Resize(ref newLayers, newLayersIdx);
        chilloutAnimatorController.layers = newLayers;

        Debug.Log("Merged");
    }

    AnimatorControllerLayer[] FixDuplicateLayerNames(AnimatorControllerLayer[] newLayers, AnimatorControllerLayer[] existingLayers) {
        foreach (AnimatorControllerLayer newLayer in newLayers) {
            foreach (AnimatorControllerLayer existingLayer in existingLayers) {
                if (existingLayer.name == newLayer.name) {
                    Debug.Log("Layer \"" + newLayer.name + "\" clashes with an existing layer, renaming...");

                    // TODO: This is fragile cause they could have another layer with the same name
                    // Maybe check again if it exists whenever we rename it
                    newLayer.name = newLayer.name + "_1";
                }
            }
        }

        return newLayers;
    }

    void CreateEmptyChilloutAnimator()
    {
        Debug.Log("Creating Chillout animator...");

        Debug.Log("Creating output directory...");

        AssetDatabase.Refresh();

        string pathInsideAssets = outputDirName + "/ChilloutVR_Gestures.controller";
        Directory.CreateDirectory(Application.dataPath + "/" + outputDirName);

        AssetDatabase.Refresh();

        Debug.Log("Copying base animator...");

        string pathToCreatedAnimator = Application.dataPath + "/" + pathInsideAssets;

        // ReplaceFile() doesn't actually replace for some reason so make sure there is none already there
        FileUtil.DeleteFileOrDirectory(pathToCreatedAnimator);

        AssetDatabase.Refresh();

        FileUtil.ReplaceFile(Application.dataPath + "/ABI.CCK/Animations/AvatarAnimator.controller", pathToCreatedAnimator);

        AssetDatabase.Refresh();

        Debug.Log("Loading animator...");

        chilloutAnimatorController = (AnimatorController)AssetDatabase.LoadAssetAtPath("Assets/" + pathInsideAssets, typeof(AnimatorController));

        if (chilloutAnimatorController == null)
        {
            throw new Exception("Failed to load the created animator!");
        }

        Debug.Log("Found number of layers: " + chilloutAnimatorController.layers.Length);

        if (chilloutAnimatorController.layers.Length != 4)
        {
            throw new Exception("Animator controller has unexpected number of layers: " + chilloutAnimatorController.layers.Length);
        }

        List<AnimatorControllerLayer> newLayers = new List<AnimatorControllerLayer>();

        string[] allowedLayerNames;

        if (shouldDeleteCvrHandLayers) {
            Debug.Log("Deleting CVR hand layers...");
            allowedLayerNames = new string[] { "Locomotion/Emotes" };
        } else {
            Debug.Log("Not deleting CVR hand layers...");
            allowedLayerNames = new string[] { "Locomotion/Emotes", "LeftHand", "RightHand" };
        }

        foreach (AnimatorControllerLayer layer in chilloutAnimatorController.layers) {
            if (Array.IndexOf(allowedLayerNames, layer.name) != -1) {
                newLayers.Add(layer);
            }
        }

        chilloutAnimatorController.layers = newLayers.ToArray();

        Debug.Log("Setting animator...");

        cvrAvatar.avatarSettings.baseController = chilloutAnimatorController;

        Debug.Log("Chillout animator created");

        EditorUtility.SetDirty(cvrAvatar);
        Repaint();
    }

    void GetValuesFromVrcAvatar()
    {
        Debug.Log("Getting values from VRC avatar component...");

        bodySkinnedMeshRenderer = vrcAvatarDescriptor.VisemeSkinnedMesh;

        if (bodySkinnedMeshRenderer == null) {
            Debug.LogWarning("Could not find viseme skinned mesh from VRC component");
        } else {
            Debug.Log("Body skinned mesh renderer: " + bodySkinnedMeshRenderer);
        }

        vrcViewPosition = vrcAvatarDescriptor.ViewPosition;

        if (vrcViewPosition == null){
            throw new Exception("Could not find view position from VRC component!");
        }

        Debug.Log("View position: " + vrcViewPosition);

        vrcVisemeBlendShapes = vrcAvatarDescriptor.VisemeBlendShapes;

        if (vrcViewPosition == null) {
            Debug.LogWarning("Could not find viseme blend shapes from VRC component");
        } else {
            if (vrcVisemeBlendShapes.Length == 0) {
                Debug.LogWarning("Found 0 viseme blend shapes from VRC component");
            } else {
                Debug.Log("Visemes: " + string.Join(", ", vrcVisemeBlendShapes));
            }
        }

        int[] eyelidsBlendshapes = vrcAvatarDescriptor.customEyeLookSettings.eyelidsBlendshapes;

        if (eyelidsBlendshapes.Length >= 1 && eyelidsBlendshapes[0] != -1) {
            if (bodySkinnedMeshRenderer != null) {
                int blinkBlendshapeIdx = eyelidsBlendshapes[0];
                Mesh mesh = bodySkinnedMeshRenderer.sharedMesh;

                if (blinkBlendshapeIdx > mesh.blendShapeCount) {
                    Debug.LogWarning("Could not use eyelid blendshape at index " + blinkBlendshapeIdx.ToString() + ": does not exist in mesh!");
                } else {
                    blinkBlendshapeName = mesh.GetBlendShapeName(blinkBlendshapeIdx);
                    Debug.Log("Blink blendshape: " + blinkBlendshapeName);
                }
            } else {
                Debug.LogWarning("Eyelid blendshapes are set but no skinned mesh renderer found");
            }
        } else {
            Debug.Log("No blink blendshape set");
        }

        VRCAvatarDescriptor.CustomAnimLayer[] vrcCustomAnimLayers = vrcAvatarDescriptor.baseAnimationLayers;
        vrcAnimatorControllers = new AnimatorController[vrcCustomAnimLayers.Length];

        for (int i = 0; i < vrcCustomAnimLayers.Length; i++)
        {
            vrcAnimatorControllers[i] = vrcCustomAnimLayers[i].animatorController as AnimatorController;
        }

        Debug.Log("Found number of vrc base animation layers: " + vrcAvatarDescriptor.baseAnimationLayers.Length);
    }

    SkinnedMeshRenderer GetSkinnedMeshRendererInCVRAvatar() {
        string pathToSkinnedMeshRenderer = GetPathToGameObjectInsideAvatar(bodySkinnedMeshRenderer.gameObject);

        Debug.Log("Path to body skinned mesh renderer: " + pathToSkinnedMeshRenderer);

        var match = cvrAvatar.transform.Find(pathToSkinnedMeshRenderer.Remove(0, 1));

        if (match == null) {
            Debug.LogWarning("Could not find body inside the CVR avatar");
            return null;
        }

        SkinnedMeshRenderer skinnedMeshRenderer = match.GetComponent<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer == null) {
            Debug.LogWarning("Could not find body skinned mesh renderer inside the CVR avatar");
            return null;
        }

        return skinnedMeshRenderer;
    }

    public static string GetPathToGameObjectInsideAvatar(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;

            if (obj.transform.parent != null) {
                path = "/" + obj.name + path;
            }
        }
        return path;
    }

    void PopulateChilloutComponent()
    {
        Debug.Log("Populating chillout avatar component...");

        if (bodySkinnedMeshRenderer != null) {
            Debug.Log("Setting face mesh...");

            cvrAvatar.bodyMesh = GetSkinnedMeshRendererInCVRAvatar();
        } else {
            Debug.Log("No body skinned mesh renderer found so not setting CVR body mesh");
        }

        Debug.Log("Setting blinking...");

        if (string.IsNullOrEmpty(blinkBlendshapeName) == false) {
            cvrAvatar.useBlinkBlendshapes = true;
            cvrAvatar.blinkBlendshape[0] = blinkBlendshapeName;
        } else {
            Debug.LogWarning("Cannot set blink: no blendshapes found");
        }

        Debug.Log("Setting visemes...");

        cvrAvatar.useVisemeLipsync = true;

        for (int i = 0; i < vrcVisemeBlendShapes.Length; i++)
        {
            cvrAvatar.visemeBlendshapes[i] = vrcVisemeBlendShapes[i];
        }

        Debug.Log("Setting view and voice position...");

        cvrAvatar.viewPosition = vrcViewPosition;
        cvrAvatar.voicePosition = vrcViewPosition;

        Debug.Log("Enabling advanced avatar settings...");

        cvrAvatar.avatarUsesAdvancedSettings = true;

        // there is a slight delay before this happens which makes our script not work
        cvrAvatar.avatarSettings = new CVRAdvancedAvatarSettings();
        cvrAvatar.avatarSettings.settings = new List<CVRAdvancedSettingsEntry>();
        cvrAvatar.avatarSettings.initialized = true;

        EditorUtility.SetDirty(cvrAvatar);
        Repaint();

        Debug.Log("Finished populating chillout component");
    }

    void CreateChilloutComponentIfNeeded()
    {
        cvrAvatar = chilloutAvatarGameObject.GetComponent<CVRAvatar>();

        if (cvrAvatar != null)
        {
            Debug.Log("Avatar has a CVRAvatar, skipping...");
            return;
        }

        Debug.Log("Avatar does not have a CVRAvatar, adding...");

        cvrAvatar = chilloutAvatarGameObject.AddComponent<CVRAvatar>() as CVRAvatar;

        Debug.Log("CVRAvatar component added");

        Repaint();
    }
}
