using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CodeGenerator.Events;
using CodeGenerator.Utils;
using CodeGenerator.Views;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using MessageBox = HandyControl.Controls.MessageBox;

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

        private bool _isUnderHandle;

        public bool IsUnderHandle
        {
            get => _isUnderHandle;
            set
            {
                _isUnderHandle = value;
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
        public DelegateCommand FileRemoveCommand { set; get; }
        public DelegateCommand AddFileSuffixTypeButton { set; get; }
        public DelegateCommand GeneratorCodeCommand { set; get; }

        #endregion

        private MainWindow _window;
        private string _dirPath;
        private string _outputFilePath;
        private readonly ObservableCollection<string> _filePathCollection = new ObservableCollection<string>();
        private readonly BackgroundWorker _backgroundWorker;

        public MainWindowViewModel(IEventAggregator eventAggregator)
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

            eventAggregator.GetEvent<FileSuffixTagEvent>().Subscribe(delegate(string s)
            {
                FileSuffixCollection.Remove(s);
            });

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
                        Growl.Error("文件夹已添加，请勿重复添加");
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

            //中间列表右键删除文件
            FileRemoveCommand = new DelegateCommand(delegate
            {
                var boxResult = MessageBox.Show(
                    "是否移除此文件", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
                );
                if (boxResult == MessageBoxResult.OK)
                {
                    FileCollection.RemoveAt(_window.FileListBox.SelectedIndex);
                }
            });

            AddFileSuffixTypeButton = new DelegateCommand(delegate
            {
                if (string.IsNullOrWhiteSpace(_suffixType))
                {
                    Growl.Error("文件类型为空，无法添加");
                    return;
                }

                if (FileSuffixCollection.Contains(_suffixType) || FileSuffixCollection.Contains($".{_suffixType}"))
                {
                    Growl.Error("文件类型已添加，请勿重复添加");
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
            });

            GeneratorCodeCommand = new DelegateCommand(delegate
            {
                if (!_fileSuffixCollection.Any())
                {
                    Growl.Error("请设置需要格式化的文件后缀");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_outputDirPath))
                {
                    _outputFilePath = @"C:\Users\Administrator\Desktop\软著代码.txt";
                }
                else
                {
                    _outputFilePath = $"{_outputDirPath}\\软著代码.txt";
                }

                //按照设置的文件后缀遍历文件
                TraverseDir();

                //启动文件处理后台线程
                if (_backgroundWorker.IsBusy)
                {
                    Growl.Error("当前正在处理文件中，无法同时运行多个任务");
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

            if (_filePathCollection.Any())
            {
                _filePathCollection.Clear();
            }

            var files = _dirPath.GetDirFiles();
            foreach (var file in files)
            {
                FileCollection.Add(file.Name);
                if (_fileSuffixCollection.Contains(file.Extension))
                {
                    _filePathCollection.Add(file.FullName);
                }
            }
        }

        private void Worker_OnDoWork(object sender, DoWorkEventArgs e)
        {
            //所有符合要求的代码文件内容
            var codeContentArray = _filePathCollection.Select(File.ReadAllText).ToList();
            //根据文件路径获取文件内容

            //去掉空行
            for (var i = 0; i < codeContentArray.Count; i++)
            {
                var percent = (i + 1) / (float)codeContentArray.Count;
                _backgroundWorker.ReportProgress((int)(percent * 100));
                Thread.Sleep(50);
            }

            //生成doc
            // File.AppendAllLines(_outputFilePath, txtArray);
        }

        private void Worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            HandleTextProgress = e.ProgressPercentage;
        }

        private void Worker_OnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsUnderHandle = false;
        }
    }
}