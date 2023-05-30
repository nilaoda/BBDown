using System;
using System.Text;
using System.Threading;

/**
 * From https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
 */
namespace BBDown
{
    class ProgressBar : IDisposable, IProgress<double>
	{
		private const int blockCount = 40;
		private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private const string animation = @"|/-\";

		private readonly Timer timer;

		private double currentProgress = 0;
		private string currentText = string.Empty;
		private bool disposed = false;
		private int animationIndex = 0;

		//速度计算
		private long lastDownloadedBytes = 0;
		private long downloadedBytes = 0;
		private string speedString = "";
        private readonly Timer speedTimer;

        public ProgressBar()
		{
			timer = new Timer(TimerHandler);
			speedTimer = new Timer(SpeedTimerHandler, null, 100, 1000);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
			{
				ResetTimer();
			}
		}

		public void Report(double value)
		{
			// Make sure value is in [0..1] range
			value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        public void Report(double value, long bytesCount)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
			Interlocked.Exchange(ref downloadedBytes, bytesCount);
        }

        private void SpeedTimerHandler(object? state)
        {
            lock (speedTimer)
            {
                if (disposed) return;

                if (downloadedBytes > 0 && downloadedBytes - lastDownloadedBytes > 0)
                {
                    speedString = " - " + BBDownUtil.FormatFileSize(downloadedBytes - lastDownloadedBytes) + "/s";
                    lastDownloadedBytes = downloadedBytes;
                }
            }
        }

        private void TimerHandler(object? state)
		{
			lock (timer)
			{
				if (disposed) return;

                int progressBlockCount = (int)(currentProgress * blockCount);
				int percent = (int)(currentProgress * 100);
				string text = string.Format("                            [{0}{1}] {2,3}% {3}{4}",
					new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
					percent,
					animation[animationIndex++ % animation.Length],
                    speedString);
				UpdateText(text);

				ResetTimer();
			}
		}

		private void UpdateText(string text)
		{
			// Get length of common portion
			int commonPrefixLength = 0;
			int commonLength = Math.Min(currentText.Length, text.Length);
			while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
			{
				commonPrefixLength++;
			}

			// Backtrack to the first differing character
			StringBuilder outputBuilder = new();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text[commonPrefixLength..]);

			// If the new text is shorter than the old one: delete overlapping characters
			int overlapCount = currentText.Length - text.Length;
			if (overlapCount > 0)
			{
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
			}

			Console.Write(outputBuilder);
			currentText = text;
		}

		private void ResetTimer()
		{
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
		}

		public void Dispose()
		{
			lock (timer)
			{
				disposed = true;
				UpdateText(string.Empty);
			}
		}
	}
}
