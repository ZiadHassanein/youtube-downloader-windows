# Contributing to Vydrop

Open an issue before a substantial change. Use a feature branch and a focused pull request describing the behavior, validation, and any remaining limitations.

On Windows with the .NET 10 SDK and Node.js 20+, run `pwsh -File scripts/check.ps1`. The checks exercise Core, process/storage boundaries, browser messages, and native WPF behavior with local fixtures. Live-site checks require an internet connection and media you have permission to download.

Keep download policy and orchestration in Core, subprocesses/storage/tool acquisition in Infrastructure, and presentation in Desktop. Browser integrations send links for review; they must not read cookies, credentials, or start hidden downloads.

Preserve English/Arabic, both themes, keyboard access, actual quality detection, and automatic media-tool preparation. Avoid personal paths, test media, secrets, generated build files, or private diagnostic logs in commits.

Vydrop's source is MIT licensed. Dependencies retain their own licenses; keep their notices and required distribution terms intact. Changes on preview branches need user testing before merging or stable publication.

## Release compatibility

Keep the repository slug `ViFree`; installed updaters pin it and refuse API redirects. Preserve native-host names, Chromium public key and Firefox add-on IDs. Brand the repository through documentation and metadata instead.

Run `./scripts/check.ps1` and `node --test tests/browser.test.cjs tests/browser-video.test.cjs tests/docs.test.cjs`. For 100% display compatibility also run the WPF test executable with `--dpi-unaware`; the default run follows the current display scale. Do not change the desktop's scale for tests.

The documentation site is a GitHub Pages-compatible Jekyll project in `docs/`. Run `bundle install --gemfile docs/Gemfile`, then `bundle exec --gemfile docs/Gemfile jekyll build --source docs --destination build/site`. Keep `docs/_data/product.yml`, facts blocks, `llms.txt`, README and release facts synchronized with `Directory.Build.props`. The documentation tests detect version drift and local link/metadata errors.

Publishing Pages, releases, signing keys and external directory submissions require an owner decision. See [SEO maintenance](docs/seo-maintenance.md). Do not merge documentation illustrations with real screenshots or label fixture renders as live-site evidence.
