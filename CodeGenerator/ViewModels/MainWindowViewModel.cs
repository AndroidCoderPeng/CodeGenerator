using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using CodeGenerator.Cache;
using CodeGenerator.Models;
using CodeGenerator.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessageBox = System.Windows.MessageBox;

namespace CodeGenerator.ViewModels
{
    [SupportedOSPlatform("windows6.1")]
    public partial class MainWindowViewModel : ObservableObject
    {
        private sealed record CodeReadResult(IReadOnlyList<string> Lines, IReadOnlyList<string> Errors);

        #region VM

        [ObservableProperty] private string _codeFolder;
        [ObservableProperty] private BlackListConfig _blackListConfig;
        [ObservableProperty] private ObservableCollection<CodeFile> _selectedFiles = [];
        [ObservableProperty] private string _documentHeader;
        [ObservableProperty] private string _softwareVersion = "V1.0.0";
        [ObservableProperty] private string _documentPage = "60";
        [ObservableProperty] private string _docFontSize = "8";
        [ObservableProperty] private string _userDefinedFileSuffix;
        [ObservableProperty] private ObservableCollection<string> _userDefinedFileSuffixTypes = [];
        [ObservableProperty] private ObservableCollection<string> _defaultFileSuffixTypes;
        [ObservableProperty] private string _currentFile = "准备就绪";
        [ObservableProperty] private double _currentProgress;
        [ObservableProperty] private bool _isToastVisible;
        [ObservableProperty] private string _toastMessage;

        #endregion

        private readonly List<string> _codeTypes =
        [
            "*.cpp", "*.hpp",
            "*.c", "*.h",
            "*.cs",
            "*.kt",
            "*.java",
            "*.swift",
            "*.rs",
            "*.vb",
            "*.py",
            "*.js",
            "*.go",
            "*.vue",
            "*.html",
            "*.ts",
            "*.php",
            "*.rb",
            "*.sh",
            "*.m"
        ];

        private readonly IDialogService _dialogService;

        public ObservableCollection<string> WhiteListSuffix =>
            [.. DefaultFileSuffixTypes.Concat(UserDefinedFileSuffixTypes)];

        private IReadOnlyList<string> _effectiveCodeLines = [];
        private string _outputFilePath;
        private DispatcherTimer _toastTimer;

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            var sourceFolderConfig =
                SettingsStore.Load<SourceFolderConfig>(ConfigSections.FileName, ConfigSections.SourceFolder);
            CodeFolder = sourceFolderConfig.Path;
            BlackListConfig = SettingsStore.Load<BlackListConfig>(ConfigSections.FileName, ConfigSections.BlackList);
            DefaultFileSuffixTypes = _codeTypes.ToObservableCollection();

            var suffixConfig =
                SettingsStore.Load<UserDefinedFileSuffixConfig>(ConfigSections.FileName,
                    ConfigSections.UserDefinedFileSuffix);

            UserDefinedFileSuffixTypes = suffixConfig.Suffixes
                .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
                .Select(NormalizeSuffix)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                .ToObservableCollection();

