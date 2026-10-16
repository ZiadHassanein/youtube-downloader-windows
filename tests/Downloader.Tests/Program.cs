internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--native-host") return await NativeHostChecks.Run(args[1]);
            if (args.Length == 2 && args[0] == "--package") return PackageArtifactChecks.Run(args[1]);
            if (args.Length == 3 && args[0] == "--direct-media-live") return DirectMediaLiveChecks.Run(args[1], args[2]);
            var output = AppContext.BaseDirectory;
            foreach (string file in Directory.GetFiles(args[0]))
                File.Copy(file, Path.Combine(output, Path.GetFileName(file)), true);
            if (CoreTests.Run() != 0)
                return 1;
            await ReliabilityChecks.Run(args[0]);
            await BoundaryChecks.Run(args[0]);
            await ServiceChecks.Run();
            await WorkflowChecks.Run(args[0]);
            OutputProfileChecks.Run(args[0]);
            await AutomaticToolChecks.Run(args[0]);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
