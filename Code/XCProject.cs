#if !UNITY_EDITOR_OSX
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

namespace NdXUPorter
{
    public class XCProject : System.IDisposable
    {
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
            PBXFrameworksBuildPhase,
            /// <summary>
            /// .h .m .mm .c .cpp 等源文件，target 选择 UnityFramework，BuildPhase 选择 SourcesBuildPhase
            /// </summary>
            PBXSourcesBuildPhase,
            /// <summary>
            /// .json .plist .bundle .config 等资源文件，target 选择 Unity-iPhone，BuildPhase 选择 ResourcesBuildPhase
            /// </summary>
            PBXResourcesBuildPhase,
        }
        public static readonly Dictionary<string, PbxBuildType> typePhases = new Dictionary<string, PbxBuildType> {
            { ".a", PbxBuildType.PBXFrameworksBuildPhase },
            { ".app", PbxBuildType.Null },
            { ".s", PbxBuildType.PBXSourcesBuildPhase },
            { ".c", PbxBuildType.PBXSourcesBuildPhase },
            { ".cpp", PbxBuildType.PBXSourcesBuildPhase },
            { ".framework", PbxBuildType.PBXFrameworksBuildPhase },
            { ".h", PbxBuildType.PBXSourcesBuildPhase },
            { ".pch", PbxBuildType.Null },
            { ".icns", PbxBuildType.PBXResourcesBuildPhase },
            { ".m", PbxBuildType.PBXSourcesBuildPhase },
            { ".mm", PbxBuildType.PBXSourcesBuildPhase },
            { ".xcdatamodeld", PbxBuildType.PBXSourcesBuildPhase },
            { ".nib", PbxBuildType.PBXResourcesBuildPhase },
            { ".plist", PbxBuildType.PBXResourcesBuildPhase },
            { ".png", PbxBuildType.PBXResourcesBuildPhase },
            { ".rtf", PbxBuildType.PBXResourcesBuildPhase },
            { ".tiff", PbxBuildType.PBXResourcesBuildPhase },
            { ".txt", PbxBuildType.PBXResourcesBuildPhase },
            { ".json", PbxBuildType.PBXResourcesBuildPhase },
            { ".xcodeproj", PbxBuildType.Null },
            { ".xib", PbxBuildType.PBXResourcesBuildPhase },
            { ".strings", PbxBuildType.PBXResourcesBuildPhase },
            { ".bundle", PbxBuildType.PBXResourcesBuildPhase },
            { ".dylib", PbxBuildType.PBXFrameworksBuildPhase },
            { ".tbd", PbxBuildType.PBXFrameworksBuildPhase },
            { ".inl", PbxBuildType.PBXResourcesBuildPhase },
            { ".mom", PbxBuildType.PBXResourcesBuildPhase },
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
        /// <summary>
        /// Unity-iPhone guid
        /// </summary>
        public string unityMainTargetGuid { get; private set; }
        /// <summary>
        /// UnityFramework库
        /// </summary>
        public string unityFrameworkTargetGuid { get; private set; }

        private HashSet<string> usedFileNames = new HashSet<string>();

        #region Constructor


        public XCProject(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                Debug.LogWarning("XCode project path does not exist: " + filePath);
                return;
            }

            projectRootPath = filePath;
            pbxProjPath = PBXProject.GetPBXProjectPath(filePath);
            pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(pbxProjPath));

