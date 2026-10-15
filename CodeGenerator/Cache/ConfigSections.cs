namespace CodeGenerator.Cache
{
    // 分组名 + 文件名常量
    public static class ConfigSections
    {
        public const string FileName = "app-config.json";
        public const string Suffix = "Suffix";
    }
    
    // 安装包归档分组
    public class FileSuffixConfig
    {
        public string Type { get; set; }
    }
}