using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using CodeGenerator.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Xceed.Document.NET;
using Xceed.Words.NET;
using Application = System.Windows.Application;
using Formatting = Xceed.Document.NET.Formatting;
using MessageBox = System.Windows.MessageBox;

namespace CodeGenerator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        private string _folderPath;

        public string FolderPath
        {
            get => _folderPath;
            set
            {
                _folderPath = value;
                RaisePropertyChanged();
            }
        }

        private string _codePageLimit;

        public string CodePageLimit
        {
            get => _codePageLimit;
            set
            {
                _codePageLimit = value;
                RaisePropertyChanged();
            }
        }

        private string _fontSize;

        public string FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                RaisePropertyChanged();
            }
        }

        private string _suffixType;

        public string SuffixType
        {
            get => _suffixType;
            set
            {
                _suffixType = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _suffixCollection = new ObservableCollection<string>();

        public ObservableCollection<string> SuffixCollection
        {
            get => _suffixCollection;
            set
            {
                _suffixCollection = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<FileInfo> _fileCollection = new ObservableCollection<FileInfo>();

        public ObservableCollection<FileInfo> FileCollection
        {
            get => _fileCollection;
            set
            {
                _fileCollection = value;
                RaisePropertyChanged();
            }
        }

        private int _handleTextProgress;

        public int HandleTextProgress
        {
            get => _handleTextProgress;
            set
            {
                _handleTextProgress = value;
                RaisePropertyChanged();
            }
        }

        private string _effectiveCodeLines;

        public string EffectiveCodeLines
        {
            get => _effectiveCodeLines;
            set
            {
                _effectiveCodeLines = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand SelectFolderCommand { set; get; }
        public DelegateCommand<FileInfo> MouseDoubleClickCommand { set; get; }
        public DelegateCommand<FileInfo> DeleteFileCommand { set; get; }
        public DelegateCommand AddFileSuffixTypeCommand { set; get; }
        public DelegateCommand<string> DeleteFileSuffixCommand { set; get; }
        public DelegateCommand GeneratorCodeCommand { set; get; }
        public DelegateCommand SelectPathCommand { set; get; }

        #endregion

        private string _outputFilePath;
        private int _effectiveCodeCount;
        private int _size;
        private readonly BackgroundWorker _backgroundWorker;

        /// <summary>
        /// 需要格式化的文件全路径集
        /// </summary>
        private List<string> _generateFilePaths;

        public MainWindowViewModel()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += Worker_OnDoWork;
            _backgroundWorker.ProgressChanged += Worker_OnProgressChanged;
            _backgroundWorker.RunWorkerCompleted += Worker_OnRunWorkerCompleted;

            SelectFolderCommand = new DelegateCommand(SelectFolder);
            MouseDoubleClickCommand = new DelegateCommand<FileInfo>(OpenFile);
            DeleteFileCommand = new DelegateCommand<FileInfo>(DeleteFile);
            AddFileSuffixTypeCommand = new DelegateCommand(AddFileSuffixType);
            DeleteFileSuffixCommand = new DelegateCommand<string>(DeleteFileSuffix);
            GeneratorCodeCommand = new DelegateCommand(GeneratorCode);
            SelectPathCommand = new DelegateCommand(SelectDocSavePath);
        }

        /// <summary>
        /// 选择文件夹
        /// </summary>
        private void SelectFolder()
        {
            var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            FolderPath = dialog.SelectedPath;

            // 清空旧数据
            FileCollection.Clear();
            SuffixCollection.Clear();

            Task.Run(async () =>
            {
                FileCollection = await GetFilesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!_fileCollection.Any())
                    {
                        MessageBox.Show("文件夹为空", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            });
        }

        private async Task<ObservableCollection<FileInfo>> GetFilesAsync()
        {
            return await Task.Run(() =>
            {
                var result = new ObservableCollection<FileInfo>();
                _folderPath.TraverseFolder(result);
                return result;
            });
        }

        /// <summary>
        /// 双击打开文件
        /// </summary>
        /// <param name="selectedItem"></param>
        private void OpenFile(FileInfo selectedItem)
        {
            //本机默认程序打开
            Process.Start(selectedItem.FullName);
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="file"></param>
        private void DeleteFile(FileInfo file)
        {
            FileCollection.Remove(file);
        }

        /// <summary>
        /// 添加需要格式化的后缀
        /// </summary>
        private void AddFileSuffixType()
        {
            if (string.IsNullOrWhiteSpace(_suffixType))
            {
                MessageBox.Show("文件类型为空，无法添加", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_suffixType.Contains("*"))
            {
                MessageBox.Show("文件类型不用自带『*』", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SuffixCollection.Contains($"*{_suffixType}") || SuffixCollection.Contains($"*.{_suffixType}"))
            {
                MessageBox.Show("文件类型已添加，请勿重复添加", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SuffixCollection.Add(_suffixType.Contains(".") ? $"*{_suffixType}" : $"*.{_suffixType}");

            //添加之后将输入框置空
            SuffixType = string.Empty;
        }

        /// <summary>
        /// 删除后缀
        /// </summary>
        /// <param name="suffix"></param>
        private void DeleteFileSuffix(string suffix)
        {
            SuffixCollection.Remove(suffix);
        }

        /// <summary>
        /// 生成格式化后的代码文档
        /// </summary>
        private void GeneratorCode()
        {
            if (!_suffixCollection.Any())
            {
                MessageBox.Show("请设置需要格式化的文件后缀", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //如果用户没有设置过保存路径，那就默认已登录账号桌面路径为保存文档的路径
            if (string.IsNullOrEmpty(_outputFilePath))
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                _outputFilePath = Path.Combine(desktopPath, "软著代码");
            }

            //如果没有设置代码页数，默认60页（前后各30页）
            if (string.IsNullOrEmpty(_codePageLimit))
            {
                _effectiveCodeCount = 60 * 50;
            }
            else
            {
                if (!_codePageLimit.IsNumber())
                {
                    MessageBox.Show("页码格式不对，请输入数字", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _effectiveCodeCount = Convert.ToInt32(_codePageLimit) * 50;
            }

            //如果没有设置字体，默认8号。软著要求每页50行代码，8号字体正好合适
            if (string.IsNullOrEmpty(_fontSize))
            {
                _size = 8;
            }
            else
            {
                if (!_fontSize.IsNumber())
                {
                    MessageBox.Show("字体大小格式不对，请输入数字", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _size = Convert.ToInt32(_fontSize);
            }

            //按照设置的文件后缀遍历文件
            _generateFilePaths = _folderPath.GetFilesBySuffix(_suffixCollection);
            if (!_generateFilePaths.Any())
            {
                MessageBox.Show("没找检索到符合条件的代码源文件", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //启动文件处理后台线程
            if (_backgroundWorker.IsBusy)
            {
                MessageBox.Show("当前正在处理文件中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// 选择文档保存的路径
        /// </summary>
        private void SelectDocSavePath()
        {
            var fileDialog = new SaveFileDialog();
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                _outputFilePath = fileDialog.FileName;
            }
        }

        private void Worker_OnDoWork(object sender, DoWorkEventArgs e)
        {
            //所有符合要求的代码文件内容
            var codeContentArray = new List<string>();
            var effectiveCode = new List<string>();
            var i = 0;
            foreach (var filePath in _generateFilePaths)
            {
                //读取源文件，跳过读取空白行
                var lines = File.ReadAllLines(filePath);
                codeContentArray.AddRange(lines.Where(line => !string.IsNullOrWhiteSpace(line)));

                //更新处理进度
                i++;
                var percent = i / (float)_generateFilePaths.Count;
                _backgroundWorker.ReportProgress((int)(percent * 100));

                //此行代码根据情况可选择删除或者保留
                Thread.Sleep(20);
            }

            //筛选代码行区间
            if (codeContentArray.Count <= _effectiveCodeCount)
            {
                effectiveCode = codeContentArray;
            }
            else
            {
                //选择前后30页代码，写入Txt
                for (var j = 0; j < _effectiveCodeCount / 2; j++)
                {
                    effectiveCode.Add(codeContentArray[j]);
                }

                var end = codeContentArray.Count - _effectiveCodeCount / 2;
                for (var k = end; k < codeContentArray.Count; k++)
                {
                    effectiveCode.Add(codeContentArray[k]);
                }
            }

            //设置有效代码行数
            EffectiveCodeLines = $"代码文档已生成，有效代码共：{effectiveCode.Count}行";

            //生成带有缩进格式的Text，便于写入word
            File.WriteAllLines($"{_outputFilePath}.txt", effectiveCode);

            //读取整篇格式化好了的Text写入word
            var text = File.ReadAllText($"{_outputFilePath}.txt");
            var docX = DocX.Create(_outputFilePath);
            var paragraph = docX.InsertParagraph();
            var fmt = new Formatting
            {
                FontFamily = new Font("微软雅黑"),
                Size = _size
            };
            paragraph.Append(text, fmt);
            try
            {
                docX.Save();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            HandleTextProgress = e.ProgressPercentage;
        }

        private void Worker_OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Growl.Success($"软著代码生成完毕，保存在{_outputFilePath}");
        }
    }
}