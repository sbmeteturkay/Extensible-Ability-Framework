#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CaseStudy.Feature.AbilitySystem.Data;
using UnityEditor;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Editor
{
    [CustomEditor(typeof(AbilityDataSO))]
    public sealed class AbilityDataSOEditor : UnityEditor.Editor
    {
        private static readonly GUIContent CREATE_CONFIG_LABEL = new("Create Config");
        private static readonly GUIContent REPLACE_CONFIG_LABEL = new("Replace Config");
        private static readonly GUIContent REMOVE_LABEL = new("Remove");
        private static readonly GUIContent PING_LABEL = new("Ping");

        private SerializedProperty _displayNameProperty;
        private SerializedProperty _iconProperty;
        private SerializedProperty _cooldownSecondsProperty;
        private SerializedProperty _energyCostProperty;
        private SerializedProperty _targetGroupsProperty;
        private SerializedProperty _movementPolicyProperty;
        private SerializedProperty _minimumMovementLockDurationSecondsProperty;
        private SerializedProperty _executorProperty;
        private SerializedProperty _mechanicConfigProperty;
        private SerializedProperty _optionalModulesProperty;

        private readonly Dictionary<int, UnityEditor.Editor> _inlineEditors = new();

        private void OnEnable()
        {
            _displayNameProperty = serializedObject.FindProperty("_displayName");
            _iconProperty = serializedObject.FindProperty("_icon");
            _cooldownSecondsProperty = serializedObject.FindProperty("_cooldownSeconds");
            _energyCostProperty = serializedObject.FindProperty("_energyCost");
            _targetGroupsProperty = serializedObject.FindProperty("_targetGroups");
            _movementPolicyProperty = serializedObject.FindProperty("_movementPolicy");
            _minimumMovementLockDurationSecondsProperty = serializedObject.FindProperty("_minimumMovementLockDurationSeconds");
            _executorProperty = serializedObject.FindProperty("_executor");
            _mechanicConfigProperty = serializedObject.FindProperty("_mechanicConfig");
            _optionalModulesProperty = serializedObject.FindProperty("_optionalModules");
        }

        private void OnDisable()
        {
            ClearInlineEditors();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCommonSection();
            EditorGUILayout.Space(8f);

            DrawExecutionSection();
            EditorGUILayout.Space(8f);

            DrawOptionalModulesSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCommonSection()
        {
            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_displayNameProperty);
            EditorGUILayout.PropertyField(_iconProperty);
            EditorGUILayout.PropertyField(_cooldownSecondsProperty);
            EditorGUILayout.PropertyField(_energyCostProperty);
            EditorGUILayout.PropertyField(_targetGroupsProperty);
            EditorGUILayout.PropertyField(_movementPolicyProperty);
            EditorGUILayout.PropertyField(_minimumMovementLockDurationSecondsProperty);
        }

        private void DrawExecutionSection()
        {
            EditorGUILayout.LabelField("Execution", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_executorProperty);

            AbilityExecutorSO executor = _executorProperty.objectReferenceValue as AbilityExecutorSO;
            Type requiredType = ResolveRequiredMechanicConfigType(executor);
            ScriptableObject currentConfig = _mechanicConfigProperty.objectReferenceValue as ScriptableObject;

            EditorGUILayout.PropertyField(_mechanicConfigProperty, new GUIContent("Mechanic Config"));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent createLabel = currentConfig == null ? CREATE_CONFIG_LABEL : REPLACE_CONFIG_LABEL;
                if (GUILayout.Button(createLabel))
                {
                    ShowCreateMechanicConfigMenu(requiredType);
                }

                using (new EditorGUI.DisabledScope(currentConfig == null))
                {
                    if (GUILayout.Button(REMOVE_LABEL))
                    {
                        RemoveObjectReference(_mechanicConfigProperty, destroySubAsset: true);
                    }

                    if (GUILayout.Button(PING_LABEL))
                    {
                        EditorGUIUtility.PingObject(currentConfig);
                    }
                }
            }

            if (executor == null)
            {
                EditorGUILayout.HelpBox("Executor secildiginde gerekli mechanic config tipi otomatik yonetilir.", MessageType.Info);
            }
            else if (currentConfig == null)
            {
                EditorGUILayout.HelpBox($"'{executor.name}' icin mechanic config gerekli. Create Config ile ekleyebilirsin.", MessageType.Warning);
            }
            else if (requiredType != null && !requiredType.IsAssignableFrom(currentConfig.GetType()))
            {
                EditorGUILayout.HelpBox(
                    $"Secili config tipi executor ile uyumlu degil. Beklenen: {requiredType.Name}, mevcut: {currentConfig.GetType().Name}.",
                    MessageType.Error);
            }

            DrawInlineEditor(currentConfig);
        }

        private void DrawOptionalModulesSection()
        {
            EditorGUILayout.LabelField("Optional Modules", EditorStyles.boldLabel);
            DrawSubAssetList(
                _optionalModulesProperty,
                typeof(AbilityOptionalModuleSO),
                uniqueByType: true,
                emptyMessage: "Bu liste executor'dan bagimsiz, tak-cikar eklenti verilerini tutar.");
        }

        private void DrawSubAssetList(SerializedProperty listProperty, Type baseType, bool uniqueByType, string emptyMessage)
        {
            if (listProperty == null)
            {
                return;
            }

            int count = listProperty.arraySize;
            if (count == 0)
            {
                EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
            }

            for (int i = 0; i < count; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                ScriptableObject entry = element.objectReferenceValue as ScriptableObject;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string entryLabel = entry != null ? entry.GetType().Name : $"Element {i}";
                        EditorGUILayout.LabelField(entryLabel, EditorStyles.boldLabel);

                        if (GUILayout.Button(REMOVE_LABEL, GUILayout.Width(72f)))
                        {
                            RemoveListElement(listProperty, i, entry);
                            return;
                        }
                    }

                    EditorGUILayout.PropertyField(element, GUIContent.none);
                    DrawInlineEditor(entry);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(96f)))
                {
                    ShowAddSubAssetMenu(listProperty, baseType, uniqueByType);
                }
            }
        }

        private void ShowCreateMechanicConfigMenu(Type requiredType)
        {
            if (requiredType == null)
            {
                requiredType = typeof(AbilityMechanicConfigSO);
            }

            GenericMenu menu = new GenericMenu();
            List<Type> options = GetCreatableTypes(requiredType);

            if (options.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No mechanic config type available"));
            }
            else
            {
                for (int i = 0; i < options.Count; i++)
                {
                    Type optionType = options[i];
                    menu.AddItem(new GUIContent(optionType.Name), false, () => AssignNewSubAsset(_mechanicConfigProperty, optionType));
                }
            }

            menu.ShowAsContext();
        }

        private void ShowAddSubAssetMenu(SerializedProperty listProperty, Type baseType, bool uniqueByType)
        {
            GenericMenu menu = new GenericMenu();
            List<Type> options = GetCreatableTypes(baseType);
            HashSet<Type> existingTypes = uniqueByType ? CollectExistingTypes(listProperty) : null;

            if (options.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No type available"));
                menu.ShowAsContext();
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                Type optionType = options[i];
                bool alreadyExists = uniqueByType && existingTypes != null && existingTypes.Contains(optionType);
                GUIContent label = new GUIContent(optionType.Name);

                if (alreadyExists)
                {
                    menu.AddDisabledItem(label);
                    continue;
                }

                menu.AddItem(label, false, () => AddNewSubAssetToList(listProperty, optionType));
            }

            menu.ShowAsContext();
        }

        private void AssignNewSubAsset(SerializedProperty property, Type subAssetType)
        {
            if (property == null || subAssetType == null)
            {
                return;
            }

            RemoveObjectReference(property, destroySubAsset: true, applyAfter: false);

            ScriptableObject created = CreateSubAsset(subAssetType);
            if (created == null)
            {
                return;
            }

            property.objectReferenceValue = created;
            serializedObject.ApplyModifiedProperties();
            MarkAbilityDirty();
        }

        private void AddNewSubAssetToList(SerializedProperty listProperty, Type subAssetType)
        {
            if (listProperty == null || subAssetType == null)
            {
                return;
            }

            ScriptableObject created = CreateSubAsset(subAssetType);
            if (created == null)
            {
                return;
            }

            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            element.objectReferenceValue = created;

            serializedObject.ApplyModifiedProperties();
            MarkAbilityDirty();
        }

        private void RemoveListElement(SerializedProperty listProperty, int index, ScriptableObject objectToRemove)
        {
            if (listProperty == null || index < 0 || index >= listProperty.arraySize)
            {
                return;
            }

            if (objectToRemove != null && IsSubAssetOfCurrentAbility(objectToRemove))
            {
                Undo.DestroyObjectImmediate(objectToRemove);
            }

            DeleteArrayElementAtIndex(listProperty, index);
            serializedObject.ApplyModifiedProperties();
            MarkAbilityDirty();
        }

        private void RemoveObjectReference(SerializedProperty property, bool destroySubAsset, bool applyAfter = true)
        {
            if (property == null)
            {
                return;
            }

            if (destroySubAsset)
            {
                ScriptableObject referenced = property.objectReferenceValue as ScriptableObject;
                if (referenced != null && IsSubAssetOfCurrentAbility(referenced))
                {
                    Undo.DestroyObjectImmediate(referenced);
                }
            }

            property.objectReferenceValue = null;

            if (applyAfter)
            {
                serializedObject.ApplyModifiedProperties();
                MarkAbilityDirty();
            }
        }

        private void DrawInlineEditor(ScriptableObject targetObject)
        {
            if (targetObject == null)
            {
                return;
            }

            int instanceId = targetObject.GetInstanceID();
            if (!_inlineEditors.TryGetValue(instanceId, out UnityEditor.Editor inlineEditor)
                || inlineEditor == null
                || inlineEditor.target != targetObject)
            {
                UnityEditor.Editor.CreateCachedEditor(targetObject, null, ref inlineEditor);
                _inlineEditors[instanceId] = inlineEditor;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                inlineEditor?.OnInspectorGUI();
            }
        }

        private ScriptableObject CreateSubAsset(Type type)
        {
            AbilityDataSO abilityData = target as AbilityDataSO;
            if (abilityData == null)
            {
                return null;
            }

            string abilityPath = AssetDatabase.GetAssetPath(abilityData);
            if (string.IsNullOrWhiteSpace(abilityPath))
            {
                Debug.LogError("AbilityDataSO must be saved as an asset before creating embedded configs/modules.");
                return null;
            }

            ScriptableObject created = ScriptableObject.CreateInstance(type);
            if (created == null)
            {
                return null;
            }

            created.name = type.Name;
            Undo.RegisterCreatedObjectUndo(created, $"Create {type.Name}");
            AssetDatabase.AddObjectToAsset(created, abilityPath);
            AssetDatabase.ImportAsset(abilityPath);
            return created;
        }

        private bool IsSubAssetOfCurrentAbility(UnityEngine.Object candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            AbilityDataSO abilityData = target as AbilityDataSO;
            if (abilityData == null)
            {
                return false;
            }

            string abilityPath = AssetDatabase.GetAssetPath(abilityData);
            if (string.IsNullOrWhiteSpace(abilityPath))
            {
                return false;
            }

            if (!string.Equals(AssetDatabase.GetAssetPath(candidate), abilityPath, StringComparison.Ordinal))
            {
                return false;
            }

            return !AssetDatabase.IsMainAsset(candidate);
        }

        private static void DeleteArrayElementAtIndex(SerializedProperty listProperty, int index)
        {
            int beforeCount = listProperty.arraySize;
            listProperty.DeleteArrayElementAtIndex(index);
            if (listProperty.arraySize == beforeCount)
            {
                listProperty.DeleteArrayElementAtIndex(index);
            }
        }

        private static Type ResolveRequiredMechanicConfigType(AbilityExecutorSO executor)
        {
            if (executor == null)
            {
                return typeof(AbilityMechanicConfigSO);
            }

            Type requiredType = executor.RequiredMechanicConfigType;
            if (requiredType == null || !typeof(AbilityMechanicConfigSO).IsAssignableFrom(requiredType))
            {
                return typeof(AbilityMechanicConfigSO);
            }

            return requiredType;
        }

        private static List<Type> GetCreatableTypes(Type baseType)
        {
            if (baseType == null)
            {
                return new List<Type>();
            }

            if (!baseType.IsAbstract)
            {
                return new List<Type> { baseType };
            }

            return TypeCache.GetTypesDerivedFrom(baseType)
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(type => type.Name)
                .ToList();
        }

        private static HashSet<Type> CollectExistingTypes(SerializedProperty listProperty)
        {
            var types = new HashSet<Type>();
            if (listProperty == null)
            {
                return types;
            }

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                UnityEngine.Object entry = listProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (entry != null)
                {
                    types.Add(entry.GetType());
                }
            }

            return types;
        }

        private void MarkAbilityDirty()
        {
            AbilityDataSO abilityData = target as AbilityDataSO;
            if (abilityData == null)
            {
                return;
            }

            EditorUtility.SetDirty(abilityData);
            AssetDatabase.SaveAssets();
        }

        private void ClearInlineEditors()
        {
            foreach (KeyValuePair<int, UnityEditor.Editor> pair in _inlineEditors)
            {
                if (pair.Value != null)
                {
                    DestroyImmediate(pair.Value);
                }
            }

            _inlineEditors.Clear();
        }
    }
}
#endif
