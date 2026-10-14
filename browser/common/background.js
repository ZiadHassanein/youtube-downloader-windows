const api = globalThis.browser ?? chrome;
const nativeHost = "com.vifree.preview";

function publicLink(value) {
  if (typeof value !== "string" || value.length > 8192 || /[\u0000-\u001f\u007f]/u.test(value)) throw new Error("Choose a public video link.");
  const url = new URL(value);
  if (!["https:", "http:"].includes(url.protocol) || url.username || url.password || url.port) throw new Error("Choose a public video link.");
  return url.href;
}

async function sendLink(value) {
  const url = publicLink(value);
  const response = await api.runtime.sendNativeMessage(nativeHost, { version: 1, type: "open-link", url });
  if (!response?.ok) throw new Error(response?.message ?? "Open ViFree Preview and enable browser integration first.");
  return response;
}

api.runtime.onInstalled.addListener(async () => {
  await api.contextMenus.removeAll();
  api.contextMenus.create({ id: "vifree-send-link", title: "Open link in ViFree Preview", contexts: ["link"], targetUrlPatterns: ["https://*/*", "http://*/*"] });
  api.contextMenus.create({ id: "vifree-send-page", title: "Open page in ViFree Preview", contexts: ["page"] });
});
api.contextMenus.onClicked.addListener(async (info, tab) => {
  try {
    await sendLink(info.linkUrl ?? tab?.url);
    await api.action.setBadgeText({ text: "OK", tabId: tab?.id });
    await api.action.setBadgeBackgroundColor({ color: "#275BD8", tabId: tab?.id });
  } catch {
    await api.action.setBadgeText({ text: "!", tabId: tab?.id });
    await api.action.setBadgeBackgroundColor({ color: "#AF2527", tabId: tab?.id });
    await api.action.setTitle({ title: "Open ViFree Preview, enable Browser integration, then try again.", tabId: tab?.id });
  }
});
