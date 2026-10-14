using System.Windows;
using CodeGenerator.DataService;
using CodeGenerator.DataService.Impl;
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

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Data
            containerRegistry.RegisterSingleton<IAppDataService, AppDataServiceImpl>();
        }
    }
}