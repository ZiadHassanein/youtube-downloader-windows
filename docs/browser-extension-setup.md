---
title: "Set up Vydrop in Chrome, Edge and Firefox"
description: "Install the free Vydrop companion in Chrome, Edge or Firefox. Illustrated setup, website permissions, reload steps and native connection troubleshooting."
lang: en
dir: ltr
alternate: true
english: /browser-extension-setup.html
arabic: /ar/browser-extension-setup.html
---

# Set up the Vydrop browser extension

Use the optional companion to send the video you are watching to the desktop app, then review it before downloading. Setup is free. You can also paste a link directly into the app.

**Use the matching package:** [Vydrop 4.2.1 for Windows](https://github.com/ZiadHassanein/Vydrop/releases/download/v4.2.1/Vydrop-v4.2.1-win-x64.zip) includes companion **Vydrop 1.3.1**. Open **Vydrop.exe** and use its included **Browser** folder. When upgrading from ViFree, enable the connection from Vydrop and reload the companion from the new folder.

[Published downloads](https://github.com/ZiadHassanein/Vydrop/releases/latest) · [Chrome](#chrome) · [Edge](#edge) · [Firefox](#firefox) · [Troubleshooting](#troubleshooting) · [العربية](#arabic)

The images are cursor-free **setup illustrations**, not browser screenshots; control positions can vary by browser version.

## 1. Extract and keep the app folder

Extract the app ZIP into a permanent writable folder, such as **Documents\Vydrop**. Open **Vydrop.exe** from that folder and keep **Browser** beside it. Keep the extracted files after setup.

![Folder illustration: the executable stays beside Browser; Chromium browsers select Browser/chromium and Firefox selects Browser/firefox/manifest.json](images/extension/01-folders.svg)

## 2. Enable the desktop connection

In the app, choose **Settings → Browser integration → Enable connection**. Wait for **Browser connection enabled**, then close the connection dialog. This registers the app location for your Windows account. Browser installation is the next step.

![Connection illustration: Settings, Browser integration and Enable connection](images/extension/02-enable-connection.svg)

<a id="chrome-and-edge"></a>

## Chrome

1. Enter `chrome://extensions` in Chrome's address bar.
2. Switch **Developer mode** on.
3. Choose **Load unpacked**, then select **Browser\chromium** inside the extracted app folder.
4. Check that the Vydrop/ViFree card is enabled. In **Details → Site access**, allow your desired websites; **On all sites** enables detection across supported sites.
5. Refresh any already-open video page. Optionally pin the companion from Chrome's Extensions menu.

![Chrome setup illustration showing Developer mode, Load unpacked, Browser/chromium and website access](images/extension/03-chromium-setup.svg)

Choose the folder containing `manifest.json`, rather than selecting the ZIP or outer Browser folder. These loading steps follow the [official Chrome instructions](https://developer.chrome.com/docs/extensions/get-started/tutorial/hello-world#load-unpacked).

## Edge

1. Enter `edge://extensions` in Microsoft Edge's address bar.
2. Turn on **Developer mode** in the extensions page.
3. Choose **Load unpacked** and select **Browser\chromium**. Edge uses the same folder as Chrome.
4. Enable the companion. Open its **Details** and allow the websites where you want detection to run.
5. Refresh the video page. Use Edge's Extensions menu to show the companion on the toolbar if desired.

![Edge setup illustration showing its Developer mode control, Load unpacked and the shared chromium folder](images/extension/04-edge-setup.svg)

See Microsoft's [official Edge sideloading instructions](https://learn.microsoft.com/en-us/microsoft-edge/extensions/getting-started/extension-sideloading). Browser wording and control positions may differ.

## Firefox

1. Enter `about:debugging#/runtime/this-firefox` in Firefox's address bar.
2. Select **This Firefox** if it is not already selected.
3. Choose **Load Temporary Add-on…** and open **Browser\firefox\manifest.json** from the extracted app folder.
4. Refresh the page and play the video.

![Firefox illustration showing This Firefox, Load Temporary Add-on and Browser/firefox/manifest.json, with the restart limitation](images/extension/05-firefox-setup.svg)

**This is a temporary installation:** Firefox removes the locally loaded companion when it restarts. Load it again next session. A permanent normal Firefox installation requires a Mozilla-signed add-on; this ZIP is not a signed store package. See Mozilla's [temporary installation documentation](https://extensionworkshop.com/documentation/develop/temporary-installation-in-firefox/).

## 5. Send the playing video

Play a video, then click **Download by Vydrop** just outside its top-right border. The button can use clear space immediately to the right when the upper position is blocked. If neither placement is available, use the companion's toolbar popup.

![Playback illustration with the V-only icon and Download by Vydrop above the video's right border](images/extension/04-send-video.svg)

The click copies the detected link and opens it in the app. It does not start saving automatically.

## 6. Review the video in Vydrop

Check the link and media details. Choose **Video** or **Audio**, an available quality and a save folder, then click **Download**. Public-link detection does not guarantee that every website or video can be downloaded.

## Updating or removing the companion

When you update files in the same folder, use **Reload** on Chrome/Edge's extensions page or Firefox's debugging page, then refresh video tabs. If you move the folder, load the new location. If you move the desktop app, enable its connection again.

To disconnect, choose **Disable connection** in the app and remove the browser companion. An extension named **Vydrop Preview** or **ViFree Preview** connects to its preview app; use the regular companion for the regular app.

## Troubleshooting

| What happens | What to try |
| --- | --- |
| The app does not open | Open it manually, then use Settings → Browser integration → Enable connection again. Keep its folder in place. |
| No tab appears beside the video | Check the companion is enabled and allowed on that site, refresh the page and play the visible video. Try its toolbar popup outside native fullscreen or picture-in-picture. |
| Several videos are present, or the wrong link appears | Open the specific video's public page, or paste the site's Share → Copy link into the app. |
| The right video opens, but downloading fails | DRM, private/sign-in-only media, custom players and site restrictions can prevent downloading. Report the public link and app version in [GitHub issues](https://github.com/ZiadHassanein/Vydrop/issues). |
| Firefox loses the companion after restarting | Expected for a temporary add-on; load Browser\firefox\manifest.json again. |
| The app opens Preview | Load the regular companion from the matching desktop package and enable the connection from that app. |
| The icon or name looks old after an update | Reload the companion, then refresh the video page. The current Vydrop icon is the red V alone. |

The companion locates visible players and sends a public link after your click. It does not import accounts or cookies. Read the [permissions and limitations](https://github.com/ZiadHassanein/Vydrop/blob/main/browser/README.md). These local packages are not Chrome, Edge or Firefox store listings.

<a id="arabic"></a>

## تفعيل الإضافة بالعربية

<div dir="rtl" lang="ar" markdown="1">

الإضافة اختيارية ومجانية. الصور أعلاه رسومات توضيحية دون مؤشر ماوس. يتضمن إصدار **Vydrop 4.2.1** إضافة **Vydrop 1.3.1**. فكّ ضغط حزمة البرنامج وافتح **Vydrop.exe**. عند الترقية من ViFree، فعّل الاتصال من Vydrop وأعد تحميل الإضافة من مجلد **Browser** الجديد.

1. **فك الضغط:** احتفظ بمجلد البرنامج في مكان دائم، وبمجلد **Browser** بجوار الملف التنفيذي.
2. **فعّل الاتصال:** اختر **الإعدادات ← الربط بالمتصفح ← تفعيل الاتصال** وانتظر رسالة التأكيد.
3. **Chrome:** افتح `chrome://extensions`، فعّل **Developer mode**، ثم اختر **Load unpacked** وحدد **Browser\chromium**. [صورة Chrome](images/extension/03-chromium-setup.svg).
4. **Edge:** افتح `edge://extensions` وفعّل **Developer mode**، ثم اختر **Load unpacked** وحدد **Browser\chromium** نفسه. [صورة Edge](images/extension/04-edge-setup.svg).
5. **Firefox:** افتح `about:debugging#/runtime/this-firefox`، واختر **Load Temporary Add-on**، ثم ملف **Browser\firefox\manifest.json**. [صورة Firefox](images/extension/05-firefox-setup.svg). هذه إضافة مؤقتة وتحتاج إلى تحميلها مجددًا بعد إعادة تشغيل Firefox؛ الحزمة الحالية ليست إضافة موقعة للتثبيت الدائم.
6. **السماح للمواقع:** فعّل الإضافة واسمح لها بالعمل على المواقع المطلوبة، ثم حدّث صفحات الفيديو المفتوحة.
7. **شغّل ثم راجع:** اضغط **تنزيل بواسطة Vydrop** خارج إطار الفيديو، أو استخدم زر الإضافة من شريط المتصفح. راجع الرابط والصيغة والجودة ومجلد الحفظ داخل البرنامج، ثم اضغط **تنزيل**.

إذا نقلت مجلد البرنامج، فعّل الاتصال مجددًا. عند تحديث الإضافة، أعد تحميلها وحدّث صفحة الفيديو. تختلف إمكانية التنزيل حسب الموقع وإتاحة الفيديو؛ لا تستورد الإضافة حساباتك أو ملفات تعريف الارتباط.


</div>
