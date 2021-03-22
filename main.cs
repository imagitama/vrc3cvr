#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
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

public class VRC_Chillout_Converter : EditorWindow
{
    bool isConverting = false;
    VRCAvatarDescriptor vrcAvatarDescriptor;
    CVRAvatar cvrAvatar;
    SkinnedMeshRenderer bodySkinnedMeshRenderer;
    Vector3 vrcViewPosition;
    string[] vrcVisemeBlendShapes;
    string blinkBlendshapeName;
    AnimatorController chilloutAnimatorController;
    AnimatorController[] vrcAnimatorControllers;
    string outputDirName = "VRC_Chillout_Converter_Output";
    bool shouldDeleteVrcComponents = true;

    [MenuItem("PeanutTools/VRC Chillout Converter _%#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<VRC_Chillout_Converter>();
        window.titleContent = new GUIContent("VRC Chillout Converter");
        window.minSize = new Vector2(250, 50);
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Select your VRChat avatar and click Convert to convert it to ChilloutVR");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("Please ensure you are in a new scene or Unity project to avoid deleting your VRChat components");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        vrcAvatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", vrcAvatarDescriptor, typeof(VRCAvatarDescriptor));
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        shouldDeleteVrcComponents = GUILayout.Toggle(shouldDeleteVrcComponents, "Delete VRChat components after");

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Convert") && GetIsReadyForConvert())
        {
            Convert();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        GUILayout.Label("https://github.com/imagitama/vrc3sdk");
        GUILayout.Label("https://twitter.com/@HiPeanutBuddha");
        GUILayout.Label("Peanut#1756");

    }

    bool GetIsReadyForConvert()
    {
        return vrcAvatarDescriptor != null;
    }

    void Convert()
    {
        if (isConverting == true)
        {
            Debug.Log("Cannot convert - already in progress");
        }

        isConverting = true;

        Debug.Log("Starting to convert...");

        GetValuesFromVrcAvatar();
        CreateChilloutComponentIfNeeded();
        PopulateChilloutComponent();
        CreateEmptyChilloutAnimator();
        MergeVrcAnimatorsIntoChilloutAnimator();
        ConvertVrcParametersToChillout();
        // BuildChilloutAnimatorWithParams();
        InsertChilloutOverride();

        if (shouldDeleteVrcComponents)
        {
            DeleteVrcComponents();
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
        Debug.Log("Deleting vrc components...");

        VRC.Core.PipelineManager pipelineManager = vrcAvatarDescriptor.gameObject.GetComponent<VRC.Core.PipelineManager>();

        if (pipelineManager == null)
        {
            throw new Exception("Cannot delete pipeline manager: cannot find it!");
        }

        DestroyImmediate(vrcAvatarDescriptor);
        DestroyImmediate(pipelineManager);

        Debug.Log("Vrc components deleted");
    }

    void ConvertVrcParametersToChillout()
    {
        Debug.Log("Converting vrc parameters to chillout...");

        VRCExpressionParameters vrcParams = vrcAvatarDescriptor.expressionParameters;

        List<CVRAdvancedSettingsEntry> newParams = new List<CVRAdvancedSettingsEntry>();

        for (int i = 0; i < vrcParams.parameters.Length; i++)
        {
            VRCExpressionParameter vrcParam = vrcParams.parameters[i];

            Debug.Log("Param \"" + vrcParam.name + "\" type \"" + vrcParam.valueType + "\" default \"" + vrcParam.defaultValue + "\"");

            CVRAdvancedSettingsEntry newParam = new CVRAdvancedSettingsEntry()
            {
                name = vrcParam.name,
                machineName = vrcParam.name,
                type = CVRAdvancedSettingsEntry.SettingsType.Slider,
                setting = new CVRAdvancesAvatarSettingSlider()
            };

            switch (vrcParam.valueType)
            {
                case VRCExpressionParameters.ValueType.Int:
                    ((CVRAdvancesAvatarSettingSlider)newParam.setting).defaultValue = vrcParam.defaultValue;
                    break;
                case VRCExpressionParameters.ValueType.Float:
                    ((CVRAdvancesAvatarSettingSlider)newParam.setting).defaultValue = vrcParam.defaultValue;
                    break;
                case VRCExpressionParameters.ValueType.Bool:
                    ((CVRAdvancesAvatarSettingSlider)newParam.setting).defaultValue = vrcParam.defaultValue != 0 ? 1 : 0;
                    break;
                default:
                    throw new Exception("Cannot convert vrc parameter to chillout: unknown type \"" + vrcParam.valueType + "\"");
            }

            newParams.Add(newParam);
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
            // fist
            case 1:
                return 1;
            // open hand
            case 2:
                return 0;
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

            if (doesAlreadyExist == false)
            {
                finalParams.Add(newParams[x]);
            }
        }

        return finalParams.ToArray();
    }

    void ProcessTransitions(AnimatorStateTransition[] transitions)
    {
        for (int t = 0; t < transitions.Length; t++)
        {
            List<AnimatorCondition> conditionsToAdd = new List<AnimatorCondition>();

            // Debug.Log(transitions[t].conditions.Length + " conditions");

            for (int c = 0; c < transitions[t].conditions.Length; c++)
            {
                AnimatorCondition condition = transitions[t].conditions[c];

                Debug.Log("CHECK " + condition.parameter + " " + condition.mode + " " + condition.threshold);

                if (condition.mode == AnimatorConditionMode.Equals)
                {
                    // no expression in vrchat
                    if (condition.threshold == 0)
                    {
                        AnimatorCondition newConditionNoGesture = new AnimatorCondition();
                        newConditionNoGesture.parameter = condition.parameter;
                        newConditionNoGesture.mode = AnimatorConditionMode.Less;
                        newConditionNoGesture.threshold = (float)-0.9;

                        conditionsToAdd.Add(newConditionNoGesture);
                        continue;
                    }

                    float chilloutGestureNumber = GetChilloutGestureNumberForVrchatGestureNumber(condition.threshold);

                    AnimatorCondition newConditionLessThan = new AnimatorCondition();
                    newConditionLessThan.parameter = condition.parameter;
                    newConditionLessThan.mode = AnimatorConditionMode.Less;
                    newConditionLessThan.threshold = (float)(chilloutGestureNumber + 0.1);

                    conditionsToAdd.Add(newConditionLessThan);

                    AnimatorCondition newConditionGreaterThan = new AnimatorCondition();
                    newConditionGreaterThan.parameter = condition.parameter;
                    newConditionGreaterThan.mode = AnimatorConditionMode.Greater;
                    newConditionGreaterThan.threshold = (float)(chilloutGestureNumber - 0.1);

                    conditionsToAdd.Add(newConditionGreaterThan);
                }
                else if (condition.mode == AnimatorConditionMode.NotEqual) {
                    float chilloutGestureNumber = GetChilloutGestureNumberForVrchatGestureNumber(condition.threshold);

                    AnimatorCondition newConditionLessThan = new AnimatorCondition();
                    newConditionLessThan.parameter = condition.parameter;
                    newConditionLessThan.mode = AnimatorConditionMode.Less;
                    newConditionLessThan.threshold = (float)(chilloutGestureNumber - 0.1);

                    conditionsToAdd.Add(newConditionLessThan);

                    // TODO: Add transition with another condition for greater than the value
                }
                else if (condition.mode == AnimatorConditionMode.If)
                {
                    AnimatorCondition newConditionLessThan = new AnimatorCondition();
                    newConditionLessThan.parameter = condition.parameter;
                    newConditionLessThan.mode = AnimatorConditionMode.Less;
                    newConditionLessThan.threshold = (float)1.1;

                    conditionsToAdd.Add(newConditionLessThan);

                    AnimatorCondition newConditionGreaterThan = new AnimatorCondition();
                    newConditionGreaterThan.parameter = condition.parameter;
                    newConditionGreaterThan.mode = AnimatorConditionMode.Greater;
                    newConditionGreaterThan.threshold = (float)0.5;

                    conditionsToAdd.Add(newConditionGreaterThan);
                }
                else if (condition.mode == AnimatorConditionMode.IfNot)
                {
                    AnimatorCondition newConditionLessThan = new AnimatorCondition();
                    newConditionLessThan.parameter = condition.parameter;
                    newConditionLessThan.mode = AnimatorConditionMode.Less;
                    newConditionLessThan.threshold = (float)0.49;

                    conditionsToAdd.Add(newConditionLessThan);

                    AnimatorCondition newConditionGreaterThan = new AnimatorCondition();
                    newConditionGreaterThan.parameter = condition.parameter;
                    newConditionGreaterThan.mode = AnimatorConditionMode.Greater;
                    newConditionGreaterThan.threshold = (float)-0.1;

                    conditionsToAdd.Add(newConditionGreaterThan);
                }
            }

            transitions[t].conditions = conditionsToAdd.ToArray();
        }
    }

    void ProcessStateMachine(AnimatorStateMachine stateMachine)
    {
        for (int s = 0; s < stateMachine.states.Length; s++)
        {
            // Debug.Log(stateMachine.states[s].state.transitions.Length + " transitions");

            ProcessTransitions(stateMachine.states[s].state.transitions);
        }

        ProcessTransitions(stateMachine.anyStateTransitions);

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

        for (int p = 0; p < newParams.Length; p++) {
            newParams[p].type = AnimatorControllerParameterType.Float;
        }

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
            newLayers[newLayersIdx] = existingLayers[i];
            newLayersIdx++;
        }

        for (int i = 0; i < layersToMerge.Length; i++)
        {
            AnimatorControllerLayer layer = layersToMerge[i];

            Debug.Log("Layer \"" + layer.name + "\" with " + layer.stateMachine.states.Length + " states");

            ProcessStateMachine(layer.stateMachine);

            newLayers[newLayersIdx] = layer;
            newLayersIdx++;
        }

        chilloutAnimatorController.layers = newLayers;

        // PurgeAnimator(animatorToMerge);

        Debug.Log("Merged");
    }

    AnimatorControllerLayer[] FixDuplicateLayerNames(AnimatorControllerLayer[] newLayers, AnimatorControllerLayer[] existingLayers) {
        foreach (AnimatorControllerLayer newLayer in newLayers) {
            foreach (AnimatorControllerLayer existingLayer in existingLayers) {
                if (existingLayer.name == newLayer.name) {
                    Debug.Log("Layer \"" + newLayer.name + \" clashes with an existing layer, renaming...");

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

        AnimatorControllerLayer[] newLayers = new AnimatorControllerLayer[] { chilloutAnimatorController.layers[0] };

        chilloutAnimatorController.layers = newLayers;

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

        if (bodySkinnedMeshRenderer == null)
        {
            throw new Exception("Could not find viseme skinned mesh from VRC component!");
        }

        Debug.Log("Body skinned mesh renderer: " + bodySkinnedMeshRenderer);

        vrcViewPosition = vrcAvatarDescriptor.ViewPosition;

        if (vrcViewPosition == null)
        {
            throw new Exception("Could not find view position from VRC component!");
        }

        Debug.Log("View position: " + vrcViewPosition);

        vrcVisemeBlendShapes = vrcAvatarDescriptor.VisemeBlendShapes;

        if (vrcViewPosition == null)
        {
            throw new Exception("Could not find viseme blend shapes from VRC component!");
        }

        Debug.Log("Found number of visemes: " + vrcVisemeBlendShapes.Length);

        if (vrcVisemeBlendShapes.Length == 0)
        {
            throw new Exception("Found 0 blend shapes from VRC component!");
        }

        Debug.Log("Visemes: " + string.Join(", ", vrcVisemeBlendShapes));

        int blinkBlendshapeIdx = vrcAvatarDescriptor.customEyeLookSettings.eyelidsBlendshapes[0];
        Mesh mesh = bodySkinnedMeshRenderer.sharedMesh;

        blinkBlendshapeName = mesh.GetBlendShapeName(blinkBlendshapeIdx);

        Debug.Log("Blink blendshape: " + blinkBlendshapeName);

        VRCAvatarDescriptor.CustomAnimLayer[] vrcCustomAnimLayers = vrcAvatarDescriptor.baseAnimationLayers;
        vrcAnimatorControllers = new AnimatorController[vrcCustomAnimLayers.Length];

        for (int i = 0; i < vrcCustomAnimLayers.Length; i++)
        {
            vrcAnimatorControllers[i] = vrcCustomAnimLayers[i].animatorController as AnimatorController;
        }

        Debug.Log("Found number of vrc base animation layers: " + vrcAvatarDescriptor.baseAnimationLayers.Length);

        Debug.Log("Gotten all values from VRC avatar component");
    }

    void PopulateChilloutComponent()
    {
        Debug.Log("Populating chillout avatar component...");

        Debug.Log("Setting face mesh...");

        cvrAvatar.bodyMesh = bodySkinnedMeshRenderer;

        Debug.Log("Setting blinking...");

        cvrAvatar.useBlinkBlendshapes = true;
        cvrAvatar.blinkBlendshape[0] = blinkBlendshapeName;

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
        // cvrAvatar.avatarSettings.baseController = chilloutAnimatorController;
        // cvrAvatar.avatarSettings.baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guids[0]));
        cvrAvatar.avatarSettings.settings = new List<CVRAdvancedSettingsEntry>();
        cvrAvatar.avatarSettings.initialized = true;

        EditorUtility.SetDirty(cvrAvatar);
        Repaint();

        Debug.Log("Finished populating chillout component");
    }

    void CreateChilloutComponentIfNeeded()
    {
        cvrAvatar = vrcAvatarDescriptor.gameObject.GetComponent<CVRAvatar>();

        if (cvrAvatar != null)
        {
            Debug.Log("Avatar has a CVRAvatar, skipping...");
            return;
        }

        Debug.Log("Avatar does not have a CVRAvatar, adding...");

        cvrAvatar = vrcAvatarDescriptor.gameObject.AddComponent<CVRAvatar>() as CVRAvatar;

        Debug.Log("CVRAvatar component added");

        Repaint();
    }
}

#endif