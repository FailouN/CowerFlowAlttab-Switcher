using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CoverflowAltTab
{
    public partial class HotkeyWindow : Window
    {
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;

        private const uint VK_KEY = 0x51; // Q
        private readonly int _hotkeyId = 1;
        private HwndSource? _source;

        private SwitcherWindow? _switcher;
        private DispatcherTimer? _releasePollTimer;

        public HotkeyWindow()
        {
            InitializeComponent();
            this.Hide(); // само окно HotkeyWindow не показываем
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _source = (HwndSource)PresentationSource.FromVisual(this)!;
            _source.AddHook(WndProc);

            bool ok = Native.RegisterHotKey(_source.Handle, _hotkeyId,
                                            MOD_CONTROL | MOD_ALT | MOD_NOREPEAT,
                                            VK_KEY);
            if (!ok)
            {
                MessageBox.Show("❌ Не удалось зарегистрировать хоткей Ctrl+Alt+Q");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_source != null)
            {
                Native.UnregisterHotKey(_source.Handle, _hotkeyId);
                _source.RemoveHook(WndProc);
                _source = null;
            }
            StopReleasePoll();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
            {
                if (_switcher == null || !_switcher.IsVisible)
                {
                    _switcher = new SwitcherWindow();
                    _switcher.Show();
                    _switcher.Activate();
                    _switcher.Focus();

                    StartReleasePoll();
                }
                else
                {
                    _switcher.NextWindow();
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void StartReleasePoll()
        {
            if (_releasePollTimer != null)
            {
                _releasePollTimer.Stop();
                _releasePollTimer.Tick -= ReleasePollTimer_Tick;
            }

            _releasePollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _releasePollTimer.Tick += ReleasePollTimer_Tick;
            _releasePollTimer.Start();
        }

        private void StopReleasePoll()
        {
            if (_releasePollTimer != null)
            {
                _releasePollTimer.Stop();
                _releasePollTimer.Tick -= ReleasePollTimer_Tick;
                _releasePollTimer = null;
            }
        }

        private void ReleasePollTimer_Tick(object? sender, EventArgs e)
        {
            bool ctrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            bool altDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

            if (!ctrlDown && !altDown)
            {
                if (_switcher != null && _switcher.IsVisible)
                {
                    _switcher.ConfirmAndClose();
                    _switcher = null;
                }
                StopReleasePoll();
            }
        }
    }
}
