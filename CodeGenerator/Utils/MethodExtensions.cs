using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CodeGenerator.Models;

namespace CodeGenerator.Utils
{
    public static class MethodExtensions
    {
        /// <summary>
        /// 遍历文件夹并生成相应的数据类型集合
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="codeFiles"></param>
        public static void TraverseFolder(this string folderPath, List<CodeFile> codeFiles)
        {
            var files = new DirectoryInfo(folderPath).GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var codeFile = new CodeFile
                {
                    FileName = file.Name,
                    FullPath = file.FullName,
                    FileSuffix = file.Extension
                };

                codeFiles.Add(codeFile);
            }
        }

        public static bool IsNumber(this string value)
        {
            return int.TryParse(value, out _);
        }

        /// <summary>
        /// List 转 ObservableCollection
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this List<T> list)
        {
            var collection = new ObservableCollection<T>();
            foreach (var t in list)
            {
                collection.Add(t);
            }

            return collection;
        }
    }
}