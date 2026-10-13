internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var output = AppContext.BaseDirectory;
            foreach (string file in Directory.GetFiles(args[0]))
                File.Copy(file, Path.Combine(output, Path.GetFileName(file)), true);
            if (CoreTests.Run() != 0)
                return 1;
            await ReliabilityChecks.Run(args[0]);
            await BoundaryChecks.Run(args[0]);
            await ServiceChecks.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
