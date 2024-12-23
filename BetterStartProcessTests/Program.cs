using BetterStartProcess;

public static class Application
{
    public static void Main()
    {
        Console.WriteLine("Starting process...");
        var processId1 = Process.Start("/opt/homebrew/bin/python3");
        var processId2 = Process.Start("/opt/homebrew/bin/python3");
        // var processId = ForkProcess.Start("python3");

        Console.WriteLine($"Process started {processId1}.");
        Thread.Sleep(3000);
        Process.Kill();
        Console.WriteLine("Exiting process...");
    }
}
