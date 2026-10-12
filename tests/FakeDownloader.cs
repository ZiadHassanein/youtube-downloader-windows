using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;

// Deterministic process fixture. These are text files, never real downloaded media.
internal static class FakeDownloader {
    public static int Main(string[] args) {
        var json=new JavaScriptSerializer(); string url=args.Last();
        if(args.Contains("--dump-single-json")) {
            Console.WriteLine("{\"title\":\"Test playlist\",\"entries\":[{\"id\":\"jNQXAC9IVRw\",\"title\":\"First\"},{\"id\":\"abcdefghijk\",\"title\":\"Unavailable\"},{\"id\":\"aqz-KE-bpKQ\",\"title\":\"Last\"}]}"); return 0;
        }
        if(url.EndsWith("abcdefghijk")) { Console.Error.WriteLine("ERROR: Video unavailable"); return 1; }
        if(url.EndsWith("rateLimit01")) { Console.Error.WriteLine("ERROR: HTTP 429 Too Many Requests"); return 1; }
        if(url.EndsWith("slowVideo01")) Thread.Sleep(10000);
        int index=Array.IndexOf(args,"--paths"); string folder=args[index+1]; Directory.CreateDirectory(folder);
        string file=Path.Combine(folder,"fixture "+url.Substring(url.Length-11)+".mp4");
        Console.WriteLine("YTDL_TITLE:"+json.Serialize("Fixture title"));
        Console.WriteLine("YTDL_PROGRESS:{\"downloaded_bytes\":512,\"total_bytes\":1024,\"speed\":128,\"eta\":4}");
        File.WriteAllText(file,"Local fixture only");
        Console.WriteLine("YTDL_FILE:"+json.Serialize(file)); return 0;
    }
}
