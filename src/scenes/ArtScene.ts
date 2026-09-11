import Phaser from 'phaser';
import { PlayerController } from '../player/PlayerController';
import { PlayerCombat } from '../combat/PlayerCombat';
import { PlayerDamage } from '../combat/PlayerDamage';
import { COMBAT } from '../combat/tuning';
import { Guard } from '../enemies/Guard';
import { ENEMY } from '../enemies/tuning';

const SPAWN = { x: 48, y: 134 };

// A small visual laboratory. Gameplay remains in the accepted controllers.
export class ArtScene extends Phaser.Scene {
  private player!: Phaser.Physics.Arcade.Image;
  private controller!: PlayerController;
  private combat!: PlayerCombat;
  private damage!: PlayerDamage;
  private guard!: Guard;
  private terrain!: Phaser.Physics.Arcade.StaticGroup;
  private playerArt!: Phaser.GameObjects.Image;
  private guardArt!: Phaser.GameObjects.Image;
  private playerBlade: Phaser.GameObjects.Image[] = [];
  private playerTip!: Phaser.GameObjects.Image;
  private guardBlade: Phaser.GameObjects.Image[] = [];
  private guardTip!: Phaser.GameObjects.Image;
  private healthCells: Phaser.GameObjects.Image[] = [];
  private stateIcon!: Phaser.GameObjects.Image;
  private keys!: Record<'left' | 'right' | 'jump' | 'attack' | 'reset' | 'debug' | 'lab', Phaser.Input.Keyboard.Key>;
  private cursors!: Phaser.Types.Input.Keyboard.CursorKeys;
  private debugGraphics!: Phaser.GameObjects.Graphics;
  private debugText!: Phaser.GameObjects.Text;
  private debugVisible = false;
  private swingFacing: -1 | 1 = 1;
  private hazard = new Phaser.Geom.Rectangle(288, 128, 32, 16);
  private hazardContact = false;

  constructor() { super('art'); }

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
    this.cameras.main.setBackgroundColor('#22283D').setBounds(0, 0, 640, 180);
    this.physics.world.setBounds(0, 0, 640, 180);
    this.terrain = this.physics.add.staticGroup();
    const solid = (x: number, y: number, w: number, h: number): void => {
      this.terrain.add(this.add.rectangle(x, y, w, h).setOrigin(0).setVisible(false));
    };
    const tile = (x: number, y: number, frame: number): void => {
      this.add.image(x, y, 'structure-art', frame).setOrigin(0);
    };
    // Recessed frame panels: sparse, dark, and visually separate from solids.
    for (const [left, top, columns] of [[32, 32, 4], [240, 32, 4], [464, 48, 5], [576, 32, 3]]) {
      for (let x = 0; x < columns; x++) for (let y = 0; y < 3; y++) tile(left + x * 16, top + y * 16, 5);
    }
    this.add.image(32, 80, 'swatch-art').setOrigin(0);
    this.add.image(336, 48, 'swatch-art').setOrigin(0);
    solid(0, 144, 640, 48);
    for (let x = 0; x < 640; x += 16) {
      tile(x, 144, 0); tile(x, 160, 1); tile(x, 176, 1);
    }
    for (const [x, y] of [[80, 112], [144, 80], [320, 112]]) {
      solid(x, y, 32, 10); tile(x, y, 2); tile(x + 16, y, 3);
    }
    for (const [x, top] of [[0, 16], [384, 48], [624, 16]]) {
      solid(x, top, 16, 144 - top);
      for (let y = top; y < 144; y += 16) tile(x, y, 4);
    }
    this.add.image(288, 128, 'comb-art', 1).setOrigin(0);
    this.add.image(304, 128, 'comb-art', 1).setOrigin(0);
    this.add.image(560, 128, 'comb-art', 0).setOrigin(0);

