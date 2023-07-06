#if UNITY_EDITOR_OSX
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using System;
using System.Reflection;

namespace NdXUPorter
{
    public class XCProject : System.IDisposable
    {

        public bool needSwiftModule;
        /// <summary>
        /// 构建类型
        /// </summary>
        public enum PbxBuildType
        {
            /// <summary>
            /// 无
            /// </summary>
            Null = 0,
            /// <summary>
            /// .framework 文件，target 选择 UnityFramework，可以不用指定 BuildPhase。
            /// .a .dylib .tbd 文件，target 选择 UnityFramework，BuildPhase 选择 FrameworksBuildPhase。
            /// </summary>
            PBXFrameworks_UnityFramework,
            /// <summary>
            /// .m .mm .c .cpp 等源文件，target 选择 UnityFramework，BuildPhase 选择 SourcesBuildPhase
            /// </summary>
            PBXSources_UnityFramework,
            /// <summary>
            ///  .h  等源文件，target 选择 UnityFramework，不加入 BuildPhase
            /// </summary>
            PBXSources_UnityFramework_NoBuildPhase,
            /// <summary>
            /// .json .plist .bundle .config .xcdatamodeld 等资源文件，target 选择 Unity-iPhone，BuildPhase 选择 ResourcesBuildPhase
            /// </summary>
            PBXResources_UnityIPhone,
        }
        public static readonly Dictionary<string, PbxBuildType> typePhases = new Dictionary<string, PbxBuildType> {
            { ".a", PbxBuildType.PBXFrameworks_UnityFramework },
            { ".app", PbxBuildType.Null },
            { ".s", PbxBuildType.PBXSources_UnityFramework },
            { ".c", PbxBuildType.PBXSources_UnityFramework },
            { ".cpp", PbxBuildType.PBXSources_UnityFramework },
            { ".framework", PbxBuildType.PBXFrameworks_UnityFramework },
            { ".h", PbxBuildType.PBXSources_UnityFramework_NoBuildPhase },
            { ".pch", PbxBuildType.Null },
            { ".icns", PbxBuildType.PBXResources_UnityIPhone },
            { ".m", PbxBuildType.PBXSources_UnityFramework },
            { ".mm", PbxBuildType.PBXSources_UnityFramework },
            { ".xcdatamodeld", PbxBuildType.PBXResources_UnityIPhone },
            { ".nib", PbxBuildType.PBXResources_UnityIPhone },
            { ".plist", PbxBuildType.PBXResources_UnityIPhone },
            { ".png", PbxBuildType.PBXResources_UnityIPhone },
            { ".rtf", PbxBuildType.PBXResources_UnityIPhone },
            { ".tiff", PbxBuildType.PBXResources_UnityIPhone },
            { ".txt", PbxBuildType.PBXResources_UnityIPhone },
            { ".json", PbxBuildType.PBXResources_UnityIPhone },
            { ".xcodeproj", PbxBuildType.Null },
            { ".xib", PbxBuildType.PBXResources_UnityIPhone },
            { ".strings", PbxBuildType.PBXResources_UnityIPhone },
            { ".bundle", PbxBuildType.PBXResources_UnityIPhone },
            { ".dylib", PbxBuildType.PBXFrameworks_UnityFramework },
            { ".tbd", PbxBuildType.PBXFrameworks_UnityFramework },
            { ".inl", PbxBuildType.PBXResources_UnityIPhone },
            { ".mom", PbxBuildType.PBXResources_UnityIPhone },
            { ".meta", PbxBuildType.Null},
        };
        /// <summary>
        /// 项目跟目录
        /// </summary>
        public string projectRootPath { get; private set; }
        /// <summary>
        /// pbxProject 文件路径
        /// </summary>
        public string pbxProjPath { get; private set; }
        public PBXProject pbxProject { get; private set; }
        public ProjectCapabilityManager capabilityManager { get; private set; }
        public XCPlist plist { get; private set; }
        /// <summary>
        /// Unity-iPhone guid
        /// </summary>
        public string unityMainTargetGuid { get; private set; }
        /// <summary>
        /// UnityFramework库
        /// </summary>
        public string unityFrameworkTargetGuid { get; private set; }

        private HashSet<string> usedFileNames = new HashSet<string>();

        private bool hasCapabilityModify;

        #region Constructor


        public XCProject(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                Debug.LogWarning("XCode project path does not exist: " + filePath);
                return;
            }

