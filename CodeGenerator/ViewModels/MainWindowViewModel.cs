using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CodeGenerator.Events;
using CodeGenerator.Utils;
using CodeGenerator.Views;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Xceed.Document.NET;
using Xceed.Words.NET;
using DialogResult = System.Windows.Forms.DialogResult;
using MessageBox = System.Windows.MessageBox;

namespace CodeGenerator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<string> _dirItemCollection = new ObservableCollection<string>();

        public ObservableCollection<string> DirItemCollection
        {
            get => _dirItemCollection;
            set
            {
                _dirItemCollection = value;
                RaisePropertyChanged();
            }
        }

        private string _outputDirPath = string.Empty;

        public string OutputDirPath
        {
            get => _outputDirPath;
            set
            {
                _outputDirPath = value;
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

        private ObservableCollection<string> _fileSuffixCollection = new ObservableCollection<string>();

        public ObservableCollection<string> FileSuffixCollection
        {
            get => _fileSuffixCollection;
            set
            {
                _fileSuffixCollection = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _fileCollection = new ObservableCollection<string>();

        public ObservableCollection<string> FileCollection
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

        private int _effectiveCodeLines;

        public int EffectiveCodeLines
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

        public DelegateCommand<MainWindow> WindowLoadedCommand { set; get; }
        public DelegateCommand OutputSettingCommand { set; get; }
        public DelegateCommand MiniSizeWindowCommand { set; get; }
        public DelegateCommand CloseWindowCommand { set; get; }
        public DelegateCommand SelectDirCommand { set; get; }
        public DelegateCommand DirItemSelectedCommand { set; get; }
        public DelegateCommand DirItemRemoveCommand { set; get; }
        public DelegateCommand MouseDoubleClickCommand { set; get; }
        public DelegateCommand AddFileSuffixTypeCommand { set; get; }
        public DelegateCommand GeneratorCodeCommand { set; get; }

        #endregion

        private MainWindow _window;
        private readonly IDialogService _dialogService;
        private string _dirPath;
        private string _outputFilePath;

        /// <summary>
        /// 需要格式化的文件全路径集
        /// </summary>
        private readonly ObservableCollection<string> _generateFilePathCollection = new ObservableCollection<string>();

        /// <summary>
        /// 不做限制的文件全路径集
        /// </summary>
        private readonly ObservableCollection<string> _filePathCollection = new ObservableCollection<string>();

        private readonly BackgroundWorker _backgroundWorker;

        public MainWindowViewModel(IEventAggregator eventAggregator, IDialogService dialogService)
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += Worker_OnDoWork;
            _backgroundWorker.ProgressChanged += Worker_OnProgressChanged;
            _backgroundWorker.RunWorkerCompleted += Worker_OnRunWorkerCompleted;

            WindowLoadedCommand = new DelegateCommand<MainWindow>(delegate(MainWindow window)
            {
                _window = window;
                _window.MouseDown += delegate
                {
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        _window.DragMove();
                    }
                };
            });

            eventAggregator.GetEvent<FileNameTagEvent>().Subscribe(delegate(string s) { FileCollection.Remove(s); });

            eventAggregator.GetEvent<FileSuffixTagEvent>().Subscribe(delegate(string s)
            {
                FileSuffixCollection.Remove(s);
            });

            _dialogService = dialogService;

            OutputSettingCommand = new DelegateCommand(delegate
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = @"请选择文档保存路径";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    OutputDirPath = dialog.SelectedPath;
                }
            });
            MiniSizeWindowCommand = new DelegateCommand(delegate { _window.WindowState = WindowState.Minimized; });
            CloseWindowCommand = new DelegateCommand(delegate { _window.Close(); });

            SelectDirCommand = new DelegateCommand(delegate
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _dirPath = dialog.SelectedPath;
                    if (DirItemCollection.Contains(_dirPath))
                    {
                        _dialogService.ShowDialog(
                            "AlertDialogView",
                            new DialogParameters { { "Title", "错误" }, { "Message", "文件夹已添加，请勿重复添加" } },
                            delegate { }
                        );
                        return;
                    }

                    DirItemCollection.Add(_dirPath);

                    //遍历文件夹
                    TraverseDir();
                }
            });

            //左侧列表选中事件
            DirItemSelectedCommand = new DelegateCommand(delegate
            {
                _dirPath = _window.DirListBox.SelectedItem.ToString();
                TraverseDir();
            });

            //左侧列表右键删除功能菜单
            DirItemRemoveCommand = new DelegateCommand(delegate
            {
                var boxResult = MessageBox.Show(
                    "是否从列表移除此文件夹", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
                );
                if (boxResult == MessageBoxResult.OK)
                {
                    DirItemCollection.RemoveAt(_window.DirListBox.SelectedIndex);
                    FileCollection.Clear();
                }
            });

            //打开文件
            MouseDoubleClickCommand = new DelegateCommand(delegate
            {
                var fileIndex = _window.FileListBox.SelectedIndex;
                var file = new FileInfo(_filePathCollection[fileIndex]);
                if (RuntimeCache.ImageSuffixArray.Contains(file.Extension))
                {
                    new ShowImageWindow(file) { Owner = _window }.Show();
                }
                else
                {
                    if (RuntimeCache.TextSuffixArray.Contains(file.Extension))
                    {
                        new ShowTextWindow(file) { Owner = _window }.Show();
                    }
                    else
                    {
                        _dialogService.ShowDialog(
                            "AlertDialogView",
                            new DialogParameters { { "Title", "错误" }, { "Message", "文件类型无法打开，请重新选择" } },
                            delegate { }
                        );
                    }
                }
            });

            AddFileSuffixTypeCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrWhiteSpace(_suffixType))
                {
                    _dialogService.ShowDialog(
                        "AlertDialogView",
                        new DialogParameters { { "Title", "错误" }, { "Message", "文件类型为空，无法添加" } },
                        delegate { }
                    );
                    return;
                }

                if (FileSuffixCollection.Contains(_suffixType) || FileSuffixCollection.Contains($".{_suffixType}"))
                {
                    _dialogService.ShowDialog(
                        "AlertDialogView",
                        new DialogParameters { { "Title", "错误" }, { "Message", "文件类型已添加，请勿重复添加" } },
                        delegate { }
                    );
                    return;
                }

                if (_suffixType.Contains("."))
                {
                    FileSuffixCollection.Add(_suffixType);
                }
                else
                {
                    FileSuffixCollection.Add($".{_suffixType}");
                }

                //添加之后将输入框置空
                SuffixType = string.Empty;
            });

            GeneratorCodeCommand = new DelegateCommand(delegate
            {
                if (!_fileSuffixCollection.Any())
                {
                    _dialogService.ShowDialog(
                        "AlertDialogView",
                        new DialogParameters { { "Title", "错误" }, { "Message", "请设置需要格式化的文件后缀" } },
                        delegate { }
                    );
                    return;
                }

                var current = WindowsIdentity.GetCurrent();
                //DESKTOP-3JOGREU\Administrator
                var currentName = current.Name;
                var userName = currentName.Split('\\')[1];

                if (string.IsNullOrWhiteSpace(_outputDirPath))
                {
                    _outputFilePath = $@"C:\Users\{userName}\Desktop\软著代码";
                }
                else
                {
                    _outputFilePath = $"{_outputDirPath}\\软著代码";
                }

                //按照设置的文件后缀遍历文件
                TraverseDir();

                //启动文件处理后台线程
                if (_backgroundWorker.IsBusy)
                {
                    _dialogService.ShowDialog(
                        "AlertDialogView",
                        new DialogParameters { { "Title", "错误" }, { "Message", "当前正在处理文件中" } },
                        delegate { }
                    );
                    return;
                }

                _backgroundWorker.RunWorkerAsync();
            });
        }

        /// <summary>
        /// 遍历文件夹
        /// </summary>
        private void TraverseDir()
        {
            if (FileCollection.Any())
            {
                FileCollection.Clear();
            }

            if (_generateFilePathCollection.Any())
            {
                _generateFilePathCollection.Clear();
            }

            EffectiveCodeLines = 0;

            var files = _dirPath.GetDirFiles();
            foreach (var file in files)
            {
                FileCollection.Add(file.Name);
                _filePathCollection.Add(file.FullName);
                if (_fileSuffixCollection.Contains(file.Extension))
                {
                    _generateFilePathCollection.Add(file.FullName);
                }
            }
        }

        private void Worker_OnDoWork(object sender, DoWorkEventArgs e)
        {
            //所有符合要求的代码文件内容
            var codeContentArray = new List<string>();
            var i = 0;
            foreach (var filePath in _generateFilePathCollection)
            {
                //读取源文件，跳过读取空白行
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        codeContentArray.Add(line);
                    }
                }

                //更新处理进度
                i++;
                var percent = i / (float)_generateFilePathCollection.Count;
                _backgroundWorker.ReportProgress((int)(percent * 100));

                //此行代码根据情况可选择删除或者保留
                Thread.Sleep(20);
            }

            //生成带有缩进格式的Text，便于写入word
            File.WriteAllLines($"{_outputFilePath}.txt", codeContentArray);

            //设置有效代码行数
            EffectiveCodeLines = codeContentArray.Count;

            //读取整篇格式化好了的Text写入word
            var text = File.ReadAllText($"{_outputFilePath}.txt");
            var docX = DocX.Create(_outputFilePath);
            var paragraph = docX.InsertParagraph();
            var fmt = new Formatting
            {
                FontFamily = new Font("微软雅黑"),
                Size = 8 //软著要求煤业50行代码，8号字体正好合适
            };
            paragraph.Append(text, fmt);
            try
            {
                docX.Save();
            }
            catch (ArgumentException)
            {
                _dialogService.ShowDialog(
                    "AlertDialogView",
                    new DialogParameters { { "Title", "错误" }, { "Message", "文件类型错误，无法生成代码文件" } },
                    delegate { }
                );
            }
        }

        private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            HandleTextProgress = e.ProgressPercentage;
        }

        private void Worker_OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}