namespace YouTubeDownloader
{
    public sealed class SavedState
    {
        public string Folder;
        public int FolderVersion;
        public string Theme = "Dark";
        public bool ThemeSelected;
        public string Language = "en";
        public int Format, Quality;
        public int AudioBitrate = 320;
        public AudioEncoding AudioEncoding = AudioEncoding.Mp3;
        public List<string> Recent = new List<string>();
    }
}
