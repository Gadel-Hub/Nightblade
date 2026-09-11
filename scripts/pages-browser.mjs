// Test the production directory with a strict static mount, without SPA fallbacks.
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
import assert from 'node:assert/strict';
import { chromium } from 'playwright';
import config from '../vite.config.mjs';

const base = process.argv[2] || '/';
assert.match(base, /^\/(?:[\w.-]+\/)*$/);
const directory = resolve('dist');
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png' };
const server = createServer(async (request, response) => {
  const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
  const file = resolve(directory, pathname.slice(base.length) || 'index.html');
  if (!pathname.startsWith(base) || !file.startsWith(directory + sep)) {
    response.writeHead(404).end(); return;
  }
  try {
    response.setHeader('Content-Type', types[extname(file)] || 'application/octet-stream');
    response.end(await readFile(file));
  } catch { response.writeHead(404).end(); }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
let browser;
try {
  // Check project, root Pages and local-development base selection.
  process.env.GITHUB_PAGES = 'true';
  process.env.GITHUB_REPOSITORY = 'example/pages-smoke';
  assert.equal(config({ command: 'build' }).base, '/pages-smoke/');
  process.env.GITHUB_REPOSITORY = 'example/different-name.GITHUB.IO';
  assert.equal(config({ command: 'build' }).base, '/');
  assert.equal(config({ command: 'serve' }).base, '/');
  delete process.env.GITHUB_PAGES;
  assert.equal(config({ command: 'build' }).base, '/');

  const html = await readFile(resolve(directory, 'index.html'), 'utf8');
  assert.ok(html.includes(`src="${base}assets/`), 'script URL respects base');
  assert.ok(html.includes(`href="${base}assets/`), 'stylesheet URL respects base');
  browser = await chromium.launch();
  const origin = `http://127.0.0.1:${server.address().port}`;
  for (const [hash, scene] of [['', 'art'], ['#art', 'art'], ['#dev', 'development'], ['#level1', 'level1']]) {
    const page = await browser.newPage();
    const failures = [], loaded = new Set();
    page.on('pageerror', error => failures.push(error.message));
    page.on('requestfailed', request => failures.push(request.url()));
    page.on('response', response => {
      if (response.status() >= 400) failures.push(`${response.status()} ${response.url()}`);
      if (response.ok()) loaded.add(new URL(response.url()).pathname);
    });
    // Expose the existing instance for assertions only in the test response.
    await page.route('**/assets/*.js', async route => {
      const response = await route.fetch();
      const source = await response.text();
      const match = source.match(/const ([\w$]+)=new [\w$]+\.Game\(/);
      assert.ok(match, 'locate game instance in production bundle');
      await route.fulfill({ response, body: source + `\nwindow.__pagesGame=${match[1]};` });
    });
    await page.goto(`${origin}${base}${hash}`);
    await page.waitForFunction(key => window.__pagesGame?.scene.getScene(key).controller?.grounded, scene);
    assert.equal(await page.locator('canvas').getAttribute('width'), '320');
    const x = await page.evaluate(key => window.__pagesGame.scene.getScene(key).player.x, scene);
    await page.keyboard.down('d'); await page.waitForTimeout(120); await page.keyboard.up('d');
    assert.ok(await page.evaluate(([key,x]) => window.__pagesGame.scene.getScene(key).player.x > x, [scene,x]), 'movement runs');
    for (const visible of [true, false]) {
      await page.keyboard.down('F1');
      await page.waitForTimeout(100);
      await page.keyboard.up('F1');
      await page.waitForTimeout(100);
      assert.equal(await page.evaluate(key => window.__pagesGame.scene.getScene(key).debugText.visible, scene), visible, `${scene} F1`);
    }
    if (scene !== 'development') {
      for (const asset of ['player/courier.png','enemies/guard/guard.png','environment/visual-design/structure.png',
        'environment/visual-design/swatch.png','environment/visual-design/cutting-comb.png','ui/health.png']) {
        assert.ok(loaded.has(`${base}assets/${asset}`), `loaded ${asset} at base`);
      }
    }
    assert.ok([...loaded].some(path => path.endsWith('.css')), 'stylesheet loaded');
    assert.deepEqual(failures, []);
    console.log(`PASS ${base}${hash}: production resources, scene, movement and F1`);
    await page.close();
  }
} finally {
  await browser?.close();
  await new Promise(resolve => server.close(resolve));
}
