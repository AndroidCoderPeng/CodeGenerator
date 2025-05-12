using System.Collections.Generic;

namespace CodeGenerator.DataService.Impl
{
    public class AppDataServiceImpl : IAppDataService
    {
        public List<string> GetCodeTypeList()
        {
            return new List<string>
            {
                "*.py", // Python
                "*.cpp", "*.hpp", // C++
                "*.c", "*.h", // C
                "*.java", // Java
                "*.cs", // C#
                "*.js", // JavaScript
                "*.go", // Go
                "*.vb", // Visual Basic
                "*.sql", // SQL
                "*.vue", // Vue.js
                "*.html", //HTML
                "*.ts", // TypeScript
                "*.php", // PHP
                "*.rs", // Rust
                "*.swift", // Swift
                "*.kt", // Kotlin
                "*.scala", // Scala
                "*.rb", // Ruby
                "*.pl", // Perl
                "*.cobol", // COBOL
                "*.f90", // Fortran
                "*.sh", // Shell Script
                "*.m" // Object-c
            };
        }
    }
}