globalThis.ViFreeConnection = (() => {
  const api = globalThis.browser ?? chrome;
  const preview = api.runtime.getManifest().name.endsWith(" Preview");
  const name = preview ? "Vydrop Preview" : "Vydrop";
  const host = preview ? "com.vifree.preview" : "com.vifree";
  // Errors carry a stable code so each surface can show its own language; the message stays English for logs.
  function fail(message, code) { const error = new Error(message); if (code) error.code = code; return error; }
  async function sendLink(value) {
    if (typeof value !== "string" || value.length > 8192 || /[\u0000-\u001f\u007f]/u.test(value)) throw new Error("Choose a public video link.");
    let url;
    try { url = new URL(value); } catch { throw new Error("Choose a public video link."); }
    if (!["https:", "http:"].includes(url.protocol) || url.username || url.password || url.port) throw new Error("Choose a public video link.");
    let response;
    try { response = await api.runtime.sendNativeMessage(host, { version: 1, type: "open-link", url: url.href }); }
    catch { throw fail(`Open ${name} and enable Browser integration, then try again.`, "unavailable"); }
    if (!response?.ok) throw fail(response?.message ?? `Open ${name} and enable browser integration first.`, response?.code === "busy" ? "busy" : undefined);
    return response;
  }
  return { api, name, sendLink };
})();
