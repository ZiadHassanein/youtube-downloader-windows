# ViFree browser companion

Optional Chrome/Edge and Firefox companions are included in the app ZIP under `Browser`. Normal app downloads need no extension. On Facebook and TikTok, clicking the companion finds the visible playing video's permalink. Other sites retain current-page forwarding; right-clicking a link sends that explicit link. You review the result, choose quality and click Download in ViFree.

1. Extract the app ZIP into a permanent folder and open `ViFree.exe`.
2. Choose **Browser integration → Enable connection** in the app. This registers the connection for your Windows account only.
3. Chrome/Edge: open the extensions page, enable Developer mode, choose **Load unpacked**, and select `Browser/chromium`. Firefox: open `about:debugging#/runtime/this-firefox`, choose **Load Temporary Add-on**, and select `Browser/firefox/manifest.json`.
4. Click the extension → **Open in ViFree**, right-click a link → **Open link in ViFree**, or right-click a Facebook/TikTok video → **Open video in ViFree**. Finish/cancel any active download before sending another link.

For a companion-only update, extract it into a permanent folder. Chrome/Edge: open the extensions page and reload the existing unpacked companion if its folder was updated in place. If you extracted elsewhere, remove the old companion and load the new `chromium` folder. Keep that folder after installation. The preview companion connects to Preview 27 without replacing the accepted app. If the app moved, open its Settings and enable Browser integration again.

Use **Disable connection** and remove the extension to disconnect. Firefox temporary add-ons disappear when Firefox closes. These local-loading packages are not published to browser stores.

Permissions: `activeTab`, `scripting`, `contextMenus`, `nativeMessaging`. Inspection runs only after your click, in the active Facebook/TikTok tab's isolated world. There are no persistent content scripts, website-wide host grants, cookie access, account imports, analytics or automatic downloads. It reads player visibility and nearby public video identifiers/links, not login fields or private app state. Blob playback addresses are never sent as download links.

Hidden/preloaded players are excluded; TikTok's visible canvas-backed player is supported. More than one equally prominent player or a missing permalink produces guidance instead of sending the homepage or a neighboring video's link. Open the specific video or use **Share → Copy link** when detection cannot determine it. The page may need reloading after an extension update.

The app validates messages and limits the pipe to the current Windows user. Same-user programs are not an exclusive trust boundary. Detection does not bypass a site's access restrictions; protected/sign-in media remains unsupported. A separately built preview companion connects only to the preview app.
