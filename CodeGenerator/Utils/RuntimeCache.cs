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

        //代码量按前、后各连续30页，共60页，（不足60页全部提交）第60页为模块结束页，每页不少于50行（结束页除外）
        public const int EffectiveCodeCount = 60 * 55;
    }
}