import Phaser from 'phaser';
import { EnemyBody } from './EnemyBody';
import { Projectile } from './Projectile';
import { ENEMY } from './tuning';

const T = ENEMY.ranged;

export class RangedAttacker extends EnemyBody {
  projectiles: Projectile[] = [];
  private remaining = 0;
  constructor(private readonly scene: Phaser.Scene, private readonly terrain: Phaser.Physics.Arcade.StaticGroup,
    x: number, y: number, private readonly usefulBounds: Phaser.Geom.Rectangle) {
    super(scene, terrain, 'Ranged', x, y, 16, 24, T.health, 0x8892c2);
    this.state = 'idle';
  }
  get damage(): number { return T.damage; }

  update(delta: number, player: Phaser.Geom.Rectangle | null): void {
    if (!this.alive(delta)) return;
    this.projectiles = this.projectiles.filter(projectile => !projectile.removed);
    switch (this.state) {
      case 'idle':
        if (player && Math.abs(player.centerX - this.view.x) <= T.detectionRange
          && Math.abs(player.centerY - this.view.y) <= T.verticalDetection) {
          this.facing = player.centerX < this.view.x ? -1 : 1;
          this.state = 'windup'; this.remaining = T.startup; this.telegraph(true);
        }
        break;
      case 'windup':
        this.remaining -= delta;
        if (this.remaining <= 0) {
          if (this.projectiles.length < T.maxProjectiles) {
            this.projectiles.push(new Projectile(this.scene, this.terrain,
              this.view.x + this.facing * (this.hurtbox.width / 2 + 4), this.view.y,
              this.facing, this.usefulBounds));
          }
          this.state = 'recovery'; this.remaining = T.recovery; this.telegraph(false);
        }
        break;
      case 'recovery':
        this.remaining -= delta;
        if (this.remaining <= 0) this.state = 'idle';
        break;
    }
  }

  afterHit(): void {
    super.afterHit();
    if (this.state === 'dead') this.clearProjectiles();
  }

  drawDebug(graphics: Phaser.GameObjects.Graphics, visible: boolean): void {
    super.drawDebug(graphics, visible);
    if (visible) for (const projectile of this.projectiles) {
      if (!projectile.removed) graphics.lineStyle(1, 0xff4466).strokeRectShape(projectile.bounds);
    }
  }

  private clearProjectiles(): void {
    for (const projectile of this.projectiles) projectile.destroy();
    this.projectiles = [];
  }

  destroy(): void { this.clearProjectiles(); super.destroy(); }
}
