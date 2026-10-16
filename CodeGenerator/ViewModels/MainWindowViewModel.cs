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
        #region VM

        [ObservableProperty] private string _codeFolder;
        [ObservableProperty] private BlackListConfig _blackListConfig;
        [ObservableProperty] private ObservableCollection<CodeFile> _selectedFiles = [];
        [ObservableProperty] private int _docFontSize = 8;
        [ObservableProperty] private string _documentHeader;
        [ObservableProperty] private string _softwareVersion = "V1.0.0";
        [ObservableProperty] private string _documentPage = "60";
        [ObservableProperty] private string _userDefinedFileSuffix;
        [ObservableProperty] private ObservableCollection<string> _userDefinedFileSuffixTypes = [];
        [ObservableProperty] private ObservableCollection<string> _defaultFileSuffixTypes;
        [ObservableProperty] private string _currentFile;
        [ObservableProperty] private double _currentProgress;

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
        private void GeneratorCode()
        {
            // if (string.IsNullOrWhiteSpace(_codeHeader))
            // {
            //     MessageBox.Show("请设置文档页眉", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //     return;
            // }
            //
            // if (string.IsNullOrWhiteSpace(_softVersion))
            // {
            //     MessageBox.Show("请设置文档页眉软件版本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //     return;
            // }
            //
            // var suffixSet = new HashSet<string>();
            // //添加默认文件后缀（如果有的话）
            // foreach (var item in _defaultSuffixCollection)
            // {
            //     if (item.IsChecked)
            //     {
            //         suffixSet.Add(item.Suffix);
            //     }
            // }
            //
            // //添加用户自定义的文件后缀（如果有的话）
            // foreach (var suffix in _suffixCollection)
            // {
            //     suffixSet.Add(suffix);
            // }
            //
            // if (!suffixSet.Any())
            // {
            //     MessageBox.Show("请设置需要格式化的文件后缀", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            //     return;
            // }
            //
            // // 如果用户没有设置过保存路径，那就默认已登录账号桌面路径为保存文档的路径
            // if (string.IsNullOrEmpty(_outputFilePath))
            // {
            //     var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //     _outputFilePath = Path.Combine(desktopPath, "软著代码");
            // }
            //
            // // 如果没有设置代码页数，默认60页（前后各30页）
            // if (string.IsNullOrEmpty(_codePageLimit))
            // {
            //     _effectiveCodeCount = 60 * 50;
            // }
            // else
            // {
            //     if (!_codePageLimit.IsNumber())
            //     {
            //         MessageBox.Show("页码格式不对，请输入数字", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //         return;
            //     }
            //
            //     _effectiveCodeCount = Convert.ToInt32(_codePageLimit) * 50;
            // }
            //
            // //如果没有设置字体，默认8号。软著要求每页50行代码，8号字体正好合适
            // if (string.IsNullOrEmpty(_fontSize))
            // {
            //     _size = 8;
            // }
            // else
            // {
            //     if (!_fontSize.IsNumber())
            //     {
            //         MessageBox.Show("字体大小格式不对，请输入数字", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //         return;
            //     }
            //
            //     _size = Convert.ToInt32(_fontSize);
            // }
            //
            // //从用户筛选后的文件列表中，按照设置的文件后缀遍历文件
            // _generateFilePaths = FileCollection.Where(f => suffixSet.Contains(f.Extension)).Select(f => f.FullName)
            //     .ToList();
            // if (!_generateFilePaths.Any())
            // {
            //     MessageBox.Show("没找检索到符合条件的代码源文件", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            //     return;
            // }
            //
            // //启动文件处理后台线程
            // if (_backgroundWorker.IsBusy)
            // {
            //     MessageBox.Show("当前正在处理文件中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            //     return;
            // }
            //
            // _backgroundWorker.RunWorkerAsync();
        }

        [RelayCommand]
        private void SaveDocument()
        {
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="file"></param>
        private void DeleteFile(FileInfo file)
        {
            // FileCollection.Remove(file);
        }

        /// <summary>
        /// 删除后缀
        /// </summary>
        /// <param name="suffix"></param>
        private void DeleteFileSuffix(string suffix)
        {
            // SuffixCollection.Remove(suffix);
        }

        /// <summary>
        /// 选择文档保存的路径
        /// </summary>
        private void SelectDocSavePath()
        {
            // var fileDialog = new SaveFileDialog();
            // if (fileDialog.ShowDialog() == DialogResult.OK)
            // {
            //     _outputFilePath = fileDialog.FileName;
            // }
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
    }
}