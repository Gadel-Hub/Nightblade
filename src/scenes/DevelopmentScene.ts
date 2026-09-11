import Phaser from 'phaser';
import { PlayerController } from '../player/PlayerController';
import { PlayerCombat } from '../combat/PlayerCombat';
import { COMBAT } from '../combat/tuning';
import { PlayerDamage } from '../combat/PlayerDamage';

const SPAWN = { x: 48, y: 280 };
const COMBAT_SPAWN = { x: 1480, y: 438 };
const WORLD = { width: 1920, height: 540 };

export class DevelopmentScene extends Phaser.Scene {
  private player!: Phaser.Physics.Arcade.Image;
  private controller!: PlayerController;
  private combat!: PlayerCombat;
  private damage!: PlayerDamage;
  private hazard = new Phaser.Geom.Rectangle(1776, 432, 32, 16);
  private hazardContact = false;
  private statusText!: Phaser.GameObjects.Text;
  private targets: { health: number; maxHealth: number; hurtbox: Phaser.Geom.Rectangle;
    view: Phaser.GameObjects.Rectangle; label: Phaser.GameObjects.Text }[] = [];
  private combatDebug!: Phaser.GameObjects.Graphics;
  private slash!: Phaser.GameObjects.Rectangle;
  private cursors!: Phaser.Types.Input.Keyboard.CursorKeys;
  private keys!: Record<'left' | 'right' | 'jump' | 'reset' | 'debug' | 'attack' | 'combatTest', Phaser.Input.Keyboard.Key>;
  private debugText!: Phaser.GameObjects.Text;
  private debugVisible = false;

  constructor() { super('development'); }

  create(): void {
    const graphics = this.make.graphics({ x: 0, y: 0 });
    graphics.fillStyle(0xffffff).fillRect(0, 0, 1, 1);
    graphics.generateTexture('block', 1, 1);
    graphics.destroy();

    this.physics.world.setBounds(0, 0, WORLD.width, WORLD.height);
    const terrain = this.physics.add.staticGroup();
    const block = (x: number, y: number, width: number, height: number): void => {
      const tile = this.add.rectangle(x, y, width, height, 0x536479).setOrigin(0);
      terrain.add(tile);
    };

    block(0, 320, 1280, 32); // Long flat floor.
    block(0, 0, 16, 540); // Left boundary wall.
    block(1904, 0, 16, 540); // Right boundary wall.
    block(0, 0, 1920, 16); // World ceiling.
    block(0, 524, 1920, 16); // Lower safety floor beneath the drop.
    block(200, 288, 64, 16);
    block(300, 256, 64, 16);
    block(400, 224, 80, 16);
    block(560, 272, 176, 16); // Low ceiling: 32-pixel corridor.
    block(800, 160, 16, 112); // Wall-jump shaft, entered underneath.
    block(848, 128, 16, 144);
    block(848, 112, 32, 16); // Partial ceiling; the left side remains an exit.
    block(928, 256, 64, 16);
    block(1040, 208, 80, 16);
    block(1168, 272, 64, 16);
    block(1440, 448, 464, 16); // Reserved combat floor, after drop.
    block(1360, 288, 16, 192); // Single wall accessible from either side.
    // Lower jump lane: 32-pixel gap, then 104-pixel near-maximum gap.
    block(32, 448, 80, 16);
    block(144, 448, 80, 16);
    block(328, 448, 80, 16);
    block(480, 400, 16, 124); // Wider shaft for alternating wall jumps.
    block(544, 368, 16, 156);

    const label = (x: number, y: number, text: string): void => {
      this.add.text(x, y, text, { fontFamily: 'monospace', fontSize: '8px', color: '#aebcd0' });
    };
    label(32, 300, 'FLAT / START');
    label(208, 204, 'HEIGHTS');
    label(560, 250, 'CORRIDOR');
    label(776, 92, 'WALL TEST');
    label(1248, 294, 'DROP >');
    label(32, 426, '32 GAP / 104 GAP');
    label(464, 350, 'OPEN SHAFT');
    label(1304, 266, 'SINGLE WALL');
    this.add.rectangle(1664, 408, 416, 80).setStrokeStyle(1, 0x809070);
    label(1496, 360, 'COMBAT TEST / C TO RESET HERE');
    this.add.rectangle(this.hazard.x, this.hazard.y, this.hazard.width, this.hazard.height, 0xb95060).setOrigin(0);
    label(1760, 416, 'DAMAGE');
    for (const [x, health] of [[1552, COMBAT.targetHealth], [1664, COMBAT.durableTargetHealth]]) {
      this.targets.push({ health, maxHealth: health,
        hurtbox: new Phaser.Geom.Rectangle(x - 8, 420, 16, 28),
        view: this.add.rectangle(x, 434, 16, 28, 0x92a6b0),
        label: this.add.text(x - 12, 406, `${health}`, { fontFamily: 'monospace', fontSize: '8px' }),
      });
    }

    this.player = this.physics.add.image(SPAWN.x, SPAWN.y, 'block');
    this.player.setDisplaySize(12, 20).setTint(0xe6c66a);
    this.player.setCollideWorldBounds(true);
    this.controller = new PlayerController(this.player);
    this.combat = new PlayerCombat(this.player);
    this.damage = new PlayerDamage(this.player);
    this.slash = this.add.rectangle(0, 0, 1, 1, 0xf3e8b4).setOrigin(0).setVisible(false);
    this.combatDebug = this.add.graphics().setDepth(99).setVisible(false);
    this.physics.add.collider(this.player, terrain);
    this.cameras.main.setBounds(0, 0, WORLD.width, WORLD.height);
    this.cameras.main.startFollow(this.player, true);

    const keyboard = this.input.keyboard!;
    this.cursors = keyboard.createCursorKeys();
    this.keys = keyboard.addKeys({ left: 'A', right: 'D', jump: 'SPACE', reset: 'R', debug: 'F1', attack: 'J', combatTest: 'C' }) as typeof this.keys;
    this.physics.world.createDebugGraphic();
    this.physics.world.drawDebug = false;
    this.physics.world.debugGraphic.setDepth(98).setVisible(false);
    this.debugText = this.add.text(4, 4, '', {
      fontFamily: 'monospace', fontSize: '8px', color: '#ffffff', backgroundColor: '#11151f', padding: { x: 3, y: 3 },
    }).setScrollFactor(0).setDepth(100).setVisible(false);
    this.statusText = this.add.text(4, 166, '', {
      fontFamily: 'monospace', fontSize: '8px', backgroundColor: '#11151f',
    }).setScrollFactor(0).setDepth(100);
  }

