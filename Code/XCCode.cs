using System.Collections;
using System.IO;
using UnityEngine;

namespace NdXUPorter
{
    public class XClass
    {
        private string filePath;

        public XClass(string fPath)
        {
            filePath = fPath;
        }
        /// <summary>
        /// 写在开头
        /// </summary>
        /// <param name="str"></param>
        public void WriteStart(string str)
        {
            Debug.Log("write start str=" + str);
            StreamReader streamReader = new StreamReader(filePath);
            string text_all = streamReader.ReadToEnd();
            streamReader.Close();
            text_all = str + "\n" + text_all;
            StreamWriter streamWriter = new StreamWriter(filePath);
            streamWriter.Write(text_all);
            streamWriter.Close();
        }
        /// <summary>
        /// 写在某句下面
        /// </summary>
        /// <param name="below"></param>
        /// <param name="text"></param>
        public void WriteBelow(string below, string text)
        {
            Debug.Log("write below ori=" + below + " ;det=" + text);
            StreamReader streamReader = new StreamReader(filePath);
            string text_all = streamReader.ReadToEnd();
            streamReader.Close();

            int beginIndex = text_all.IndexOf(below);
            if (beginIndex == -1)
            {
                Debug.LogError(filePath + "中没有找到文本" + below);
                return;
            }

            int endIndex = text_all.LastIndexOf("\n", beginIndex + below.Length);

            text_all = text_all.Substring(0, endIndex) + "\n" + text + "\n" + text_all.Substring(endIndex);

            StreamWriter streamWriter = new StreamWriter(filePath);
            streamWriter.Write(text_all);
            streamWriter.Close();
        }
        /// <summary>
        /// 替换
        /// </summary>
        public void Replace(string replace, string newText)
        {
            Debug.Log("write repalce ori=" + replace + " ;det=" + newText);
            StreamReader streamReader = new StreamReader(filePath);
            string text_all = streamReader.ReadToEnd();
            streamReader.Close();

            int beginIndex = text_all.IndexOf(replace);
            if (beginIndex == -1)
            {
                Debug.LogError(filePath + "中没有找到文本" + replace);
                return;
            }

            text_all = text_all.Replace(replace, newText);
            StreamWriter streamWriter = new StreamWriter(filePath);
            streamWriter.Write(text_all);
            streamWriter.Close();

        }
    }

    public class XCCode
    {
        /* 示例：
         "code_modify":[
		    {"path":"/UnityAppController.h","operate":"below","ori":"#import <QuartzCore/CADisplayLink.h>","det":"//使用below 写在下面"},
		    {"path":"/UnityAppController.h","operate":"start","det":"//使用start 写在开头"},
		    {"path":"/UnityAppController.h","operate":"replace","ori":"#import <QuartzCore/CADisplayLink.h>","det":"#import <QuartzCore/CADisplayLink.h>//使用replace 替换"},
          ]
         */
        private static string CCodeFile = "path";
        private static string CCodeOperate = "operate";
        private static string CCodeOri = "ori";
        private static string CCodeDet = "det";

        private string classesPath;

        public XCCode(string classesPath)
        {
            this.classesPath = classesPath;
            Debug.Log("初始化 classes 路径=" + classesPath);
        }

        public void Process(ArrayList codeModify)
        {
            foreach (Hashtable table in codeModify)
            {
                string file = (string)table[CCodeFile];
                if (string.IsNullOrEmpty(file))
                {
                    continue;
                }
                else
                {
                    file = classesPath + file;
                }

                if (!System.IO.File.Exists(file))
                {
                    Debug.LogError(file + "路径下文件不存在");
                    return;
                }

                var xclass = new XClass(file);

                string opt = (string)table[CCodeOperate];

                switch (opt)
                {
                    case "start":
                        xclass.WriteStart((string)table[CCodeDet]);
                        break;
                    case "below":
                        xclass.WriteBelow((string)table[CCodeOri], (string)table[CCodeDet]);
                        break;
                    case "replace":
                        xclass.Replace((string)table[CCodeOri], (string)table[CCodeDet]);
                        break;
                    default:
                        Debug.LogError("operate 必须是  start below replace！！");
                        break;
                }
            }
        }
    }
}
