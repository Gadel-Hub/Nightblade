import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';

export const root = fileURLToPath(new URL('../', import.meta.url));
export const palette = JSON.parse(readFileSync(resolve(root, 'art/palette.json'), 'utf8'));
export const manifest = JSON.parse(readFileSync(resolve(root, 'art/manifest.json'), 'utf8'));
const rgba = Object.fromEntries(Object.entries(palette).map(([key, value]) =>
  [key, [...value.slice(1).match(/../g).map(channel => parseInt(channel, 16)), 255]]));
const approved = new Set(Object.values(rgba).map(color => color.join(',')));

export function sourcePixels(asset) {
  const source = JSON.parse(readFileSync(resolve(root, 'art/source', asset.source), 'utf8'));
  if (asset.width !== source.frames.length * asset.frameWidth || asset.height !== asset.frameHeight) {
    throw Error(`${asset.source}: frame count does not match ${asset.width}×${asset.height}`);
  }
  const data = Buffer.alloc(asset.width * asset.height * 4);
  for (const [frame, { name, rows }] of source.frames.entries()) {
    if (rows.length !== asset.frameHeight) throw Error(`${asset.source}/${name}: expected ${asset.frameHeight} rows`);
    const used = new Set();
    rows.forEach((row, y) => {
      if (row.length !== asset.frameWidth) throw Error(`${asset.source}/${name}: row ${y} has ${row.length} cells, expected ${asset.frameWidth}`);
      [...row].forEach((cell, x) => {
        if (cell === '.') return;
        if (!rgba[cell]) throw Error(`${asset.source}/${name} (${x},${y}): unknown index ${cell}`);
        used.add(cell);
        data.set(rgba[cell], (y * asset.width + frame * asset.frameWidth + x) * 4);
      });
    });
    if (used.size > (source.maxColors?.[name] ?? 4)) throw Error(`${asset.source}/${name}: ${used.size} colors exceeds frame budget`);
  }
  return data;
}

export function validatePNG(asset, encoded) {
  const png = PNG.sync.read(encoded, { checkCRC: true });
  if (png.width !== asset.width || png.height !== asset.height) {
    throw Error(`${asset.output}: dimensions ${png.width}×${png.height}, expected ${asset.width}×${asset.height}`);
  }
  const expected = sourcePixels(asset);
  for (let i = 0; i < png.data.length; i += 4) {
    const x = (i / 4) % png.width, y = Math.floor(i / 4 / png.width);
    const pixel = png.data.subarray(i, i + 4);
    if (pixel[3] !== 0 && pixel[3] !== 255) throw Error(`${asset.output} (${x},${y}): partial alpha ${pixel[3]}`);
    if (pixel[3] === 255 && !approved.has([...pixel].join(','))) throw Error(`${asset.output} (${x},${y}): off-palette RGB ${[...pixel].slice(0,3)}`);
    if (!pixel.equals(expected.subarray(i, i + 4))) throw Error(`${asset.output} (${x},${y}): differs from native source grid (altered/scaled export)`);
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    const mode = process.argv[2];
    if (!['export', 'validate'].includes(mode)) throw Error('Usage: node scripts/pixel-assets.mjs export|validate');
    for (const asset of manifest) {
      const path = resolve(root, 'public/assets', asset.output);
      if (mode === 'export') {
        const png = new PNG({ width: asset.width, height: asset.height });
        png.data = sourcePixels(asset);
        const encoded = PNG.sync.write(png, { colorType: 6, inputColorType: 6 });
        validatePNG(asset, encoded);
        mkdirSync(dirname(path), { recursive: true });
        writeFileSync(path, encoded);
      } else validatePNG(asset, readFileSync(path));
      console.log(`${mode}: ${asset.output} ${asset.width}×${asset.height} — exact native grid, palette and alpha`);
    }
  } catch (error) { console.error(error.message); process.exitCode = 1; }
}
