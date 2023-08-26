using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CodeGenerator.Views;
using HandyControl.Controls;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using MessageBox = HandyControl.Controls.MessageBox;

namespace CodeGenerator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<string> _itemCollection = new ObservableCollection<string>();

        public ObservableCollection<string> ItemCollection
        {
            get => _itemCollection;
            set
            {
                _itemCollection = value;
                RaisePropertyChanged();
            }
        }

        private string _outputFilePath = "文档输出路径：";

        public string OutputFilePath
        {
            get => _outputFilePath;
            set
            {
                _outputFilePath = value;
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

        #endregion

        #region DelegateCommand

        public DelegateCommand<MainWindow> WindowLoadedCommand { set; get; }
        public DelegateCommand OutputSettingCommand { set; get; }
        public DelegateCommand MiniSizeWindowCommand { set; get; }
        public DelegateCommand CloseWindowCommand { set; get; }
        public DelegateCommand SelectDirCommand { set; get; }
        public DelegateCommand ItemSelectedCommand { set; get; }
        public DelegateCommand ItemRemoveCommand { set; get; }
        public DelegateCommand AddFileSuffixTypeButton { set; get; }
        public DelegateCommand DeleteFileSuffixCommand { set; get; }

        #endregion

        private MainWindow _window;

        public MainWindowViewModel()
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

            OutputSettingCommand = new DelegateCommand(delegate
            {
                var dialog = new FolderBrowserDialog();
                dialog.Description = @"请选择文档保存路径";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    OutputFilePath = $"文档输出路径：{dialog.SelectedPath}";
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
                    var lastIndex = dirPath.LastIndexOf("\\", StringComparison.Ordinal);

                    var dirName = dirPath.Substring(lastIndex + 1);
                    if (ItemCollection.Contains(dirName))
                    {
                        Growl.Error("文件夹已添加，请勿重复添加");
                        return;
                    }

                    ItemCollection.Add(dirName);

                    //遍历文件夹
                }
            });

            ItemSelectedCommand = new DelegateCommand(delegate { });

            //ListBox右键删除功能菜单
            ItemRemoveCommand = new DelegateCommand(delegate
            {
                var boxResult = MessageBox.Show(
                    "是否从列表移除此文件夹", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Warning
                );
                if (boxResult == MessageBoxResult.OK)
                {
                    ItemCollection.RemoveAt(_window.FileDirListBox.SelectedIndex);
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

            DeleteFileSuffixCommand = new DelegateCommand(delegate
            {
                Console.WriteLine(_window.FileSuffixListBox.SelectedIndex);
                Console.WriteLine(JsonConvert.SerializeObject(FileSuffixCollection));
                // FileSuffixCollection.RemoveAt(_window.FileSuffixListBox.SelectedIndex);
            });
        }
    }
}