using System.Collections.Generic;

namespace CodeGenerator.Utils
{
    public class RuntimeCache
    {
        public static readonly List<string> ImageSuffixArray = new List<string>
            { ".jpg", ".jpeg", ".ico", ".webp", ".png", ".bmp" };

        public static readonly List<string> TextSuffixArray = new List<string>
        {
            ".py",
            ".c", ".h", ".m",
            ".cpp",
            ".java",
            ".cs", ".xaml",
            ".js", ".ts", ".vue", ".cs", ".html",
            ".sql",
            ".php",
            ".go",
            ".swift",
            ".kt", ".xml",
            ".oc",
            ".dart",
            ".yml", ".gradle", ".properties", ".ini", ".config",
            ".md", ".txt", ".json", ".log",
            ".gitignore"
        };
    }
}