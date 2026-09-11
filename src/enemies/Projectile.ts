import Phaser from 'phaser';
import { ENEMY } from './tuning';

export class Projectile {
  readonly view: Phaser.Physics.Arcade.Image;
  readonly bounds = new Phaser.Geom.Rectangle();
  removed = false;
  private remaining: number = ENEMY.ranged.projectileLifetime;
  private readonly collider: Phaser.Physics.Arcade.Collider;

  constructor(scene: Phaser.Scene, terrain: Phaser.Physics.Arcade.StaticGroup,
    x: number, y: number, direction: -1 | 1, private readonly usefulBounds: Phaser.Geom.Rectangle) {
    this.view = scene.physics.add.image(x, y, 'block').setDisplaySize(6, 4).setTint(0xf0cf70);
    (this.view.body as Phaser.Physics.Arcade.Body).allowGravity = false;
    this.view.setVelocityX(direction * ENEMY.ranged.projectileSpeed);
    (this.view.body as Phaser.Physics.Arcade.Body).updateFromGameObject();
    this.collider = scene.physics.add.collider(this.view, terrain, () => this.destroy());
    this.syncBounds();
  }

  private syncBounds(): void {
    const body = this.view.body as Phaser.Physics.Arcade.Body;
    this.bounds.setTo(body.x, body.y, body.width, body.height);
  }

  update(delta: number, player: Phaser.Geom.Rectangle | null,
    receiveDamage: (amount: number, sourceX: number) => boolean): void {
    if (this.removed) return;
    this.remaining -= delta;
    this.syncBounds();
    if (this.remaining <= 0 || !Phaser.Geom.Intersects.RectangleToRectangle(this.bounds, this.usefulBounds)) {
      this.destroy(); return;
    }
    if (player && Phaser.Geom.Intersects.RectangleToRectangle(this.bounds, player)) {
      // Consume even if the player's hit protection rejects the damage.
      receiveDamage(ENEMY.ranged.damage, this.view.x);
      this.destroy();
    }
  }

  destroy(): void {
    if (this.removed) return;
    this.removed = true;
    this.collider.destroy();
    this.view.destroy();
  }
}