    this.player = this.physics.add.image(SPAWN.x, SPAWN.y, 'block').setDisplaySize(12, 20).setAlpha(0);
    this.player.setCollideWorldBounds(true);
    this.physics.add.collider(this.player, this.terrain);
    this.controller = new PlayerController(this.player);
    this.combat = new PlayerCombat(this.player);
    this.damage = new PlayerDamage(this.player);
    this.playerArt = this.add.image(SPAWN.x, 144, 'courier-art', 0).setOrigin(12 / 24, 29 / 32).setDepth(2);
    this.guardArt = this.add.image(208, 144, 'guard-art', 0).setOrigin(16 / 32, 29 / 32).setDepth(2);
    // Reuse native pixels from the active poses to span the accepted reach.
    // Repeated native image columns avoid TileSprite power-of-two resampling.
    this.textures.get('courier-art').add('shaft', 0, 7 * 24 + 18, 15, 1, 3);
    this.textures.get('courier-art').add('tip', 0, 7 * 24 + 21, 13, 3, 5);
    this.textures.get('guard-art').add('shaft', 0, 4 * 32 + 21, 14, 1, 3);
    this.textures.get('guard-art').add('tip', 0, 4 * 32 + 24, 14, 3, 4);
    this.playerBlade = Array.from({ length: COMBAT.attackHitboxWidth - 3 }, () =>
      this.add.image(0, 0, 'courier-art', 'shaft').setOrigin(0).setDepth(3).setVisible(false));
    this.playerTip = this.add.image(0, 0, 'courier-art', 'tip').setOrigin(0).setDepth(3).setVisible(false);
    this.guardBlade = Array.from({ length: ENEMY.guard.attackRange - 3 }, () =>
      this.add.image(0, 0, 'guard-art', 'shaft').setOrigin(0).setDepth(3).setVisible(false));
    this.guardTip = this.add.image(0, 0, 'guard-art', 'tip').setOrigin(0).setDepth(3).setVisible(false);
    this.resetActors();
    this.cameras.main.startFollow(this.player, true);
    this.healthCells = [4, 14, 24].map(x => this.add.image(x, 4, 'health-art', 0).setOrigin(0).setScrollFactor(0).setDepth(100));
    this.stateIcon = this.add.image(36, 4, 'health-art', 2).setOrigin(0).setScrollFactor(0).setDepth(100);

