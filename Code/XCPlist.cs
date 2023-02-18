using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.iOS.Xcode;

namespace NdXUPorter
{
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
        }

        public void Process(Hashtable plist)
        {
            try
            {
                if (plist == null) return;
                plistDoc = new PlistDocument();
                plistDoc.ReadFromFile(plistPath);
                //get root
                rootDict = plistDoc.root;
                foreach (DictionaryEntry entry in plist)
                {
                    this.AddPlistItems((string)entry.Key, entry.Value);
                }
                if (plistModified)
                {
                    Debug.Log("Save Plist");
                    plistDoc.WriteToFile(plistPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // http://stackoverflow.com/questions/20618809/hashtable-to-dictionary
        public static Dictionary<K, V> HashtableToDictionary<K, V>(Hashtable table)
        {
            Dictionary<K, V> dict = new Dictionary<K, V>();
            foreach (DictionaryEntry kvp in table)
                dict.Add((K)kvp.Key, (V)kvp.Value);
            return dict;
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
                //单独支持String类型
                if (vtype == typeof(System.String) || vtype == typeof(System.Boolean) || vtype == typeof(System.Array))
                {
                    SetPlistDictValue(rootDict, key, value);
                }
                else
                {
                    //默认是字典类型
                    PlistElementDict plistDict = rootDict.values[key].AsDict();
                    var dict = HashtableToDictionary<string, object>((Hashtable)value);
                    foreach (var o in dict)
                    {
                        SetPlistDictValue(plistDict, o.Key, o.Value);
                    }
                }
                plistModified = true;
            }
        }

        private void SetPlistDictValue(PlistElementDict e, string key, object value)
        {
            var vtype = value.GetType();
            if (vtype == typeof(System.String))
            {
                e.SetString(key, (string)value);
            }
            else if (vtype == typeof(System.Boolean))
            {
                e.SetBoolean(key, (bool)value);
            }
            else if (vtype == typeof(System.Array))
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
            PlistElementArray plistArray = e.values[key].AsArray();
            if (plistArray == null)
                plistArray = e.CreateArray(key);
            foreach (object s in array)
            {
                if (s is string)
                {
                    Debug.Log($"add array {key} ： element={s}");
                    plistArray.AddString((string)s);
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
