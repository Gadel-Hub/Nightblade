# GitHub Pages

`.github/workflows/deploy-pages.yml` builds on pushes to `main` and manual
workflow dispatch on `main`. It uses Ubuntu, Node `lts/*`, npm's lockfile cache,
`npm ci`, and `npm run build` (including asset validation and TypeScript checks).
Only `dist/` is uploaded with the official Pages artifact action and deployed
through the `github-pages` environment. The deployment step exposes the site URL.
The `pages` concurrency group lets an active deployment finish before the next.
No custom token or deployment branch is required.

The owner must select **Settings → Pages → Build and deployment → Source →
GitHub Actions** and allow the official actions if repository/organization policy
restricts them. Any `github-pages` environment protection must allow `main`.
After these files reach `main`, a push or manual run deploys the site. No remote
or GitHub settings were changed as part of the local setup.

## Paths

`vite.config.mjs` opts into Pages paths only when building with
`GITHUB_PAGES=true`. It reads GitHub's `GITHUB_REPOSITORY=owner/repository`:

- Project repository: `/repository/`, preserving repository-name case.
- Repository ending in `.github.io` (case-insensitive): `/`.
- Normal builds and local development: `/`.

Phaser's two asset-loading scenes use `import.meta.env.BASE_URL` as their loader
base. The six PNG URLs remain otherwise unchanged. Vite rewrites JS/CSS URLs.
For a project site, use `/repository/#art` and `/repository/#level1`; the default
route opens the mechanics laboratory. Hashes do not require static-host rewrites.
Custom domains are outside this automatic repository-path configuration.

## Local verification

```sh
npm ci
npm run test:assets
npm run build
npm run test:pages
GITHUB_PAGES=true GITHUB_REPOSITORY=example/pages-smoke npm run build
npm run test:pages -- /pages-smoke/
GITHUB_PAGES=true GITHUB_REPOSITORY=example/example.github.io npm run build
npm run test:pages
```

`test:pages` uses the existing Playwright dependency (install its browser with
`npx playwright install chromium` if needed). It serves the current `dist/` at a
strict local mount, with no development-server fallback. It checks built HTML
paths, JS/CSS/PNG responses, scene startup, player movement and F1 on the default,
art and level routes. Test-only response instrumentation exposes the existing
production game instance; no test global is emitted into the actual build.
This verifies local deployment assumptions separately from the hosted Actions
deployment.

A Pages build prints `GitHub Pages base: /repository/` (or `/` for a root site).
Use that line and the generated HTML to identify the build configuration:
ordinary local output uses `/assets/...`; project Pages output must use
`/repository/assets/...`. Filenames and artifact directory listings alone do
not establish which base was compiled. The existing workflow supplies the
Pages flag; a root-based local `dist/` is not evidence that its Actions build
used the same configuration.
