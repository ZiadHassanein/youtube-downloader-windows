const { api, name, sendLink } = globalThis.ViFreeConnection;
// The popup follows the browser's interface language; the in-page overlay follows the page language.
const arabic = /^ar\b/iu.test(api.i18n?.getUILanguage?.() ?? globalThis.navigator?.language ?? "");
const text = arabic ? {
  intro: `افتح الفيديو الذي تشاهده في ${name}، ثم اختر ما تريد تنزيله.`, send: `فتح في ${name}`,
  finding: "جارٍ تحديد الفيديو…", opening: "جارٍ الفتح…", opened: `تم الفتح في ${name}. راجع الرابط قبل التنزيل.`,
  busy: `${name} مشغول. أنهِ التنزيل الحالي أو ألغِه، ثم حاول مجددًا.`,
  unavailable: `افتح ${name} وفعّل الربط بالمتصفح، ثم حاول مجددًا.`,
  missing: "تعذّر العثور على رابط هذا الفيديو. افتح الفيديو نفسه أو استخدم مشاركة ثم نسخ الرابط.",
  ambiguous: "يظهر أكثر من فيديو. افتح الفيديو المطلوب ثم حاول مجددًا.",
  inspect: "تعذّر فحص هذه الصفحة. أعد تحميل الإضافة والصفحة ثم حاول مجددًا."
} : {
  intro: `Open the video you’re watching in ${name}, then choose what to download.`, send: `Open in ${name}`,
  finding: "Finding video…", opening: "Opening…", opened: `Opened in ${name}. Review the link before downloading.`,
  busy: `${name} is busy. Finish or cancel the current download, then try again.`,
  unavailable: `Open ${name} and enable Browser integration, then try again.`
};
const button = document.querySelector("#send");
const status = document.querySelector("#status");
if (arabic) {
  document.documentElement.lang = "ar";
  document.documentElement.dir = "rtl";
  const intro = document.querySelector("#intro");
  if (intro) intro.textContent = text.intro;
  button.textContent = text.send;
}
button.addEventListener("click", async () => {
  button.disabled = true;
  status.textContent = text.finding;
  try {
    const [tab] = await api.tabs.query({ active: true, currentWindow: true });
    const url = await globalThis.ViFreeVideoDetection.resolve(tab);
    status.textContent = text.opening;
    await sendLink(url);
    status.textContent = text.opened;
  } catch (error) {
    status.textContent = text[error.code] ?? error.message ?? text.unavailable;
  } finally { button.disabled = false; }
});
