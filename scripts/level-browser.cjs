// Start Vite separately; keep test instrumentation out of the shipped game.
const { chromium } = require('playwright');
const base = process.env.LEVEL_TEST_URL || 'http://127.0.0.1:5173';
(async () => {
    const browser = await chromium.launch();
    try {
        const page = await browser.newPage({ viewport: { width: 960, height: 600 } });
        const errors = [];
        page.on('pageerror', e => errors.push(e.message));
        await page.route('**/src/main.ts*', async (r) => { const res = await r.fetch(); await r.fulfill({ response: res, body: await res.text() + '\nwindow.__testGame=game;' }); });
        await page.goto(`${base}/#level1`);
        await page.waitForFunction(() => window.__testGame?.scene.getScene('level1').controller?.grounded);
        console.log(await page.evaluate(() => {
            const g = window.__testGame, s = g.scene.getScene('level1'), b = s.controller.body;
            g.loop.stop();
            const pass = [];
            const check = (v, m) => { if (!v)
                throw Error(m); pass.push(m); };
            const step = () => { s.physics.world.singleStep(); s.update(0, 1000 / 60); };
            const frames = n => { for (let i = 0; i < n; i++)
                step(); };
            check(b.width === 12 && b.height === 20 && s.enemies.length === 13, 'spawn and original collision');
            s.keys.right.isDown = true;
            frames(10);
            check(b.velocity.x === 140, 'run');
            s.keys.jump._justDown = true;
            step();
            check(b.velocity.y === -280, 'jump');
            s.keys.right.isDown = false;
            frames(60);
            s.resetActors();
            s.controller.reset(1254, 294);
            step();
            check(s.damage.health === 2, 'comb damage');
            for (let i = 0; i < 70; i++) {
                b.reset(1254, 294);
                step();
            }
            check(s.damage.health === 2, 'sustained contact latched');
            s.resetActors();
            s.receiveDamage(3, 0);
            frames(52);
            check(s.damage.health === 3 && s.checkpoint.active === false && Math.abs(b.center.x - 48) < 1, 'death before checkpoint');
            s.controller.reset(3008, 294);
            step();
            check(s.checkpoint.active, 'checkpoint activates');
            for (let i = 0; i < 5; i++) {
                s.damage.invulnerabilityRemaining = 0;
                s.receiveDamage(3, 0);
                frames(52);
                check(s.damage.health === 3 && s.damage.state === 'normal' && Math.abs(b.center.x - 3008) < 1 && s.enemies.length === 10 && s.physics.world.bodies.size === 11, 'checkpoint death reset ' + i);
            }
            const ranged = s.enemies.find(e => e.name === 'Ranged');
            s.controller.reset(ranged.view.x - 80, 244);
            frames(40);
            check(ranged.projectiles.length > 0, 'ranged fires');
            const shot = ranged.projectiles[0];
            s.resetActors();
            check(shot.removed && s.enemies.every(e => !e.projectiles?.length), 'respawn removes stale projectiles');
            const guard = s.enemies.find(e => e.name === 'Guard');
            s.controller.reset(guard.view.x - 24, 294);
            s.combat.update(0);
            s.combat.startAttack();
            s.combat.update(.081);
            step();
            check(guard.health === 2, 'player melee');
            frames(3);
            check(guard.health === 2, 'single swing hit');
            s.keys.debug._justDown = true;
            step();
            check(s.debugText.visible && s.debugGraphics.visible, 'F1 on');
            s.keys.debug._justDown = true;
            step();
            check(!s.debugText.visible && !s.debugGraphics.visible, 'F1 off');
            s.resetActors();
            s.controller.reset(6056, 294);
            step();
            frames(10);
            check(s.completed && s.completionCount === 1 && s.physics.world.isPaused && s.enemies.length === 0, 'completion once and stops combat');
            return pass;
        }));
        await page.evaluate(() => { const s = window.__testGame.scene.getScene('level1'); s.keys.reset._justDown = true; s.update(0, 16); window.__testGame.scene.update(0, 16); });
        await page.waitForTimeout(300);
        console.log(await page.evaluate(() => { const s = window.__testGame.scene.getScene('level1'); return { completed: s.completed, cp: s.checkpoint.active, paused: s.physics.world.isPaused, ground: s.controller.grounded }; }));
        await page.waitForFunction(() => { const s = window.__testGame.scene.getScene('level1'); return !s.completed && !s.checkpoint.active && !s.physics.world.isPaused; });
        console.log('PASS restart');
        // Camera clamp and solid world boundaries use the same production scene.
        await page.reload();
        await page.waitForFunction(() => window.__testGame?.scene.getScene('level1').controller?.grounded);
        for (const x of [20, 2016, 6100]) {
            await page.evaluate(x => { const s = window.__testGame.scene.getScene('level1'); s.controller.reset(x, 294); }, x);
            await page.waitForTimeout(80);
            if (!await page.evaluate(() => { const c = window.__testGame.scene.getScene('level1').cameras.main; return c.scrollX >= 0 && c.scrollX <= 5824 && c.scrollY >= 0 && c.scrollY <= 180 && c.roundPixels; }))
                throw Error('camera bounds');
        }
        console.log('PASS bounded pixel camera');
        // Real keyboard/restart and sleep/wake scene transitions with a fresh live loop.
        await page.reload();
        await page.waitForFunction(() => window.__testGame?.scene.getScene('level1').controller?.grounded);
        const press = async (key) => { await page.keyboard.down(key); await page.waitForTimeout(80); await page.keyboard.up(key); await page.waitForTimeout(80); };
        await page.evaluate(() => { const s = window.__testGame.scene.getScene('level1'); s.controller.reset(6056, 294); });
        await page.waitForFunction(() => window.__testGame.scene.getScene('level1').completed);
        await press('r');
        await page.waitForFunction(() => { const s = window.__testGame.scene.getScene('level1'); return !s.completed && s.controller.grounded && !s.physics.world.isPaused; });
        await press('v');
        await page.waitForFunction(() => window.__testGame.scene.isActive('development'));
        await press('v');
        await page.waitForFunction(() => window.__testGame.scene.isActive('art'));
        await press('v');
        await page.waitForFunction(() => window.__testGame.scene.isActive('development'));
        for (let i = 0; i < 3; i++) {
            await press('l');
            await page.waitForFunction(() => window.__testGame.scene.isActive('level1'));
            await press('v');
            await page.waitForFunction(() => window.__testGame.scene.isActive('development'));
        }
        console.log('PASS real keyboard restart and repeated level/lab/art access');
        if (errors.length)
            throw Error(errors.join('\n'));
    }
    finally {
        await browser.close();
    }
})().catch(e => { console.error(e); process.exit(1); });
