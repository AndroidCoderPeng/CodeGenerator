using System;
using System.Collections.ObjectModel;
using CodeGenerator.Cache;
using CodeGenerator.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodeGenerator.ViewModels;

public partial class EditTraverseRuleDialogViewModel : ObservableObject
{
    #region VM

    [ObservableProperty] private string _blackListFolder;
    [ObservableProperty] private string _blackListFile;
    [ObservableProperty] private ObservableCollection<string> _defaultBlackListFolders;
    [ObservableProperty] private ObservableCollection<string> _defaultBlackListFiles;
    [ObservableProperty] private bool _isSkipHiddenFileEnabled;
    [ObservableProperty] private bool _isSkipSymbolEnabled;
    [ObservableProperty] private int _fileSizeLimit;
    [ObservableProperty] private int _maxTraverseDepth;
    [ObservableProperty] private int _maxFileCount;

    #endregion

    public sealed class DialogCloseRequestedEventArgs(bool? dialogResult) : EventArgs
    {
        public bool? DialogResult { get; } = dialogResult;
    }
    
    private readonly BlackListConfig _config;
    public event EventHandler<DialogCloseRequestedEventArgs> RequestClose;

    public EditTraverseRuleDialogViewModel()
    {
        _config = SettingsStore.Load<BlackListConfig>(ConfigSections.FileName, ConfigSections.BlackList);
        DefaultBlackListFolders = _config.Folders.ToObservableCollection();
        DefaultBlackListFiles = _config.Files.ToObservableCollection();
        IsSkipHiddenFileEnabled = _config.SkipHiddenFile;
        IsSkipSymbolEnabled = _config.SkipSymbol;
        FileSizeLimit = _config.FileSizeLimit;
        MaxTraverseDepth = _config.MaxTraverseDepth;
        MaxFileCount = _config.MaxFileCount;
    }

    [RelayCommand]
    private void AddBlackListFolder()
    {
        if (string.IsNullOrWhiteSpace(BlackListFolder) || DefaultBlackListFolders.Contains(BlackListFolder)) return;

        DefaultBlackListFolders.Add(BlackListFolder);
        BlackListFolder = string.Empty;
    }

    [RelayCommand]
    private void DeleteRule(CommandParameter parameter)
    {
        switch (parameter.Type)
        {
            case "Folder":
                DefaultBlackListFolders.Remove(parameter.BoundValue);
                break;
            case "File":
                DefaultBlackListFiles.Remove(parameter.BoundValue);
                break;
            default:
                Console.WriteLine(parameter.Type);
                break;
        }
    }

    [RelayCommand]
    private void AddBlackListFile()
    {
        if (string.IsNullOrWhiteSpace(BlackListFile) || DefaultBlackListFiles.Contains(BlackListFile)) return;

        DefaultBlackListFiles.Add(BlackListFile);
        BlackListFile = string.Empty;
    }

    [RelayCommand]
    private void ResetRule()
    {
        var defaultConfig = new BlackListConfig();
        DefaultBlackListFolders = defaultConfig.Folders.ToObservableCollection();
        DefaultBlackListFiles = defaultConfig.Files.ToObservableCollection();
        IsSkipHiddenFileEnabled = defaultConfig.SkipHiddenFile;
        IsSkipSymbolEnabled = defaultConfig.SkipSymbol;
        FileSizeLimit = defaultConfig.FileSizeLimit;
        MaxTraverseDepth = defaultConfig.MaxTraverseDepth;
        MaxFileCount = defaultConfig.MaxFileCount;
    }

    [RelayCommand]
    private void DismissDialog()
    {
        RequestClose?.Invoke(this, new DialogCloseRequestedEventArgs(false));
    }

    [RelayCommand]
    private void SaveRule()
    {
        _config.Folders = [.. DefaultBlackListFolders];
        _config.Files = [.. DefaultBlackListFiles];
        _config.SkipHiddenFile = IsSkipHiddenFileEnabled;
        _config.SkipSymbol = IsSkipSymbolEnabled;
        _config.FileSizeLimit = FileSizeLimit;
        _config.MaxTraverseDepth = MaxTraverseDepth;
        _config.MaxFileCount = MaxFileCount;
        SettingsStore.Save(ConfigSections.FileName, ConfigSections.BlackList, _config);
        RequestClose?.Invoke(this, new DialogCloseRequestedEventArgs(true));
    }
}