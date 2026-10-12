using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

    public sealed class YouTubeLink {
        public string VideoUrl, PlaylistUrl, PlaylistId;
    }
    public static class VideoLink {
        public static YouTubeLink Parse(string text) {
            text = (text ?? "").Trim();
            if (text.Length == 0) throw new ArgumentException("Paste a YouTube video link first.");
            if (text.Length > 4096 || text.Any(Char.IsControl)) throw new ArgumentException("Paste one complete YouTube video link.");
            if (!text.Contains("://")) text = "https://" + text;
            Uri uri;
            if (!Uri.TryCreate(text, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http") || !String.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort)
                throw new ArgumentException("Enter a valid YouTube video link.");
            string host = uri.IdnHost.ToLowerInvariant(), id = null, list = null;
            foreach(string pair in uri.Query.TrimStart('?').Split('&')) {
                string[] entry=pair.Split(new[]{'='},2);
                if(entry.Length==2 && entry[0]=="list") list=Uri.UnescapeDataString(entry[1]);
            }
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
            bool validVideo=id!=null && Regex.IsMatch(id,"^[A-Za-z0-9_-]{11}$");
            bool validList=list!=null && Regex.IsMatch(list,"^[A-Za-z0-9_-]{2,150}$");
            bool playlistPath=parts.Length==1 && parts[0]=="playlist";
            if(!validVideo && !(validList && playlistPath)) throw new ArgumentException("Use a link to a YouTube video, Short, or playlist.");
            return new YouTubeLink { VideoUrl=validVideo ? "https://www.youtube.com/watch?v="+id : null,PlaylistId=validList ? list : null,PlaylistUrl=validList ? "https://www.youtube.com/playlist?list="+list : null };
        }
        public static string Normalize(string text) { var link=Parse(text); return link.VideoUrl ?? link.PlaylistUrl; }
        public static string Normalize(string text,bool playlist) {
            var link=Parse(text); string result=playlist ? link.PlaylistUrl : link.VideoUrl;
            if(result==null) throw new ArgumentException(playlist ? "This link does not contain a playlist." : "Choose Whole playlist for a playlist-only link.");
            return result;
        }
    }

    public sealed class DownloadOptions {
        public string Url, Folder;
        public string FormatSelector;
        public bool Audio;
        public int Height = 1080;
        public int AudioBitrate = 320;
        public PlaylistInfo Playlist;
        public bool PlaylistItem;
        internal string ResumeDirectory { get; set; }
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
        public List<string> Files=new List<string>();
        public int Total, Completed, Failed, Skipped, Remaining;
        public string StopReason;
    }
    public sealed class DownloadProgress {
        public string Stage, Detail;
        public double Percent;
        public bool Indeterminate;
        public int Item, Total;
        public string ItemTitle;
    }

    public sealed class PlaylistEntry {
        public string Id, Title;
        public string Url { get { return Id==null ? null : "https://www.youtube.com/watch?v="+Id; } }
    }
    public sealed class PlaylistInfo {
        public string Id, Url, Title;
        public List<PlaylistEntry> Entries=new List<PlaylistEntry>();
        public static PlaylistInfo Read(string url,Dictionary<string,object> data) {
            var link=VideoLink.Parse(url); object entries,title;
            if(link.PlaylistUrl==null || !data.TryGetValue("entries",out entries) || !(entries is IEnumerable)) throw new InvalidOperationException("The playlist could not be read. Check the link and try again.");
            var info=new PlaylistInfo { Id=link.PlaylistId,Url=link.PlaylistUrl,Title=data.TryGetValue("title",out title) && title!=null ? Convert.ToString(title) : "Playlist" };
            foreach(object value in (IEnumerable)entries) {
                var entry=value as Dictionary<string,object>; object id,name;
                string videoId=entry!=null && entry.TryGetValue("id",out id) && id!=null ? Convert.ToString(id) : null;
                if(videoId!=null && !Regex.IsMatch(videoId,"^[A-Za-z0-9_-]{11}$")) videoId=null;
                info.Entries.Add(new PlaylistEntry { Id=videoId,Title=entry!=null && entry.TryGetValue("title",out name) && name!=null ? Convert.ToString(name) : "Unavailable video" });
            }
            if(info.Entries.Count==0) throw new InvalidOperationException("This playlist is empty or unavailable. Try a public playlist.");
            return info;
        }
    }

    public static class DownloadErrors {
        public static bool IsDiskFull(string message) {
            string error=(message??"").ToLowerInvariant();
            return error.Contains("no space") || error.Contains("disk full") || error.Contains("not enough space") || error.Contains("enospc") || Regex.IsMatch(error,@"\b(?:winerror\s*112|errno\s*28)\b");
        }
    }

    internal sealed class PlaylistRecords {
        internal readonly string FilePath;
        internal Dictionary<string,string> Completed;
        private PlaylistRecords(string path) { FilePath=path; }
        private static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength=32*1024*1024 }; }
        internal static string DefaultDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YouTubeDownloader","PlaylistState"); } }
        internal static string RecordPath(string folder,string profile,string directory) {
            string key=Path.GetFullPath(folder).TrimEnd('\\','/').ToUpperInvariant()+"\n"+profile;
            using(var hash=SHA256.Create()) return Path.Combine(directory,BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-","").ToLowerInvariant()+".json");
        }
        internal static PlaylistRecords Open(string folder,string profile,string directory=null) {
            directory=directory ?? DefaultDirectory; Directory.CreateDirectory(directory);
            foreach(string legacy in Directory.GetFiles(folder,".yd-completed-*.json")) {
                string previousProfile=Path.GetFileNameWithoutExtension(legacy).Substring(".yd-completed-".Length);
                if(Regex.IsMatch(previousProfile,@"^mp(?:4-\d{1,5}|3-(?:192|320))$")) Import(folder,previousProfile,directory);
            }
            return Load(folder,profile,directory);
        }
        private static PlaylistRecords Load(string folder,string profile,string directory) {
            var records=new PlaylistRecords(RecordPath(folder,profile,directory));
            records.Completed=Read(records.FilePath) ?? new Dictionary<string,string>();
            return records;
        }
        private static void Import(string folder,string profile,string directory) {
            var records=Load(folder,profile,directory);
            string legacy=Path.Combine(folder,".yd-completed-"+profile+".json");
            if(File.Exists(legacy)) {
                var previous=Read(legacy);
                if(previous!=null) {
                    foreach(var entry in previous) if(!records.Completed.ContainsKey(entry.Key)) records.Completed[entry.Key]=entry.Value;
                    records.Save();
                } else File.Copy(legacy,records.FilePath+".legacy",true);
                // Remove the old visible record only after its data is safely stored privately.
                File.Delete(legacy);
            }
        }
        private static Dictionary<string,string> Read(string file) {
            if(!File.Exists(file)) return null;
            try { return Json().Deserialize<Dictionary<string,string>>(File.ReadAllText(file,Encoding.UTF8)); }
            catch(ArgumentException) { return null; } catch(InvalidOperationException) { return null; }
        }
        internal void Save() {
            string temporary=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                File.WriteAllText(temporary,Json().Serialize(Completed),Encoding.UTF8);
                if(File.Exists(FilePath)) File.Replace(temporary,FilePath,null); else File.Move(temporary,FilePath);
            } finally { if(File.Exists(temporary)) File.Delete(temporary); }
        }
    }

    // The flat playlist is fetched once. Each item has bounded retries and a single active process.
    // Completed paths are recorded per output profile and revalidated before skipping on a later run.
    public static class PlaylistQueue {
        public static string OutputFolder(DownloadOptions options) {
            var link=VideoLink.Parse(options.Url);
            if(options.Playlist==null || link.PlaylistId!=options.Playlist.Id || options.Playlist.Url!=link.PlaylistUrl) throw new ArgumentException("Check the playlist link again before downloading.");
            if(String.IsNullOrWhiteSpace(options.Folder)) throw new ArgumentException("Choose a folder for your download.");
            string root=Path.GetFullPath(options.Folder);
            if(root.StartsWith(@"\\")) throw new ArgumentException("Choose a folder on this computer.");
            return Path.Combine(root,"Playlist ["+link.PlaylistId+"]");
        }
        public static bool ShouldStop(string error) {
            error=(error??"").ToLowerInvariant();
            return DownloadErrors.IsDiskFull(error) || new[]{"429","too many requests","403","confirm you're not a bot","confirm you’re not a bot","access is denied","permission denied","timed out","connection","resolve","network","missing tools"}.Any(error.Contains);
        }
        private static bool Unavailable(string error) {
            error=(error??"").ToLowerInvariant(); return new[]{"unavailable","private video","removed","age-restricted","not available in your country","requested format"}.Any(error.Contains);
        }
        public static DownloadResult Run(DownloadOptions options,Func<DownloadOptions,CancellationToken,DownloadResult> download,Action<int,int,string> itemStarted,CancellationToken token,Action<CancellationToken> pace=null) {
            string folder=OutputFolder(options); Directory.CreateDirectory(folder);
            string profile=options.Audio ? "mp3-"+options.AudioBitrate : "mp4-"+options.Height;
            var records=PlaylistRecords.Open(folder,profile,options.ResumeDirectory); var completed=records.Completed;
            var result=new DownloadResult { Total=options.Playlist.Entries.Count }; int done=0,consecutiveFailures=0;
            foreach(var entry in options.Playlist.Entries) {
                if(token.IsCancellationRequested) { result.Cancelled=true; break; }
                if(itemStarted!=null) itemStarted(done+1,result.Total,entry.Title);
                string file;
                if(entry.Id!=null && completed.TryGetValue(entry.Id,out file) && Inside(folder,file) && File.Exists(file)) {
                    result.Skipped++; result.Completed++; result.Files.Add(file); result.FilePath=file; done++; continue;
                }
                try {
                    if(entry.Url==null) throw new InvalidOperationException("This video is unavailable.");
                    // Waits are cancellable, including the gaps between processes.
                    if(done>0) { if(pace!=null) pace(token); else if(token.WaitHandle.WaitOne(2000)) token.ThrowIfCancellationRequested(); }
                    token.ThrowIfCancellationRequested();
                    var item=new DownloadOptions { Url=entry.Url,Folder=folder,Audio=options.Audio,AudioBitrate=options.AudioBitrate,Height=options.Height,PlaylistItem=true };
                    var saved=download(item,token);
                    if(saved.Cancelled) { result.Cancelled=true; break; }
                    file=saved.FilePath;
                    if(!Inside(folder,file) || !File.Exists(file)) throw new InvalidOperationException("The downloader did not produce a completed file.");
                    completed[entry.Id]=file; result.Files.Add(file); result.FilePath=file; result.Completed++; consecutiveFailures=0;
                    try {
                        records.Save();
                    } catch(IOException) { result.StopReason="Completed file saved, but resume information could not be saved."; break; }
                    catch(UnauthorizedAccessException) { result.StopReason="Completed file saved, but resume information could not be saved."; break; }
                } catch(OperationCanceledException) { result.Cancelled=true; break; }
                catch(Exception ex) {
                    result.Failed++; consecutiveFailures=Unavailable(ex.Message) ? 0 : consecutiveFailures+1;
                    if(ShouldStop(ex.Message) || consecutiveFailures>=3) { result.StopReason=ex.Message; done++; break; }
                }
                done++;
            }
            result.Remaining=Math.Max(0,result.Total-result.Completed-result.Failed);
            result.Files=result.Files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }
        private static bool Inside(string folder,string file) {
            if(String.IsNullOrWhiteSpace(file)) return false;
            try { return Path.GetFullPath(file).StartsWith(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase); } catch { return false; }
        }
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
            string url = VideoLink.Normalize(options.Url,options.Playlist!=null);
            if (String.IsNullOrWhiteSpace(options.Folder)) throw new ArgumentException("Choose a folder for your download.");
            string folder = Path.GetFullPath(options.Folder);
            if (folder.StartsWith(@"\\")) throw new ArgumentException("Choose a folder on this computer.");
            if (options.Height<0 || options.Height>10000) throw new ArgumentException("Choose a supported video quality.");
            if(options.Audio && options.AudioBitrate!=192 && options.AudioBitrate!=320) throw new ArgumentException("Choose a supported MP3 bitrate.");
            var args = Common(tools);
            bool playlist=options.Playlist!=null || options.PlaylistItem;
            if(options.Playlist!=null) { args.Remove("--no-playlist"); args.Add("--yes-playlist"); }
            if(playlist) args.AddRange(new[]{"--sleep-requests","0.75","--sleep-interval","2","--max-sleep-interval","4","--retry-sleep","http:exp=1:8","--match-filters","!is_live"});
            string filename="%(title).100B [%(id)s]"+(options.Audio ? " - "+options.AudioBitrate+"kbps" : " - "+(options.Height>0 ? (playlist ? "max" : "")+options.Height+"p" : "best"))+".%(ext)s";
            args.AddRange(new [] { "--no-simulate", "--continue", "--part", "--newline", "--progress", "--progress-delta", "0.3", "--windows-filenames", "--no-overwrites", "--trim-filenames", "180", "--paths", folder, "--output", filename, "--progress-template", "download:YTDL_PROGRESS:%(progress)j", "--progress-template", "postprocess:YTDL_POST:%(progress)j", "--print", "before_dl:YTDL_TITLE:%(title)j", "--print", "after_move:YTDL_FILE:%(filepath)j" });
            if (options.Audio) args.AddRange(new [] { "--format", "bestaudio/best", "--extract-audio", "--audio-format", "mp3", "--audio-quality", options.AudioBitrate+"K" });
            else {
                string limit = options.Height == 0 ? "" : "[height<=" + options.Height.ToString(CultureInfo.InvariantCulture) + "]";
                string selector=playlist && options.Height>0 ? "(bv*[height<="+options.Height+"]/bv*[width<="+options.Height+"])+ba/(b[height<="+options.Height+"]/b[width<="+options.Height+"])" : String.IsNullOrEmpty(options.FormatSelector) ? "bv*" + limit + "+ba/b" + limit : options.FormatSelector;
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
        private int playlistItem, playlistTotal;
        private double playlistFraction;
        private string playlistTitle;
        public DownloadEngine(ToolSet tools) { this.tools = tools; }
        public event Action<DownloadProgress> Progress;
        public event Action<string> Title;
        public event Action<string> Log;
        public bool IsBusy { get { lock (gate) return active != null; } }
        private static JavaScriptSerializer Json() { return new JavaScriptSerializer { MaxJsonLength = 32 * 1024 * 1024 }; }
        private void Emit(string stage, string detail, double percent, bool unknown) {
            if(playlistTotal>0) playlistFraction=Math.Max(playlistFraction,stage=="Download complete" ? 1 : Math.Min(0.95,Math.Max(0,percent)/100));
            var handler = Progress; if (handler != null) handler(new DownloadProgress { Stage=stage, Detail=detail, Percent=playlistTotal>0 ? 100.0*(playlistItem-1+playlistFraction)/playlistTotal : percent, Indeterminate=unknown && playlistTotal==0,Item=playlistItem,Total=playlistTotal,ItemTitle=playlistTitle });
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
            url=VideoLink.Normalize(url,false); var args=Arguments.Common(tools);
            args.AddRange(new [] { "--skip-download", "--dump-single-json", "--", url });
            Emit("Checking video", "Getting the title and available formats…",0,true);
            var data=Json().Deserialize<Dictionary<string,object>>(Execute(args,null,token));
            return new VideoInfo { Title=Value(data,"title"), Uploader=Value(data,"uploader"), Duration=Number(data,"duration"), Live=Value(data,"is_live")=="True",Qualities=AvailableFormats.Read(data),HasAudio=AvailableFormats.HasAudio(data) };
        }
        public PlaylistInfo InspectPlaylist(string url,CancellationToken token=default(CancellationToken)) {
            url=VideoLink.Normalize(url,true); var args=Arguments.Common(tools); args.Remove("--no-playlist");
            args.AddRange(new[]{"--yes-playlist","--flat-playlist","--skip-download","--dump-single-json","--sleep-requests","0.75","--",url});
            Emit("Checking playlist","Getting the playlist titles…",0,true);
            return PlaylistInfo.Read(url,Json().Deserialize<Dictionary<string,object>>(Execute(args,null,token)));
        }
        public DownloadResult Download(DownloadOptions options, CancellationToken token=default(CancellationToken)) {
            if(options.Playlist!=null) {
                try { return PlaylistQueue.Run(options,delegate(DownloadOptions item,CancellationToken itemToken) { return Download(item,itemToken); },delegate(int index,int total,string title) { playlistItem=index; playlistTotal=total; playlistTitle=title; playlistFraction=0; Emit("Downloading playlist","Preparing your download…",0,false); },token); }
                finally { playlistItem=playlistTotal=0; playlistTitle=null; }
            }
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
            if(String.IsNullOrWhiteSpace(saved) || !File.Exists(saved)) throw new InvalidOperationException(options.PlaylistItem ? "This video is live or unavailable." : "The downloader did not produce a completed file. Try again or choose another quality.");
            string root=Path.GetFullPath(options.Folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if(!Path.GetFullPath(saved).StartsWith(root,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The downloaded file is outside the selected folder.");
            Emit("Download complete",Path.GetFileName(saved),100,false); return new DownloadResult { FilePath=saved,Files=new List<string>{saved},Total=1 };
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