    const keyboard = this.input.keyboard!;
    this.cursors = keyboard.createCursorKeys();
    this.keys = keyboard.addKeys({ left: 'A', right: 'D', jump: 'SPACE', attack: 'J', reset: 'R', debug: 'F1', lab: 'V' }) as typeof this.keys;
    this.events.on(Phaser.Scenes.Events.WAKE, () => keyboard.resetKeys());
    this.physics.world.createDebugGraphic().setDepth(98).setVisible(false);
    this.physics.world.drawDebug = false;
    this.debugGraphics = this.add.graphics().setDepth(99).setVisible(false);
    this.debugText = this.add.text(4, 18, '', { fontFamily: 'monospace', fontSize: '8px', backgroundColor: '#101522' })
      .setScrollFactor(0).setDepth(100).setVisible(false);
  }

  update(time: number, delta: number): void {
    const seconds = delta / 1000;
    if (Phaser.Input.Keyboard.JustDown(this.keys.lab)) { this.scene.switch('development'); return; }
    this.damage.update(seconds);
    if (this.damage.readyToRespawn || Phaser.Input.Keyboard.JustDown(this.keys.reset)) this.resetActors();
    const direction = Number(this.keys.right.isDown || this.cursors.right.isDown) - Number(this.keys.left.isDown || this.cursors.left.isDown);
    const jump = Phaser.Input.Keyboard.JustDown(this.keys.jump);
    const attack = Phaser.Input.Keyboard.JustDown(this.keys.attack);
    if (this.damage.state === 'normal') {
      this.controller.update(direction, jump, seconds);
      if (direction) this.combat.facing = direction < 0 ? -1 : 1;
    }
    this.combat.update(seconds);
    if (attack && this.damage.state === 'normal' && this.combat.startAttack()) this.swingFacing = this.combat.facing;
    const contact = this.damage.state !== 'dead' && Phaser.Geom.Intersects.RectangleToRectangle(this.hazard, this.combat.hurtbox);
    if (contact && !this.hazardContact) this.receiveDamage(COMBAT.hazardDamage, this.hazard.centerX);
    this.hazardContact = contact;
    if (!this.guard.removed) {
      this.guard.syncHurtbox();
      if (this.combat.hitTarget(this.guard)) this.guard.afterHit();
      this.guard.update(seconds, this.damage.state === 'dead' ? null : this.combat.hurtbox);
      if (this.guard.attackHitbox && !this.guard.attackSpent && this.damage.state !== 'dead'
        && Phaser.Geom.Intersects.RectangleToRectangle(this.guard.attackHitbox, this.combat.hurtbox)) {
        this.guard.attackSpent = true;
        this.receiveDamage(this.guard.damage, this.guard.view.x);
      }
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
        `X ${b.center.x.toFixed(1)} Y ${b.center.y.toFixed(1)}`,
        `VX ${b.velocity.x.toFixed(1)} VY ${b.velocity.y.toFixed(1)}`,
        `Ground ${this.controller.grounded}`,
        `L ${this.controller.touchingLeftWall} R ${this.controller.touchingRightWall}`,
        `${this.controller.state} / ${this.combat.phase}`,
        `Box ${this.combat.attackHitbox !== null} HP ${this.damage.health}`,
        `${this.damage.state} Inv ${this.damage.invulnerabilityRemaining.toFixed(2)}`,
      ]);
    }
    this.guard.drawDebug(this.debugGraphics, this.debugVisible);
  }

  private updateArtwork(time: number): void {
    const b = this.controller.body;
    let frame = 0, facing = this.combat.facing;
    if (this.damage.state === 'hit') frame = 8;
    else if (this.combat.phase !== 'idle') { frame = this.combat.phase === 'active' ? 7 : 6; facing = this.swingFacing; }
    else if (!this.controller.grounded) {
      if (this.controller.state !== 'wall-jump' && (this.controller.touchingLeftWall || this.controller.touchingRightWall)) {
        frame = 5; facing = this.controller.touchingLeftWall ? -1 : 1;
      } else frame = b.velocity.y < 0 ? 3 : 4;
    } else if (b.velocity.x !== 0) frame = 1 + Math.floor(time / 125) % 2;
    this.playerArt.setFrame(frame).setFlipX(facing === -1).setPosition(Math.round(b.center.x), Math.round(b.bottom))
      .setVisible(this.damage.state !== 'dead');
    this.drawBlade(this.playerBlade, this.playerTip, this.combat.attackHitbox, this.swingFacing, -4, -6);
    this.guardArt.setVisible(!this.guard.removed);
    if (!this.guard.removed) {
      const guardFrame = this.guard.state === 'dead' ? 5 : this.guard.state === 'startup' ? 3
        : this.guard.state === 'active' ? 4 : this.guard.body.velocity.x === 0 ? 0 : 1 + Math.floor(time / 250) % 2;
      this.guardArt.setFrame(guardFrame).setFlipX(this.guard.facing === -1)
        .setPosition(Math.round(this.guard.body.center.x), Math.round(this.guard.body.bottom));
    }
    this.drawBlade(this.guardBlade, this.guardTip, this.guard.attackHitbox, this.guard.facing, -3, -3);
    this.healthCells.forEach((cell, i) => cell.setFrame(i < this.damage.health ? 0 : 1));
    this.stateIcon.setFrame(this.damage.state === 'dead' ? 4 : this.damage.state === 'hit' ? 3 : 2);
  }

  private drawBlade(shaft: Phaser.GameObjects.Image[], tip: Phaser.GameObjects.Image,
    box: Phaser.Geom.Rectangle | null, facing: -1 | 1, shaftY: number, tipY: number): void {
    for (const column of shaft) column.setVisible(box !== null);
    tip.setVisible(box !== null);
    if (!box) return;
    shaft.forEach((column, x) => column.setPosition(Math.round(box.x + (facing === -1 ? 3 : 0)) + x, Math.round(box.centerY + shaftY)));
    tip.setPosition(Math.round(facing === 1 ? box.right - 3 : box.left), Math.round(box.centerY + tipY)).setFlipX(facing === -1);
  }

  private resetActors(): void {
    this.damage.reset(); this.controller.reset(SPAWN.x, SPAWN.y); this.combat.reset(); this.combat.update(0);
    this.hazardContact = false; this.swingFacing = 1;
    this.guard?.destroy();
    this.guard = new Guard(this, this.terrain, 208, 132);
    this.guard.setPlaceholderVisible(false);
  }

  private receiveDamage(amount: number, sourceX: number): boolean {
    if (!this.damage.receive(amount, sourceX, this.combat.facing)) return false;
    this.controller.interrupt(); this.combat.interruptAttack(); return true;
  }
}
