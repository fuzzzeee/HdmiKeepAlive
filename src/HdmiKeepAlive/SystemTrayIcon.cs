using System;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace HdmiKeepAlive
{
    class SystemTrayIcon : IDisposable
    {
        private readonly SoundPlayer _player = new SoundPlayer(new MemoryStream(CreateSilence()));
        private readonly NotifyIcon _notifyIcon = new NotifyIcon();
        private readonly Timer _timer = new Timer();

        public SystemTrayIcon()
        {
            _notifyIcon.Icon = Properties.Resources.heart;
            _notifyIcon.Text = "HDMI Keep-Alive";
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();

            var exit = _notifyIcon.ContextMenuStrip.Items.Add("Exit");
            exit.Click += ExitOnClick;

            _timer.Tick += TimerOnTick;
            _timer.Interval = 5000;
            _timer.Enabled = true;
        }

        private void ExitOnClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                _player.Play();
            }
            catch (Exception ex)
            {
                _notifyIcon.ShowBalloonTip(5000, "Error", ex.Message, ToolTipIcon.Error);
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer.Enabled = false;
            _timer.Dispose();
            _player.Stop();
            _player.Dispose();
            _notifyIcon.Dispose();
        }

        private static byte[] CreateSilence(int seconds = 2, int sampleRate = 48000, short channels = 1, short bitsPerSample = 16)
        {
            var bytesPerSample = (short)(bitsPerSample / 8);

            var numSamples = seconds * sampleRate;
            var numBytes = numSamples * channels * bytesPerSample;

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(0x46464952); // "RIFF"
                    writer.Write(numBytes + 36);
                    writer.Write(0x45564157); // "WAVE"
                    writer.Write(0x20746d66); // "fmt "
                    writer.Write(16); // size of PCM = 16
                    writer.Write((short)1); // AudioFormat
                    writer.Write(channels); // NumChannels
                    writer.Write(sampleRate); // sample rate
                    writer.Write(sampleRate * channels * bytesPerSample); // byte rate
                    writer.Write((short)(channels * bytesPerSample)); // block align
                    writer.Write(bitsPerSample); // bps
                    writer.Write(0x61746164); // "data"
                    writer.Write(numBytes); // byte rate
                    writer.Write(new byte[numBytes]);
                }
                return stream.ToArray();
            }
        }
    }
}
