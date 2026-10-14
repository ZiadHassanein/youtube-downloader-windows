# ViFree browser companion

Optional Chrome/Edge and Firefox companions are included in the app ZIP under `Browser`. Normal app downloads need no extension. The companion sends the current page or a clicked link to ViFree for review; you choose the quality and click Download in the app.

1. Extract the app ZIP into a permanent folder and open `ViFree.exe`.
2. Choose **Browser integration → Enable connection** in the app. This registers the connection for your Windows account only.
3. Chrome/Edge: open the extensions page, enable Developer mode, choose **Load unpacked**, and select `Browser/chromium`. Firefox: open `about:debugging#/runtime/this-firefox`, choose **Load Temporary Add-on**, and select `Browser/firefox/manifest.json`.
4. Click the extension → **Open in ViFree**, or right-click a link → **Open link in ViFree**. Finish/cancel any active download before sending another link.

Use **Disable connection** and remove the extension to disconnect. If you move the EXE, enable the connection again from its new folder. Firefox temporary add-ons disappear when Firefox closes. These local-loading packages are not published to browser stores. When updating the companion, extract the new app ZIP and reload its Browser folder.

Permissions: `activeTab`, `contextMenus`, `nativeMessaging`. No cookie access, analytics, content scripts or automatic downloads. The app validates messages and limits the pipe to the current Windows user. Same-user programs are not an exclusive trust boundary. Protected/sign-in media is unsupported. A separately built preview companion connects only to the preview app.
