using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CodeGenerator.Utils
{
    public static class MethodExtensions
    {
        /// <summary>
        /// 递归遍历路径，得到所有文件
        /// </summary>
        /// <param name="path">路径一定是存在的，因为路径是系统Dialog返回的</param>
        /// <param name="suffixes">后缀集合</param>
        /// <returns></returns>
        public static List<string> GetFilesBySuffix(this string path, HashSet<string> suffixes)
        {
            var result = new List<string>();
            var directoryInfo = new DirectoryInfo(path);
            foreach (var suffix in suffixes)
            {
                var files = directoryInfo.GetFiles($"{suffix}", SearchOption.AllDirectories);
                result.AddRange(files.Select(file => file.FullName));
            }

            return result;
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