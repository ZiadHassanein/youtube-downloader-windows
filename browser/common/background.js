if (!globalThis.ViFreeConnection) importScripts("connection.js", "video-detection.js");
const { api, name, sendLink } = globalThis.ViFreeConnection;

api.runtime.onMessage.addListener((message, sender, respond) => {
  if (message?.type !== "vifree-open-video" || message.version !== 1) return false;
  let allowed = false;
  try {
    const page = new URL(sender.url);
    allowed = sender.id === api.runtime.id && Number.isInteger(sender.tab?.id) &&
      Number.isInteger(sender.frameId) && sender.frameId >= 0 && /^https?:$/u.test(page.protocol) && !page.username && !page.password;
  } catch { /* Only this extension's web content scripts can forward an explicit video click. */ }
  if (!allowed) { respond({ ok: false, message: "This video page cannot open ViFree." }); return false; }
  sendLink(message.url).then(() => respond({ ok: true }), error => respond({ ok: false, message: error.message }));
  return true;
});

api.runtime.onInstalled.addListener(async () => {
  await api.contextMenus.removeAll();
  api.contextMenus.create({ id: "vifree-send-link", title: `Open link in ${name}`, contexts: ["link"], targetUrlPatterns: ["https://*/*", "http://*/*"] });
  api.contextMenus.create({ id: "vifree-send-page", title: `Open video in ${name}`, contexts: ["page", "video"] });
});
api.contextMenus.onClicked.addListener(async (info, tab) => {
  try {
    await sendLink(await globalThis.ViFreeVideoDetection.resolve(tab, info));
    await api.action.setBadgeText({ text: "OK", tabId: tab?.id });
    await api.action.setBadgeBackgroundColor({ color: "#275BD8", tabId: tab?.id });
    await api.action.setTitle({ title: `Open video in ${name}`, tabId: tab?.id });
  } catch (error) {
    await api.action.setBadgeText({ text: "!", tabId: tab?.id });
    await api.action.setBadgeBackgroundColor({ color: "#AF2527", tabId: tab?.id });
    await api.action.setTitle({ title: error.message || `Open ${name}, enable Browser integration, then try again.`, tabId: tab?.id });
  }
});
