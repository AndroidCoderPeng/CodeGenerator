using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using CodeGenerator.Cache;
using CodeGenerator.Models;
using CodeGenerator.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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

            _effectiveCodeLines = SelectEffectiveCodeLines(readResult.Lines, pageCount * linesPerPage);
            if (_effectiveCodeLines.Count == 0)
            {
                MessageBox.Show("未读取到有效代码行。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 默认写入到软件共工作目录，如果_outputFilePath不为空，就保存到_outputFilePath
            var documentPath = string.IsNullOrWhiteSpace(_outputFilePath)
                ? Path.Combine(Environment.CurrentDirectory, "软著代码.docx")
                : _outputFilePath;
            var textFilePath = Path.ChangeExtension(documentPath, ".txt");

            try
            {
                await Task.Run(() =>
                {
                    var outputDirectory = Path.GetDirectoryName(documentPath);
                    if (!string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    File.WriteAllLines(textFilePath, _effectiveCodeLines, new UTF8Encoding(false));
                    CreateWordDocument(
                        documentPath,
                        _effectiveCodeLines,
                        DocumentHeader,
                        SoftwareVersion,
                        fontSize,
                        linesPerPage);
                });

                CurrentProgress = 100;
                CurrentFile = $"代码文档已生成，共 {_effectiveCodeLines.Count} 行";
                ShowToast($"已生成 TXT 和 Word 文档：{Path.GetDirectoryName(documentPath)}");
            }
            catch (UnauthorizedAccessException exception)
            {
                MessageBox.Show($"无权限写入文本文件：\n{exception.Message}",
                    "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException exception)
            {
                MessageBox.Show($"无法写入文本文件：\n{exception.Message}",
                    "导出失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        /// <summary>
        /// 分别取总代码的前1500行和后1500行，组成最终的有效代码行集合
        /// </summary>
        /// <param name="codeLines"></param>
        /// <param name="maximumLineCount"></param>
        /// <returns></returns>
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

        private static void CreateWordDocument(
            string filePath,
            IReadOnlyList<string> codeLines,
            string documentHeader,
            string softwareVersion,
            int fontSize,
            int linesPerPage)
        {
            using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);

            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(CreateHeaderParagraph($"{documentHeader} {softwareVersion}", fontSize));
            headerPart.Header.Save();

            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(CreatePageNumberParagraph(fontSize));
            footerPart.Footer.Save();

            var body = mainPart.Document.Body!;

            for (var index = 0; index < codeLines.Count; index++)
            {
                body.Append(CreateCodeParagraph(codeLines[index], fontSize));
            }

            var headerReference = new HeaderReference
            {
                Type = HeaderFooterValues.Default,
                Id = mainPart.GetIdOfPart(headerPart)
            };
            var footerReference = new FooterReference
            {
                Type = HeaderFooterValues.Default,
                Id = mainPart.GetIdOfPart(footerPart)
            };
            var pageSize = new PageSize
            {
                Width = 11906U,
                Height = 16838U
            };
            var pageMargin = new PageMargin
            {
                Top = 850,
                Right = 850U,
                Bottom = 850,
                Left = 850U,
                Header = 425U,
                Footer = 425U,
                Gutter = 0U
            };
            var sectionProperties = new SectionProperties(headerReference, footerReference, pageSize, pageMargin);
            body.Append(sectionProperties);
            var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new Settings(new UpdateFieldsOnOpen { Val = true });
            settingsPart.Settings.Save();
            mainPart.Document.Save();
        }

        private static Paragraph CreateHeaderParagraph(string text, int fontSize)
        {
            var borders = new ParagraphBorders(new BottomBorder
            {
                Val = BorderValues.Single,
                Size = 4U,
                Color = "000000"
            });
            var paragraphProperties = new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                borders);
            return new Paragraph(paragraphProperties, CreateRun(text, fontSize));
        }

        private static Paragraph CreatePageNumberParagraph(int fontSize)
        {
            return new Paragraph(new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }),
                new Run(CreateRunProperties(fontSize),
                    new FieldChar { FieldCharType = FieldCharValues.Begin }),
                new Run(CreateRunProperties(fontSize),
                    new FieldCode("PAGE")),
                new Run(CreateRunProperties(fontSize),
                    new FieldChar { FieldCharType = FieldCharValues.Separate }),
                new Run(CreateRunProperties(fontSize),
                    new Text("1")),
                new Run(CreateRunProperties(fontSize),
                    new FieldChar { FieldCharType = FieldCharValues.End }));
        }

        private static Paragraph CreateCodeParagraph(string codeLine, int fontSize)
        {
            return new Paragraph(new ParagraphProperties(
                    new SpacingBetweenLines
                    {
                        Before = "0",
                        After = "0",
                        Line = "300",
                        LineRule = LineSpacingRuleValues.Exact
                    }),
                CreateRun(codeLine, fontSize));
        }

        private static Run CreateRun(string text, int fontSize)
        {
            return new Run(CreateRunProperties(fontSize),
                new Text(text)
                {
                    Space = SpaceProcessingModeValues.Preserve
                }
            );
        }

        private static RunProperties CreateRunProperties(int fontSize)
        {
            var openXmlFontSize = (fontSize * 2).ToString(CultureInfo.InvariantCulture);
            var runFonts = new RunFonts
            {
                Ascii = "Consolas",
                HighAnsi = "Consolas",
                EastAsia = "Consolas",
                ComplexScript = "Consolas"
            };
            return new RunProperties(runFonts,
                new FontSize { Val = openXmlFontSize },
                new FontSizeComplexScript { Val = openXmlFontSize }
            );
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