using System;
using System.Diagnostics;

namespace Chiapos.Dotnet
{
    public class PerformanceTimer
    {
        Stopwatch timer = Stopwatch.StartNew();

        public void PrintElapsed(string text)
        {
            timer.Stop();
            if (timer.ElapsedMilliseconds >= 20000)
            {
                var elapsed = timer.ElapsedMilliseconds / 1000;
                Console.WriteLine($"{text} {elapsed}s");
            }
            else
            {
                var elapsed = timer.ElapsedMilliseconds;
                Console.WriteLine($"{text} {elapsed}ms");
            }
        }

        public void ResetAndPrintElapsed(string text)
        {
            timer.Stop();
            PrintElapsed(text);
            timer.Restart();
        }
    }
}