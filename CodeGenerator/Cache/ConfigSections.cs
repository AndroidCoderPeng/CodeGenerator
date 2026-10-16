using System.Collections.Generic;

namespace CodeGenerator.Cache
{
    public class ConfigSections
    {
        public const string FileName = "app-config.json";
        public const string BlackList = "BlackList";
    }

    public class BlackListConfig
    {
        public List<string> Folders { get; set; } =
        [
            // 版本控制与 IDE 配置
            ".git", ".svn", ".hg", ".vs", ".vscode", ".idea", ".fleet", ".settings",

            // JavaScript
            "node_modules", "bower_components", ".next", ".nuxt", ".output", ".svelte-kit", ".vite",
            "dist", "coverage", ".nyc_output",

            // Python
            "__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache", ".tox", ".nox",
            ".venv", "venv", "env", ".eggs", "site-packages",

            // .NET、Java、Go 与 C / C++
            "bin", "obj", "Debug", "Release", "x64", "x86", "TestResults", ".nuget", "packages",
            "target", "build", "out", ".gradle", ".mvn", "CMakeFiles", "cmake-build-debug",
            "cmake-build-release", "bazel-bin", "bazel-out", "bazel-testlogs", "vendor",

            // R、MATLAB 与 Delphi / Object Pascal
            ".Rproj.user", "slprj", "codegen", "work",
            "__history", "Win32", "Win64", "Linux64", "OSX64", "Android", "iOS",

            // 临时文件与通用缓存
            ".cache", "tmp", "temp", "logs", "htmlcov"
        ];

        public List<string> Files { get; set; } =
        [
            // 压缩、映射与前端构建产物
            ".min.js", ".min.css", ".map", ".tsbuildinfo",

            // .NET 自动生成与编译产物
            ".designer.cs", ".generated.cs", ".g.cs", ".g.i.cs", ".AssemblyInfo.cs", ".AssemblyAttributes.cs",
            ".deps.json", ".runtimeconfig.json", ".dll", ".exe", ".pdb", ".nupkg", ".snupkg",

            // C / C++、Fortran 与 Delphi / Object Pascal 编译产物
            ".o", ".obj", ".a", ".lib", ".so", ".dylib", ".exp", ".ilk", ".idb",
            ".mod", ".smod", ".dcu", ".dcp", ".bpl",

            // Java、Python、PHP、R、MATLAB 与 Visual Basic 构建产物或缓存
            ".class", ".jar", ".war", ".ear", ".pyc", ".pyo", ".pyd", ".whl", ".phar",
            ".RData", ".Rhistory", ".asv", ".mexw64", ".mexa64", ".mexmaci64",
            ".Designer.vb", ".My Project.AssemblyInfo.vb",

            // 常见代码生成器输出
            ".pb.cs", ".pb.go", ".pb.cc", ".pb.cpp", ".pb.h", ".pb.hpp", ".generated.java"
        ];
    }
}