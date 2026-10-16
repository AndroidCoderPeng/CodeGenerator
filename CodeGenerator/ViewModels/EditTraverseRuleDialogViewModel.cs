using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    [ObservableProperty] private bool _isSkipHiddenFileEnabled = true;
    [ObservableProperty] private bool _isSkipSymbolEnabled = true;
    [ObservableProperty] private int _fileSizeLimit = 2;
    [ObservableProperty] private int _maxTraverseDepth = 32;
    [ObservableProperty] private int _maxFileCount = 5000;

    #endregion

    private readonly BlackListConfig _config;
    public event EventHandler RequestClose;

    public EditTraverseRuleDialogViewModel()
    {
        _config = SettingsStore.Load<BlackListConfig>(ConfigSections.FileName, ConfigSections.BlackList);
        _defaultBlackListFolders = _config.Folders.ToObservableCollection();
        _defaultBlackListFiles = _config.Files.ToObservableCollection();
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
        DefaultBlackListFolders = _config.Folders.ToObservableCollection();
        DefaultBlackListFiles = _config.Files.ToObservableCollection();
    }

    [RelayCommand]
    private void DismissDialog()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private void SaveRule()
    {
        _config.Folders = DefaultBlackListFolders.ToList();
        _config.Files = DefaultBlackListFiles.ToList();
        SettingsStore.Save(ConfigSections.FileName, ConfigSections.BlackList, _config);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}