using System.IO;

namespace CodeGenerator.Utils
{
    public static class MethodExtensions
    {
        /// <summary>
        /// 递归遍历路径，得到所有文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static FileInfo[] TraverseFolder(this string path)
        {
            return string.IsNullOrEmpty(path)
                ? null
                : new DirectoryInfo(path).GetFiles("*.*", SearchOption.AllDirectories);
        }
    }
}