using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

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

        public static void TraverseFolder(this string path, ObservableCollection<FileInfo> result)
        {
            try
            {
                Parallel.ForEach(Directory.GetFiles(path), file => { result.Add(new FileInfo(file)); });

                foreach (var dir in Directory.GetDirectories(path))
                {
                    TraverseFolder(dir, result);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                // 可选：记录无法访问的目录
                Console.WriteLine($@"无法访问目录：{path}，原因：{ex.Message}");
            }
        }

        public static bool IsNumber(this string value)
        {
            return int.TryParse(value, out _);
        }
    }
}