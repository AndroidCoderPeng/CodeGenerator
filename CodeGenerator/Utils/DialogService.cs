using CodeGenerator.Dialog;
using CodeGenerator.ViewModels;
using System.Windows;

namespace CodeGenerator.Utils;

public sealed class DialogService : IDialogService
{
    public void ShowEditTraverseRuleDialog()
    {
        var dialog = new EditTraverseRuleDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = new EditTraverseRuleDialogViewModel()
        };

        dialog.ShowDialog();
    }
}