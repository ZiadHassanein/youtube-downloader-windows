const { api, name, sendLink } = globalThis.ViFreeConnection;
const button = document.querySelector("#send");
const status = document.querySelector("#status");
button.addEventListener("click", async () => {
  button.disabled = true;
  status.textContent = "Finding video…";
  try {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const url = await globalThis.ViFreeVideoDetection.resolve(tab);
    status.textContent = "Opening…";
    await sendLink(url);
    status.textContent = "Opened in ViFree. Review the link before downloading.";
  } catch (error) {
    status.textContent = error.message || `Open ${name} and enable Browser integration, then try again.`;
  } finally { button.disabled = false; }
});
