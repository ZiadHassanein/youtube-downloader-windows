const api = globalThis.browser ?? chrome;
const button = document.querySelector("#send");
const status = document.querySelector("#status");
button.addEventListener("click", async () => {
  button.disabled = true;
  status.textContent = "Opening…";
  try {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const url = new URL(tab?.url ?? "");
    if (!["http:", "https:"].includes(url.protocol) || url.username || url.password || url.href.length > 8192) throw new Error("Choose a public video page first.");
    const response = await api.runtime.sendNativeMessage("com.vifree.preview", { version: 1, type: "open-link", url: url.href });
    if (!response?.ok) throw new Error(response?.message ?? "Could not open ViFree.");
    status.textContent = "Opened in ViFree. Review the link before downloading.";
  } catch {
    status.textContent = "Open ViFree Preview and enable Browser integration, then try again.";
  } finally { button.disabled = false; }
});
