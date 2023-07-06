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

        const string XCODE_IMAGES_FOLDER = "Unity-iPhone/Images.xcassets";
        const string SOURCE_FOLDER_NAME = "LaunchImageLandscape.imageset";
        static string SOURCE_FOLDER_ROOT = Application.dataPath + "/../../Code/XCodeProjs/SplashLauncher/SplashLauncher/Assets.xcassets";
        static string STOTYBOARD_FOLDER_ROOT = Application.dataPath + "/../../Code/XCodeProjs/SplashLauncher/SplashLauncher/Base.lproj";
        private const string STORYBOARD_NAME = "LaunchScreen.storyboard";

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
            project.CustomMod();
            //是否需求Swift混编
            if (project.needSwiftModule) OpenSwift(project, pathToBuiltProject);
            //构建结束并保存
            project.Save();

            //启动页构建
            CopyXcodeLanchImage(pathToBuiltProject);
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

        //Xcode storyboard 开屏页 UI资源
        static void CopyXcodeLanchImage(string path)
        {
            string sourcePath = $"{SOURCE_FOLDER_ROOT}/{SOURCE_FOLDER_NAME}";
            string targetPath = $"{path}/{XCODE_IMAGES_FOLDER}/{SOURCE_FOLDER_NAME}";
            FileUtil.DeleteFileOrDirectory(targetPath);
            FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
            //storyboard
            sourcePath = $"{STOTYBOARD_FOLDER_ROOT}/{STORYBOARD_NAME}";
            targetPath = $"{path}/{STORYBOARD_NAME}";
            FileUtil.DeleteFileOrDirectory(targetPath);
            FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
        }
#if UNITY_EDITOR_OSX
        static void OpenSwift(XCProject project, string xcodeProjPath)
        {
            string dirPath = kChannelFolder + "swiftFolder";
            if (Directory.Exists(dirPath))
            {
                Debug.Log($"拷贝SwiftFolder:{dirPath}");
                var path = xcodeProjPath;
                //将目标文件拷贝到Xcode工程里
                FileUtils.CopyFilesToDestDirRecursive(dirPath, path, ".meta");
                //添加构建配置
                project.SwiftSetting();
            }
        }
#endif
    }
}
