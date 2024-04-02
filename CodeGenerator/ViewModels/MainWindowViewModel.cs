﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using CodeGenerator.Events;
using CodeGenerator.Models;
using CodeGenerator.Utils;
using CodeGenerator.Views;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Xceed.Document.NET;
using Xceed.Words.NET;
using DialogResult = System.Windows.Forms.DialogResult;

namespace CodeGenerator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region VM

        private ObservableCollection<FolderModel> _folderItemCollection = new ObservableCollection<FolderModel>();

        public ObservableCollection<FolderModel> FolderItemCollection
        {
            get => _folderItemCollection;
            set
            {
                _folderItemCollection = value;
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

        private ObservableCollection<string> _fileNameCollection = new ObservableCollection<string>();

        public ObservableCollection<string> FileNameCollection
        {
            get => _fileNameCollection;
            set
            {
                _fileNameCollection = value;
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

        public DelegateCommand<MainWindow> WindowLoadedCommand { set; get; }
        public DelegateCommand SelectDirCommand { set; get; }
        public DelegateCommand FolderItemSelectedCommand { set; get; }
        public DelegateCommand MouseDoubleClickCommand { set; get; }
        public DelegateCommand AddFileSuffixTypeCommand { set; get; }
        public DelegateCommand GeneratorCodeCommand { set; get; }
        public DelegateCommand SelectPathCommand { set; get; }

        #endregion

        private MainWindow _window;
        private readonly IDialogService _dialogService;
        private FolderModel _folderModel;
        private string _outputFilePath;
        private readonly BackgroundWorker _backgroundWorker;

        /// <summary>
        /// 需要格式化的文件全路径集
        /// </summary>
        private ObservableCollection<string> _generateFilePathCollection;

        public MainWindowViewModel(IEventAggregator eventAggregator, IDialogService dialogService)
        {
            eventAggregator.GetEvent<DirectoryEvent>().Subscribe(delegate(int i)
            {
                FolderItemCollection.RemoveAt(i);
                FileNameCollection.Clear();
            });

            eventAggregator.GetEvent<FileNameTagEvent>()
                .Subscribe(delegate(string s) { FileNameCollection.Remove(s); });

            eventAggregator.GetEvent<FileSuffixTagEvent>().Subscribe(delegate(string s)
            {
                FileSuffixCollection.Remove(s);
            });

            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += Worker_OnDoWork;
            _backgroundWorker.ProgressChanged += Worker_OnProgressChanged;
            _backgroundWorker.RunWorkerCompleted += Worker_OnRunWorkerCompleted;

            _dialogService = dialogService;

            WindowLoadedCommand = new DelegateCommand<MainWindow>(delegate(MainWindow window) { _window = window; });

            SelectDirCommand = new DelegateCommand(delegate
            {
                var temp = FolderItemCollection.Select(file => file.FullPath).ToList();

                var dialog = new FolderBrowserDialog { ShowNewFolderButton = false };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var folderPath = dialog.SelectedPath;
                    if (temp.Contains(folderPath))
                    {
                        _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                            {
                                { "AlertType", AlertType.Warning }, { "Title", "温馨提示" }, { "Message", "文件夹已添加，请勿重复添加" }
                            },
                            delegate { }
                        );
                        return;
                    }

                    var folder = new FileInfo(folderPath);
                    _folderModel = new FolderModel
                    {
                        Name = folder.Name,
                        FullPath = folderPath
                    };
                    FolderItemCollection.Add(_folderModel);

                    //遍历刚刚添加的文件夹
                    var traverseResult = _folderModel.FullPath.TraverseFolder();
                    //更新中间区域文件九宫格
                    UpdateCenterGridView(traverseResult.Item2);
                }
            });

            //左侧列表选中事件
            FolderItemSelectedCommand = new DelegateCommand(delegate
            {
                var selectedItem = _window.FolderListBox.SelectedItem;
                if (selectedItem == null)
                {
                    return;
                }

                _folderModel = (FolderModel)selectedItem;
                //遍历刚刚添加的文件夹
                var traverseResult = _folderModel.FullPath.TraverseFolder();
                //更新中间区域文件九宫格
                UpdateCenterGridView(traverseResult.Item2);
            });

            //打开文件
            MouseDoubleClickCommand = new DelegateCommand(delegate
            {
                var selectedFileName = _window.FileListBox.SelectedItem as string;
                var fullPaths = _folderModel.FullPath.TraverseFolder().Item1;
                foreach (var fullPath in fullPaths.Where(fullPath =>
                             selectedFileName != null && fullPath.Contains(selectedFileName)))
                {
                    //本机默认文件打开
                    Process.Start(fullPath);
                    return;
                }
            });

            AddFileSuffixTypeCommand = new DelegateCommand(delegate
            {
                if (string.IsNullOrWhiteSpace(_suffixType))
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Error }, { "Title", "错误" }, { "Message", "文件类型为空，无法添加" }
                        },
                        delegate { }
                    );
                    return;
                }

                if (FileSuffixCollection.Contains(_suffixType) || FileSuffixCollection.Contains($".{_suffixType}"))
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Warning }, { "Title", "温馨提示" }, { "Message", "文件类型已添加，请勿重复添加" }
                        },
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
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Warning }, { "Title", "温馨提示" }, { "Message", "请设置需要格式化的文件后缀" }
                        },
                        delegate { }
                    );
                    return;
                }

                //如果用户没有设置过保存路径，那就默认已登录账号桌面路径为保存文档的路径
                if (string.IsNullOrEmpty(_outputFilePath))
                {
                    var current = WindowsIdentity.GetCurrent();
                    var currentName = current.Name;
                    var userName = currentName.Split('\\')[1];

                    _outputFilePath = $@"C:\Users\{userName}\Desktop\软著代码";
                }

                //按照设置的文件后缀遍历文件
                _generateFilePathCollection = new ObservableCollection<string>();

                var files = _folderModel.FullPath.GetDirFiles();
                foreach (var file in files.Where(file => _fileSuffixCollection.Contains(file.Extension)))
                {
                    _generateFilePathCollection.Add(file.FullName);
                }

                //启动文件处理后台线程
                if (_backgroundWorker.IsBusy)
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Warning }, { "Title", "温馨提示" }, { "Message", "当前正在处理文件中" }
                        },
                        delegate { }
                    );
                    return;
                }

                _backgroundWorker.RunWorkerAsync();
            });

            //选择文档保存的路径
            SelectPathCommand = new DelegateCommand(delegate
            {
                var fileDialog = new SaveFileDialog();
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    _outputFilePath = fileDialog.FileName;
                }
            });
        }

        /// <summary>
        /// 更新中间区域文件九宫格
        /// </summary>
        private void UpdateCenterGridView(List<string> result)
        {
            //每次选中文件夹都需要同步刷新中间九宫格文件以
            if (FileNameCollection.Any())
            {
                FileNameCollection.Clear();
            }

            foreach (var name in result)
            {
                FileNameCollection.Add(name);
            }
        }

        private void Worker_OnDoWork(object sender, DoWorkEventArgs e)
        {
            //所有符合要求的代码文件内容
            var codeContentArray = new List<string>();
            var effectiveCode = new List<string>();
            var i = 0;
            foreach (var filePath in _generateFilePathCollection)
            {
                //读取源文件，跳过读取空白行
                var lines = File.ReadAllLines(filePath);
                codeContentArray.AddRange(lines.Where(line => !string.IsNullOrWhiteSpace(line)));

                //更新处理进度
                i++;
                var percent = i / (float)_generateFilePathCollection.Count;
                _backgroundWorker.ReportProgress((int)(percent * 100));

                //此行代码根据情况可选择删除或者保留
                Thread.Sleep(20);
            }

            //筛选代码行区间
            if (codeContentArray.Count <= RuntimeCache.EffectiveCodeCount)
            {
                effectiveCode = codeContentArray;
                //设置有效代码行数
                EffectiveCodeLines = $"代码文档已生成，有效代码共：{effectiveCode.Count}行";
            }
            else
            {
                //选择前后30页代码，写入Txt
                for (var j = 0; j < RuntimeCache.EffectiveCodeCount / 2; j++)
                {
                    effectiveCode.Add(codeContentArray[j]);
                }

                var end = codeContentArray.Count - RuntimeCache.EffectiveCodeCount / 2;
                for (var k = end; k < codeContentArray.Count; k++)
                {
                    effectiveCode.Add(codeContentArray[k]);
                }

                //设置有效代码行数
                EffectiveCodeLines = $"有效代码共：{effectiveCode.Count}行，源代码共：{codeContentArray.Count}行";
            }

            //生成带有缩进格式的Text，便于写入word
            File.WriteAllLines($"{_outputFilePath}.txt", effectiveCode);

            //读取整篇格式化好了的Text写入word
            var text = File.ReadAllText($"{_outputFilePath}.txt");
            var docX = DocX.Create(_outputFilePath);
            var paragraph = docX.InsertParagraph();
            var fmt = new Formatting
            {
                FontFamily = new Font("微软雅黑"),
                Size = 8 //软著要求每页50行代码，8号字体正好合适
            };
            paragraph.Append(text, fmt);
            try
            {
                docX.Save();
            }
            catch (ArgumentException)
            {
                _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                    {
                        { "AlertType", AlertType.Error }, { "Title", "错误" }, { "Message", "文件类型错误，无法生成代码文件" }
                    },
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
            Growl.Success($"软著代码生成完毕，保存在{_outputFilePath}");
        }
    }
}