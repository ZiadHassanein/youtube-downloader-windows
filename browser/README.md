# ViFree Preview browser companion

Optional local-test extension for Windows. Ordinary app downloads need no extension or tool setup. This companion sends a page or clicked link to ViFree for review; it never starts a download or reads browser cookies.

1. Extract the app preview into a permanent folder and open `ViFree.exe`.
2. In the app, choose **Browser integration → Enable connection**. This registers the native host for your Windows account only. No administrator privileges are needed.
3. Extract the companion ZIP. Chrome/Edge: enable Developer mode on the extensions page and choose **Load unpacked**, selecting `chromium`. Firefox: open `about:debugging#/runtime/this-firefox`, choose **Load Temporary Add-on**, and select `firefox/manifest.json`.
4. Open a public video page, click the extension, and choose **Open in ViFree**. Or right-click a link and choose **Open link in ViFree Preview**.
5. Review the link and choose Download in the app. If ViFree is busy, finish/cancel the current operation and send the link again.

Use **Disable connection** to remove ViFree's registered connection and remove the extension in your browser. If you move the EXE, enable the connection again from its new location. Firefox temporary add-ons disappear when Firefox closes. These development packages are not published to extension stores.

Permissions: `activeTab`, `contextMenus`, `nativeMessaging`. No persistent website permission, content script, cookie access or analytics. Native message size is capped, URLs validated again in the app, and the pipe is limited to the current Windows user. Same-user programs are not an exclusive trust boundary. Supported sites depend on yt-dlp; DRM/sign-in media is not supported.
