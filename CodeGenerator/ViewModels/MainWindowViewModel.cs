using System.Collections.ObjectModel;
using System.Linq;
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

        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
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
                    var dirPath = dialog.SelectedPath;
                    if (DirItemCollection.Contains(dirPath))
                    {
                        Growl.Error("文件夹已添加，请勿重复添加");
                        return;
                    }

                    DirItemCollection.Add(dirPath);

                    //遍历文件夹
                    if (FileCollection.Any())
                    {
                        FileCollection.Clear();
                    }

                    var files = dirPath.GetDirFiles();
                    foreach (var file in files)
                    {
                        FileCollection.Add(file);
                    }
                }
            });

            //左侧列表选中事件
            DirItemSelectedCommand = new DelegateCommand(delegate
            {
                var dirPath = _window.DirListBox.SelectedItem.ToString();
                if (FileCollection.Any())
                {
                    FileCollection.Clear();
                }

                var files = dirPath.GetDirFiles();
                foreach (var file in files)
                {
                    FileCollection.Add(file);
                }
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
                string outputFilePath;
                if (string.IsNullOrWhiteSpace(_outputDirPath))
                {
                    outputFilePath = @"C:\Users\Administrator\Desktop\软著代码.doc";
                }
                else
                {
                    outputFilePath = $"{_outputDirPath}\\软著代码.doc";
                }

                if (!_fileSuffixCollection.Any())
                {
                    Growl.Error("请设置需要格式化的文件后缀");
                    return;
                }
                
                //按照设置的文件后缀遍历文件，然后生成doc
            });
        }
    }
}