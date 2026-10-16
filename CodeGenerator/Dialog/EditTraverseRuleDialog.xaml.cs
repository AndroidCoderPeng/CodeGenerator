using System.Windows;
using CodeGenerator.ViewModels;

namespace CodeGenerator.Dialog;

public partial class EditTraverseRuleDialog : Window
{
    public EditTraverseRuleDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is EditTraverseRuleDialogViewModel viewModel)
            {
                viewModel.RequestClose += (_, _) => Close();
            }
        };
    }
}