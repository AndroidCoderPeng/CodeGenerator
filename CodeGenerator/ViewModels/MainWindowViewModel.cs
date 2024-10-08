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
using CodeGenerator.Utils;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Xceed.Document.NET;
using Xceed.Words.NET;
using DialogResult = System.Windows.Forms.DialogResult;
using Formatting = Xceed.Document.NET.Formatting;

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

        public DelegateCommand SelectFolderCommand { set; get; }
        public DelegateCommand<string> MouseDoubleClickCommand { set; get; }
        public DelegateCommand<string> DeleteFileCommand { set; get; }
        public DelegateCommand AddFileSuffixTypeCommand { set; get; }
        public DelegateCommand<string> DeleteFileSuffixCommand { set; get; }
        public DelegateCommand GeneratorCodeCommand { set; get; }
        public DelegateCommand SelectPathCommand { set; get; }

        #endregion

        private readonly IDialogService _dialogService;
        private string _outputFilePath;
        private int _effectiveCodeCount;
        private int _size;
        private readonly BackgroundWorker _backgroundWorker;

        /// <summary>
        /// 需要格式化的文件全路径集
        /// </summary>
        private List<string> _generateFilePaths;

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += Worker_OnDoWork;
            _backgroundWorker.ProgressChanged += Worker_OnProgressChanged;
            _backgroundWorker.RunWorkerCompleted += Worker_OnRunWorkerCompleted;

            SelectFolderCommand = new DelegateCommand(SelectFolder);
            MouseDoubleClickCommand = new DelegateCommand<string>(OpenFile);
            DeleteFileCommand = new DelegateCommand<string>(DeleteFile);
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
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FolderPath = dialog.SelectedPath;

                if (_folderPath == null)
                {
                    return;
                }

                //遍历刚刚添加的文件夹
                var traverseResult = _folderPath.TraverseFolder();
                //更新中间区域文件九宫格
                if (FileNameCollection.Any())
                {
                    FileNameCollection.Clear();
                }

                foreach (var file in traverseResult)
                {
                    FileNameCollection.Add(file.Name);
                }

                FileSuffixCollection.Clear();
            }
        }

        /// <summary>
        /// 双击打开文件
        /// </summary>
        /// <param name="selectedItem"></param>
        private void OpenFile(string selectedItem)
        {
            var files = _folderPath.TraverseFolder();
            foreach (var file in files)
            {
                if (selectedItem != null && file.FullName.Contains(selectedItem))
                {
                    //本机默认程序打开
                    Process.Start(file.FullName);
                    return;
                }
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName"></param>
        private void DeleteFile(string fileName)
        {
            FileNameCollection.Remove(fileName);
        }

        /// <summary>
        /// 添加需要格式化的后缀
        /// </summary>
        private void AddFileSuffixType()
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

            if (FileSuffixCollection.Contains($"*{_suffixType}") ||
                FileSuffixCollection.Contains($"*.{_suffixType}"))
            {
                _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                    {
                        { "AlertType", AlertType.Warning }, { "Title", "温馨提示" }, { "Message", "文件类型已添加，请勿重复添加" }
                    },
                    delegate { }
                );
                return;
            }

            FileSuffixCollection.Add(_suffixType.Contains(".") ? $"*{_suffixType}" : $"*.{_suffixType}");

            //添加之后将输入框置空
            SuffixType = string.Empty;
        }

        /// <summary>
        /// 删除后缀
        /// </summary>
        /// <param name="fileSuffix"></param>
        private void DeleteFileSuffix(string fileSuffix)
        {
            FileSuffixCollection.Remove(fileSuffix);
        }

        /// <summary>
        /// 生成格式化后的代码文档
        /// </summary>
        private void GeneratorCode()
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

            //如果没有设置代码页数，默认60页（前后各30页）
            if (string.IsNullOrEmpty(_codePageLimit))
            {
                _effectiveCodeCount = 60 * 50;
            }
            else
            {
                if (!_codePageLimit.IsNumber())
                {
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Error }, { "Title", "温馨提示" }, { "Message", "页码格式不对，请输入数字" }
                        },
                        delegate { }
                    );
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
                    _dialogService.ShowDialog("AlertMessageDialog", new DialogParameters
                        {
                            { "AlertType", AlertType.Error }, { "Title", "温馨提示" }, { "Message", "字体大小格式不对，请输入数字" }
                        },
                        delegate { }
                    );
                    return;
                }

                _size = Convert.ToInt32(_fontSize);
            }

            //按照设置的文件后缀遍历文件
            _generateFilePaths = new List<string>();

            var files = _folderPath.TraverseFolder();
            foreach (var file in files.Where(file => _fileSuffixCollection.Contains($"*{file.Extension}")))
            {
                _generateFilePaths.Add(file.FullName);
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