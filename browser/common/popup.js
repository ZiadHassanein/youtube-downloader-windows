const { api, name, sendLink } = globalThis.ViFreeConnection;
const button = document.querySelector("#send");
const status = document.querySelector("#status");
button.addEventListener("click", async () => {
  button.disabled = true;
  status.textContent = "Opening…";
  try {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    await sendLink(tab?.url ?? "");
    status.textContent = "Opened in ViFree. Review the link before downloading.";
  } catch {
    status.textContent = `Open ${name} and enable Browser integration, then try again.`;
  } finally { button.disabled = false; }
});
