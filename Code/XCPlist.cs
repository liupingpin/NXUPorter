#if UNITY_EDITOR_OSX
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using System.Linq;

namespace NdXUPorter
{
    /// <summary>
    /// plist的修改
    /// </summary>
    public class XCPlist
    {
        string plistPath;
        bool plistModified;
        private PlistDocument plistDoc;
        private PlistElementDict rootDict;

        //-----demo-----
        // URLTypes constant --- plist
        const string BundleUrlTypes = "CFBundleURLTypes";
        const string BundleTypeRole = "CFBundleTypeRole";
        const string BundleUrlName = "CFBundleURLName";
        const string BundleUrlSchemes = "CFBundleURLSchemes";

        // URLTypes constant --- projmods
        const string PlistUrlType = "urltype";
        const string PlistRole = "role";
        const string PlistEditor = "Editor";
        const string PlistName = "name";
        const string PlistSchemes = "schemes";
        //------------
        //---ndsdk----
        // const string PlistLSApplicationQueriesSchemes = "LSApplicationQueriesSchemes";
        //------------


        public XCPlist(string plistPath)
        {
            this.plistPath = plistPath;
            plistDoc = new PlistDocument();
            plistDoc.ReadFromFile(plistPath);
            //get root
            rootDict = plistDoc.root;
        }

