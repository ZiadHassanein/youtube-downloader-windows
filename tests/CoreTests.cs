using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using YouTubeDownloader;

public static class CoreTests {
    private static int checks;
    private static void Check(bool condition,string name) { if(!condition) throw new Exception(name); checks++; Console.WriteLine("PASS: "+name); }
    public static int Main() {
        try {
            const string expected="https://www.youtube.com/watch?v=jNQXAC9IVRw";
            foreach(string url in new[]{expected,expected+"&list=example","https://youtu.be/jNQXAC9IVRw?t=3","https://youtube.com/shorts/jNQXAC9IVRw"}) Check(VideoLink.Normalize(url)==expected,"Normalize video URL");
            foreach(string url in new[]{"", "https://example.com/watch?v=jNQXAC9IVRw", "https://youtube.com.evil.invalid/watch?v=jNQXAC9IVRw", "file:///C:/Windows/system32/cmd.exe", "https://youtube.com/playlist?list=example", "https://youtu.be/jNQXAC9IVRw\n--exec"}) {
                try { VideoLink.Normalize(url); throw new Exception("Invalid link was accepted"); } catch(ArgumentException) { checks++; }
            }
            var serializer=new JavaScriptSerializer();
            var metadata=serializer.Deserialize<Dictionary<string,object>>(@"{""formats"":[{""format_id"":""hd"",""height"":720,""width"":1280,""vcodec"":""avc1"",""acodec"":""none""},{""format_id"":""sd"",""height"":480,""width"":854,""vcodec"":""avc1"",""acodec"":""none""},{""format_id"":""audio"",""vcodec"":""none"",""acodec"":""aac""}]}");
            var qualities=AvailableFormats.Read(metadata);
            Check(qualities.Select(x=>x.Resolution).SequenceEqual(new[]{720,480}),"Only actual video qualities are offered");
            Check(qualities[AvailableFormats.RecommendedIndex(qualities)].Resolution==720,"Do not suggest unavailable 1080p");
            Check(AvailableFormats.HasAudio(metadata),"Recognize an audio stream");
            var portrait=serializer.Deserialize<Dictionary<string,object>>(@"{""formats"":[{""format_id"":""short"",""height"":1920,""width"":1080,""vcodec"":""avc1"",""acodec"":""none""},{""format_id"":""locked"",""height"":2160,""width"":3840,""vcodec"":""avc1"",""has_drm"":true}]}");
            Check(AvailableFormats.Read(portrait).Single().Resolution==1080,"Portrait quality and protected-format filtering");
            var tools=new ToolSet { YtDlp="yt-dlp.exe",FFmpeg="ffmpeg.exe",Deno="deno.exe" };
            var options=new DownloadOptions { Url=expected,Folder=Environment.CurrentDirectory,Height=720,FormatSelector=qualities[0].Selector };
            var args=Arguments.ForDownload(tools,options);
            Check(args.Contains(qualities[0].Selector),"Use actual format IDs");
            Check(args.Contains("%(title).100B [%(id)s] - 720p.%(ext)s"),"Separate filenames by selected quality");
            Check(args.Contains("--ignore-config") && args.Contains("--no-playlist") && args.Contains("--no-overwrites"),"Keep predictable single-video behavior");
            Check(args.Last()==expected && args[args.Count-2]=="--","Safe final URL argument");
            options.Audio=true; Check(Arguments.ForDownload(tools,options).Contains("--extract-audio"),"MP3 extraction");
            Check(Arguments.Quote("a\"b")=="\"a\\\"b\"","Windows argument quoting");
            Console.WriteLine(checks+" core checks passed."); return 0;
        } catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
