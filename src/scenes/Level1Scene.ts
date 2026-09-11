import Phaser from 'phaser';
import { PlayerController } from '../player/PlayerController';
import { PlayerCombat } from '../combat/PlayerCombat';
import { PlayerDamage } from '../combat/PlayerDamage';
import { COMBAT } from '../combat/tuning';
import { Guard } from '../enemies/Guard';
import { ENEMY } from '../enemies/tuning';
import { Checkpoint } from '../levels/Checkpoint';
import { LEVEL, SOLIDS, HAZARDS, GUARDS, RANGED, PURSUERS } from '../levels/visualDesign';
import { PLAYER_FRAME as P, GUARD_FRAME as G, TILE, HEALTH } from '../levels/artFrames';
import { EnemyBody } from '../enemies/EnemyBody';
import { RangedAttacker } from '../enemies/RangedAttacker';
import { Pursuer } from '../enemies/Pursuer';

const SPAWN = LEVEL.start;

// Level-specific orchestration; accepted mechanics remain in their controllers.
export class Level1Scene extends Phaser.Scene {
  private player!: Phaser.Physics.Arcade.Image;
  private controller!: PlayerController;
  private combat!: PlayerCombat;
  private damage!: PlayerDamage;
  private enemies: EnemyBody[] = [];
  private guardVisuals: { enemy: Guard; image: Phaser.GameObjects.Image; blade: Phaser.GameObjects.Image[]; tip: Phaser.GameObjects.Image }[] = [];
  private checkpoint = new Checkpoint(LEVEL.start, LEVEL.checkpoint);
  private checkpointMark!: Phaser.GameObjects.Graphics;
  private completed = false;
  private completionCount = 0;
  private completionText!: Phaser.GameObjects.Text;
  private terrain!: Phaser.Physics.Arcade.StaticGroup;
  private playerArt!: Phaser.GameObjects.Image;
  private playerBlade: Phaser.GameObjects.Image[] = [];
  private playerTip!: Phaser.GameObjects.Image;
  private healthCells: Phaser.GameObjects.Image[] = [];
  private stateIcon!: Phaser.GameObjects.Image;
  private keys!: Record<'left' | 'right' | 'jump' | 'attack' | 'reset' | 'debug' | 'lab', Phaser.Input.Keyboard.Key>;
  private cursors!: Phaser.Types.Input.Keyboard.CursorKeys;
  private debugGraphics!: Phaser.GameObjects.Graphics;
  private debugText!: Phaser.GameObjects.Text;
  private debugVisible = false;
  private swingFacing: -1 | 1 = 1;
  private hazards = HAZARDS.map(([x,y,w]) => ({ box: new Phaser.Geom.Rectangle(x,y,w,16), contact: false }));

  constructor() { super('level1'); }

  preload(): void {
    this.load.spritesheet('courier-art', 'assets/player/courier.png', { frameWidth: 24, frameHeight: 32 });
    this.load.spritesheet('guard-art', 'assets/enemies/guard/guard.png', { frameWidth: 32, frameHeight: 32 });
    this.load.spritesheet('structure-art', 'assets/environment/visual-design/structure.png', { frameWidth: 16, frameHeight: 16 });
    this.load.image('swatch-art', 'assets/environment/visual-design/swatch.png');
    this.load.spritesheet('comb-art', 'assets/environment/visual-design/cutting-comb.png', { frameWidth: 16, frameHeight: 16 });
    this.load.spritesheet('health-art', 'assets/ui/health.png', { frameWidth: 8, frameHeight: 8 });
  }

