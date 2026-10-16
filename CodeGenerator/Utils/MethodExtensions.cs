using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeGenerator.Utils
{
    public static class MethodExtensions
    {
        public static string FormatFileSize(this long bytes)
        {
            const long kilobyte = 1024;
            const long megabyte = kilobyte * 1024;
            const long gigabyte = megabyte * 1024;

            return bytes switch
            {
                >= gigabyte => $"{bytes / (double)gigabyte:F2} GB",
                >= megabyte => $"{bytes / (double)megabyte:F2} MB",
                >= kilobyte => $"{bytes / (double)kilobyte:F2} KB",
                _ => $"{bytes} B"
            };
        }

        public static string GetFileTypeColor(this string fileExtension)
        {
            var extension = fileExtension?.Trim().ToLowerInvariant();
            return extension switch
            {
                ".py" => "#4B8BBE",
                ".c" => "#A8B9CC",
                ".cpp" or ".cxx" or ".cc" or ".hpp" => "#F34B7D",
                ".java" => "#E76F00",
                ".cs" => "#7B4FE0",
                ".js" or ".mjs" or ".cjs" => "#EFD81D",
                ".ts" or ".tsx" => "#3178C6",
                ".sql" => "#CC2927",
                ".vb" or ".vbs" => "#945DB7",
                ".go" => "#29BEB0",
                ".pas" or ".pp" or ".dpr" => "#E3A300",
                ".pl" or ".pm" => "#39457E",
                ".f" or ".for" or ".f90" or ".f95" or ".f03" => "#734F96",
                ".php" => "#777BB4",
                ".r" => "#276DC3",
                ".m" => "#E16737",
                ".rs" => "#DEA584",
                ".asm" or ".s" => "#6E4C13",
                ".cob" or ".cbl" => "#005CA9",
                ".swift" => "#F05138",
                ".rb" => "#CC342D",
                ".kt" or ".kts" => "#A97BFF",

                ".html" or ".htm" => "#E44D26",
                ".css" or ".scss" or ".sass" or ".less" => "#563D7C",
                ".vue" => "#41B883",
                ".md" or ".markdown" => "#519ABA",
                ".json" => "#8BC9FF",
                ".xaml" => "#2FA8E0",
                _ => GetDeterministicColor(extension)
            };
        }

        private static string GetDeterministicColor(string value)
        {
            var hash = 2166136261u;

            foreach (var character in value ?? string.Empty)
            {
                hash ^= character;
                hash *= 16777619;
            }

            var red = 80 + (int)(hash & 0x7F);
            var green = 80 + (int)((hash >> 8) & 0x7F);
            var blue = 80 + (int)((hash >> 16) & 0x7F);

            return $"#{red:X2}{green:X2}{blue:X2}";
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