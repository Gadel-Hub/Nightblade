import Phaser from 'phaser';
import { EnemyBody } from './EnemyBody';
import { ENEMY } from './tuning';

const T = ENEMY.pursuer;

export class Pursuer extends EnemyBody {
  private remaining = 0;
  constructor(scene: Phaser.Scene, terrain: Phaser.Physics.Arcade.StaticGroup, x: number, y: number) {
    super(scene, terrain, 'Pursuer', x, y, 12, 20, T.health, 0xb881a0);
    this.state = 'chase';
  }
  get damage(): number { return T.damage; }

  update(delta: number, player: Phaser.Geom.Rectangle | null): void {
    if (!this.alive(delta)) return;
    switch (this.state) {
      case 'chase': {
        this.body.setVelocityX(0);
        if (!player || Math.abs(player.centerX - this.view.x) > T.detectionRange
          || Math.abs(player.centerY - this.view.y) > T.verticalDetection) break;
        const distance = player.centerX - this.view.x;
        this.facing = distance < 0 ? -1 : 1;
        if (Math.abs(distance) <= T.attackRange + (this.hurtbox.width + player.width) / 2) {
          this.state = 'startup'; this.remaining = T.startup; this.telegraph(true);
        } else {
          // Terrain stops this body; it never jumps or searches for a route.
          this.body.setVelocityX(this.facing * T.speed);
        }
        break;
      }
      case 'startup':
        this.remaining -= delta;
        if (this.remaining <= 0) {
          this.state = 'active'; this.remaining = T.active; this.attackSpent = false;
          this.telegraph(false); this.showAttack(T.attackRange, T.attackHeight);
        }
        break;
      case 'active':
        this.remaining -= delta;
        if (this.remaining <= 0) { this.clearAttack(); this.state = 'recovery'; this.remaining = T.recovery; }
        else this.showAttack(T.attackRange, T.attackHeight);
        break;
      case 'recovery':
        this.remaining -= delta;
        if (this.remaining <= 0) this.state = 'chase';
        break;
    }
  }
}
