﻿using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace CodeGenerator.ViewModels
{
    public class AlertMessageDialogViewModel : BindableBase, IDialogAware
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

        public DelegateCommand CloseAlertCommand { set; get; }

        #endregion

        public AlertMessageDialogViewModel()
        {
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