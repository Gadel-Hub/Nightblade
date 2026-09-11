const { chromium } = require('playwright');
const base = process.env.LEVEL_TEST_URL || 'http://127.0.0.1:5173';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage({ viewport: { width: 960, height: 600 } });
        await page.route('**/src/main.ts*', async (r) => { const res = await r.fetch(); await r.fulfill({ response: res, body: await res.text() + '\nwindow.__testGame=game;' }); });
        await page.goto(`${base}/#level1`);
        await page.waitForFunction(() => window.__testGame?.scene.getScene('level1').controller?.grounded);
        console.log(await page.evaluate(() => {
            const g = window.__testGame, s = g.scene.getScene('level1');
            g.loop.stop();
            const b = s.controller.body;
            let tick = 0;
            const pass = [];
            const step = (dir, jump = false, attack = false) => { s.keys.left.isDown = dir < 0; s.keys.right.isDown = dir > 0; s.keys.jump._justDown = jump; s.keys.attack._justDown = attack; s.physics.world.singleStep(); s.update(tick++ * 1000 / 60, 1000 / 60); };
            // Exercise every non-wall obstacle and comb using actual controller/physics arcs.
            const blocks = [[192, 288, 64], [352, 272, 64], [480, 256, 64], [1360, 272, 48], [1568, 272, 48], [2480, 272, 64], [2752, 272, 64], [3280, 272, 48], [3472, 256, 128], [3776, 272, 48], [4064, 272, 64], [4288, 256, 128], [5456, 272, 48], [5648, 256, 128]];
            s.clearEnemies();
            for (const [x, y, w] of blocks) {
                s.damage.reset();
                s.hazards.forEach(h => h.contact = false);
                s.controller.reset(x - 30, 294);
                step(0);
                step(1, true);
                for (let i = 0; i < 80; i++)
                    step(1);
                if (b.center.x < x + w)
                    throw Error('blocked plate ' + x + ' at ' + b.center.x);
                pass.push('plate ' + x);
            }
            for (const h of s.hazards) {
                s.damage.reset();
                s.controller.reset(h.box.x - 28, 294);
                step(0);
                step(1, true);
                for (let i = 0; i < 48; i++)
                    step(1);
                if (s.damage.health !== 3)
                    throw Error('hazard jump ' + h.box.x);
                pass.push('comb ' + h.box.x);
            }
            for (const left of [1984, 4800]) {
                s.damage.reset();
                s.controller.reset(left + 30, 294);
                step(0);
                let dir = 1, jumps = 0;
                for (let i = 0; i < 300 && b.center.x < left + 110; i++) {
                    let jump = false;
                    if (s.controller.grounded && b.blocked.right) {
                        jump = true;
                        dir = 1;
                    }
                    else if (!s.controller.grounded && b.blocked.right) {
                        jump = true;
                        dir = -1;
                        jumps++;
                    }
                    else if (!s.controller.grounded && b.blocked.left) {
                        jump = true;
                        dir = 1;
                        jumps++;
                    }
                    if (b.bottom < 176)
                        dir = 1;
                    step(dir, jump);
                }
                if (b.center.x < left + 110)
                    throw Error('shaft stuck ' + left + ' ' + b.center.x + ',' + b.center.y);
                pass.push('shaft ' + left + ' jumps ' + jumps);
            }
            // Optional descending ledges can also be crossed without falling to the floor.
            for (const [fromX, fromY, toX, toY] of [[2054, 176, 2160, 208], [2210, 208, 2304, 240], [4870, 176, 4976, 208], [5026, 208, 5120, 240]]) {
                s.damage.reset();
                s.controller.reset(fromX, fromY - 10);
                step(0);
                step(1, true);
                let landed = false;
                for (let i = 0; i < 75; i++) {
                    step(b.center.x < toX + 24 ? 1 : 0);
                    if (s.controller.grounded && Math.abs(b.bottom - toY) < 1)
                        landed = true;
                }
                if (!landed)
                    throw Error('descending ledge ' + toX);
                pass.push('ledge ' + toX);
            }
            return pass;
        }));
        console.log('PASS traversal');
    }
    finally {
        await browser.close();
    }
})().catch(e => { console.error(e); process.exit(1); });