  update(_time: number, delta: number): void {
    const body = this.player.body as Phaser.Physics.Arcade.Body;
    const direction = Number(this.cursors.right.isDown || this.keys.right.isDown)
      - Number(this.cursors.left.isDown || this.keys.left.isDown);
    this.damage.update(delta / 1000);
    if (this.damage.readyToRespawn) this.resetPlayer(COMBAT_SPAWN.x, COMBAT_SPAWN.y);
    const jumpPressed = Phaser.Input.Keyboard.JustDown(this.keys.jump);
    const attackPressed = Phaser.Input.Keyboard.JustDown(this.keys.attack);
    if (this.damage.state === 'normal') {
      this.controller.update(direction, jumpPressed, delta / 1000);
      if (direction !== 0) this.combat.facing = direction < 0 ? -1 : 1;
    }
    this.combat.update(delta / 1000);
    if (attackPressed && this.damage.state === 'normal') this.combat.startAttack();
    if (Phaser.Input.Keyboard.JustDown(this.keys.reset)) {
      this.resetPlayer(SPAWN.x, SPAWN.y);
    }
    if (Phaser.Input.Keyboard.JustDown(this.keys.combatTest)) this.resetPlayer(COMBAT_SPAWN.x, COMBAT_SPAWN.y);
    const touchingHazard = this.damage.state !== 'dead'
      && Phaser.Geom.Intersects.RectangleToRectangle(this.combat.hurtbox, this.hazard);
    // One event per entry, including entries rejected during invulnerability.
    if (touchingHazard && !this.hazardContact) this.receiveDamage(COMBAT.hazardDamage, this.hazard.centerX);
    this.hazardContact = touchingHazard;
    for (const target of this.targets) {
      this.combat.hitTarget(target);
      target.view.setVisible(target.health > 0);
      target.label.setText(`${target.health}/${target.maxHealth}`);
    }
    const hitbox = this.combat.attackHitbox;
    this.slash.setVisible(hitbox !== null);
    if (hitbox) this.slash.setPosition(hitbox.x, hitbox.y).setSize(hitbox.width, hitbox.height);
    this.statusText.setText(`HP ${this.damage.health}/${COMBAT.playerHealth} / ${this.damage.state === 'dead' ? 'DEAD - RESPAWNING' : this.damage.state}`);
    if (Phaser.Input.Keyboard.JustDown(this.keys.debug)) {
      this.debugVisible = !this.debugVisible;
      this.debugText.setVisible(this.debugVisible);
      this.combatDebug.setVisible(this.debugVisible);
      this.physics.world.drawDebug = this.debugVisible;
      this.physics.world.debugGraphic.clear().setVisible(this.debugVisible);
    }
    if (this.debugVisible) {
      this.combatDebug.clear().lineStyle(1, 0x55ffff);
      if (this.damage.state !== 'dead') this.combatDebug.strokeRectShape(this.combat.hurtbox);
      if (hitbox) this.combatDebug.lineStyle(1, 0xffff55).strokeRectShape(hitbox);
      this.combatDebug.lineStyle(1, 0xff9955);
      for (const target of this.targets) if (target.health > 0) this.combatDebug.strokeRectShape(target.hurtbox);
      this.debugText.setText([
        'DEVELOPMENT / MOVEMENT',
        `X ${this.player.x.toFixed(1)} Y ${this.player.y.toFixed(1)}`,
        `VX ${body.velocity.x.toFixed(1)} VY ${body.velocity.y.toFixed(1)}`,
        `Ground ${this.controller.grounded}`,
        `Wall L ${this.controller.touchingLeftWall} R ${this.controller.touchingRightWall}`,
        `State ${this.controller.state}`,
        `Attack ${this.combat.phase} Box ${hitbox !== null}`,
        `Hits ${this.combat.hitTargets.size}`,
        `HP ${this.damage.health} Damage ${this.damage.state}`,
        `Invuln ${this.damage.invulnerabilityRemaining.toFixed(2)}`,
      ]);
    }
  }

  private resetPlayer(x: number, y: number): void {
    this.damage.reset();
    this.controller.reset(x, y);
    this.combat.reset();
    this.combat.update(0);
    for (const target of this.targets) target.health = target.maxHealth;
    this.hazardContact = false;
  }

  private receiveDamage(amount: number, sourceX: number): boolean {
    if (!this.damage.receive(amount, sourceX, this.combat.facing)) return false;
    this.controller.interrupt();
    this.combat.interruptAttack();
    return true;
  }
}
