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
    }
}