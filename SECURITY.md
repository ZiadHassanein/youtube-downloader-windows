# Security

Report a suspected vulnerability privately through [GitHub security reporting](https://github.com/ZiadHassanein/ViFree/security/advisories/new). If private reporting is unavailable, contact the email listed in the app; do not put credentials, private media URLs, or exploitable details in a public issue.

Include the ViFree version, Windows version, affected feature, and concise reproduction steps. Redact personal paths and tokens. Support focuses on the latest stable release and the active preview.

Media URLs and metadata are untrusted. ViFree uses bounded input handling, explicit subprocess arguments, verified tool downloads, private application state, and output validation. Browser integration is optional and requires a user action to send a link. It does not import browser cookies or collect telemetry.

Supported sites depend on upstream extractors. Public-link validation is not a full network sandbox, and this application does not bypass DRM or authentication. Download media only when you have permission.
