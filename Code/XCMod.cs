using System;
using UnityEngine;
using System.Collections;
using System.IO;

namespace NdXUPorter
{
    public class XCMod
    {
        private Hashtable _datastore = new Hashtable();
        private ArrayList _libs = null;

        public string name { get; private set; }
        /// <summary>
        /// 全路径
        /// </summary>
		public string path { get; private set; }
        /// <summary>
        /// 优先级，从小到大的 加载 projmods 配置。
        /// </summary>
        public int priority
        {
            get
            {
                if (_datastore != null && _datastore.Contains("priority"))
                {
                    try
                    {
                        return Convert.ToInt32(_datastore["priority"]);
                    }
                    catch (Exception)
                    {
                        foreach (DictionaryEntry item in _datastore)
                        {
                            Debug.LogWarning(item.Key + "=" + item.Value.ToString() + " type=" + item.Value.GetType().ToString());
                        }
                        // ignored
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// The group name in Xcode to which files and folders will added by this projmods file
        /// </summary>
        public string group
        {
            get
            {
                if (_datastore != null && _datastore.Contains("group"))
                    return (string)_datastore["group"];
                return string.Empty;
            }
        }

        public ArrayList patches
        {
            get
            {
                return (ArrayList)_datastore["patches"];
            }
        }

        public ArrayList libs
        {
            get
            {
                if (_libs == null)
                {
                    _libs = new ArrayList(((ArrayList)_datastore["libs"]).Count);
                    foreach (string fileRef in (ArrayList)_datastore["libs"])
                    {
                        Debug.Log("Adding to Libs: " + fileRef);
                        _libs.Add(new XCModFile(fileRef));
                    }
                }
                return _libs;
            }
        }

        public ArrayList frameworks
        {
            get
            {
                return (ArrayList)_datastore["frameworks"];
            }
        }

        public ArrayList headerpaths
        {
            get
            {
                return (ArrayList)_datastore["headerpaths"];
            }
        }

        public ArrayList files
        {
            get
            {
                return (ArrayList)_datastore["files"];
            }
        }

        public ArrayList folders
        {
            get
            {
                return (ArrayList)_datastore["folders"];
            }
        }
        /// <summary>
        /// Regular expression pattern. Matched files will not be added.
        /// </summary>
        public ArrayList excludes
        {
            get
            {
                return (ArrayList)_datastore["excludes"];
            }
        }

        public ArrayList compiler_flags
        {
            get
            {
                return (ArrayList)_datastore["compiler_flags"];
            }
        }

        public ArrayList linker_flags
        {
            get
            {
                return (ArrayList)_datastore["linker_flags"];
            }
        }

        public ArrayList embed_binaries
        {
            get
            {
                return (ArrayList)_datastore["embed_binaries"];
            }
        }

        public Hashtable plist
        {
            get
            {
                return (Hashtable)_datastore["plist"];
            }
        }

        public ArrayList code_modifys
        {
            get
            {
                return (ArrayList)_datastore["code_modify"];
            }
        }

        public XCMod(string filename)
        {
            FileInfo projectFileInfo = new FileInfo(filename);
            if (!projectFileInfo.Exists)
            {
                Debug.LogWarning("File does not exist.");
            }

            name = System.IO.Path.GetFileNameWithoutExtension(filename);
            path = System.IO.Path.GetDirectoryName(filename);

            string contents = projectFileInfo.OpenText().ReadToEnd();
            Debug.Log(contents);
            _datastore = (Hashtable)XUPorterJSON.MiniJSON.jsonDecode(contents);
            if (_datastore == null || _datastore.Count == 0)
            {
                Debug.Log(contents);
                throw new UnityException("Parse error in file " + System.IO.Path.GetFileName(filename) + "! Check for typos such as unbalanced quotation marks, etc.");
            }
            /*else
            {
                foreach (DictionaryEntry item in _datastore)
                {
                    Debug.LogError(item.Key + "="+ item.Value.ToString() + " type=" + item.Value.GetType().ToString());
                }
            }*/
        }
    }

    public class XCModFile
    {
        public string filePath { get; private set; }
        public bool isWeak { get; private set; }

        public XCModFile(string inputString)
        {
            isWeak = false;

            if (inputString.Contains(":"))
            {
                string[] parts = inputString.Split(':');
                filePath = parts[0];
                isWeak = (parts[1].CompareTo("weak") == 0);
            }
            else
            {
                filePath = inputString;
            }
        }
    }
}
