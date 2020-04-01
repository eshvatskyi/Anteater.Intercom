using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Anteater.Intercom.Gui.ViewModel;

namespace Anteater.Intercom.Gui.Views
{
    public partial class VideoPlayer
    {
        private static readonly TimeSpan ResizeHandleTimeout = TimeSpan.FromMilliseconds(500);

        private Task _handleSizeChangedTask = Task.CompletedTask;
        private CancellationTokenSource _resizeCancellationTokenSource = new CancellationTokenSource();

        private int _width;
        private int _height;

        public VideoPlayer()
        {
            var app = Application.Current as App;

            if (app != null)
            {
                ViewModel = new VideoPlayerViewModel(app.AlarmEvents, app.Rtsp);
            }

            DataContext = ViewModel;

            InitializeComponent();

            Dispatcher.ShutdownStarted += (s, e) => ViewModel?.Dispose();
        }

        public VideoPlayerViewModel ViewModel { get; }

        protected override Size MeasureOverride(Size constraint)
        {
            var newWidth = (int)constraint.Width;
            var newHeight = (int)constraint.Height;

            if (_width != newWidth || _height != newHeight)
            {
                _width = newWidth;
                _height = newHeight;

                _resizeCancellationTokenSource.Cancel();
                _resizeCancellationTokenSource = new CancellationTokenSource();

                _handleSizeChangedTask = _handleSizeChangedTask.ContinueWith(async prev =>
                {
                    try
                    {
                        var cancellationToken = _resizeCancellationTokenSource.Token;

                        await Task.Delay(ResizeHandleTimeout, cancellationToken).ConfigureAwait(false);

                        Dispatcher.Invoke(() => ViewModel.Resize(newWidth, newHeight));
                    }
                    catch (OperationCanceledException) { }
                });
            }

            return base.MeasureOverride(constraint);
        }
    }
}
