using System;
using System.Collections.Generic;
using System.Linq;
using ReferenceBindTool.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using BindObjectData = ReferenceBindTool.Runtime.ReferenceBindComponent.BindObjectData;

namespace ReferenceBindTool.Editor
{
    /// <summary>
    /// 引用绑定工具类
    /// </summary>
    public static class ReferenceBindExtensions
    {
        /// <summary>
        /// 规则绑定所有组件
        /// </summary>
        public static void RuleBindComponents(this ReferenceBindComponent self)
        {
            self.BindComponentsRuleHelper.BindComponents(self.gameObject, self.BindComponents,bindList =>
            {
                self.BindComponents.Clear();
                foreach ((string fieldName, Component bindComponent) item in bindList)
                {
                    self.AddBindComponent(item.fieldName,item.bindComponent,false);
                }
                self.SyncBindObjects();
            });
        }
        
        /// <summary>
        /// 规则绑定所有组件
        /// </summary>
        public static void RuleBindAssetsOrPrefabs(this ReferenceBindComponent self,string fieldName,Object obj)
        {
            self.BindAssetOrPrefabRuleHelper.BindAssetOrPrefab(fieldName,obj,
                isCanAdd =>
                {
                    if (isCanAdd)
                    {
                        self.AddBindAssetsOrPrefabs(fieldName, obj);
                    }
                });
            self.SyncBindObjects();
        }

        /// <summary>
        /// 排序
        /// </summary>
        public static void Sort(this ReferenceBindComponent self)
        {
            self.BindAssetsOrPrefabs.Sort((x, y) =>
                string.Compare(x.FieldName, y.FieldName, StringComparison.Ordinal));
            self.BindComponents.Sort((x, y) =>
                string.Compare(x.FieldName, y.FieldName, StringComparison.Ordinal));
            self.SyncBindObjects();
        }

        /// <summary>
        /// 同步绑定数据
        /// </summary>
        public static void SyncBindObjects(this ReferenceBindComponent self)
        {
            self.BindObjects.Clear();
            foreach (var bindData in self.BindAssetsOrPrefabs)
            {
                self.BindObjects.Add(bindData.BindObject);
            }

            foreach (var bindData in self.BindComponents)
            {
                self.BindObjects.Add(bindData.BindObject);
            }

            EditorUtility.SetDirty(self);
        }


        /// <summary>
        /// 删除Missing Or Null
        /// </summary>
        public static void RemoveNull(this ReferenceBindComponent self)
        {
            for (int i = self.BindAssetsOrPrefabs.Count - 1; i >= 0; i--)
            {
                var bindData = self.BindAssetsOrPrefabs[i];

                if (bindData.BindObject == null)
                {
                    self.BindAssetsOrPrefabs.RemoveAt(i);
                }
            }

            for (int i = self.BindComponents.Count - 1; i >= 0; i--)
            {
                var bindData = self.BindComponents[i];

                if (bindData.BindObject == null)
                {
                    self.BindComponents.RemoveAt(i);
                }
            }

            self.SyncBindObjects();
        }

        /// <summary>
        /// 全部删除
        /// </summary>
        public static void RemoveAll(this ReferenceBindComponent self)
        {
            self.BindAssetsOrPrefabs.Clear();
            self.BindComponents.Clear();
            self.SyncBindObjects();
        }

        /// <summary>
        /// 添加绑定资源或预制体
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <param name="bindObject"></param>
        public static void AddBindAssetsOrPrefabs(this ReferenceBindComponent self, string name,
            UnityEngine.Object bindObject)
        {
            foreach (var item in self.BindAssetsOrPrefabs)
            {
                if (item.BindObject == bindObject)
                {
                    return;
                }
            }

            bool isRepeat = false;
            for (int j = 0; j < self.BindAssetsOrPrefabs.Count; j++)
            {
                if (self.BindAssetsOrPrefabs[j].FieldName == name)
                {
                    isRepeat = true;
                    break;
                }
            }

            if (!isRepeat)
            {
                for (int i = 0; i < self.BindComponents.Count; i++)
                {
                    if (self.BindComponents[i].FieldName == name)
                    {
                        isRepeat = true;
                        break;
                    }
                }
            }

            self.BindAssetsOrPrefabs.Add(new BindObjectData(isRepeat, name, bindObject)
            {
                FieldNameIsInvalid = self.BindAssetOrPrefabRuleHelper.CheckFieldNameIsInvalid(name)
            });
            self.SyncBindObjects();
        }

