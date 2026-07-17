using System;

namespace fraction;
public static class Config {
    


    public static void HandleArgs(string[] args) {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--bench":
                    int d = 5;
                    if (args.Length > i + 1 && int.TryParse(args[i + 1], out int parsed)) d = parsed;
                    Testing.BenchmarkPERFT(d);
                    Environment.Exit(0);
                    return;
                case "--ethe":
                    Testing.LoadAndTest();
                    Environment.Exit(0);
                    return;

                default:
                break;
            }
        }
    }
}