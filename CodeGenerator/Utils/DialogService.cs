using System.Windows;
using CodeGenerator.Dialog;

namespace CodeGenerator.Utils;

public sealed class DialogService : IDialogService
{
    public void ShowDialog<T>(object parameter = null) where T : class, new()
    {
        var dialog = new EditTraverseRuleDialog
        {
            Owner = Application.Current.MainWindow,
            DataContext = new T()
        };

        dialog.ShowDialog();
    }
}