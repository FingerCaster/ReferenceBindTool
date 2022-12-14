using System;
using System.Collections.Generic;
using System.Linq;
using ReferenceBindTool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReferenceBindTool.Editor
{
    [CustomEditor(typeof(ReferenceBindComponent))]
    public class ReferenceBindComponentEditor : UnityEditor.Editor
    {
        private enum InitError
        {
            None,
            NotExistSettingData,
            Other
        }

        private ReferenceBindComponent m_Target;
        private Page m_Page;
        private SerializedProperty m_Searchable;
        private CodeGeneratorSettingConfig m_CodeGeneratorSettingConfig;
        private bool m_SettingDataExpanded = true;
        private int m_LastSettingDataNameIndex;
        private bool m_SettingDataError;

        private RuleHelperInfo<IBindComponentsRuleHelper> m_BindComponentsRuleHelperInfo;
        private RuleHelperInfo<IBindAssetOrPrefabRuleHelper> m_BindAssetOrPrefabRuleHelperInfo;
        private RuleHelperInfo<ICodeGeneratorRuleHelper> m_CodeGeneratorRuleHelperInfo;

        // private bool m_IsInitError = false;
        private InitError m_InitError = InitError.None;

        private void OnEnable()
        {
            try
            {
                m_Target = (ReferenceBindComponent)target;
                m_Page = new Page(10, m_Target.GetAllBindObjectsCount());
                if (!CheckCodeGeneratorSettingData())
                {
                    m_InitError = InitError.NotExistSettingData;
                }

                if (m_InitError != InitError.NotExistSettingData)
                {
                    InitSearchable();
                }
                InitHelperInfos();
                m_Target.SetClassName(string.IsNullOrEmpty(m_Target.GeneratorCodeName)
                    ? m_Target.gameObject.name
                    : m_Target.GeneratorCodeName);
                serializedObject.ApplyModifiedProperties();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                m_InitError = InitError.Other;
            }
        }


        public override void OnInspectorGUI()
        {
            if (m_InitError == InitError.NotExistSettingData)
            {
                if (CheckCodeGeneratorSettingData(false))
                {
                    m_InitError = InitError.None;
                    InitSearchable();
                }
            }
            
            if (m_InitError != InitError.None)
            { 
                return;
            }

            if (m_IsCompiling && !EditorApplication.isCompiling)
            {
                m_IsCompiling = false;
                OnCompileComplete();
            }
            else if (!m_IsCompiling && EditorApplication.isCompiling)
            {
                m_IsCompiling = true;
            }

            serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawTopButton();
                EditorGUILayout.Space();
                DrawHelperSelect();
                EditorGUILayout.Space();
                DrawBindAssetOrPrefab();
                EditorGUILayout.Space();
                DrawSetting();
                EditorGUILayout.Space();
                DrawBindObjects();
                m_Page.SetAllCount(m_Target.GetAllBindObjectsCount());
                m_Page.Draw();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region ???????????????

        private bool m_IsCompiling = false;

        private void InitHelperInfos()
        {
            m_Target.SetBindAssetOrPrefabRuleHelperTypeName(
                string.IsNullOrEmpty(m_Target.BindAssetOrPrefabRuleHelperTypeName)
                    ? typeof(DefaultBindAssetOrPrefabRuleHelper).FullName
                    : m_Target.BindAssetOrPrefabRuleHelperTypeName);

            m_BindAssetOrPrefabRuleHelperInfo =
                new RuleHelperInfo<IBindAssetOrPrefabRuleHelper>("m_BindAssetOrPrefabRule", null);

            m_BindAssetOrPrefabRuleHelperInfo.Init(m_Target.BindAssetOrPrefabRuleHelperTypeName, typeName =>
            {
                m_Target.SetBindAssetOrPrefabRuleHelperTypeName(typeName);
                return m_Target.BindAssetOrPrefabRuleHelperTypeName;
            });

            m_Target.SetBindComponentsRuleHelperTypeName(string.IsNullOrEmpty(m_Target.BindComponentsRuleHelperTypeName)
                ? typeof(DefaultBindComponentsRuleHelper).FullName
                : m_Target.BindComponentsRuleHelperTypeName);

            m_BindComponentsRuleHelperInfo = new RuleHelperInfo<IBindComponentsRuleHelper>("m_BindComponentsRule", null);

            m_BindComponentsRuleHelperInfo.Init(m_Target.BindComponentsRuleHelperTypeName, typeName =>
            {
                m_Target.SetBindComponentsRuleHelperTypeName(typeName);
                return m_Target.BindComponentsRuleHelperTypeName;
            });
            m_Target.SetCodeGeneratorRuleHelperTypeName(string.IsNullOrEmpty(m_Target.CodeGeneratorRuleHelperTypeName)
                ? typeof(DefaultCodeGeneratorRuleHelper).FullName
                : m_Target.CodeGeneratorRuleHelperTypeName);

            m_CodeGeneratorRuleHelperInfo = new RuleHelperInfo<ICodeGeneratorRuleHelper>("m_CodeGeneratorRule", new[]
            {
                typeof(TransformFindCodeGeneratorRuleHelper).FullName
            });

            m_CodeGeneratorRuleHelperInfo.Init(m_Target.CodeGeneratorRuleHelperTypeName, typeName =>
            {
                m_Target.SetCodeGeneratorRuleHelperTypeName(typeName);
                return m_Target.CodeGeneratorRuleHelperTypeName;
            });

            RefreshHelperTypeNames();
        }

        void OnCompileComplete()
        {
            RefreshHelperTypeNames();
        }

        void RefreshHelperTypeNames()
        {
            m_BindAssetOrPrefabRuleHelperInfo.Refresh();
            m_BindComponentsRuleHelperInfo.Refresh();
            m_CodeGeneratorRuleHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// ????????????????????????
        /// </summary>
        private void DrawHelperSelect()
        {
            m_BindAssetOrPrefabRuleHelperInfo.Draw();
            m_BindComponentsRuleHelperInfo.Draw();
            m_CodeGeneratorRuleHelperInfo.Draw();
        }

        #endregion

        #region ?????????

        /// <summary>
        /// ???????????????????????????????????????
        /// </summary>
        private void InitSearchable()
        {
            if (m_Target.CodeGeneratorSettingData == null || m_Target.CodeGeneratorSettingData.IsEmpty())
            {
                m_Target.SetSettingData(m_CodeGeneratorSettingConfig.Default);
                m_LastSettingDataNameIndex = 0;
            }
            else
            {
                int index = m_CodeGeneratorSettingConfig.GetSettingDataIndex(m_Target.CodeGeneratorSettingData.Name);
                if (index == -1)
                {
                    Debug.LogError(
                        $"??????????????????{m_Target.CodeGeneratorSettingData.Name}??????{nameof(CodeGeneratorSettingData)}");
                    m_SettingDataError = true;
                    return;
                }

                m_Target.SetSettingData(m_CodeGeneratorSettingConfig.GetSettingData(index));
                m_LastSettingDataNameIndex = index;
            }
            string [] settingDataNames = m_CodeGeneratorSettingConfig.GetAllSettingNames().ToArray();

            m_Searchable = serializedObject.FindProperty("m_SettingDataSearchable");
            m_Target.SetSearchable(settingDataNames, m_LastSettingDataNameIndex);
        }

        /// <summary>
        /// ???????????????
        /// </summary>
        private void DrawSetting()
        {
            m_SettingDataExpanded = EditorGUILayout.Foldout(m_SettingDataExpanded, "SettingData", true);

            if (!m_SettingDataExpanded)
            {
                return;
            }

            if (m_SettingDataError)
            {
                EditorGUILayout.HelpBox($"??????????????????{m_Target.CodeGeneratorSettingData.Name}??????AutoBindSettingData",
                    MessageType.Error);
                if (!string.IsNullOrEmpty(m_Target.CodeGeneratorSettingData.Name))
                {
                    if (GUILayout.Button($"?????? {m_Target.CodeGeneratorSettingData.Name} ????????????"))
                    {
                        bool result =
                            ReferenceBindUtility.AddAutoBindSetting(m_Target.CodeGeneratorSettingData.Name, "", "");
                        if (!result)
                        {
                            EditorUtility.DisplayDialog("????????????", "???????????????????????????????????????????????????????????????", "??????");
                            return;
                        }

                        m_Target.SetSettingData(m_Target.CodeGeneratorSettingData.Name);
                        m_SettingDataError = false;
                    }
                }

                if (GUILayout.Button("??????????????????"))
                {
                    m_Target.SetSettingData(m_CodeGeneratorSettingConfig.Default);
                    m_SettingDataError = false;
                }

                return;
            }

            m_Searchable = m_Searchable ?? serializedObject.FindProperty("m_SettingDataSearchable");
            EditorGUILayout.PropertyField(m_Searchable);
            if (m_Target.SettingDataSearchable.Select != m_LastSettingDataNameIndex)
            {
                if (m_Target.SettingDataSearchable.Select >= m_CodeGeneratorSettingConfig.GetCount())
                {
                    m_SettingDataError = true;
                    return;
                }

                m_Target.SetSettingData(m_CodeGeneratorSettingConfig.GetSettingData(m_Target.SettingDataSearchable.Select));
                m_Target.SetClassName(string.IsNullOrEmpty(m_Target.GeneratorCodeName)
                    ? m_Target.gameObject.name
                    : m_Target.GeneratorCodeName);
                m_LastSettingDataNameIndex = m_Target.SettingDataSearchable.Select;
            }


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel("???????????????");
            EditorGUILayout.LabelField(m_Target.CodeGeneratorSettingData.Namespace);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            m_Target.SetClassName(EditorGUILayout.TextField(new GUIContent("?????????"), m_Target.GeneratorCodeName));

            if (GUILayout.Button("?????????"))
            {
                m_Target.SetClassName(m_Target.gameObject.name);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("?????????????????????");
            EditorGUILayout.LabelField(m_Target.CodeGeneratorSettingData.CodePath);
            EditorGUILayout.EndHorizontal();
            if (string.IsNullOrEmpty(m_Target.CodeGeneratorSettingData.CodePath))
            {
                EditorGUILayout.HelpBox("??????????????????????????????!", MessageType.Error);
            }
        }

        /// <summary>
        /// ??????????????????????????????
        /// </summary>
        /// <returns></returns>
        private bool CheckCodeGeneratorSettingData(bool isDebug = true)
        {
            string[] paths = AssetDatabase.FindAssets($"t:{nameof(CodeGeneratorSettingConfig)}");
            if (paths.Length == 0)
            {
                if (isDebug)
                {
                    Debug.LogError($"?????????{nameof(CodeGeneratorSettingConfig)}");
                }

                return false;
            }

            if (paths.Length > 1)
            {
                if (isDebug)
                {
                    Debug.LogError($"{nameof(CodeGeneratorSettingConfig)}????????????1");
                }

                return false;
            }

            string path = AssetDatabase.GUIDToAssetPath(paths[0]);
            m_CodeGeneratorSettingConfig = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettingConfig>(path);
            if (m_CodeGeneratorSettingConfig.GetCount() == 0)
            {
                if (isDebug)
                {
                    Debug.LogError($"?????????{nameof(CodeGeneratorSettingData)}");
                }
                return false;
            }

            return true;
        }

        #endregion

        #region ??????????????????

        /// <summary>
        /// ??????????????????
        /// </summary>
        private void DrawTopButton()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("??????"))
            {
                Sort();
            }

            if (GUILayout.Button("????????????"))
            {
                Refresh();
            }

            if (GUILayout.Button("?????????????????????"))
            {
                ResetAllFieldName();
            }

            if (GUILayout.Button("???????????????"))
            {
                RemoveNull();
            }

            if (GUILayout.Button("????????????"))
            {
                RemoveAll();
            }

            if (GUILayout.Button("????????????"))
            {
                RuleBindComponent();
            }

            if (GUILayout.Button("??????????????????"))
            {
                string className = !string.IsNullOrEmpty(m_Target.GeneratorCodeName)
                    ? m_Target.GeneratorCodeName
                    : m_Target.gameObject.name;


                var bindDataList = new List<ReferenceBindComponent.BindObjectData>(m_Target.GetAllBindObjectsCount());
                bindDataList.AddRange(m_Target.BindAssetsOrPrefabs);
                bindDataList.AddRange(m_Target.BindComponents);
                m_Target.GetCodeGeneratorRuleHelper().GeneratorCodeAndWriteToFile(bindDataList,
                    m_Target.CodeGeneratorSettingData.Namespace, className, m_Target.CodeGeneratorSettingData.CodePath,
                    null);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// ?????????????????????
        /// </summary>
        private void ResetAllFieldName()
        {
            m_Target.ResetAllFieldName();
        }

        /// <summary>
        /// ????????????
        /// </summary>
        private void Refresh()
        {
            m_Target.Refresh();
        }

        /// <summary>
        /// ??????
        /// </summary>
        private void Sort()
        {
            m_Target.Sort();
        }

        /// <summary>
        /// ????????????
        /// </summary>
        private void RemoveAll()
        {
            m_Target.RemoveAll();
        }

        /// <summary>
        /// ??????Missing Or Null
        /// </summary>
        private void RemoveNull()
        {
            m_Target.RemoveNull();
        }

        /// <summary>
        /// ??????????????????
        /// </summary>
        private void RuleBindComponent()
        {
            m_Target.RuleBindComponents();
        }

        #endregion

        #region ?????????????????????????????????

        private UnityEngine.Object m_NeedBindObject = null;

        /// <summary>
        /// ???????????????????????????????????????
        /// </summary>
        private void DrawBindAssetOrPrefab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("????????????????????????");
            m_NeedBindObject = EditorGUILayout.ObjectField(m_NeedBindObject, typeof(UnityEngine.Object), false);
            GUI.enabled = m_NeedBindObject != null;
            if (GUILayout.Button("??????", GUILayout.Width(50)))
            {
                m_Target.RuleBindAssetsOrPrefabs(
                    m_Target.GetBindAssetOrPrefabRuleHelper().GetDefaultFieldName(m_NeedBindObject),
                    m_NeedBindObject);
                m_NeedBindObject = null;
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region ??????????????????

        private void DrawBindObjects()
        {
            int bindAopNeedDeleteIndex = -1;
            int bindComNeedDeleteIndex = -1;

            int i = m_Page.CurrentPage * m_Page.ShowCount;
            int count = i + m_Page.ShowCount;

            if (count > m_Target.GetAllBindObjectsCount())
            {
                count = m_Target.GetAllBindObjectsCount();
            }

            if (count == 0)
            {
                return;
            }

            EditorGUILayout.BeginVertical();

            int bindAssetShowCount = m_Target.BindAssetsOrPrefabs.Count - i;

            if (bindAssetShowCount > 0)
            {
                EditorGUILayout.LabelField("???????????????????????????");
                for (; i < bindAssetShowCount; i++)
                {
                    if (DrawBindObjectData(m_Target.BindAssetsOrPrefabs[i], i))
                    {
                        bindAopNeedDeleteIndex = i;
                    }
                }
            }

            int bindComponentShowCount = count - i;
            if (bindComponentShowCount > 0)
            {
                EditorGUILayout.LabelField("???????????????");
                int index = i > m_Target.BindAssetsOrPrefabs.Count ? 0 : m_Target.BindAssetsOrPrefabs.Count - i;
                int startIndex = i - m_Target.BindAssetsOrPrefabs.Count;
                for (; index < bindComponentShowCount; index++, i++)
                {
                    if (DrawBindObjectData(m_Target.BindComponents[index + startIndex], i))
                    {
                        bindComNeedDeleteIndex = index;
                    }
                }
            }

            //??????data
            if (bindAopNeedDeleteIndex != -1)
            {
                m_Target.BindAssetsOrPrefabs.RemoveAt(bindAopNeedDeleteIndex);
                m_Target.SyncBindObjects();
            }

            if (bindComNeedDeleteIndex != -1)
            {
                m_Target.BindComponents.RemoveAt(bindComNeedDeleteIndex);
                m_Target.SyncBindObjects();
            }

            EditorGUILayout.EndVertical();
        }

        private bool DrawBindObjectData(ReferenceBindComponent.BindObjectData bindObjectData, int index)
        {
            bool isDelete = false;
            Rect rect = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{index}]", GUILayout.Width(40));

            EditorGUI.BeginChangeCheck();
            string fieldName = EditorGUILayout.TextField(bindObjectData.FieldName);
            if (EditorGUI.EndChangeCheck())
            {
                bindObjectData.FieldName = fieldName;
                Refresh();
            }

            GUI.enabled = false;
            EditorGUILayout.ObjectField(bindObjectData.BindObject, typeof(UnityEngine.Object), true);
            GUI.enabled = true;

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                //??????????????????????????????list
                isDelete = true;
            }

            EditorGUILayout.EndHorizontal();
            OnBindObjectDataClick(rect, bindObjectData);

            if (bindObjectData.FieldNameIsInvalid)
            {
                EditorGUILayout.HelpBox("???????????????????????? ?????????????????? ?????????!", MessageType.Error);
            }

            if (bindObjectData.IsRepeatName)
            {
                EditorGUILayout.HelpBox("?????????????????????????????? ?????????!", MessageType.Error);
            }

            return isDelete;
        }

        private void OnBindObjectDataClick(Rect contextRect, ReferenceBindComponent.BindObjectData bindObjectData)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ContextClick)
            {
                Vector2 mousePos = evt.mousePosition;
                if (contextRect.Contains(mousePos))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Refresh FieldName"), false, data =>
                    {
                        bool isComponent = bindObjectData.BindObject is Component;
                        bindObjectData.FieldName = isComponent
                            ? m_Target.GetBindComponentsRuleHelper().GetDefaultFieldName(
                                (Component)bindObjectData.BindObject)
                            : m_Target.GetBindAssetOrPrefabRuleHelper().GetDefaultFieldName(bindObjectData.BindObject);

                        Refresh();
                    }, bindObjectData);
                    menu.ShowAsContext();

                    evt.Use();
                }
            }
        }

        #endregion
    }
}