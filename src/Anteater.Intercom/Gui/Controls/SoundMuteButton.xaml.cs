using System;
using System.Threading.Tasks;
using Anteater.Pipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Anteater.Intercom.Gui.Controls
{
    public partial class SoundMuteButton : Button
    {
        public static readonly DependencyProperty IsSoundMutedProperty = DependencyProperty
            .Register(nameof(IsSoundMuted), typeof(bool), typeof(SoundMuteButton), PropertyMetadata
            .Create(false));

        private readonly IEventPublisher _pipe;

        public SoundMuteButton()
        {
            _pipe = App.ServiceProvider.GetRequiredService<IEventPublisher>();

            var callStateChanged = _pipe.Subscribe<CallStateChanged>(x =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    IsEnabled = !x.IsCalling;
                    _pipe.Publish(new SoundStateChanged(x.IsCalling ? false : IsSoundMuted));
                });

                return Task.CompletedTask;
            });

            InitializeComponent();

            IsSoundMuted = true;

            _pipe.Publish(new SoundStateChanged(IsSoundMuted));

            void UnloadEventHandler()
            {
                callStateChanged.Dispose();
            };

            Unloaded += (_, _) => UnloadEventHandler();
            MainWindow.Instance.Closed += (_, _) => UnloadEventHandler();
        }

        public bool IsSoundMuted
        {
            get => Convert.ToBoolean(GetValue(IsSoundMutedProperty));
            set => SetValue(IsSoundMutedProperty, value);
        }

        protected override void OnTapped(TappedRoutedEventArgs e)
        {
            e.Handled = false;

            IsSoundMuted = !IsSoundMuted;

            _pipe.Publish(new SoundStateChanged(IsSoundMuted));
        }
    }
}