  create(): void {
    if (!this.textures.exists('block')) {
      const graphics = this.make.graphics({ x: 0, y: 0 });
      graphics.fillStyle(0xffffff).fillRect(0, 0, 1, 1).generateTexture('block', 1, 1);
      graphics.destroy();
    }
    this.checkpoint = new Checkpoint(LEVEL.start, LEVEL.checkpoint);
    this.completed = false; this.completionCount = 0; this.debugVisible = false;
    this.cameras.main.setBackgroundColor('#22283D').setBounds(0, 0, LEVEL.width, LEVEL.height);
    this.physics.world.setBounds(0, 0, LEVEL.width, LEVEL.height);
    this.physics.world.resume();
    this.terrain = this.physics.add.staticGroup();
    // Repeated physical frame panels and swatches, not readable application UI.
    for (let x = 64; x < LEVEL.width; x += 256) {
      for (let dx = 0; dx < 80; dx += 16) for (let dy = 0; dy < 48; dy += 16)
        this.add.image(x + dx, 128 + dy, 'structure-art', TILE.background).setOrigin(0);
      this.add.image(x + 96, 224, 'swatch-art').setOrigin(0);
    }
    for (const [x,y,w,h] of SOLIDS) {
      this.terrain.add(this.add.rectangle(x,y,w,h).setOrigin(0).setVisible(false));
      for (let dx = 0; dx < w; dx += 16) for (let dy = 0; dy < h; dy += 16)
        this.add.image(x+dx,y+dy,'structure-art', w === 16 ? TILE.wall : dy === 0 ? TILE.top : TILE.fill).setOrigin(0);
    }
    this.hazards = HAZARDS.map(([x,y,w]) => ({ box: new Phaser.Geom.Rectangle(x,y,w,16), contact: false }));
    for (const {box} of this.hazards) for (let x = box.x; x < box.right; x += 16)
      this.add.image(x,box.y,'comb-art',1).setOrigin(0);
    // Provisional palette-only checkpoint and exit markers; no raster exports.
    this.checkpointMark = this.add.graphics();
    this.drawCheckpoint();
    this.add.graphics().lineStyle(2,0xE8DEC2).strokeRect(LEVEL.exit.x,LEVEL.exit.y,32,48)
      .fillStyle(0xA28ACB).fillRect(LEVEL.exit.x+12,LEVEL.exit.y+16,8,16);
    this.player = this.physics.add.image(SPAWN.x, SPAWN.y, 'block').setDisplaySize(12, 20).setAlpha(0);
    this.player.setCollideWorldBounds(true);
    this.physics.add.collider(this.player, this.terrain);
    this.controller = new PlayerController(this.player);
    this.combat = new PlayerCombat(this.player);
    this.damage = new PlayerDamage(this.player);
    this.playerArt = this.add.image(SPAWN.x, LEVEL.floor, 'courier-art', 0).setOrigin(12 / 24, 29 / 32).setDepth(2);
    // Reuse native pixels from the active poses to span the accepted reach.
    // Repeated native image columns avoid TileSprite power-of-two resampling.
    this.textures.get('courier-art').add('shaft', 0, 7 * 24 + 18, 15, 1, 3);
    this.textures.get('courier-art').add('tip', 0, 7 * 24 + 21, 13, 3, 5);
    this.textures.get('guard-art').add('shaft', 0, 4 * 32 + 21, 14, 1, 3);
    this.textures.get('guard-art').add('tip', 0, 4 * 32 + 24, 14, 3, 4);
    this.playerBlade = Array.from({ length: COMBAT.attackHitboxWidth - 3 }, () =>
      this.add.image(0, 0, 'courier-art', 'shaft').setOrigin(0).setDepth(3).setVisible(false));
    this.playerTip = this.add.image(0, 0, 'courier-art', 'tip').setOrigin(0).setDepth(3).setVisible(false);
    this.resetActors();
    this.cameras.main.startFollow(this.player, true);
    this.healthCells = [4, 14, 24].map(x => this.add.image(x, 4, 'health-art', 0).setOrigin(0).setScrollFactor(0).setDepth(100));
    this.stateIcon = this.add.image(36, 4, 'health-art', 2).setOrigin(0).setScrollFactor(0).setDepth(100);

    const keyboard = this.input.keyboard!;
    keyboard.resetKeys();
    this.cursors = keyboard.createCursorKeys();
    this.keys = keyboard.addKeys({ left: 'A', right: 'D', jump: 'SPACE', attack: 'J', reset: 'R', debug: 'F1', lab: 'V' }) as typeof this.keys;
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => this.clearEnemies());
    this.completionText = this.add.text(160,80,'CLEAR\nR  RESTART    V  LAB', { fontFamily: 'monospace', fontSize: '8px', color: '#E8DEC2', backgroundColor: '#101522', align: 'center', padding: { x: 12, y: 12 } }).setOrigin(0.5).setScrollFactor(0).setDepth(110).setVisible(false);
    this.physics.world.createDebugGraphic().setDepth(98).setVisible(false);
    this.physics.world.drawDebug = false;
    this.debugGraphics = this.add.graphics().setDepth(99).setVisible(false);
    this.debugText = this.add.text(4, 18, '', { fontFamily: 'monospace', fontSize: '8px', backgroundColor: '#101522' })
      .setScrollFactor(0).setDepth(100).setVisible(false);
  }

  update(time: number, delta: number): void {
    const seconds = delta / 1000;
    if (Phaser.Input.Keyboard.JustDown(this.keys.lab)) { this.scene.run('development'); this.scene.stop(); return; }
    if (Phaser.Input.Keyboard.JustDown(this.keys.reset)) { this.scene.restart(); return; }
    if (this.completed) return;
    this.damage.update(seconds);
    if (this.damage.readyToRespawn) this.resetActors();
    const direction = Number(this.keys.right.isDown || this.cursors.right.isDown) - Number(this.keys.left.isDown || this.cursors.left.isDown);
    const jump = Phaser.Input.Keyboard.JustDown(this.keys.jump);
    const attack = Phaser.Input.Keyboard.JustDown(this.keys.attack);
    if (this.damage.state === 'normal') {
      this.controller.update(direction, jump, seconds);
      if (direction) this.combat.facing = direction < 0 ? -1 : 1;
    }
    this.combat.update(seconds);
    if (attack && this.damage.state === 'normal' && this.combat.startAttack()) this.swingFacing = this.combat.facing;
    for (const hazard of this.hazards) {
      const contact = this.damage.state !== 'dead' && Phaser.Geom.Intersects.RectangleToRectangle(hazard.box, this.combat.hurtbox);
      if (contact && !hazard.contact) this.receiveDamage(COMBAT.hazardDamage, hazard.box.centerX);
      hazard.contact = contact;
    }
    for (const enemy of this.enemies) {
      if (enemy.removed) continue;
      enemy.syncHurtbox();
      if (this.combat.hitTarget(enemy)) enemy.afterHit();
      enemy.update(seconds, this.damage.state === 'dead' ? null : this.combat.hurtbox);
      if (enemy instanceof RangedAttacker) {
        for (const shot of enemy.projectiles) shot.update(seconds, this.damage.state === 'dead' ? null : this.combat.hurtbox,
          (amount,x) => this.receiveDamage(amount,x));
        enemy.projectiles = enemy.projectiles.filter(shot => !shot.removed);
      }
      if (enemy.attackHitbox && !enemy.attackSpent && this.damage.state !== 'dead'
        && Phaser.Geom.Intersects.RectangleToRectangle(enemy.attackHitbox,this.combat.hurtbox)) {
        enemy.attackSpent = true; this.receiveDamage(enemy.damage,enemy.view.x);
      }
    }
    if (this.damage.state !== 'dead') {
      if (Phaser.Geom.Intersects.RectangleToRectangle(this.combat.hurtbox,
        new Phaser.Geom.Rectangle(LEVEL.checkpoint.x-16,256,32,48)) && this.checkpoint.activate()) this.drawCheckpoint();
      if (Phaser.Geom.Intersects.RectangleToRectangle(this.combat.hurtbox, new Phaser.Geom.Rectangle(
        LEVEL.exit.x,LEVEL.exit.y,LEVEL.exit.width,LEVEL.exit.height))) this.complete();
    }
    this.updateArtwork(time);
    if (Phaser.Input.Keyboard.JustDown(this.keys.debug)) {
      this.debugVisible = !this.debugVisible;
      this.debugText.setVisible(this.debugVisible);
      this.debugGraphics.setVisible(this.debugVisible);
      this.physics.world.drawDebug = this.debugVisible;
      this.physics.world.debugGraphic.clear().setVisible(this.debugVisible);
    }
    this.debugGraphics.clear();
    if (this.debugVisible) {
      const b = this.controller.body;
      this.debugGraphics.lineStyle(1, 0x55ffff).strokeRectShape(this.combat.hurtbox);
      if (this.combat.attackHitbox) this.debugGraphics.lineStyle(1, 0xffff55).strokeRectShape(this.combat.attackHitbox);
      this.debugText.setText([
        `LEVEL 1 / CP ${this.checkpoint.active}`,
        `X ${b.center.x.toFixed(1)} Y ${b.center.y.toFixed(1)}`,
        `VX ${b.velocity.x.toFixed(1)} VY ${b.velocity.y.toFixed(1)}`,
        `Ground ${this.controller.grounded}`,
        `L ${this.controller.touchingLeftWall} R ${this.controller.touchingRightWall}`,
        `${this.controller.state} / ${this.combat.phase}`,
        `Box ${this.combat.attackHitbox !== null} HP ${this.damage.health}`,
        `${this.damage.state} Inv ${this.damage.invulnerabilityRemaining.toFixed(2)}`,
      ]);
    }
    for (const enemy of this.enemies) enemy.drawDebug(this.debugGraphics, this.debugVisible);
  }

  private updateArtwork(time: number): void {
    const b = this.controller.body;
    let frame: number = P.idle, facing = this.combat.facing;
    if (this.damage.state === 'hit') frame = P.hurt;
    else if (this.combat.phase !== 'idle') { frame = this.combat.phase === 'active' ? P.slash : P.startup; facing = this.swingFacing; }
    else if (!this.controller.grounded) {
      if (this.controller.state !== 'wall-jump' && (this.controller.touchingLeftWall || this.controller.touchingRightWall)) {
        frame = P.wall; facing = this.controller.touchingLeftWall ? -1 : 1;
      } else frame = b.velocity.y < 0 ? P.jump : P.fall;
    } else if (b.velocity.x !== 0) frame = P.run + Math.floor(time / 125) % 2;
    this.playerArt.setFrame(frame).setFlipX(facing === -1).setPosition(Math.round(b.center.x), Math.round(b.bottom))
      .setVisible(this.damage.state !== 'dead');
    this.drawBlade(this.playerBlade, this.playerTip, this.combat.attackHitbox, this.swingFacing, -4, -6);
    for (const {enemy,image,blade,tip} of this.guardVisuals) {
      image.setVisible(!enemy.removed);
      if (!enemy.removed) {
        const frame = enemy.state === 'dead' ? G.dead : enemy.state === 'startup' ? G.startup
          : enemy.state === 'active' ? G.attack : enemy.body.velocity.x === 0 ? G.idle : G.walk + Math.floor(time/250)%2;
        image.setFrame(frame).setFlipX(enemy.facing === -1).setPosition(Math.round(enemy.body.center.x),Math.round(enemy.body.bottom));
      }
      this.drawBlade(blade,tip,enemy.attackHitbox,enemy.facing,-3,-3);
    }
    this.healthCells.forEach((cell, i) => cell.setFrame(i < this.damage.health ? HEALTH.full : HEALTH.empty));
    this.stateIcon.setFrame(this.damage.state === 'dead' ? HEALTH.dead : this.damage.state === 'hit' ? HEALTH.hit : HEALTH.normal);
  }

  private drawBlade(shaft: Phaser.GameObjects.Image[], tip: Phaser.GameObjects.Image,
    box: Phaser.Geom.Rectangle | null, facing: -1 | 1, shaftY: number, tipY: number): void {
    for (const column of shaft) column.setVisible(box !== null);
    tip.setVisible(box !== null);
    if (!box) return;
    shaft.forEach((column, x) => column.setPosition(Math.round(box.x + (facing === -1 ? 3 : 0)) + x, Math.round(box.centerY + shaftY)));
    tip.setPosition(Math.round(facing === 1 ? box.right - 3 : box.left), Math.round(box.centerY + tipY)).setFlipX(facing === -1);
  }

  private drawCheckpoint(): void {
    const x = LEVEL.checkpoint.x;
    this.checkpointMark.clear().fillStyle(0x101522).fillRect(x-12,264,24,40)
      .lineStyle(2,this.checkpoint.active ? 0xE6AD45 : 0x66728A).strokeRect(x-10,266,20,36)
      .fillStyle(this.checkpoint.active ? 0xE6AD45 : 0x66728A).fillRect(x-4,274,8,16);
  }

  private clearEnemies(): void {
    for (const enemy of this.enemies) enemy.destroy();
    this.enemies = [];
    for (const visual of this.guardVisuals) {
      visual.image.destroy(); visual.tip.destroy(); visual.blade.forEach(part => part.destroy());
    }
    this.guardVisuals = [];
  }

  private resetActors(): void {
    const spawn = this.checkpoint.spawn;
    this.damage.reset(); this.controller.reset(spawn.x,spawn.y); this.combat.reset(); this.combat.update(0);
    this.hazards.forEach(hazard => { hazard.contact = false; }); this.swingFacing = 1;
    this.clearEnemies();
    const relevant = (x: number): boolean => !this.checkpoint.active || x > LEVEL.checkpoint.x;
    for (const x of GUARDS.filter(relevant)) {
      const enemy = new Guard(this,this.terrain,x,LEVEL.floor-12);
      enemy.setPlaceholderVisible(false); this.enemies.push(enemy);
      this.guardVisuals.push({ enemy,
        image: this.add.image(x,LEVEL.floor,'guard-art',G.idle).setOrigin(16/32,29/32).setDepth(2),
        blade: Array.from({length: ENEMY.guard.attackRange-3}, () => this.add.image(0,0,'guard-art','shaft').setOrigin(0).setDepth(3).setVisible(false)),
        tip: this.add.image(0,0,'guard-art','tip').setOrigin(0).setDepth(3).setVisible(false) });
    }
    for (const [x,y] of RANGED.filter(([x]) => relevant(x))) this.enemies.push(new RangedAttacker(this,this.terrain,x,y,
      new Phaser.Geom.Rectangle(x-320,0,640,LEVEL.height)));
    for (const x of PURSUERS.filter(relevant)) this.enemies.push(new Pursuer(this,this.terrain,x,LEVEL.floor-10));
    this.cameras.main.centerOn(spawn.x,spawn.y);
  }

  private complete(): void {
    if (this.completed) return;
    this.completed = true; this.completionCount++;
    this.combat.interruptAttack(); this.controller.interrupt();
    this.controller.body.setVelocity(0,0); this.physics.world.pause();
    this.clearEnemies(); this.completionText.setVisible(true);
  }

  private receiveDamage(amount: number, sourceX: number): boolean {
    if (!this.damage.receive(amount, sourceX, this.combat.facing)) return false;
    this.controller.interrupt(); this.combat.interruptAttack(); return true;
  }
}
