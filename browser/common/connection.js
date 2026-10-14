globalThis.ViFreeConnection = (() => {
  const api = globalThis.browser ?? chrome;
  const preview = api.runtime.getManifest().name === "ViFree Preview";
  const name = preview ? "ViFree Preview" : "ViFree";
  const host = preview ? "com.vifree.preview" : "com.vifree";
  async function sendLink(value) {
    if (typeof value !== "string" || value.length > 8192 || /[\u0000-\u001f\u007f]/u.test(value)) throw new Error("Choose a public video link.");
    const url = new URL(value);
    if (!["https:", "http:"].includes(url.protocol) || url.username || url.password || url.port) throw new Error("Choose a public video link.");
    const response = await api.runtime.sendNativeMessage(host, { version: 1, type: "open-link", url: url.href });
    if (!response?.ok) throw new Error(response?.message ?? `Open ${name} and enable browser integration first.`);
    return response;
  }
  return { api, name, sendLink };
})();
