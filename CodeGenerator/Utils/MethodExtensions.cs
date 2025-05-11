using System;
using System.IO;

namespace CodeGenerator.Utils
{
    public static class MethodExtensions
    {
        /// <summary>
        /// 递归遍历路径，得到所有文件
        /// </summary>
        /// <param name="path">路径一定是存在的，因为路径是系统Dialog返回的</param>
        /// <returns></returns>
        public static FileInfo[] TraverseFolder(this string path)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(path);
                return directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is PathTooLongException)
            {
                return Array.Empty<FileInfo>();
            }
        }

        public static bool IsNumber(this string value)
        {
            return int.TryParse(value, out _);
        }
    }
}