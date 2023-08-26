using System;
using System.Windows;
using System.Windows.Threading;

namespace CodeGenerator.Views
{
    public partial class LoginWindow : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 0, 1)
        };

        private int _counterTime = 1;

        public LoginWindow()
        {
            InitializeComponent();

            //倒计时显示大屏Logo
            _timer.Start();
            _timer.Tick += delegate
            {
                if (_counterTime > 0)
                {
                    _counterTime--;
                }
                else
                {
                    _timer.Stop();
                    DialogResult = true;
                }
            };
        }
    }
}