        /// <summary>
        /// 添加绑定组件
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <param name="bindComponent"></param>
        /// <param name="isSyncBindObject"></param>
        public static void AddBindComponent(this ReferenceBindComponent self, string name,Component bindComponent,
            bool isSyncBindObject = true)
        {
            foreach (var item in self.BindComponents)
            {
                if (item.BindObject == bindComponent)
                {
                    return;
                }
            }

            bool isRepeat = false;
            for (int i = 0; i < self.BindComponents.Count; i++)
            {
                if (self.BindComponents[i].FieldName == name)
                {
                    isRepeat = true;
                    break;
                }
            }

            if (!isRepeat)
            {
                for (int i = 0; i < self.BindAssetsOrPrefabs.Count; i++)
                {
                    if (self.BindAssetsOrPrefabs[i].FieldName == name)
                    {
                        isRepeat = true;
                        break;
                    }
                }
            }
            

            self.BindComponents.Add(new ReferenceBindComponent.BindObjectData(isRepeat, name, bindComponent)
            {
                FieldNameIsInvalid = self.BindComponentsRuleHelper.CheckFieldNameIsInvalid(name)
            });
            if (isSyncBindObject)
            {
                self.SyncBindObjects();
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="self"></param>
        public static void Refresh(this ReferenceBindComponent self)
        {
            self.BindObjects.Clear();
            var tempList = new List<ReferenceBindComponent.BindObjectData>(self.BindAssetsOrPrefabs.Count);
            tempList.AddRange(self.BindAssetsOrPrefabs);
            self.BindAssetsOrPrefabs.Clear();
            int i = 0;
            for (; i < tempList.Count; i++)
            {
                var tempData = tempList[i];
                self.AddBindAssetsOrPrefabs(tempData.FieldName, tempData.BindObject);
            }

            tempList.AddRange(self.BindComponents);
            self.BindComponents.Clear();
            for (; i < tempList.Count; i++)
            {
                var tempData = tempList[i];
                self.AddBindComponent(tempData.FieldName, (Component)tempData.BindObject);
            }

            self.SyncBindObjects();
        }

        /// <summary>
        /// 重置所有字段名
        /// </summary>
        /// <param name="self"></param>
        public static void ResetAllFieldName(this ReferenceBindComponent self)
        {
            self.BindObjects.Clear();
            var tempList = new List<BindObjectData>(self.BindAssetsOrPrefabs.Count);
            tempList.AddRange(self.BindAssetsOrPrefabs);
            self.BindAssetsOrPrefabs.Clear();
            int i = 0;
            for (; i < tempList.Count; i++)
            {
                var tempData = tempList[i];
                self.AddBindAssetsOrPrefabs(self.BindAssetOrPrefabRuleHelper.GetDefaultFieldName(tempData.BindObject),
                    tempData.BindObject);
            }

            tempList.AddRange(self.BindComponents);
            self.BindComponents.Clear();
            for (; i < tempList.Count; i++)
            {
                var tempData = tempList[i];
                self.AddBindComponent(self.BindComponentsRuleHelper.GetDefaultFieldName((Component)tempData.BindObject),
                    (Component)tempData.BindObject);
            }
            self.SyncBindObjects();
        }


        /// <summary>
        /// 设置脚本名称
        /// </summary>
        /// <param name="self"></param>
        /// <param name="className"></param>
        public static void SetClassName(this ReferenceBindComponent self, string className)
        {
            if (self.GeneratorCodeName == className)
            {
                return;
            }

            self.GeneratorCodeName = className;
            EditorUtility.SetDirty(self);
        }

        /// <summary>
        /// 设置生成代码配置
        /// </summary>
        /// <param name="self"></param>
        /// <param name="data"></param>
        public static void SetSettingData(this ReferenceBindComponent self, CodeGeneratorSettingData data)
        {
            if (self.CodeGeneratorSettingData == data)
            {
                return;
            }

            self.CodeGeneratorSettingData = data;
            if (self.SettingDataSearchable != null)
            {
                int findIndex = self.SettingDataSearchable.Names.ToList().FindIndex(_ => _ == data.Name);
                if (findIndex == -1)
                {
                    string[] paths = AssetDatabase.FindAssets($"t:{nameof(CodeGeneratorSettingConfig)}");
                    string path = AssetDatabase.GUIDToAssetPath(paths[0]);
                    var settingConfig = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettingConfig>(path);
                    var settingDataNames = settingConfig.GetAllSettingNames().ToList();
                    self.SettingDataSearchable.Select = settingDataNames.FindIndex(_ => _ == data.Name);
                    self.SettingDataSearchable.Names = settingDataNames.ToArray();
                }
                else
                {
                    self.SettingDataSearchable.Select = findIndex;
                }
            }

            EditorUtility.SetDirty(self);
        }

        /// <summary>
        /// 设置生成代码配置
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        public static void SetSettingData(this ReferenceBindComponent self, string name)
        {
            self.CodeGeneratorSettingData = ReferenceBindUtility.GetAutoBindSetting(name: name);
            if (self.SettingDataSearchable != null)
            {
                int findIndex = self.SettingDataSearchable.Names.ToList().FindIndex(_ => _ == name);
                if (findIndex == -1)
                {
                    string[] paths = AssetDatabase.FindAssets($"t:{nameof(CodeGeneratorSettingConfig)}");
                    string path = AssetDatabase.GUIDToAssetPath(paths[0]);
                    var settingConfig = AssetDatabase.LoadAssetAtPath<CodeGeneratorSettingConfig>(path);
                    var settingDataNames = settingConfig.GetAllSettingNames().ToList();

                    self.SettingDataSearchable.Select = settingDataNames.FindIndex(_ => _ == name);
                    self.SettingDataSearchable.Names = settingDataNames.ToArray();
                }
                else
                {
                    self.SettingDataSearchable.Select = findIndex;
                }
            }

            EditorUtility.SetDirty(self);
        }

        /// <summary>
        /// 设置生成代码配置
        /// </summary>
        /// <param name="self"></param>
        /// <param name="names"></param>
        /// <param name="select"></param>
        public static void SetSearchable(this ReferenceBindComponent self, string[] names, int select)
        {
            self.SettingDataSearchable.Select = select;
            self.SettingDataSearchable.Names = names;
            EditorUtility.SetDirty(self);
        }

        /// <summary>
        /// 设置绑定组件规则帮助类
        /// </summary>
        /// <param name="self"></param>
        /// <param name="ruleHelperName"></param>
        public static void SetBindComponentsRuleHelperTypeName(this ReferenceBindComponent self, string ruleHelperName)
        {
            if (self.BindComponentsRuleHelperTypeName == ruleHelperName && self.BindComponentsRuleHelper != null)
            {
                return;
            }

            self.BindComponentsRuleHelperTypeName = ruleHelperName;
            IBindComponentsRuleHelper helper =
                (IBindComponentsRuleHelper)HelperUtility.CreateHelperInstance(self.BindComponentsRuleHelperTypeName);
            self.BindComponentsRuleHelper = helper;
            EditorUtility.SetDirty(self);
        }
        
        /// <summary>
        /// 设置绑定资源或预制体规则帮助类
        /// </summary>
        /// <param name="self"></param>
        /// <param name="ruleHelperName"></param>
        public static void SetBindAssetOrPrefabRuleHelperTypeName(this ReferenceBindComponent self, string ruleHelperName)
        {
            if (self.BindAssetOrPrefabRuleHelperTypeName == ruleHelperName && self.BindAssetOrPrefabRuleHelper != null)
            {
                return;
            }

            self.BindAssetOrPrefabRuleHelperTypeName = ruleHelperName;
            IBindAssetOrPrefabRuleHelper helper =
                (IBindAssetOrPrefabRuleHelper) HelperUtility.CreateHelperInstance(self.BindAssetOrPrefabRuleHelperTypeName);
            self.BindAssetOrPrefabRuleHelper = helper;
            EditorUtility.SetDirty(self);
        }
        
        /// <summary>
        /// 设置代码生成规则帮助类
        /// </summary>
        /// <param name="self"></param>
        /// <param name="ruleHelperName"></param>
        public static void SetCodeGeneratorRuleHelperTypeName(this ReferenceBindComponent self, string ruleHelperName)
        {
            if (self.CodeGeneratorRuleHelperTypeName == ruleHelperName && self.CodeGeneratorRuleHelper != null)
            {
                return;
            }

            self.CodeGeneratorRuleHelperTypeName = ruleHelperName;
            ICodeGeneratorRuleHelper helper =
                (ICodeGeneratorRuleHelper)HelperUtility.CreateHelperInstance(self.CodeGeneratorRuleHelperTypeName);
            self.CodeGeneratorRuleHelper = helper;
            EditorUtility.SetDirty(self);
        }
    }
}