import { test } from 'node:test';
import assert from 'node:assert/strict';
import { PNG } from 'pngjs';
import { manifest, sourcePixels, validatePNG } from './pixel-assets.mjs';
const asset = manifest[0];
const encode = data => PNG.sync.write({ width: asset.width, height: asset.height, data });

test('all native sources round-trip exactly', () => {
  for (const spec of manifest) validatePNG(spec, PNG.sync.write({ width: spec.width, height: spec.height, data: sourcePixels(spec) }));
});
test('wrong dimensions fail', () => {
  assert.throws(() => validatePNG(asset, PNG.sync.write(new PNG({ width: 1, height: 1 }))), /dimensions/);
});
test('antialias colors fail with coordinates', () => {
  const data = sourcePixels(asset); data.set([1,2,3,255], 0);
  assert.throws(() => validatePNG(asset, encode(data)), /\(0,0\): off-palette/);
});
test('partial alpha fails', () => {
  const data = sourcePixels(asset); data[3] = 128;
  assert.throws(() => validatePNG(asset, encode(data)), /partial alpha/);
});
test('palette-correct but altered/scaled pixel fails', () => {
  const data = sourcePixels(asset); data.set([16,21,34,255], 0);
  assert.throws(() => validatePNG(asset, encode(data)), /native source grid/);
});
