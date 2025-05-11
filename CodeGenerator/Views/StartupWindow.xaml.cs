using System;
using System.Windows;
using System.Windows.Threading;

namespace CodeGenerator.Views
{
    public partial class StartupWindow : Window
    {
        private const int TotalSteps = 50;
        private readonly TimeSpan _timerInterval = new TimeSpan(0, 0, 0, 0, 1);
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private int _counterTime = TotalSteps;

        public StartupWindow()
        {
            InitializeComponent();
            _timer.Interval = _timerInterval;
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (LoadingProgress != null && _counterTime > 0)
            {
                LoadingProgress.Value++;
                _counterTime--;
            }
            else
            {
                _timer.Stop();
                DialogResult = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }
    }
}