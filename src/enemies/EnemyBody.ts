import Phaser from 'phaser';
import { ENEMY } from './tuning';

// Shared physical/combat bookkeeping only. Each subtype owns its decisions.
export abstract class EnemyBody {
  readonly view: Phaser.Physics.Arcade.Image;
  readonly hurtbox = new Phaser.Geom.Rectangle();
  readonly debugLabel: Phaser.GameObjects.Text;
  readonly attackView: Phaser.GameObjects.Rectangle;
  private readonly facingMark: Phaser.GameObjects.Rectangle;
  attackHitbox: Phaser.Geom.Rectangle | null = null;
  attackSpent = false;
  facing: -1 | 1 = -1;
  state = 'patrol';
  removed = false;
  private deathRemaining = ENEMY.deathDuration as number;
  private readonly collider: Phaser.Physics.Arcade.Collider;

  constructor(scene: Phaser.Scene, terrain: Phaser.Physics.Arcade.StaticGroup,
    readonly name: string, x: number, y: number, width: number, height: number,
    public health: number, private readonly color: number) {
    this.view = scene.physics.add.image(x, y, 'block').setDisplaySize(width, height).setTint(color);
    this.collider = scene.physics.add.collider(this.view, terrain);
    this.debugLabel = scene.add.text(x, y - height, '', { fontFamily: 'monospace', fontSize: '8px', backgroundColor: '#11151f' })
      .setDepth(100).setVisible(false);
    this.attackView = scene.add.rectangle(0, 0, 1, 1, 0xce6678).setOrigin(0).setVisible(false);
    this.facingMark = scene.add.rectangle(x, y, 3, 3, 0xe9e6ce);
    this.body.updateFromGameObject();
    this.syncHurtbox();
  }

  get body(): Phaser.Physics.Arcade.Body { return this.view.body as Phaser.Physics.Arcade.Body; }
  abstract update(delta: number, player: Phaser.Geom.Rectangle | null): void;
  abstract get damage(): number;

  // Visual-only replacement for the art laboratory; physics is unchanged.
  setPlaceholderVisible(visible: boolean): void {
    this.view.setAlpha(visible ? 1 : 0);
    this.attackView.setAlpha(visible ? 1 : 0);
    this.facingMark.setAlpha(visible ? 1 : 0);
  }

  syncHurtbox(): void {
    if (this.removed) return;
    this.hurtbox.setTo(this.body.x, this.body.y, this.body.width, this.body.height);
    this.facingMark.setPosition(this.view.x + this.facing * (this.body.width / 2 - 2), this.view.y - 4);
  }

  afterHit(): void {
    if (this.health > 0 || this.state === 'dead') return;
    this.state = 'dead';
    this.body.setVelocity(0, 0);
    this.body.enable = false;
    this.clearAttack();
    this.view.setTint(0x424b58);
    this.facingMark.setVisible(false);
  }

  protected alive(delta: number): boolean {
    if (this.removed) return false;
    this.afterHit();
    if (this.state !== 'dead') { this.syncHurtbox(); return true; }
    this.deathRemaining -= delta;
    if (this.deathRemaining <= 0) this.destroy();
    return false;
  }

  protected showAttack(range: number, height: number): void {
    this.attackHitbox ??= new Phaser.Geom.Rectangle();
    this.attackHitbox.setTo(this.facing === 1 ? this.hurtbox.right : this.hurtbox.left - range,
      this.hurtbox.centerY - height / 2, range, height);
    this.attackView.setPosition(this.attackHitbox.x, this.attackHitbox.y).setSize(range, height).setVisible(true);
  }

  protected clearAttack(): void { this.attackHitbox = null; this.attackView.setVisible(false); }
  protected telegraph(on: boolean): void { this.view.setTint(on ? 0xf0cf70 : this.color); }

  drawDebug(graphics: Phaser.GameObjects.Graphics, visible: boolean): void {
    if (this.removed) return;
    this.debugLabel.setVisible(visible).setPosition(this.view.x - 30, this.hurtbox.top - 20)
      .setText(`${this.name} ${this.health}\n${this.state}`);
    if (!visible || this.state === 'dead') return;
    graphics.lineStyle(1, 0xff9955).strokeRectShape(this.hurtbox);
    if (this.attackHitbox) graphics.lineStyle(1, 0xff4466).strokeRectShape(this.attackHitbox);
  }

  destroy(): void {
    if (this.removed) return;
    this.removed = true;
    this.attackHitbox = null;
    this.collider.destroy();
    this.view.destroy();
    this.attackView.destroy();
    this.facingMark.destroy();
    this.debugLabel.destroy();
  }
}
