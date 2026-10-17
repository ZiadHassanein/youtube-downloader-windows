# Security

Report a suspected vulnerability privately through [GitHub security reporting](https://github.com/ZiadHassanein/ViFree/security/advisories/new). If private reporting is unavailable, contact the email listed in the app; do not put credentials, private media URLs, or exploitable details in a public issue.

Include the Vydrop version, Windows version, affected feature, and concise reproduction steps. Redact personal paths and tokens. Support focuses on the latest stable release and the active preview.

Media URLs and metadata are untrusted. Vydrop uses bounded input handling, explicit subprocess arguments, verified tool downloads, private application state, and output validation. Browser integration is optional and requires a user action to send a link. It does not import browser cookies or collect telemetry.

Supported sites depend on upstream extractors. Public-link validation is not a full network sandbox, and this application does not bypass DRM or authentication. Download media only when you have permission.

## Update trust boundary

Release hashes, GitHub asset digests and official tool checksums detect corruption and substitutions relative to the expected publisher. They are fetched from the same publisher accounts as the binaries, so they do not protect against a compromised publisher account or release workflow. Current desktop packages are not Authenticode-signed and there is no independent embedded signing key. New app releases require the user's install action; tool checks can happen automatically.

Compatible tools may be reused from the app folder, PATH or WinGet Links after executable probes. A version probe executes that program with your Windows user's privileges. Keep those directories trusted; the probe is a compatibility check, not a malware scanner. Windows job objects contain subprocess lifetime and inherited handles, not filesystem or network access. Public-link filtering is not a complete network sandbox.

An independent signing scheme and opt-in PATH reuse are proposed changes, not shipped protections. Report suspicious publisher assets privately. No diagnostics should contain cookies, credentials, private media URLs or personal paths.

## حدود الثقة في التحديثات

تكشف البصمات والبيانات المنشورة تلف الملفات أو تبديلها مقارنة بالناشر المتوقع، لكنها تأتي من حساب الناشر نفسه ولا تحمي من اختراق حسابه أو عملية الإصدار. لا تتضمن الحزمة الحالية توقيع Authenticode أو مفتاح توقيع مستقلًا داخل التطبيق. تحديث التطبيق يتطلب اختيار التثبيت، بينما قد تُفحص تحديثات الأدوات تلقائيًا.

قد يُعاد استخدام أدوات متوافقة من مجلد التطبيق أو PATH أو روابط WinGet بعد تشغيل فحص الإصدار بصلاحيات مستخدم ويندوز. حافظ على موثوقية هذه المجلدات؛ فحص التوافق ليس فحصًا للبرمجيات الخبيثة. احتواء العمليات يضبط عمرها والمقابض الموروثة، ولا يعزل وصولها إلى الملفات أو الشبكة. التوقيع المستقل وجعل استخدام PATH اختياريًا مقترحان ولم يُنفّذا بعد.