            // 监听默认文件后缀和用户自定义文件后缀的变化，更新白名单后缀集合
            DefaultFileSuffixTypes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(WhiteListSuffix));
            UserDefinedFileSuffixTypes.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(WhiteListSuffix));
                SettingsStore.Save(ConfigSections.FileName,
                    ConfigSections.UserDefinedFileSuffix,
                    new UserDefinedFileSuffixConfig
                    {
                        Suffixes = [.. UserDefinedFileSuffixTypes]
                    });
            };

            var outputPathConfig = SettingsStore.Load<OutputPathConfig>(ConfigSections.FileName,
                ConfigSections.OutputPath);
            _outputFilePath = outputPathConfig.Path;
        }

        [RelayCommand]
        private void SelectCodeFolder()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;

            CodeFolder = dialog.SelectedPath;

            // 缓存源码目录
            SettingsStore.Save(ConfigSections.FileName, ConfigSections.SourceFolder,
                new SourceFolderConfig { Path = CodeFolder });

            _ = TraverseFolderAsync();
        }

        [RelayCommand]
        private void ReTraverseFolder()
        {
            if (string.IsNullOrWhiteSpace(CodeFolder) || !Directory.Exists(CodeFolder))
            {
                return;
            }

            _ = TraverseFolderAsync();
        }

        /// <summary>
        /// 根据遍历规则遍历文件夹，获取所有文件，不设置白名单就获取所有文件，设置了白名单就获取白名单内的文件
        /// </summary>
        private async Task TraverseFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(CodeFolder) || !Directory.Exists(CodeFolder))
            {
                SelectedFiles.Clear();
                return;
            }

            var config = BlackListConfig;
            var files = await Task.Run(() => TraverseFiles(CodeFolder, config, WhiteListSuffix));
            SelectedFiles = files.ToObservableCollection();
        }

        private static List<CodeFile> TraverseFiles(
            string rootPath,
            BlackListConfig config,
            IEnumerable<string> whiteListSuffixes)
        {
            var result = new List<CodeFile>();
            var blackListFolders = config.Folders
                .Where(folder => !string.IsNullOrWhiteSpace(folder))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var blackListFiles = config.Files
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .ToArray();

            var suffixes = whiteListSuffixes
                .Where(suffix => !string.IsNullOrWhiteSpace(suffix))
                .Select(NormalizeSuffix)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var maxDepth = Math.Max(0, config.MaxTraverseDepth);
            var maxFileCount = Math.Max(0, config.MaxFileCount);
            var maxFileSizeBytes = Math.Max(0L, config.FileSizeLimit) * 1024 * 1024;

            var directories = new Stack<(string Path, int Depth)>();
            directories.Push((rootPath, 0));

            while (directories.Count > 0 && result.Count < maxFileCount)
            {
                var (directoryPath, depth) = directories.Pop();

                try
                {
                    var directoryInfo = new DirectoryInfo(directoryPath);
                    var directoryAttributes = directoryInfo.Attributes;

                    if (config.SkipSymbol && directoryAttributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        continue;
                    }

                    if (config.SkipHiddenFile && directoryAttributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    foreach (var filePath in Directory.EnumerateFiles(directoryPath))
                    {
                        if (result.Count >= maxFileCount)
                        {
                            break;
                        }

                        try
                        {
                            var fileInfo = new FileInfo(filePath);
                            var fileAttributes = fileInfo.Attributes;

                            if (config.SkipSymbol && fileAttributes.HasFlag(FileAttributes.ReparsePoint))
                            {
                                continue;
                            }

                            if (config.SkipHiddenFile && fileAttributes.HasFlag(FileAttributes.Hidden))
                            {
                                continue;
                            }

                            if (blackListFiles.Any(rule =>
                                    fileInfo.Name.EndsWith(rule, StringComparison.OrdinalIgnoreCase)))
                            {
                                continue;
                            }

                            if (suffixes.Count > 0 && !suffixes.Contains(fileInfo.Extension))
                            {
                                continue;
                            }

                            if (fileInfo.Length > maxFileSizeBytes)
                            {
                                continue;
                            }

                            result.Add(new CodeFile
                            {
                                FilePath = fileInfo.FullName,
                                FileName = fileInfo.Name,
                                FileSuffix = fileInfo.Extension,
                                FileTypeColor = fileInfo.Extension.GetFileTypeColor(),
                                FileSize = fileInfo.Length.FormatFileSize()
                            });
                        }
                        catch (UnauthorizedAccessException exception)
                        {
                            Console.WriteLine($@"无法读取文件：{filePath}，原因：{exception.Message}");
                        }
                        catch (IOException exception)
                        {
                            Console.WriteLine($@"无法读取文件：{filePath}，原因：{exception.Message}");
                        }
                    }

                    if (depth >= maxDepth)
                    {
                        continue;
                    }

                    foreach (var subDirectoryPath in Directory.EnumerateDirectories(directoryPath))
                    {
                        try
                        {
                            var subDirectory = new DirectoryInfo(subDirectoryPath);

                            if (blackListFolders.Contains(subDirectory.Name))
                            {
                                continue;
                            }

                            directories.Push((subDirectoryPath, depth + 1));
                        }
                        catch (UnauthorizedAccessException exception)
                        {
                            Console.WriteLine($@"无法读取目录：{subDirectoryPath}，原因：{exception.Message}");
                        }
                        catch (IOException exception)
                        {
                            Console.WriteLine($@"无法读取目录：{subDirectoryPath}，原因：{exception.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    Console.WriteLine($@"无法读取目录：{directoryPath}，原因：{exception.Message}");
                }
                catch (IOException exception)
                {
                    Console.WriteLine($@"无法读取目录：{directoryPath}，原因：{exception.Message}");
                }
            }

            return result;
        }

        private static string NormalizeSuffix(string suffix)
        {
            suffix = suffix.Trim();

            if (suffix.StartsWith("*.", StringComparison.Ordinal))
            {
                return suffix[1..];
            }

            return suffix.StartsWith('.') ? suffix : $".{suffix}";
        }

        [RelayCommand]
        private void EditTraverseRule()
        {
            if (_dialogService.ShowDialog<EditTraverseRuleDialogViewModel>() == true)
            {
                BlackListConfig =
                    SettingsStore.Load<BlackListConfig>(ConfigSections.FileName, ConfigSections.BlackList);
                _ = TraverseFolderAsync();
            }
        }

        [RelayCommand]
        private void ClearCodeFiles()
        {
            SelectedFiles.Clear();
            _effectiveCodeLines = [];
        }

        [RelayCommand]
        private void RemoveCodeFile(CodeFile file)
        {
            if (file == null)
            {
                return;
            }

            var fileToRemove = SelectedFiles.FirstOrDefault(f => f.FilePath == file.FilePath);
            if (fileToRemove != null)
            {
                SelectedFiles.Remove(fileToRemove);
            }
        }

        [RelayCommand]
        private void OpenCodeFile(CodeFile file)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.FilePath))
            {
                return;
            }

            if (!File.Exists(file.FilePath))
            {
                MessageBox.Show($"文件不存在：\n{file.FilePath}", "无法打开文件", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(file.FilePath)
                {
                    UseShellExecute = true
                });
            }
            catch (Win32Exception exception)
            {
                MessageBox.Show($"无法使用系统默认程序打开该文件。\n\n{exception.Message}",
                    "无法打开文件", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddFileSuffix()
        {
            if (string.IsNullOrWhiteSpace(UserDefinedFileSuffix)) return;

            var suffix = NormalizeSuffix(UserDefinedFileSuffix);
            if (WhiteListSuffix.Any(item =>
                    string.Equals(NormalizeSuffix(item), suffix, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("文件后缀已存在，请勿重复添加。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UserDefinedFileSuffixTypes.Add(suffix);
            UserDefinedFileSuffix = string.Empty;
        }

        [RelayCommand]
        private void DeleteUserDefinedFileSuffix(string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix)) return;
            UserDefinedFileSuffixTypes.Remove(suffix);
        }

        [RelayCommand]
        private void OpenResult()
        {
            // 默认打开软件共工作目录，如果_outputFilePath不为空，就打开_outputFilePath所在的目录
            var directoryToOpen = string.IsNullOrWhiteSpace(_outputFilePath)
                ? Environment.CurrentDirectory
                : Path.GetDirectoryName(_outputFilePath);
            if (directoryToOpen != null && Directory.Exists(directoryToOpen))
            {
                Process.Start(new ProcessStartInfo(directoryToOpen)
                {
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Windows 文件缓存：第一次读取时需要从磁盘加载文件内容；读取过的文件会被系统缓存在内存中。第二次生成读取的是内存缓存，速度会非常快
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task GeneratorCode()
        {
            if (string.IsNullOrWhiteSpace(DocumentHeader))
            {
                MessageBox.Show("请设置文档页眉", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(SoftwareVersion))
            {
                MessageBox.Show("请设置软件版本号", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            const int linesPerPage = 50;
            const int defaultPageCount = 60;

            int pageCount;
            if (string.IsNullOrWhiteSpace(DocumentPage))
            {
                pageCount = defaultPageCount;
            }
            else if (!int.TryParse(DocumentPage, out pageCount) || pageCount <= 0)
            {
                MessageBox.Show("代码页数格式不对，请输入大于 0 的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int fontSize;
            if (string.IsNullOrWhiteSpace(DocFontSize))
            {
                fontSize = 8;
            }
            else if (!int.TryParse(DocFontSize, out fontSize) || fontSize <= 0)
            {
                MessageBox.Show("字体大小格式不对，请输入大于 0 的整数。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (pageCount > int.MaxValue / linesPerPage)
            {
                MessageBox.Show("代码页数过大。", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var files = SelectedFiles
                .Where(file => !string.IsNullOrWhiteSpace(file.FilePath))
                .OrderBy(file => file.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(file => file.FilePath)
                .ToList();

            if (files.Count == 0)
            {
                MessageBox.Show("未找到可读取的代码文件。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CurrentProgress = 0;
            CurrentFile = "准备就绪";

            var progress = new Progress<(string FileName, double Progress)>(value =>
            {
                CurrentFile = $"正在读取：{value.FileName}";
                CurrentProgress = value.Progress;
            });

            var readResult = await Task.Run(() => ReadCodeFiles(files, progress));
            _effectiveCodeLines = SelectEffectiveCodeLines(readResult.Lines, pageCount * linesPerPage);
            CurrentProgress = 100;
            CurrentFile = $"代码文档已生成，有效代码共 {_effectiveCodeLines.Count} 行";

            if (readResult.Errors.Count > 0)
            {
                var errorSummary = string.Join(
                    Environment.NewLine,
                    readResult.Errors.Take(5).Select(error => $"• {error}"));

                var remainingCount = readResult.Errors.Count - 5;
                if (remainingCount > 0)
                {
                    errorSummary += $"{Environment.NewLine}另有 {remainingCount} 个文件读取失败。";
                }

                MessageBox.Show($"部分代码文件无法读取：{Environment.NewLine}{Environment.NewLine}{errorSummary}",
                    "读取完成", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (_effectiveCodeLines.Count == 0)
            {
                MessageBox.Show("未读取到有效代码行。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 默认写入到软件共工作目录，如果_outputFilePath不为空，就保存到_outputFilePath

            // 后续生成 TXT / Word 时使用 _effectiveCodeLines。
            _ = fontSize;
        }

        private static CodeReadResult ReadCodeFiles(
            IReadOnlyList<string> filePaths,
            IProgress<(string FileName, double Progress)> progress)
        {
            var lines = new List<string>();
            var errors = new List<string>();
            for (var index = 0; index < filePaths.Count; index++)
            {
                var filePath = filePaths[index];
                var fileName = Path.GetFileName(filePath);

                progress.Report((fileName, index * 100d / filePaths.Count));

                try
                {
                    using var reader = new StreamReader(filePath);

                    string line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                        }
                    }
                }
                catch (UnauthorizedAccessException exception)
                {
                    errors.Add($"{fileName}：{exception.Message}");
                }
                catch (IOException exception)
                {
                    errors.Add($"{fileName}：{exception.Message}");
                }

                progress.Report((fileName, (index + 1) * 100d / filePaths.Count));
            }

            return new CodeReadResult(lines, errors);
        }

        private static IReadOnlyList<string> SelectEffectiveCodeLines(IReadOnlyList<string> codeLines,
            int maximumLineCount)
        {
            if (codeLines.Count <= maximumLineCount)
            {
                return [.. codeLines];
            }

            var firstPartCount = maximumLineCount / 2;
            var lastPartCount = maximumLineCount - firstPartCount;
            return [.. codeLines.Take(firstPartCount), .. codeLines.TakeLast(lastPartCount)];
        }

        [RelayCommand]
        private void ChangeDocumentPath()
        {
            using var fileDialog = new SaveFileDialog();
            fileDialog.Title = @"选择文档保存位置";
            fileDialog.Filter = @"Word 文档 (*.docx)|*.docx";
            fileDialog.DefaultExt = ".docx";
            fileDialog.AddExtension = true;
            fileDialog.FileName = string.IsNullOrWhiteSpace(_outputFilePath)
                ? "软著代码.docx"
                : Path.GetFileName(_outputFilePath);

            var cachedDirectory = Path.GetDirectoryName(_outputFilePath);
            if (!string.IsNullOrWhiteSpace(cachedDirectory) && Directory.Exists(cachedDirectory))
            {
                fileDialog.InitialDirectory = cachedDirectory;
            }

            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            _outputFilePath = fileDialog.FileName;
            SettingsStore.Save(
                ConfigSections.FileName, ConfigSections.OutputPath,
                new OutputPathConfig { Path = _outputFilePath });
            ShowToast($"文档保存路径已更改为：{_outputFilePath}");
        }

        private void Worker_OnDoWork(object sender, DoWorkEventArgs e)
        {
            //所有符合要求的代码文件内容
            // var codeContentArray = new List<string>();
            // var effectiveCode = new List<string>();
            // var i = 0;
            // foreach (var filePath in _generateFilePaths)
            // {
            //     // 逐行读取源文件，跳过空白行（避免一次性加载整个文件到内存）
            //     var lines = new List<string>();
            //     using (var reader = new StreamReader(filePath))
            //     {
            //         string line;
            //         while ((line = reader.ReadLine()) != null)
            //         {
            //             if (!string.IsNullOrWhiteSpace(line))
            //             {
            //                 lines.Add(line);
            //             }
            //         }
            //     }
            //
            //     if (lines.Count > 0)
            //     {
            //         codeContentArray.AddRange(lines);
            //         i++;
            //         var percent = i / (float)_generateFilePaths.Count;
            //         _backgroundWorker.ReportProgress((int)(percent * 100));
            //     }
            //
            //     //此行代码根据情况可选择删除或者保留
            //     Thread.Sleep(10);
            // }
            //
            // //筛选代码行区间
            // if (codeContentArray.Count <= _effectiveCodeCount)
            // {
            //     effectiveCode = codeContentArray;
            // }
            // else
            // {
            //     //选择前30页代码
            //     for (var j = 0; j < _effectiveCodeCount / 2; j++)
            //     {
            //         effectiveCode.Add(codeContentArray[j]);
            //     }
            //
            //     //选择后30页代码
            //     var end = codeContentArray.Count - _effectiveCodeCount / 2;
            //     for (var k = end; k < codeContentArray.Count; k++)
            //     {
            //         effectiveCode.Add(codeContentArray[k]);
            //     }
            // }
            //
            // //设置有效代码行数
            // EffectiveCodeLines = $"代码文档已生成，有效代码共：{effectiveCode.Count}行";
            //
            // //生成带有缩进格式的Text，便于写入word
            // File.WriteAllLines($"{_outputFilePath}.txt", effectiveCode);
            //
            // //读取整篇格式化好了的Text写入word
            // var text = File.ReadAllText($"{_outputFilePath}.txt");
            // using (var document = DocX.Create(_outputFilePath))
            // {
            //     var font = new Font("微软雅黑");
            //     //页眉
            //     document.AddHeaders();
            //     var header = document.Headers.Odd;
            //     var headerParagraph = header.InsertParagraph($"{_codeHeader} {_softVersion}");
            //     headerParagraph.Font(font);
            //     headerParagraph.FontSize(_size);
            //     headerParagraph.Alignment = Alignment.center;
            //     headerParagraph.InsertHorizontalLine(
            //         HorizontalBorderPosition.bottom, BorderStyle.Tcbs_single, 1, 1, Color.Black
            //     );
            //
            //     //页码
            //     document.AddFooters();
            //     var footer = document.Footers.Odd;
            //     var footerParagraph = footer.InsertParagraph();
            //     footerParagraph.Font(font);
            //     footerParagraph.FontSize(_size);
            //     footerParagraph.AppendPageNumber(PageNumberFormat.normal);
            //     footerParagraph.Alignment = Alignment.center;
            //
            //     //正文
            //     var paragraph = document.InsertParagraph();
            //     paragraph.Append(text, new Formatting
            //     {
            //         FontFamily = font,
            //         Size = _size
            //     });
            //     document.Save();
            // }
        }

        private void Worker_OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Growl.Success($"软著代码生成完毕，保存在{_outputFilePath}");
        }

        private void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            if (_toastTimer == null)
            {
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastTimer.Stop();
                    IsToastVisible = false;
                };
            }

            _toastTimer.Stop();
            _toastTimer.Start();
        }
    }
}