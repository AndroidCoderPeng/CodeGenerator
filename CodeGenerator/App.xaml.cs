using System.Windows;
using CodeGenerator.Dialogs;
using CodeGenerator.ViewModels;
using CodeGenerator.Views;
using Prism.DryIoc;
using Prism.Ioc;

namespace CodeGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void InitializeShell(Window shell)
        {
            //可以实现登录
            var loginWindow = Container.Resolve<LoginWindow>();
            var loginResult = loginWindow.ShowDialog();
            if (loginResult == null)
            {
                Current.Shutdown();
                return;
            }

            if (loginResult.Value)
            {
                base.OnInitialized();
            }
            else
            {
                Current.Shutdown();
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialog<AlertMessageDialog, AlertMessageDialogViewModel>();
        }
    }
}