            var unityMainTargetGuidMethod = pbxProject.GetType().GetMethod("GetUnityMainTargetGuid");
            var unityFrameworkTargetGuidMethod = pbxProject.GetType().GetMethod("GetUnityFrameworkTargetGuid");
            if (unityMainTargetGuidMethod != null && unityFrameworkTargetGuidMethod != null)
            {
                unityMainTargetGuid = (string)unityMainTargetGuidMethod.Invoke(pbxProject, null);
                unityFrameworkTargetGuid = (string)unityFrameworkTargetGuidMethod.Invoke(pbxProject, null);
            }
            else
            {
                unityMainTargetGuid = pbxProject.TargetGuidByName("Unity-iPhone");
                unityFrameworkTargetGuid = unityMainTargetGuid;
            }

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
            if(relativePath.StartsWith("/"))
            {
                relativePath = relativePath.Substring(1);
            }
            //Debug.Log(relativePath);
            string fileGuid;
            switch (buildPhase)
            {
                case PbxBuildType.PBXFrameworksBuildPhase:
                    var dirPath = GetRelativeDirPath(filePath);
                    if (extension == ".framework")
                        AddFrameworkSearchPath(unityFrameworkTargetGuid, dirPath);
                    else
                    {
                        AddLibrarySearchPath(unityFrameworkTargetGuid, dirPath);
                    }
                    pbxProject.AddFileToBuild(unityFrameworkTargetGuid, pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source));
                    break;
                case PbxBuildType.PBXResourcesBuildPhase:
                    pbxProject.AddFileToBuild(unityMainTargetGuid, pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source));
                    break;
                case PbxBuildType.PBXSourcesBuildPhase:
                    fileGuid = pbxProject.AddFile(relativePath, relativePath, PBXSourceTree.Source);
                    //.h 文件不需要加入BuildPhase
                    if (extension == ".h") break;
                    var sourcesBuildPhase = pbxProject.GetSourcesBuildPhaseByTarget(unityFrameworkTargetGuid);
                    pbxProject.AddFileToBuildSection(unityFrameworkTargetGuid, sourcesBuildPhase, fileGuid);
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
                string completePath = filename[0];//System.IO.Path.Combine("System/Library/Frameworks", filename[0]);
                pbxProject.AddFrameworkToProject(unityFrameworkTargetGuid, completePath, isWeak);
            }

            //Files which should be added
            Debug.Log("Adding files...");
            //第三方文件,建议能使用文件夹就使用文件夹
            foreach (string filePath in mod.files)
            {
                string absoluteFilePath = System.IO.Path.Combine(mod.path, filePath);
                this.AddFile(absoluteFilePath);
            }
            // Optional fields, support XCode 6+ Embed Framework feature. Notice: The frameworks must added already in frameworks or files fields.
            Debug.Log("Adding embed binaries...");
            if (mod.embed_binaries != null)
            {
                //TODO:后续看需求做
                Debug.LogWarning("embed binaries want to do");
                // pbxProject.SetBuildProperty(unityFrameworkTargetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");
                //
                // var frameworksBuildPhase = pbxProject.GetFrameworksBuildPhaseByTarget(unityFrameworkTargetGuid);
                // foreach (string binary in mod.embed_binaries)
                // {
                //     string absoluteFilePath = System.IO.Path.Combine(mod.path, binary);
                //     string fileGuid = pbxProject.AddFile(absoluteFilePath, projectRootPath);
                //     PBXProjectExtensions.AddFileToEmbedFrameworks(pbxProject, unityMainTargetGuid, fileGuid);
                //     pbxProject.AddFileToBuildSection(unityFrameworkTargetGuid, frameworksBuildPhase, fileGuid);
                // }
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
            //edit the Info.plist file, only the urltype is supported currently. in url type settings, name and schemes are required, while role is optional and is Editor by default.
            Debug.Log("Adding plist items...");
            string plistPath = Path.Combine(this.projectRootPath ,"Info.plist");
            XCPlist plist = new XCPlist(plistPath);
            plist.Process(mod.plist);

            Debug.Log("Modify xcode classes...");
            XCCode xccode = new XCCode(this.projectRootPath);
            xccode.Process(mod.code_modifys);
        }

        #endregion

        public void Save()
        {
            Debug.Log("pbxProject save ="+ pbxProjPath);
            File.WriteAllText(pbxProjPath, pbxProject.WriteToString());
        }

        public void Dispose()
        {

        }
    }
}
#endif
