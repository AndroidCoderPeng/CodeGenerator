using System.Collections.Generic;
using System.Linq;
using CodeGenerator.Models;

namespace CodeGenerator.DataService.Impl
{
    public class AppDataServiceImpl : IAppDataService
    {
        private readonly List<string> _codeTypeList = new List<string>
        {
            "*.py", // Python
            "*.cpp", "*.hpp", // C++
            "*.c", "*.h", // C
            "*.java", // Java
            "*.cs", // C#
            "*.js", // JavaScript
            "*.go", // Go
            "*.vb", // Visual Basic
            "*.vue", // Vue.js
            "*.html", //HTML
            "*.ts", // TypeScript
            "*.php", // PHP
            "*.rs", // Rust
            "*.swift", // Swift
            "*.kt", // Kotlin
            "*.rb", // Ruby
            "*.sh", // Shell Script
            "*.m" // Object-c
        };

        public List<DefaultSuffix> GetCodeTypeList()
        {
            return _codeTypeList.Select(suffix => new DefaultSuffix { Suffix = suffix, IsChecked = false }).ToList();
        }
    }
}