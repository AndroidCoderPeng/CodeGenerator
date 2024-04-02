using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeGenerator.Utils
{
    public static class MethodExtensions
    {
        /// <summary>
        /// 遍历文件夹得到所有文件后缀
        /// </summary>
        /// <param name="rootDir"></param>
        /// <returns></returns>
        public static List<FileInfo> GetDirFiles(this string rootDir)
        {
            return new DirectoryInfo(rootDir).GetFiles("*.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// 递归遍历路径，得到所有文件路径集合以及文件名集合
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static (List<string>, List<string>) TraverseFolder(this string path)
        {
            var fullPaths = new List<string>();
            var fileNames = new List<string>();
            if (string.IsNullOrEmpty(path))
            {
                return (fullPaths, fileNames);
            }

            var files = new DirectoryInfo(path).GetFiles("*.*", SearchOption.AllDirectories);
            fullPaths.AddRange(files.Select(file => file.FullName));
            fileNames.AddRange(files.Select(file => file.Name));
            return (fullPaths, fileNames);
        }
    }
}