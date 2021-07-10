using System;

namespace chiapos_dotnet
{
    public class ProgressNotificator
    {
        public static void ShowProgress(int phase, long n, long max_n)
        {
            double p = (100.0 / 4) * ((phase - 1.0) + (1.0 * n / max_n));
            Console.WriteLine($"Progress: {p}");
        }
    }
}