import Phaser from 'phaser';
import { EnemyBody } from './EnemyBody';
import { ENEMY } from './tuning';

const T = ENEMY.guard;

export class Guard extends EnemyBody {
  private remaining = 0;
  constructor(scene: Phaser.Scene, terrain: Phaser.Physics.Arcade.StaticGroup, private readonly homeX: number, y: number) {
    super(scene, terrain, 'Guard', homeX, y, 14, 24, T.health, 0x739f9c);
  }
  get damage(): number { return T.damage; }

  update(delta: number, player: Phaser.Geom.Rectangle | null): void {
    if (!this.alive(delta)) return;
    switch (this.state) {
      case 'patrol': {
        if (player && Math.abs(player.centerX - this.view.x) <= T.detectionRange
          && Math.abs(player.centerY - this.view.y) <= T.verticalDetection) {
          this.facing = player.centerX < this.view.x ? -1 : 1;
          this.body.setVelocityX(0);
          this.state = 'startup'; this.remaining = T.startup; this.telegraph(true);
          break;
        }
        if (this.view.x <= this.homeX - T.patrolDistance || this.body.blocked.left) this.facing = 1;
        if (this.view.x >= this.homeX + T.patrolDistance || this.body.blocked.right) this.facing = -1;
        this.body.setVelocityX(this.facing * T.speed);
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
        if (this.remaining <= 0) this.state = 'patrol';
        break;
    }
  }
}
