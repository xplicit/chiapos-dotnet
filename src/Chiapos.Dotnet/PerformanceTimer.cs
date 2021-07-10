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
            var elapsed = timer.ElapsedMilliseconds / 1000;

            Console.WriteLine($"{text} {elapsed}s");
        }
    }
}