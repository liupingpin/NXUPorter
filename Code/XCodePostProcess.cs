using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using System.IO;
using AppInfos;

namespace NdXUPorter
{
    public static class XCodePostProcess
    {
        private const string kChannelFolder = "ChannelBuild_IOS/";
        private const string kIOSDefaltPath = "Assets/Editor/XUPorter/Mods";
        private const string kCommonFolder = "ChannelBuild_IOS/commonFolder/sdk";
        private const string kSdkFolder = "/sdk";
        private const string kSdkTestConfigFolder = "/sdk-sdk_testConfig";
        private const string kXcodeSdkFolder = "/pluginSdk";

#if UNITY_EDITOR && UNITY_EDITOR_OSX
        /// <summary>
        /// 这里是通过配置文件去
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pathToBuiltProject"></param>
        [PostProcessBuild(999)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                Debug.LogWarning("Target is not iPhone. XCodePostProcess will not run");
                return;
            }
            //第一步必须拷贝sdk到xcode工程
            CopySdkToXcodeProj(pathToBuiltProject);
            //初始化构建
            XCProject project = new XCProject(pathToBuiltProject);
            //开始构建
            string[] files = Directory.GetFiles(pathToBuiltProject, "*.projmods",
                SearchOption.AllDirectories);
            project.ApplyMods(files);
            //自定义特殊的构建
            CustomEditPBXProject(project);
            //构架结束并保存
            project.Save();
        }

        //将大部分动态配置放在XUPorter来构建。
        //要注意勾选掉 select platform for plugins 的 ios 选项，防止unity 自身拷贝到xcode 导致 xuporter 导出失败。
        private static void CustomEditPBXProject(XCProject project)
        {
            var proj = project.pbxProject;
            proj.SetBuildProperty(project.unityMainTargetGuid, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(project.unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");
            proj.SetBuildProperty(project.unityMainTargetGuid, "ENABLE_OBJECTIVE-C_EXCEPTIONS", "YES");
            proj.SetBuildProperty(project.unityMainTargetGuid, "ENABLE_C++_EXCEPTIONS", "YES");
            proj.SetBuildProperty(project.unityMainTargetGuid, "ENABLE_C++_RUNTIME_TYPES", "YES");

            //ChannelBuild_IOS\commonFolder下的默认路径
            string common_path = "ChannelBuild_IOS/commonFolder/";
            string entitlement_path = common_path;
            var path = project.projectRootPath;
            if (AppBuildPipeline.iosChannelInfo != null)
            {
                string f_name = ((EChannelId)AppBuildPipeline.iosChannelInfo.id).ToString() + "/";
                entitlement_path = "ChannelBuild_IOS/" + f_name;
            }
            //Capability
            var id_array = PlayerSettings.applicationIdentifier.Split('.');
            var a_name = "zysy";
            if (id_array.Length == 3)
                a_name = id_array[2];
            var file_name = a_name + ".entitlements";
            var src = entitlement_path + file_name;
            //向判断下渠道路径下是否存在，不存在用通用路径下的配置
            if (!File.Exists(src))
            {
                src = common_path + file_name;
            }
            //最后拷贝entitlements 来修改 Xcode的Capability
            //ProjectCapabilityManager
            if (File.Exists(src))
            {
                var target_name = "Unity-iPhone";
                var dst = path + "/" + target_name + "/" + file_name;
                Debug.Log($"Capability : 拷贝{src} 到 {dst}");
                FileUtil.CopyFileOrDirectory(src, dst);
                proj.AddFile(target_name + "/" + file_name, file_name);
                proj.AddBuildProperty(project.unityMainTargetGuid, "CODE_SIGN_ENTITLEMENTS", target_name + "/" + file_name);
            }
            else
            {
                Debug.LogWarning($"不存在 {src}");
            }
        }
#endif
        /// <summary>
        /// 流程：
        /// 1、拷贝Unity下的Editor/XUPorter/Mods 下的基础插件到XCode工程目录
        /// 2、拷贝sdk 插件到Xcode工程目录
        /// </summary>
        private static void CopySdkToXcodeProj(string pathToBuiltProject)
        {
            var xcodeProjPath = pathToBuiltProject;
            if (AppBuildPipeline.iosChannelInfo == null)
            {
                //打不带sdk的包
                var path = xcodeProjPath + kXcodeSdkFolder;
                FileUtils.EnsureDirectoryExists(path);
                //拷贝不带SDK的纯净文件到xcode 工程目录
                FileUtils.CopyFilesToDestDirRecursive(kIOSDefaltPath, path, ".meta");
                //默认集成bugly
                ImportBugly(xcodeProjPath);

                AssetDatabase.Refresh();
                return;
            }

            if (AppBuildPipeline.iosChannelInfo.needBugly)
            {
                //集成bugly
                ImportBugly(xcodeProjPath);
            }

            // 把文件夹里的内容拷过去
            string infoDirName = GetInfoName(AppBuildPipeline.iosChannelInfo).TrimEnd('-');
            string dirPath = kChannelFolder + infoDirName + kSdkFolder;
            if (Directory.Exists(dirPath))
            {
                var path = xcodeProjPath + kXcodeSdkFolder;
                FileUtils.EnsureDirectoryExists(path);
                //拷贝不带SDK的纯净文件到xcode 工程目录
                FileUtils.CopyFilesToDestDirRecursive(kIOSDefaltPath, path, ".meta");
                //拷贝通用SDK文件到项目里
                FileUtils.CopyFilesToDestDirRecursive(kCommonFolder, path, ".meta");
                //将目标文件拷贝到项目里 
                FileUtils.CopyFilesToDestDirRecursive(dirPath, path, ".meta");

                //覆盖测试文件
                if (AppBuildPipeline.isTest)
                {
                    string dirConfigPath = kChannelFolder + infoDirName + kSdkTestConfigFolder;
                    if (Directory.Exists(dirConfigPath))
                    {
                        FileUtils.CopyFilesToDestDirRecursive(dirConfigPath, path, ".meta");
                    }
                }
            }

            AssetDatabase.Refresh();
        }

        private static string GetInfoName(ChannelInfo_IOS info)
        {
            string name = ((EChannelId)info.id).ToString();
            return name.ToLower().TrimStart('_') + "-";
        }

        //集成bugly
        static void ImportBugly(string xcodeProjPath)
        {
            string dirPath = kChannelFolder + "modules/Bugly";

            var path = xcodeProjPath + kXcodeSdkFolder;
            FileUtils.EnsureDirectoryExists(path);
            //将目标文件拷贝到项目里 
            FileUtils.CopyFilesToDestDirRecursive(dirPath, path, ".meta");
        }
    }
}
