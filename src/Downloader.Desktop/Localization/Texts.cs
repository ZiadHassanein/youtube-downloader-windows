using System.Globalization;

namespace YouTubeDownloader
{
    internal static class Texts
    {
        private static readonly Dictionary<string, string> Arabic = new Dictionary<string, string>
        {
            { "Video link", "رابط الفيديو" },
            { "Audio", "صوت" },
            { "Source quality", "جودة المصدر" },
            { "Queue ({0})", "قائمة التنزيلات ({0})" },
            { "Choose videos", "اختيار الفيديوهات" },
            { "Choose videos ({0}/{1})", "اختيار الفيديوهات ({0}/{1})" },
            { "Output options", "خيارات الحفظ" },
            { "Audio format", "صيغة الصوت" },
            { "Subtitles", "الترجمة" },
            { "Automatic", "تلقائية" },
            { "Get subtitle languages", "جلب لغات الترجمة" },
            { "No subtitles", "بدون ترجمة" },
            { "Add to queue", "إضافة للقائمة" },
            { "Start queue", "بدء التنزيلات" },
            { "Retry", "إعادة المحاولة" },
            { "Remove", "إزالة" },
            { "Download queue", "قائمة التنزيلات" },
            { "One link per line. New links use the selected format and maximum quality.", "رابط واحد في كل سطر. تستخدم الروابط الجديدة الصيغة المحددة والجودة القصوى المختارة." },
            { "Browser integration", "الربط بالمتصفح" },
            { "Refresh media tools", "تحديث أدوات الوسائط" },
            { "Audio conversion cannot improve the source quality. Subtitles are saved beside the media when available.", "تحويل الصوت لا يزيد جودة المصدر. تُحفظ الترجمة بجوار الوسائط عند توفرها." },
            { "Added to queue", "تمت الإضافة للقائمة" },
            { "Open the queue to start or manage downloads.", "افتح قائمة التنزيلات لبدء التنزيل أو إدارة الروابط." },
            { "Could not add download", "تعذّرت إضافة التنزيل" },
            { "Media tools", "أدوات الوسائط" },
            { "Media tools are up to date.", "أدوات الوسائط محدّثة." },
            { "Media tools updated successfully.", "تم تحديث أدوات الوسائط بنجاح." },
            { "Checking for media-tool updates…", "جارٍ البحث عن تحديثات أدوات الوسائط…" },
            { "Languages shown come from the first available playlist video. Availability may vary for other videos.", "اللغات المعروضة من أول فيديو متاح بالقائمة. قد تختلف الترجمة المتوفرة في بقية الفيديوهات." },
            { "Paste one or more video links first.", "الصق رابط فيديو واحدًا أو أكثر أولًا." },
            { "The queue can hold at most 100 downloads. Remove finished items first.", "يمكن إضافة 100 تنزيل إلى القائمة كحد أقصى. أزل التنزيلات المكتملة أولًا." },
            { "{0} links added to the queue.", "عدد الروابط المضافة للقائمة: {0}." },
            { "Queue paused", "التنزيلات متوقفة مؤقتًا" },
            { "Queue finished", "انتهت التنزيلات" },
            { "Review failed items before starting the remaining downloads.", "راجع العناصر التي فشلت قبل بدء التنزيلات المتبقية." },
            { "Open the queue to review completed or failed downloads.", "افتح قائمة التنزيلات لمراجعة التنزيلات المكتملة أو التي فشلت." },
            { "Pending", "في الانتظار" },
            { "Preparing", "جارٍ التجهيز" },
            { "Completed", "مكتمل" },
            { "Failed", "فشل" },
            { "Cancelled", "أُلغي" },
            { "Select all", "تحديد الكل" },
            { "Clear selection", "إلغاء التحديد" },
            { "Use selection", "استخدام التحديد" },
            { "Choose at least one available video.", "اختر فيديو متاحًا واحدًا على الأقل." },
            { "Send a link from your browser", "إرسال رابط من المتصفح" },
            { "Keep ViFree in a permanent folder. Enable the connection here, then load the companion extension from the Browser folder included with the app. Chrome, Edge and Firefox support local loading.", "ضع ViFree في مجلد دائم. فعّل الاتصال هنا، ثم حمّل الإضافة من مجلد Browser المرفق مع التطبيق. يدعم التحميل المحلي Chrome وEdge وFirefox." },
            { "Click the extension or right-click a link to open it in ViFree. Nothing downloads until you choose Download.", "انقر الإضافة أو انقر بزر الفأرة الأيمن على رابط لفتحه في ViFree. لا يبدأ التنزيل حتى تختار تنزيل." },
            { "Enable connection", "تفعيل الاتصال" },
            { "Disable connection", "تعطيل الاتصال" },
            { "Could not change the browser connection. Keep ViFree.exe in a writable folder and try again.", "تعذّر تغيير اتصال المتصفح. ضع ViFree.exe في مجلد قابل للكتابة وأعد المحاولة." },
            { "Browser connection enabled. Load the companion extension to send links.", "تم تفعيل اتصال المتصفح. حمّل الإضافة لإرسال الروابط." },
            { "Browser connection disabled.", "تم تعطيل اتصال المتصفح." },
            { "Preview build", "نسخة تجريبية" },
            { "This test build stays separate from stable releases. Updates will be enabled after testing.", "هذه النسخة التجريبية منفصلة عن الإصدارات المستقرة. ستُفعّل تحديثات التطبيق بعد الاختبار." },
            { "This site or link is not supported. Try a public video link.", "الموقع أو الرابط غير مدعوم. جرّب رابط فيديو عام." },
            { "The site did not return downloadable formats for this video.", "لم يُرجع الموقع صيغًا قابلة للتنزيل لهذا الفيديو." },
            { "Preparing audio", "جارٍ تجهيز الصوت" },
            { "Requested subtitles were unavailable. The media file was saved.", "الترجمة المطلوبة غير متاحة. تم حفظ ملف الوسائط." },
            { "Some requested subtitles were unavailable. The media files were saved.", "بعض الترجمات المطلوبة غير متاحة. تم حفظ ملفات الوسائط." },
            { "The selected subtitles are no longer available. Refresh output options or choose no subtitles.", "الترجمة المختارة لم تعد متاحة. حدّث خيارات الحفظ أو اختر بدون ترجمة." },
            {
                "Paste",
                "لصق"
            },
            {
                "Save as",
                "احفظ بالصيغة"
            },
            {
                "Video · MP4",
                "فيديو · MP4"
            },
            {
                "Audio · MP3",
                "صوت · MP3"
            },
            {
                "Save to",
                "مجلد الحفظ"
            },
            {
                "Browse…",
                "تصفح…"
            },
            {
                "Download",
                "تنزيل"
            },
            {
                "Cancel",
                "إلغاء"
            },
            {
                "Open folder",
                "فتح المجلد"
            },
            {
                "Open file",
                "فتح الملف"
            },
            {
                "No newer app release is available.",
                "لا يتوفر إصدار أحدث من التطبيق."
            },
            {
                "Preparing media tools",
                "جارٍ تجهيز أدوات الوسائط"
            },
            {
                "Checking available media tools…",
                "جارٍ فحص أدوات الوسائط المتاحة…"
            },
            {
                "Checking prepared media tools…",
                "جارٍ فحص أدوات الوسائط المجهزة…"
            },
            {
                "Media tools are ready.",
                "أدوات الوسائط جاهزة."
            },
            {
                "Check your internet connection, then paste the link again to retry.",
                "تحقق من اتصال الإنترنت، ثم الصق الرابط مجددًا لإعادة المحاولة."
            },
            {
                "Media tools could not be prepared. Paste the link again to retry.",
                "تعذّر تجهيز أدوات الوسائط. الصق الرابط مجددًا لإعادة المحاولة."
            },
            {
                "Cancelling…",
                "جارٍ الإلغاء…"
            },
            {
                "Settings recovered",
                "تمت استعادة الإعدادات"
            },
            {
                "Your settings were recovered from a backup.",
                "تمت استعادة إعداداتك من نسخة احتياطية."
            },
            {
                "Your preferences could not be saved. Check available storage and try again.",
                "تعذّر حفظ تفضيلاتك. تحقق من مساحة التخزين وجرّب مرة أخرى."
            },
            {
                "Videos, Shorts, and playlists",
                "الفيديوهات والمقاطع القصيرة وقوائم التشغيل"
            },
            {
                "&This video",
                "هذا ال&فيديو"
            },
            {
                "&Whole playlist",
                "&قائمة التشغيل كاملة"
            },
            {
                "This video",
                "هذا الفيديو"
            },
            {
                "Whole playlist",
                "قائمة التشغيل كاملة"
            },
            {
                "Choose this video or the whole playlist.",
                "اختر هذا الفيديو أو قائمة التشغيل كاملة."
            },
            {
                "{0} · {1} videos\nDownloads one at a time. Quality is a maximum; lower resolutions are used when needed.",
                "{0} · {1} فيديو\nيُنزل فيديو واحدًا في كل مرة. الجودة حد أقصى؛ تُستخدم دقة أقل عند الحاجة."
            },
            {
                "Best available",
                "أفضل جودة متاحة"
            },
            {
                "Up to {0}p",
                "حتى {0}p"
            },
            {
                "Playlist maximum video quality",
                "الحد الأقصى لجودة قائمة التشغيل"
            },
            {
                "Checking playlist",
                "جارٍ فحص قائمة التشغيل"
            },
            {
                "Getting the playlist titles…",
                "جارٍ جلب عناوين قائمة التشغيل…"
            },
            {
                "Could not check playlist",
                "تعذّر فحص قائمة التشغيل"
            },
            {
                "Downloading playlist",
                "جارٍ تنزيل قائمة التشغيل"
            },
            {
                "Video {0} of {1}",
                "الفيديو {0} من {1}"
            },
            {
                "Playlist complete",
                "اكتمل تنزيل قائمة التشغيل"
            },
            {
                "Playlist cancelled",
                "أُلغي تنزيل قائمة التشغيل"
            },
            {
                "Playlist paused",
                "توقف تنزيل قائمة التشغيل"
            },
            {
                "Playlist finished with errors",
                "انتهت قائمة التشغيل مع أخطاء"
            },
            {
                "{0} saved · {1} already downloaded · {2} failed · {3} remaining",
                "{0} تم حفظه · {1} تم تنزيله سابقًا · {2} تعذّر تنزيله · {3} متبقي"
            },
            {
                "Run the same playlist again to resume. Completed files are kept.",
                "أعد تنزيل القائمة نفسها للاستئناف. تُحفظ الملفات المكتملة."
            },
            {
                "Use a link to a YouTube video, Short, or playlist.",
                "استخدم رابط فيديو أو مقطع قصير أو قائمة تشغيل من يوتيوب."
            },
            {
                "This link does not contain a playlist.",
                "هذا الرابط لا يحتوي على قائمة تشغيل."
            },
            {
                "Choose Whole playlist for a playlist-only link.",
                "اختر قائمة التشغيل كاملة لرابط قائمة التشغيل."
            },
            {
                "This playlist is empty or unavailable. Try a public playlist.",
                "قائمة التشغيل فارغة أو غير متاحة. جرّب قائمة عامة."
            },
            {
                "The playlist could not be read. Check the link and try again.",
                "تعذّر قراءة قائمة التشغيل. تحقق من الرابط وجرّب مجددًا."
            },
            {
                "Check the playlist link again before downloading.",
                "تحقق من رابط قائمة التشغيل مجددًا قبل التنزيل."
            },
            {
                "Completed file saved, but resume information could not be saved.",
                "حُفظ الملف المكتمل، لكن تعذّر حفظ معلومات الاستئناف."
            },
            {
                "ViFree",
                "ViFree"
            },
            {
                "Close",
                "إغلاق"
            },
            {
                "Could not open link",
                "تعذّر فتح الرابط"
            },
            {
                "Open the link in your browser and try again.",
                "افتح الرابط في المتصفح وجرّب مرة أخرى."
            },
            {
                "YouTube &link",
                "&رابط يوتيوب"
            },
            {
                "YouTube link",
                "رابط يوتيوب"
            },
            {
                "ViFree logo",
                "شعار ViFree"
            },
            {
                "Open the save folder and try opening the file there.",
                "افتح مجلد الحفظ وجرّب فتح الملف من هناك."
            },
            {
                "Videos, Shorts, and shared links",
                "الفيديوهات والمقاطع القصيرة والروابط المشتركة"
            },
            {
                "&Paste",
                "&لصق"
            },
            {
                "&Browse…",
                "&تصفح…"
            },
            {
                "&Download",
                "&تنزيل"
            },
            {
                "&Cancel",
                "&إلغاء"
            },
            {
                "Open &folder",
                "فتح ال&مجلد"
            },
            {
                "Open &file",
                "فتح ال&ملف"
            },
            {
                "Show &in folder",
                "إظهار في ال&مجلد"
            },
            {
                "Save &as",
                "احفظ بال&صيغة"
            },
            {
                "Save &to",
                "مجلد ال&حفظ"
            },
            {
                "&Video · MP4",
                "&فيديو · MP4"
            },
            {
                "&Audio · MP3",
                "&صوت · MP3"
            },
            {
                "Video MP4",
                "فيديو MP4"
            },
            {
                "Audio MP3",
                "صوت MP3"
            },
            {
                "Quality",
                "الجودة"
            },
            {
                "192 kbps",
                "192 كيلوبت/ث"
            },
            {
                "Available video quality",
                "جودة الفيديو المتاحة"
            },
            {
                "MP3 encoding bitrate",
                "معدل ترميز MP3"
            },
            {
                "Save folder",
                "مجلد الحفظ"
            },
            {
                "Choose a video to get started",
                "اختر فيديو للبدء"
            },
            {
                "Available qualities appear after you paste a link.",
                "تظهر الجودات المتاحة بعد لصق الرابط."
            },
            {
                "Checking your next video",
                "جارٍ فحص الفيديو"
            },
            {
                "Getting the available qualities…",
                "جارٍ جلب الجودات المتاحة…"
            },
            {
                "Paste one YouTube video link.",
                "الصق رابط فيديو واحد من يوتيوب."
            },
            {
                "Ready for another video",
                "جاهز لفيديو آخر"
            },
            {
                "Paste a new link whenever you're ready.",
                "الصق رابطًا جديدًا عندما تكون جاهزًا."
            },
            {
                "Your file is being prepared.",
                "جارٍ تجهيز ملفك."
            },
            {
                "Available: ",
                "الجودات: "
            },
            {
                "Audio only",
                "صوت فقط"
            },
            {
                "Ready for a video link",
                "جاهز لرابط فيديو"
            },
            {
                "Paste a link to get started.",
                "الصق رابطًا للبدء."
            },
            {
                "Downloader tools are missing",
                "أدوات التنزيل غير متوفرة"
            },
            {
                "Run SetupTools.cmd in the app folder, then reopen the app.",
                "شغّل SetupTools.cmd من مجلد التطبيق، ثم افتح التطبيق مجددًا."
            },
            {
                "Recent downloads",
                "التنزيلات الأخيرة"
            },
            {
                "Your completed downloads will appear here.",
                "ستظهر تنزيلاتك المكتملة هنا."
            },
            {
                "Check for updates",
                "البحث عن تحديث"
            },
            {
                "Download update",
                "تنزيل التحديث"
            },
            {
                "Checking updates…",
                "جارٍ البحث…"
            },
            {
                "Retry update",
                "إعادة التحديث"
            },
            {
                "Downloading update…",
                "جارٍ التنزيل…"
            },
            {
                "Dismiss",
                "إغلاق"
            },
            {
                "Dismiss update message",
                "إغلاق رسالة التحديث"
            },
            {
                "App update status",
                "حالة تحديث التطبيق"
            },
            {
                "Checking for updates",
                "جارٍ البحث عن تحديث"
            },
            {
                "Looking for the latest app release…",
                "نبحث عن أحدث إصدار للتطبيق…"
            },
            {
                "You're up to date",
                "لديك أحدث إصدار"
            },
            {
                "Version {0} is the latest release.",
                "الإصدار {0} هو الأحدث."
            },
            {
                "A new version is ready",
                "يتوفر تحديث جديد"
            },
            {
                "Version {0} is available. Choose Install to update the app.",
                "الإصدار {0} متاح. اضغط تثبيت لتحديث التطبيق."
            },
            {
                "Couldn't check for updates",
                "تعذّر البحث عن تحديث"
            },
            {
                "Check your internet connection, then choose Check for updates again.",
                "تحقق من اتصال الإنترنت، ثم ابحث عن تحديث مجددًا."
            },
            {
                "Preparing your update",
                "جارٍ تجهيز التحديث"
            },
            {
                "The app will restart when the verified update is ready.",
                "سيُعاد تشغيل التطبيق بعد التحقق من التحديث وتجهيزه."
            },
            {
                "Update couldn't be installed",
                "تعذّر تثبيت التحديث"
            },
            {
                "Your current app is ready to use. Check your connection and choose Retry update.",
                "يمكنك استخدام الإصدار الحالي. تحقق من الاتصال ثم اضغط إعادة التحديث."
            },
            {
                "App version",
                "إصدار التطبيق"
            },
            {
                "Switch to light theme",
                "التبديل إلى المظهر الفاتح"
            },
            {
                "Switch to dark theme",
                "التبديل إلى المظهر الداكن"
            },
            {
                "Switch to Arabic",
                "التبديل إلى العربية"
            },
            {
                "Switch to English",
                "التبديل إلى الإنجليزية"
            },
            {
                "Scroll page",
                "تمرير الصفحة"
            },
            {
                "Use the arrow keys, Page Up, Page Down, Home, or End to scroll.",
                "استخدم الأسهم أو Page Up أو Page Down أو Home أو End للتمرير."
            },
            {
                "Information",
                "معلومة"
            },
            {
                "Success",
                "نجاح"
            },
            {
                "Error",
                "خطأ"
            },
            {
                "Clipboard is empty",
                "الحافظة فارغة"
            },
            {
                "Copy a YouTube video link, then choose Paste.",
                "انسخ رابط فيديو من يوتيوب، ثم اضغط لصق."
            },
            {
                "Could not paste",
                "تعذّر اللصق"
            },
            {
                "Copy the video link again, then choose Paste.",
                "انسخ رابط الفيديو مجددًا، ثم اضغط لصق."
            },
            {
                "Check the link",
                "تحقق من الرابط"
            },
            {
                "Could not check video",
                "تعذّر فحص الفيديو"
            },
            {
                "Video is ready",
                "الفيديو جاهز"
            },
            {
                "Only qualities available for this video are shown.",
                "تظهر فقط الجودات المتاحة لهذا الفيديو."
            },
            {
                "This video has an audio download available.",
                "يمكن تنزيل الصوت لهذا الفيديو."
            },
            {
                "Check cancelled",
                "أُلغي الفحص"
            },
            {
                "You can paste another link or try again.",
                "يمكنك لصق رابط آخر أو المحاولة مجددًا."
            },
            {
                "Check your download",
                "تحقق من إعدادات التنزيل"
            },
            {
                "Download failed",
                "تعذّر التنزيل"
            },
            {
                "Download cancelled",
                "أُلغي التنزيل"
            },
            {
                "Download complete",
                "اكتمل التنزيل"
            },
            {
                "Partial files are kept so the same download can resume later.",
                "تُحفظ الملفات الجزئية لاستئناف التنزيل لاحقًا."
            },
            {
                "You can start another download.",
                "يمكنك بدء تنزيل آخر."
            },
            {
                "Cancelling",
                "جارٍ الإلغاء"
            },
            {
                "Stopping the download…",
                "جارٍ إيقاف التنزيل…"
            },
            {
                "Choose an available video quality first.",
                "اختر جودة متاحة للفيديو أولًا."
            },
            {
                "An audio download is not available for this video.",
                "تنزيل الصوت غير متاح لهذا الفيديو."
            },
            {
                "Choose a save folder with Browse.",
                "اختر مجلد الحفظ باستخدام تصفح."
            },
            {
                "Choose a valid folder on this computer with Browse.",
                "اختر مجلدًا صالحًا على هذا الجهاز باستخدام تصفح."
            },
            {
                "Folder is not available",
                "المجلد غير متوفر"
            },
            {
                "Choose an existing folder or download a video first.",
                "اختر مجلدًا موجودًا أو نزّل فيديو أولًا."
            },
            {
                "Could not open folder",
                "تعذّر فتح المجلد"
            },
            {
                "File is not available",
                "الملف غير متوفر"
            },
            {
                "The file may have been moved or deleted.",
                "ربما نُقل الملف أو حُذف."
            },
            {
                "Could not open file",
                "تعذّر فتح الملف"
            },
            {
                "Choose where to save your YouTube downloads",
                "اختر مكان حفظ تنزيلات يوتيوب"
            },
            {
                "A download is running. Cancel it and close?",
                "يوجد تنزيل جارٍ. هل تريد إلغاءه وإغلاق التطبيق؟"
            },
            {
                "Close downloader",
                "إغلاق التطبيق"
            },
            {
                "The update could not be installed. The previous app will reopen.",
                "تعذّر تثبيت التحديث. سيُفتح الإصدار السابق مجددًا."
            },
            {
                "Settings could not be saved",
                "تعذّر حفظ الإعدادات"
            },
            {
                "Paste a YouTube video link first.",
                "الصق رابط فيديو من يوتيوب أولًا."
            },
            {
                "Paste one complete YouTube video link.",
                "الصق رابط فيديو كاملًا من يوتيوب."
            },
            {
                "Enter a valid YouTube video link.",
                "أدخل رابط فيديو صالحًا من يوتيوب."
            },
            {
                "Use a youtube.com or youtu.be video link.",
                "استخدم رابط فيديو من youtube.com أو youtu.be."
            },
            {
                "Use a link to one video or Short. Playlist-only links are not supported.",
                "استخدم رابط فيديو واحد أو مقطع قصير. روابط قوائم التشغيل وحدها غير مدعومة."
            },
            {
                "Choose a folder on this computer.",
                "اختر مجلدًا على هذا الجهاز."
            },
            {
                "This site is limiting downloads right now. Wait a few minutes, then try again.",
                "يوتيوب يحدّ التنزيلات مؤقتًا. انتظر بضع دقائق ثم أعد المحاولة."
            },
            {
                "This video requires sign-in. Try a publicly available video.",
                "هذا الفيديو يتطلب تسجيل الدخول. جرّب فيديو متاحًا للجميع."
            },
            {
                "This site refused this download. Wait a little, then paste the link again.",
                "يوتيوب رفض التنزيل. انتظر قليلًا، ثم الصق الرابط مجددًا."
            },
            {
                "This video is unavailable. Try another video link.",
                "هذا الفيديو غير متاح. جرّب رابط فيديو آخر."
            },
            {
                "That quality is no longer available. Paste the link again to refresh the options.",
                "هذه الجودة لم تعد متاحة. الصق الرابط مجددًا لتحديث الخيارات."
            },
            {
                "The drive is full. Free some space or choose another save folder.",
                "القرص ممتلئ. وفر مساحة أو اختر مجلد حفظ آخر."
            },
            {
                "We can't save in that folder. Choose another folder with Browse.",
                "لا يمكن الحفظ في هذا المجلد. اختر مجلدًا آخر باستخدام تصفح."
            },
            {
                "Couldn't reach YouTube. Check your internet connection, then try again.",
                "تعذّر الاتصال بيوتيوب. تحقق من اتصال الإنترنت ثم أعد المحاولة."
            },
            {
                "Run SetupTools.cmd in the app folder to install the missing tools, then reopen the app.",
                "شغّل SetupTools.cmd من مجلد التطبيق لتثبيت الأدوات المطلوبة، ثم افتح التطبيق مجددًا."
            },
            {
                "We couldn't finish this action. Check the link and save folder, then try again.",
                "تعذّر إكمال العملية. تحقق من الرابط ومجلد الحفظ، ثم أعد المحاولة."
            },
            {
                "Checking video",
                "جارٍ فحص الفيديو"
            },
            {
                "Getting the title and available formats…",
                "جارٍ جلب العنوان والجودات المتاحة…"
            },
            {
                "Connecting",
                "جارٍ الاتصال"
            },
            {
                "Preparing your download…",
                "جارٍ تجهيز التنزيل…"
            },
            {
                "Downloading",
                "جارٍ التنزيل"
            },
            {
                "Finishing download",
                "جارٍ إنهاء التنزيل"
            },
            {
                "Preparing the final file…",
                "جارٍ تجهيز الملف النهائي…"
            },
            {
                "Making MP3",
                "جارٍ تجهيز MP3"
            },
            {
                "Preparing MP4",
                "جارٍ تجهيز MP4"
            },
            {
                "Finishing your file. This can take a moment…",
                "جارٍ إنهاء الملف. قد يستغرق ذلك لحظات…"
            }
        };
        public static string For(string language, string text)
        {
            if (language != "ar" || String.IsNullOrEmpty(text))
                return text;
            string value;
            if (Arabic.TryGetValue(text, out value))
                return value;
            if (text.StartsWith("Downloading ", StringComparison.Ordinal) && text.EndsWith("…", StringComparison.Ordinal))
                return "جارٍ تنزيل " + text.Substring(12);
            if (text.StartsWith("Install v", StringComparison.Ordinal))
                return "تثبيت الإصدار " + text.Substring(9);
            return text;
        }

        public static string Format(string language, string text, params object[] values)
        {
            return values == null || values.Length == 0 ? For(language, text) : String.Format(CultureInfo.InvariantCulture, For(language, text), values);
        }

        public static string Progress(string language, string text)
        {
            string value = For(language, text);
            return language == "ar" ? value.Replace(" of ", " من ").Replace(" remaining", " متبقية").Replace("/s", "/ث") : value;
        }
    }
}
