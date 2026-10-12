using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace YouTubeDownloader {
    public sealed class ToolSet {
        public string YtDlp, FFmpeg, Deno;
        public bool Ready { get { return YtDlp != null && FFmpeg != null && Deno != null; } }
        public string Missing { get { return String.Join(", ", new [] { YtDlp == null ? "yt-dlp" : null, FFmpeg == null ? "FFmpeg" : null, Deno == null ? "Deno" : null }.Where(x => x != null)); } }
        public static ToolSet Discover() {
            return new ToolSet { YtDlp = Locate("yt-dlp.exe"), FFmpeg = Locate("ffmpeg.exe"), Deno = Locate("deno.exe") };
        }
        private static string Locate(string name) {
            var locations = new List<string> {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links")
            };
            locations.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'));
            foreach (string folder in locations.Distinct(StringComparer.OrdinalIgnoreCase)) {
                try { string file = Path.GetFullPath(Path.Combine(folder.Trim('"'), name)); if (File.Exists(file)) return file; } catch { }
            }
            return null;
        }
    }

    public static class VideoLink {
        public static string Normalize(string text) {
            text = (text ?? "").Trim();
            if (text.Length == 0) throw new ArgumentException("Paste a YouTube video link first.");
            if (text.Length > 4096 || text.Any(Char.IsControl)) throw new ArgumentException("Paste one complete YouTube video link.");
            if (!text.Contains("://")) text = "https://" + text;
            Uri uri;
            if (!Uri.TryCreate(text, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http") || !String.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort)
                throw new ArgumentException("Enter a valid YouTube video link.");
            string host = uri.IdnHost.ToLowerInvariant(), id = null;
            string[] parts = uri.AbsolutePath.Trim('/').Split('/');
            if (host == "youtu.be" || host == "www.youtu.be") {
                if (parts.Length == 1) id = parts[0];
            } else if (host == "youtube.com" || host == "www.youtube.com" || host == "m.youtube.com" || host == "music.youtube.com") {
                if (parts.Length == 1 && parts[0] == "watch") {
                    foreach (string pair in uri.Query.TrimStart('?').Split('&')) {
                        string[] entry = pair.Split(new [] { '=' }, 2);
                        if (entry.Length == 2 && entry[0] == "v") { id = Uri.UnescapeDataString(entry[1]); break; }
                    }
                } else if (parts.Length == 2 && (parts[0] == "shorts" || parts[0] == "live" || parts[0] == "embed")) id = parts[1];
            } else throw new ArgumentException("Use a youtube.com or youtu.be video link.");
            if (id == null || !Regex.IsMatch(id, "^[A-Za-z0-9_-]{11}$"))
                throw new ArgumentException("Use a link to one video or Short. Playlist-only links are not supported.");
            return "https://www.youtube.com/watch?v=" + id;
        }
    }

    public sealed class DownloadOptions {
        public string Url, Folder;
        public string FormatSelector;
        public bool Audio;
        public int Height = 1080;
        public int AudioBitrate = 320;
    }
    public sealed class VideoInfo {
        public string Title, Uploader;
        public double Duration;
        public bool Live;
        public List<VideoQuality> Qualities = new List<VideoQuality>();
        public bool HasAudio;
    }
    public sealed class VideoQuality {
        public int Resolution;
        public string Selector;
        public override string ToString() { return Resolution + "p"; }
    }

    public static class AvailableFormats {
        private sealed class Source { public string Id; public int Resolution; public bool Audio, H264, Direct; public double Fps, Bitrate; }
        public static List<VideoQuality> Read(Dictionary<string,object> info) {
            object formats; var sources=new List<Source>();
            if(!info.TryGetValue("formats",out formats) || !(formats is IEnumerable)) return new List<VideoQuality>();
            foreach(object item in (IEnumerable)formats) {
                var f=item as Dictionary<string,object>; if(f==null) continue;
                string codec=Text(f,"vcodec"), id=Text(f,"format_id");
                if(codec=="" || codec=="none" || codec=="images" || Text(f,"protocol")=="mhtml" || Text(f,"has_drm")=="True" || !Regex.IsMatch(id,"^[A-Za-z0-9_.-]+$")) continue;
                int height=(int)Number(f,"height"), width=(int)Number(f,"width");
                int resolution=width>0 ? Math.Min(width,height) : height;
                if(resolution<=0) continue;
                sources.Add(new Source { Id=id,Resolution=resolution,Audio=Text(f,"acodec")!="" && Text(f,"acodec")!="none",H264=codec.StartsWith("avc",StringComparison.OrdinalIgnoreCase) || codec.StartsWith("h264",StringComparison.OrdinalIgnoreCase),Direct=Text(f,"protocol")=="https" || Text(f,"protocol")=="http",Fps=Number(f,"fps"),Bitrate=Number(f,"tbr") });
            }
            var qualities=new List<VideoQuality>();
            foreach(var group in sources.GroupBy(f=>f.Resolution).OrderByDescending(g=>g.Key)) {
                var alternatives=new List<string>();
                foreach(var source in group.OrderByDescending(s=>s.H264).ThenByDescending(s=>s.Direct).ThenByDescending(s=>s.Fps).ThenByDescending(s=>s.Bitrate)) {
                    if(source.Audio) alternatives.Add(source.Id);
                    else { alternatives.Add(source.Id+"+bestaudio[ext=m4a]"); alternatives.Add(source.Id+"+bestaudio"); }
                }
                qualities.Add(new VideoQuality { Resolution=group.Key,Selector=String.Join("/",alternatives.Distinct()) });
            }
            return qualities;
        }
        public static int RecommendedIndex(List<VideoQuality> qualities) {
            if(qualities.Count==0) return -1;
            int index=qualities.FindIndex(q=>q.Resolution<=1080); return index>=0 ? index : 0;
        }
        public static bool HasAudio(Dictionary<string,object> info) {
            object formats; if(!info.TryGetValue("formats",out formats) || !(formats is IEnumerable)) return false;
            foreach(object item in (IEnumerable)formats) { var f=item as Dictionary<string,object>; if(f!=null && Text(f,"acodec")!="" && Text(f,"acodec")!="none" && Text(f,"has_drm")!="True") return true; }
            return false;
        }
        private static string Text(Dictionary<string,object> data,string key) { object v; return data.TryGetValue(key,out v) && v!=null ? Convert.ToString(v,CultureInfo.InvariantCulture) : ""; }
        private static double Number(Dictionary<string,object> data,string key) { double n; return Double.TryParse(Text(data,key),NumberStyles.Any,CultureInfo.InvariantCulture,out n) ? n : 0; }
    }
    public sealed class DownloadResult {
        public string FilePath;
        public bool Cancelled;
    }
    public sealed class DownloadProgress {
        public string Stage, Detail;
        public double Percent;
        public bool Indeterminate;
    }

    public static class Arguments {
        // Windows CommandLineToArgvW quoting, including quotes and trailing slashes.
        public static string Quote(string value) {
            var result = new StringBuilder("\""); int slashes = 0;
            foreach (char ch in value) {
                if (ch == '\\') { slashes++; continue; }
                if (ch == '"') { result.Append('\\', slashes * 2 + 1).Append(ch); slashes = 0; }
                else { result.Append('\\', slashes).Append(ch); slashes = 0; }
            }
            result.Append('\\', slashes * 2).Append('"'); return result.ToString();
        }
        public static List<string> Common(ToolSet tools) {
            return new List<string> { "--ignore-config", "--no-update", "--no-playlist", "--encoding", "utf-8", "--socket-timeout", "20", "--retries", "3", "--fragment-retries", "3", "--js-runtimes", "deno:" + tools.Deno, "--ffmpeg-location", tools.FFmpeg };
        }
        public static List<string> ForDownload(ToolSet tools, DownloadOptions options) {
            string url = VideoLink.Normalize(options.Url);
            if (String.IsNullOrWhiteSpace(options.Folder)) throw new ArgumentException("Choose a folder for your download.");
            string folder = Path.GetFullPath(options.Folder);
            if (folder.StartsWith(@"\\")) throw new ArgumentException("Choose a folder on this computer.");
            if (options.Height<0 || options.Height>10000) throw new ArgumentException("Choose a supported video quality.");
            if(options.Audio && options.AudioBitrate!=192 && options.AudioBitrate!=320) throw new ArgumentException("Choose a supported MP3 bitrate.");
            var args = Common(tools);
            string filename="%(title).100B [%(id)s]"+(options.Audio ? " - "+options.AudioBitrate+"kbps" : " - "+(options.Height>0 ? options.Height+"p" : "best"))+".%(ext)s";
            args.AddRange(new [] { "--no-simulate", "--newline", "--progress", "--progress-delta", "0.3", "--windows-filenames", "--no-overwrites", "--trim-filenames", "180", "--paths", folder, "--output", filename, "--progress-template", "download:YTDL_PROGRESS:%(progress)j", "--progress-template", "postprocess:YTDL_POST:%(progress)j", "--print", "before_dl:YTDL_TITLE:%(title)j", "--print", "after_move:YTDL_FILE:%(filepath)j" });
            if (options.Audio) args.AddRange(new [] { "--format", "bestaudio/best", "--extract-audio", "--audio-format", "mp3", "--audio-quality", options.AudioBitrate+"K" });
            else {
                string limit = options.Height == 0 ? "" : "[height<=" + options.Height.ToString(CultureInfo.InvariantCulture) + "]";
                string selector=String.IsNullOrEmpty(options.FormatSelector) ? "bv*" + limit + "+ba/b" + limit : options.FormatSelector;
                if(selector.Length>18000 || selector.Any(Char.IsControl)) throw new ArgumentException("The video's format information is invalid. Check the video again.");
                args.AddRange(new [] { "--format", selector, "--format-sort", "res,vcodec:h264,acodec:aac", "--merge-output-format", "mp4", "--remux-video", "mp4" });
            }
            args.Add("--"); args.Add(url); return args;
        }
    }

    public sealed class DownloadEngine : IDisposable {
        private readonly ToolSet tools;
        private readonly object gate = new object();
        private Process active;
        private ProcessJob job;
        private volatile bool cancelled;
        public DownloadEngine(ToolSet tools) { this.tools = tools; }
        public event Action<DownloadProgress> Progress;
        public event Action<string> Title;
        public event Action<string> Log;
        public bool IsBusy { get { lock (gate) return active != null; } }
        private static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 }; }
        private void Emit(string stage, string detail, double percent, bool unknown) {
            var handler = Progress; if (handler != null) handler(new DownloadProgress { Stage=stage, Detail=detail, Percent=percent, Indeterminate=unknown });
        }
        private string Execute(List<string> args, Action<string> output, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (!tools.Ready) throw new InvalidOperationException("Missing tools: " + tools.Missing + ". Place their executables in the tools folder or install them separately.");
            cancelled = false;
            var errors = new List<string>(); var capture = new StringBuilder();
            var start = new ProcessStartInfo(tools.YtDlp, String.Join(" ", args.Select(Arguments.Quote))) {
                UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true,
                StandardOutputEncoding=Encoding.UTF8, StandardErrorEncoding=Encoding.UTF8, WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory
            };
            using (var process = new Process { StartInfo=start }) {
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (e.Data == null) return;
                    if (output != null) { try { output(e.Data); } catch (Exception ex) { lock(errors) errors.Add("Could not read downloader output: " + ex.Message); } }
                    else lock(capture) { if (capture.Length < 32 * 1024 * 1024) capture.AppendLine(e.Data); }
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) {
                    if (String.IsNullOrWhiteSpace(e.Data)) return;
                    lock(errors) { errors.Add(e.Data); if(errors.Count > 80) errors.RemoveAt(0); }
                    var handler = Log; if(handler != null) handler(e.Data);
                };
                lock(gate) {
                    if (active != null) throw new InvalidOperationException("A download is already running.");
                    token.ThrowIfCancellationRequested();
                    if (!process.Start()) throw new InvalidOperationException("Could not start yt-dlp.");
                    active=process; job=new ProcessJob(); job.Assign(process);
                }
                try {
                    using(token.Register(Cancel)) {
                        process.BeginOutputReadLine(); process.BeginErrorReadLine(); process.WaitForExit();
                    }
                    if (cancelled || token.IsCancellationRequested) throw new OperationCanceledException();
                    if (process.ExitCode != 0) {
                        string error; lock(errors) error=errors.LastOrDefault(x => x.StartsWith("ERROR:",StringComparison.Ordinal)) ?? errors.LastOrDefault() ?? "The download failed.";
                        throw new InvalidOperationException(error);
                    }
                    return capture.ToString();
                } finally { lock(gate) { active=null; if(job != null) { job.Dispose(); job=null; } } }
            }
        }
        public VideoInfo Inspect(string url, CancellationToken token=default(CancellationToken)) {
            url=VideoLink.Normalize(url); var args=Arguments.Common(tools);
            args.AddRange(new [] { "--skip-download", "--dump-single-json", "--", url });
            Emit("Checking video", "Getting the title and available formats…",0,true);
            var data=Json().Deserialize<Dictionary<string,object>>(Execute(args,null,token));
            return new VideoInfo { Title=Value(data,"title"), Uploader=Value(data,"uploader"), Duration=Number(data,"duration"), Live=Value(data,"is_live")=="True",Qualities=AvailableFormats.Read(data),HasAudio=AvailableFormats.HasAudio(data) };
        }
        public DownloadResult Download(DownloadOptions options, CancellationToken token=default(CancellationToken)) {
            var args=Arguments.ForDownload(tools,options); Directory.CreateDirectory(Path.GetFullPath(options.Folder));
            string saved=null;
            Emit("Connecting", "Preparing your download…",0,true);
            try {
                Execute(args, delegate(string line) {
                    if(line.StartsWith("YTDL_FILE:")) saved=Json().Deserialize<string>(line.Substring(10));
                    else if(line.StartsWith("YTDL_TITLE:")) { var h=Title; if(h != null) h(Json().Deserialize<string>(line.Substring(11))); }
                    else if(line.StartsWith("YTDL_PROGRESS:")) {
                        var p=Json().Deserialize<Dictionary<string,object>>(line.Substring(14));
                        double total=Number(p,"total_bytes"); if(total<=0) total=Number(p,"total_bytes_estimate");
                        double bytes=Number(p,"downloaded_bytes"), speed=Number(p,"speed"), eta=Number(p,"eta");
                        if(Value(p,"status")=="finished") Emit("Finishing download", "Preparing the final file…",100,true);
                        else {
                            string detail=Size(bytes) + (total>0 ? " of " + Size(total) : "") + (speed>0 ? "  •  " + Size(speed) + "/s" : "") + (eta>0 ? "  •  " + TimeSpan.FromSeconds(eta).ToString(@"m\:ss") + " remaining" : "");
                            Emit("Downloading",detail,total>0 ? Math.Min(100,100*bytes/total) : 0,total<=0);
                        }
                    } else if(line.StartsWith("YTDL_POST:")) Emit(options.Audio ? "Making MP3" : "Preparing MP4", "Finishing your file. This can take a moment…",100,true);
                },token);
            } catch(OperationCanceledException) { return new DownloadResult { Cancelled=true }; }
            if(String.IsNullOrWhiteSpace(saved) || !File.Exists(saved)) throw new InvalidOperationException("The downloader did not produce a completed file. Try again or choose another quality.");
            string root=Path.GetFullPath(options.Folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(saved).StartsWith(root,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The downloaded file is outside the selected folder.");
            Emit("Download complete",Path.GetFileName(saved),100,false); return new DownloadResult { FilePath=saved };
        }
        private static string Value(Dictionary<string,object> data,string key) { object v; return data.TryGetValue(key,out v) && v!=null ? Convert.ToString(v,CultureInfo.InvariantCulture) : ""; }
        private static double Number(Dictionary<string,object> data,string key) { double n; return Double.TryParse(Value(data,key),NumberStyles.Any,CultureInfo.InvariantCulture,out n) ? n : 0; }
        public static string Size(double bytes) { string[] names={"B","KB","MB","GB"}; int unit=0; while(bytes>=1024 && unit<3) { bytes/=1024; unit++; } return bytes.ToString(unit==0 ? "0" : "0.0",CultureInfo.InvariantCulture)+" "+names[unit]; }
        public void Cancel() {
            lock(gate) {
                if(active==null) return; cancelled=true;
                if(job!=null && job.Assigned) { job.Dispose(); job=null; }
                else {
                    try {
                        if(!active.HasExited) {
                            using(var killer=Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"taskkill.exe"),"/PID "+active.Id+" /T /F") { UseShellExecute=false, CreateNoWindow=true })) { killer.WaitForExit(5000); }
                        }
                    } catch { try { if(!active.HasExited) active.Kill(); } catch { } }
                }
            }
        }
        public void Dispose() { Cancel(); }
    }

    internal sealed class ProcessJob : IDisposable {
        private IntPtr handle;
        public bool Assigned { get; private set; }
        public ProcessJob() {
            handle=CreateJobObject(IntPtr.Zero,null); if(handle==IntPtr.Zero) return;
            var info=new ExtendedLimits(); info.Basic.LimitFlags=0x2000;
            int size=Marshal.SizeOf(typeof(ExtendedLimits)); IntPtr memory=Marshal.AllocHGlobal(size);
            try { Marshal.StructureToPtr(info,memory,false); if(!SetInformationJobObject(handle,9,memory,(uint)size)) Dispose(); }
            finally { Marshal.FreeHGlobal(memory); }
        }
        public void Assign(Process process) { if(handle!=IntPtr.Zero) Assigned=AssignProcessToJobObject(handle,process.Handle); }
        public void Dispose() { if(handle!=IntPtr.Zero) { CloseHandle(handle); handle=IntPtr.Zero; } }
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits { public long PerProcessUserTimeLimit, PerJobUserTimeLimit; public uint LimitFlags; public UIntPtr MinimumWorkingSetSize,MaximumWorkingSetSize; public uint ActiveProcessLimit; public UIntPtr Affinity; public uint PriorityClass,SchedulingClass; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong ReadOperationCount,WriteOperationCount,OtherOperationCount,ReadTransferCount,WriteTransferCount,OtherTransferCount; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits { public BasicLimits Basic; public IoCounters IO; public UIntPtr ProcessMemoryLimit,JobMemoryLimit,PeakProcessMemoryUsed,PeakJobMemoryUsed; }
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr CreateJobObject(IntPtr attributes,string name);
        [DllImport("kernel32.dll")] private static extern bool SetInformationJobObject(IntPtr job,int infoClass,IntPtr info,uint length);
        [DllImport("kernel32.dll")] private static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    }
}