        public void Process(Hashtable plist)
        {
            try
            {
                if (plist == null) return;
                foreach (DictionaryEntry entry in plist)
                {
                    this.AddPlistItems((string)entry.Key, entry.Value);
                }

                CustomPlistMod();

                
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void Save()
        {
            if (plistModified)
            {
                Debug.Log("Save Plist");
                plistDoc.WriteToFile(plistPath);
            }
        }
        /// <summary>
        /// 自定义构建plist，这里主要构建版本信息
        /// </summary>
        private void CustomPlistMod()
        {
            //Version
            var buildKey = "CFBundleShortVersionString";
            rootDict.SetString(buildKey, AppBuildPipeline.versionNumber);

            //Build
            buildKey = "CFBundleVersion";
            rootDict.SetString(buildKey, AppBuildPipeline.buildNumber.ToString());

            // 测试版开启文件共享
            if (PlayerSettings.productName[PlayerSettings.productName.Length - 1] == 'β')
            {
                rootDict.SetBoolean("UIFileSharingEnabled", true);
            }

            //bundleID
            //        buildKey = "CFBundleIdentifier";
            //        rootDict.SetString(buildKey, BuildParams.iosBundleId);

            //PlistElementDict dic1 = rootDict.CreateDict("NSAppTransportSecurity");
            //dic1.SetBoolean("NSAllowsArbitraryLoads", true);

            //TODO:移到XUPort projmods 配置里添加
            //        rootDict.SetString("NSPhotoLibraryUsageDescription", "App需要您的同意,才能访问相册");
            //        rootDict.SetString("NSPhotoLibraryAddUsageDescription", "App需要您的同意,才能保存图片到相册");
            //虽然代码里没有用到，苹果还是建议加上。
            //NSLocationWhenInUseUsageDescription key with a user-facing purpose string explaining clearly and completely why your app needs the data.
            //While your app might not use these APIs, a purpose string is still required.
            //        rootDict.SetString("NSLocationUsageDescription", "App需要您的同意,才能访问位置");

            //删除UIApplicationExitsOnSuspend（UIApplicationExitsOnSuspend暂时忽略处理）
            string exitsOnSuspendKey = "UIApplicationExitsOnSuspend";
            if (rootDict.values.ContainsKey(exitsOnSuspendKey))
            {
                rootDict.values.Remove(exitsOnSuspendKey);
            }

            //升级到unity2020 不需要了
            // string rdc = "UIRequiredDeviceCapabilities";
            // if (rootDict.values.ContainsKey(rdc))
            // {
            //     // unity默认<string>armv7</string>
            //     // 2022 ios 提审不通过，UIRequiredDeviceCapabilities键输入信息。plist的设置方式使该应用程序不会安装在iPhone和iPad上。
            //     var capabilities = rootDict[rdc].AsArray();
            //     capabilities.values.RemoveAll(item => item.AsString() == "armv7"); // 移除v7
            //     capabilities.values.Add(new PlistElementString("arm64"));
            // }
            plistModified = true;
        }

        // http://stackoverflow.com/questions/20618809/hashtable-to-dictionary
        //public static Dictionary<K, V> HashtableToDictionary<K, V>(Hashtable table)
        //{
        //    Dictionary<K, V> dict = new Dictionary<K, V>();
        //    foreach (DictionaryEntry kvp in table)
        //        dict.Add((K)kvp.Key, (V)kvp.Value);
        //    return dict;
        //}

        public static Dictionary<K, V> HashtableToDictionary<K, V>(Hashtable table)
        {
            return table
              .Cast<DictionaryEntry>()
              .ToDictionary(kvp => (K)kvp.Key, kvp => (V)kvp.Value);
        }

        /// <summary>
        /// plist 根据实际使用情况进行定制
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddPlistItems(string key, object value)
        {
            Debug.Log("AddPlistItems: key=" + key + "  ;value =" + value + " ;type =" + value.GetType());

            //urltype
            if (string.Compare(key, PlistUrlType, System.StringComparison.Ordinal) == 0)
            {
                processUrlTypes((ArrayList)value);
            }
            //nd sdk 需求的 LSApplicationQueriesSchemes
            // else if (string.Compare(key, PlistLSApplicationQueriesSchemes, System.StringComparison.Ordinal) == 0)
            // {
            //     processArray(PlistLSApplicationQueriesSchemes, (ArrayList)value);
            // }
            else
            {
                var vtype = value.GetType();
                if (vtype == typeof(System.String) || vtype == typeof(System.Boolean) || vtype == typeof(System.Array) || vtype == typeof(ArrayList))
                {
                    SetPlistDictValue(rootDict, key, value);
                }
                else
                {
                    //默认是字典类型
                    var plistDictElement = TryGetValue(rootDict, key);
                    PlistElementDict plistDict;
                    if (plistDictElement==null)
                    {
                        plistDict = rootDict.CreateDict(key);
                    }
                    else
                        plistDict = plistDictElement.AsDict();
                    var dict = HashtableToDictionary<string, object>((Hashtable)value);
                    foreach (var o in dict)
                    {
                        SetPlistDictValue(plistDict, o.Key, o.Value);
                    }
                }
                plistModified = true;
            }
        }

        private PlistElement TryGetValue(PlistElementDict dict, string key)
        {
            if (dict.values.TryGetValue(key, out var e))
                return e;
            return null;
        }

        private void SetPlistDictValue(PlistElementDict e, string key, object value)
        {
            Debug.Log("SetPlistDictValue: key=" + key + "  ;value =" + value + " ;type =" + value.GetType());
            var vtype = value.GetType();
            if (vtype == typeof(System.String))
            {
                e.SetString(key, (string)value);
            }
            else if (vtype == typeof(System.Boolean))
            {
                e.SetBoolean(key, (bool)value);
            }
            else if (vtype == typeof(System.Array) || vtype == typeof(ArrayList))
            {
                SetPlistDictArray(e, key, (ArrayList)value);
            }
            else
            {
                Debug.LogError($"暂不支持 vtype={vtype} 的类型");
            }
        }
        //Array
        private void SetPlistDictArray(PlistElementDict e, string key, ArrayList array)
        {
            Debug.Log($"add plist Array key = {key}");
            var plistArrayElement = TryGetValue(rootDict, key);
            PlistElementArray plistArray;
            if (plistArrayElement == null)
                plistArray = e.CreateArray(key);
            else
                plistArray = plistArrayElement.AsArray();

            foreach (object s in array)
            {
                var vtype = s.GetType();
                Debug.Log($"add array element={s}" + " ;type =" + vtype);                
                if (vtype == typeof(System.String))
                {                    
                    plistArray.AddString((string)s);
                }
                else if (vtype == typeof(System.Boolean))
                {
                    plistArray.AddBoolean((bool)s);
                }
                else if(vtype == typeof(System.Int32))
                {
                    plistArray.AddInteger((Int32)s);
                }
                else if(vtype == typeof(Hashtable))
                {           
                    PlistElementDict plistDict = plistArray.AddDict();              
                    var dict = HashtableToDictionary<string, object>((Hashtable)s);
                    foreach (var o in dict)
                    {
                        SetPlistDictValue(plistDict, o.Key, o.Value);
                    }
                }
                else
                {
                    Debug.LogError("数组存在其他类型，请拓展支持");
                }
            }
            plistModified = true;
        }
        //urltype
        private void processUrlTypes(ArrayList urltypes)
        {
            Debug.Log("processUrlTypes");
            if (urltypes == null)
            {
                Debug.LogWarning("urltypes is null");
                return;
            }
            PlistElementArray urlTypes = rootDict[BundleUrlTypes]?.AsArray();
            if (urlTypes == null)
            {
                urlTypes = rootDict.CreateArray(BundleUrlTypes);
            }
            foreach (Hashtable table in urltypes)
            {
                string role = (string)table[PlistRole];
                if (string.IsNullOrEmpty(role))
                {
                    role = PlistEditor;
                }
                string name = (string)table[PlistName];
                if(string.IsNullOrEmpty(name))
                {
                    Debug.LogWarning("name is null ,not support");
                    continue;
                }
                ArrayList shcemes = (ArrayList)table[PlistSchemes];
                if (shcemes == null)
                {
                    Debug.LogWarning("shcemens is null ,not support");
                    continue;
                }
                Debug.Log("Add shcemes name=" + name);
                //查找name是否存在
                PlistElementDict urlDict = null;
                var urlData = urlTypes.values.Find(x =>
                {
                    var dict = x.AsDict();
                    if (dict == null) return false;
                    return dict[BundleUrlName].AsString() == name;
                });
                if (urlData != null)
                {
                    urlDict = urlData.AsDict();
                }

                if (urlDict == null)
                {
                    urlDict = urlTypes.AddDict();
                    urlDict[BundleUrlName] = new PlistElementString(name);
                }

                urlDict[BundleTypeRole] = new PlistElementString(role);
                var schemesArray = urlDict.CreateArray(BundleUrlSchemes);
                foreach (string s in shcemes)
                {
                    if (string.IsNullOrEmpty(s)) continue;
                    Debug.Log("Add shcemes s=" + s);
                    schemesArray.AddString(s);
                }

                plistModified = true;
            }
        }
    }
}

#endif