            needSwiftModule = false;
            projectRootPath = filePath;
            pbxProjPath = PBXProject.GetPBXProjectPath(filePath);
            pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(pbxProjPath));

            unityMainTargetGuid = pbxProject.GetUnityMainTargetGuid();
            unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

            //Capability
            capabilityManager = new ProjectCapabilityManager(pbxProjPath, "Entitlements.entitlements", null, unityMainTargetGuid);

            //plist
            string plistPath = Path.Combine(this.projectRootPath, "Info.plist");
            plist = new XCPlist(plistPath);
        }


        #endregion


        #region PBXMOD

        private string GetRelativeDirPath(string filePath)
        {
            var fileDirPath = System.IO.Path.GetDirectoryName(filePath);
            var relativeDirPath = fileDirPath?.Replace(projectRootPath, "");
            return relativeDirPath;
        }
        private void AddFrameworkSearchPath(string targetGuid, string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
            {
                //$(PROJECT_DIR)项目目录
            }
            else
            {
                Debug.Log("AddFrameworkSearchPath:" + dirPath);
                //新的查找路径
                pbxProject.AddBuildProperty(
                    targetGuid,
                    "FRAMEWORK_SEARCH_PATHS",
                    "$(PROJECT_DIR)" + dirPath
                );
            }
        }

        private void AddLibrarySearchPath(string targetGuid, string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath))
            {
                //$(PROJECT_DIR)项目目录
            }
            else
            {
                Debug.Log("AddLibrarySearchPath:" + dirPath);
                //新的查找路径
                pbxProject.AddBuildProperty(
                    targetGuid,
                    "LIBRARY_SEARCH_PATHS",
                    "$(PROJECT_DIR)" + dirPath
                );
            }
        }

        /// <summary>
        /// 加载unity-iphone 上
        /// </summary>
        /// <param name="filePath"></param>
        public void AddEmbedFile(string filePath)
        {
            Debug.Log("AddEmbedFile " + filePath);
            if (filePath == null)
            {
                Debug.LogError("AddEmbedFile called with null filePath");
                return;
            }
            //判读是否在文件是否工程下
            if (!filePath.Contains(projectRootPath))
            {
                Debug.LogError("AddEmbedFile called with not in project root path");
                return;
            }
            //Check if there is already a file
            var fileName = System.IO.Path.GetFileName(filePath);
            if (usedFileNames.Contains(fileName))
            {
                Debug.Log("File already exists: " + filePath); //not a warning, because this is normal for most builds!
                return;
            }

            usedFileNames.Add(fileName);
            string extension = System.IO.Path.GetExtension(filePath);
            typePhases.TryGetValue(extension, out var buildPhase);
            var relativePath = filePath?.Replace(projectRootPath, "");
            if (relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }
            //Debug.Log(relativePath);
            var dirPath = GetRelativeDirPath(filePath);
            if (extension == ".framework")
            {
                AddFrameworkSearchPath(unityMainTargetGuid, dirPath);

                string fileGuid = pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                PBXProjectExtensions.AddFileToEmbedFrameworks(pbxProject, unityMainTargetGuid, fileGuid);
            }
            else
            {
                Debug.LogError("AddEmbedFile not supported file");
            }
        }

        public void AddFile(string filePath)
        {
            Debug.Log("AddFile " + filePath);
            if (filePath == null)
            {
                Debug.LogError("AddFile called with null filePath");
                return;
            }
            //判读是否在文件是否工程下
            if (!filePath.Contains(projectRootPath))
            {
                Debug.LogError("AddFile called with not in project root path");
                return;
            }
            //Check if there is already a file
            var fileName = System.IO.Path.GetFileName(filePath);
            if (usedFileNames.Contains(fileName))
            {
                Debug.Log("File already exists: " + filePath); //not a warning, because this is normal for most builds!
                return;
            }

            usedFileNames.Add(fileName);
            string extension = System.IO.Path.GetExtension(filePath);
            typePhases.TryGetValue(extension, out var buildPhase);
            var relativePath = filePath?.Replace(projectRootPath, "");
            if (relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }
            //Debug.Log(relativePath);
            string fileGuid;
            switch (buildPhase)
            {
                case PbxBuildType.PBXFrameworks_UnityFramework:
                    var dirPath = GetRelativeDirPath(filePath);
                    if (extension == ".framework")
                        AddFrameworkSearchPath(unityFrameworkTargetGuid, dirPath);
                    else
                    {
                        AddLibrarySearchPath(unityFrameworkTargetGuid, dirPath);
                    }
                    pbxProject.AddFileToBuild(unityFrameworkTargetGuid, pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source));
                    break;
                case PbxBuildType.PBXResources_UnityIPhone:
                    fileGuid = pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                    pbxProject.AddFileToBuild(unityMainTargetGuid, fileGuid);
                    pbxProject.AddFileToBuild(unityFrameworkTargetGuid, fileGuid);
                    break;
                case PbxBuildType.PBXSources_UnityFramework:
                    fileGuid = pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                    //.h 文件不需要加入BuildPhase
                    if (extension == ".h") break;
                    var sourcesBuildPhase = pbxProject.GetSourcesBuildPhaseByTarget(unityFrameworkTargetGuid);
                    pbxProject.AddFileToBuildSection(unityFrameworkTargetGuid, sourcesBuildPhase, fileGuid);
                    break;
                case PbxBuildType.PBXSources_UnityFramework_NoBuildPhase:
                    pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                    break;
                case PbxBuildType.Null:
                    Debug.LogWarning("File Not Supported: " + filePath);
                    break;
                default:
                    Debug.LogWarning("File Not Supported." + filePath);
                    return;
            }
        }

        public bool AddFolder(string folderPath, string[] exclude = null, bool recursive = true)
        {
            Debug.Log("Folder PATH: " + folderPath);
            //判读是否在文件是否工程下
            if (!folderPath.Contains(projectRootPath))
            {
                Debug.LogError("AddFile called with not in project root path");
                return false;
            }
            if (!Directory.Exists(folderPath))
            {
                Debug.Log("Directory doesn't exist?");
                return false;
            }

            if (folderPath.EndsWith(".lproj"))
            {
                Debug.LogError("Ended with .lproj");
                return false;
            }

            if (exclude == null)
            {
                Debug.Log("Exclude was null");
                exclude = new string[] { };
            }

            foreach (string directory in Directory.GetDirectories(folderPath))
            {
                Debug.Log("DIR: " + directory);
                if (directory.EndsWith(".bundle", System.StringComparison.Ordinal))
                {
                    // Treat it like a file and copy even if not recursive
                    Debug.LogWarning("adding This is a special folder: " + directory);
                    AddFile(directory);
                    continue;
                }
                if (directory.EndsWith(".xcdatamodeld", System.StringComparison.Ordinal))
                {
                    Debug.LogWarning("adding This is a special folder: " + directory);
                    AddFile(directory);
                    continue;
                }

                if (directory.EndsWith(".framework", System.StringComparison.Ordinal))
                {
                    Debug.LogWarning("adding This is a framework: " + directory);
                    AddFile(directory);
                    continue;
                }

                if (recursive)
                {
                    Debug.Log("recursive");
                    AddFolder(directory, exclude, recursive);
                }
            }

            // Adding files.
            string regexExclude = string.Format(@"{0}", string.Join("|", exclude));
            foreach (string file in Directory.GetFiles(folderPath))
            {
                if (Regex.IsMatch(file, regexExclude))
                {
                    continue;
                }
                Debug.Log("Adding Files for Folder");
                AddFile(file);
            }

            return true;
        }
        /// <summary>
        /// 修改Capability
        /// </summary>
        public bool AddCapability(int c)
        {
            Debug.Log("Add capability " + c);
            switch (c)
            {
                case 1:
                    //内购
                    pbxProject.AddFrameworkToProject(unityMainTargetGuid, "StoreKit.framework", false);
                    capabilityManager.AddInAppPurchase();
                    return false;
                case 2:
                    //苹果登录
                    capabilityManager.AddSignInWithApple();
                    break;
                case 3:
                    //推送
                    capabilityManager.AddPushNotifications(false);
                    capabilityManager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
                    break;
                default:
                    Debug.LogWarning("未实现类型 " + c);
                    return false;
            }

            return true;
        }

        #endregion

        #region 项目自定义部分
        /// <summary>
        /// 将大部分动态配置放在XUPorter来构建。
        /// 要注意勾选掉 select platform for plugins 的 ios 选项，防止unity 自身拷贝到xcode 导致 xuporter 导出失败。
        /// </summary>
        public void CustomMod()
        {
            //ProductName
            string productName = "unknow";
            var identifier = PlayerSettings.applicationIdentifier;
            var strs = identifier.Split('.');
            if (strs.Length > 0)
            {
                productName = strs[strs.Length - 1];
            }
            AppBuildPipeline.productName = productName;
            Debug.Log("Xcode ProductName=" + productName);
            pbxProject.SetBuildProperty(unityMainTargetGuid, "PRODUCT_NAME_APP", productName);

            pbxProject.SetBuildProperty(unityMainTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(unityMainTargetGuid, "ENABLE_OBJECTIVE-C_EXCEPTIONS", "YES");
            pbxProject.SetBuildProperty(unityMainTargetGuid, "ENABLE_C++_EXCEPTIONS", "YES");
            pbxProject.SetBuildProperty(unityMainTargetGuid, "ENABLE_C++_RUNTIME_TYPES", "YES");
        }

        public void SwiftSetting()
        {
            //文件加入build phases
            var uibhPath = "Unity-iPhone-Bridging-Header.h";
            pbxProject.AddFile(uibhPath, uibhPath);

            var ndSwiftPath = "NdSwift.swift";
            var ndSwiftFileGuid = pbxProject.AddFile(ndSwiftPath, ndSwiftPath,PBXSourceTree.Source);
            pbxProject.AddFileToBuild(unityFrameworkTargetGuid, ndSwiftFileGuid);
            pbxProject.AddFileToBuild(unityMainTargetGuid, ndSwiftFileGuid);

            //设置开启swift  
            pbxProject.SetBuildProperty(unityMainTargetGuid, "SWIFT_OBJC_BRIDGING_HEADER", "Unity-iPhone-Bridging-Header.h");

            pbxProject.SetBuildProperty(unityMainTargetGuid, "SWIFT_OBJC_INTERFACE_HEADER_NAME", $"{AppBuildPipeline.productName}-Swift.h");
            pbxProject.SetBuildProperty(unityFrameworkTargetGuid, "SWIFT_OBJC_INTERFACE_HEADER_NAME", "UnityFramework-Swift.h");

            pbxProject.AddBuildProperty(unityMainTargetGuid, "SWIFT_VERSION", "5.0");
            pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "SWIFT_VERSION", "5.0");

            pbxProject.AddBuildProperty(unityMainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        }

        #endregion

        #region Mods
        public void ApplyMods(string[] pbxmods)
        {
            var mods = new List<XCMod>();
            foreach (string pbxmod in pbxmods)
            {
                Debug.Log("ProjMod File: " + pbxmod);
                XCMod mod = new XCMod(pbxmod);
                mods.Add(mod);
            }
            mods.Sort((l, r) => l.priority.CompareTo(r.priority));

            foreach (var xcMod in mods)
            {
                UnityEngine.Debug.Log("Apply Mod: " + xcMod.name);
                foreach (var lib in xcMod.libs)
                {
                    Debug.Log("Library: " + lib);
                }
                ApplyMod(xcMod);
            }
        }

        public void ApplyMod(string pbxmod)
        {
            XCMod mod = new XCMod(pbxmod);
            ApplyMod(mod);
        }

        public void ApplyMod(XCMod mod)
        {
            //The name of libs should be added in Xcode Build Phases, libz.dylib for example. If you want to import a .a lib to Xcode, add it as a file(in "files")
            Debug.Log("Adding libraries...");
            //系统库，非第三方库
            foreach (XCModFile libRef in mod.libs)
            {
                string completeLibPath = libRef.filePath;//System.IO.Path.Combine("usr/lib", libRef.filePath);
                Debug.Log("Adding library " + completeLibPath);
                pbxProject.AddFrameworkToProject(unityFrameworkTargetGuid, completeLibPath, libRef.isWeak);
            }
            //The name of framework should be added in Xcode Build Phases, Security.framework for example. If you want to add a third party framework, add it using a relative path to files section instead of here.
            Debug.Log("Adding frameworks...");
            //系统framework，非第三方framework
            foreach (string framework in mod.frameworks)
            {
                string[] filename = framework.Split(':');
                bool isWeak = (filename.Length > 1) ? true : false;
                string completePath = filename[0];
                Debug.Log("Add framework " + completePath + " isWeek:" + isWeak);
                //系统framework，后缀必须是.framework ， isWeak True 对应 Optional ,False 对应 Required
                pbxProject.AddFrameworkToProject(unityFrameworkTargetGuid, completePath, isWeak);
            }
            // Optional fields, support XCode 6+ Embed Framework feature. Notice: The frameworks must added already in frameworks or files fields.
            // 添加 到 Embed Frameworks,也就是设置成 Embed&sign，要加在 Unity-iPhone上，不是加载UnityFramework
            Debug.Log("Adding embed binaries...");
            if (mod.embed_binaries != null)
            {
                foreach (string binary in mod.embed_binaries)
                {
                    string absoluteFilePath = System.IO.Path.Combine(mod.path, binary);
                    AddEmbedFile(absoluteFilePath);
                }
            }
            //Files which should be added
            Debug.Log("Adding files...");
            //第三方文件,建议能使用文件夹就使用文件夹
            foreach (string filePath in mod.files)
            {
                string absoluteFilePath = System.IO.Path.Combine(mod.path, filePath);
                this.AddFile(absoluteFilePath);
            }
            //Folders which should be added. All file and folders(recursively) will be added
            Debug.Log("Adding folders...");
            //文件夹方式，更简洁的加文件，并且会主动加上 查找路径
            foreach (string folderPath in mod.folders)
            {
                string absoluteFolderPath = System.IO.Path.Combine(mod.path, folderPath);
                Debug.Log("Adding folder " + absoluteFolderPath);
                this.AddFolder(absoluteFolderPath, (string[])mod.excludes.ToArray(typeof(string)));
            }
            //Header Search Paths in Build Setting of Xcode
            Debug.Log("Adding headerpaths...");
            //添加头文件查找路径
            foreach (string headerpath in mod.headerpaths)
            {
                Debug.Log("add header path : " + headerpath);
                pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "HEADER_SEARCH_PATHS", "$(PROJECT_DIR)/" + headerpath);
            }

            //Optional fields,添加库文件查找路径
            Debug.Log("Adding librarypaths...");
            if (mod.librarypaths != null)
            {
                foreach (string librarypath in mod.librarypaths)
                {
                    Debug.Log("add library path :" + librarypath);
                    pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "LIBRARY_SEARCH_PATHS", librarypath);
                }
            }

            //Optional fields,添加runpath_search_paths
            Debug.Log("Adding runpaths...");
            if (mod.runpaths != null)
            {
                foreach (string runpath in mod.runpaths)
                {
                    Debug.Log("add runpath path :" + runpath);
                    pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "LD_RUNPATH_SEARCH_PATHS", runpath);

                }
            }

            //Compiler flags which should be added, e.g. "-Wno-vexing-parse"
            Debug.Log("Adding compiler flags...");
            foreach (string flag in mod.compiler_flags)
            {
                //OTHER_CFLAGS
                pbxProject.AddBuildProperty(unityMainTargetGuid, "OTHER_CFLAGS", flag);
                pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_CFLAGS", flag);
            }
            // Linker flags which should be added, e.g. "-ObjC"
            Debug.Log("Adding linker flags...");
            foreach (string flag in mod.linker_flags)
            {
                //OTHER_LDFLAGS
                pbxProject.AddBuildProperty(unityMainTargetGuid, "OTHER_LDFLAGS", flag);
                pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_LDFLAGS", flag);
            }

            if (mod.needSwift)
            {
                Debug.Log("Need Swift...");
                needSwiftModule = true;
            }

            Debug.Log("Adding Capability items...");
            foreach (var capability in mod.capabilitys)
            {
                //Debug.Log(capability);
                if (AddCapability(Convert.ToInt32(capability)))
                    hasCapabilityModify = true;
            }

            //edit the Info.plist file, only the urltype is supported currently. in url type settings, name and schemes are required, while role is optional and is Editor by default.
            Debug.Log("Adding plist items...");
            plist.Process(mod.plist);

            Debug.Log("Modify xcode classes...");
            XCCode xccode = new XCCode(this.projectRootPath);
            xccode.Process(mod.code_modifys);
        }

        #endregion

        public void Save()
        {
            //保存先后顺序会影响设置的内容
            if (hasCapabilityModify)
            {
                Debug.Log("Capability writeToFile");
                pbxProject.AddFile("Entitlements.entitlements", "Entitlements.entitlements");
                pbxProject.AddBuildProperty(unityMainTargetGuid, "CODE_SIGN_ENTITLEMENTS", "Entitlements.entitlements");
                capabilityManager.WriteToFile();
            }

            Debug.Log("pbxProject save =" + pbxProjPath);
            File.WriteAllText(pbxProjPath, pbxProject.WriteToString());

            plist.Save();

        }

        public void Dispose()
        {

        }
    }
}
#endif
