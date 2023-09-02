using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace CodeGenerator.ViewModels
{
    public class AlertControlDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; private set; }
        public event Action<IDialogResult> RequestClose;

        #region VM

        private string _alertMessage;

        public string AlertMessage
        {
            get => _alertMessage;
            private set
            {
                _alertMessage = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region DelegateCommand

        public DelegateCommand ConfirmAlertCommand { set; get; }
        public DelegateCommand CloseAlertCommand { set; get; }

        #endregion

        public AlertControlDialogViewModel()
        {
            ConfirmAlertCommand = new DelegateCommand(delegate
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            });

            CloseAlertCommand = new DelegateCommand(delegate
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
            });
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>("Title");
            AlertMessage = parameters.GetValue<string>("Message");
        }
    }
}