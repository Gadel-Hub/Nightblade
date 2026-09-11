import Phaser from 'phaser';
import { EnemyBody } from './EnemyBody';
import { ENEMY } from './tuning';

const T = ENEMY.heavy;

export class Heavy extends EnemyBody {
  private remaining = 0;
  constructor(scene: Phaser.Scene, terrain: Phaser.Physics.Arcade.StaticGroup, private readonly homeX: number, y: number) {
    super(scene, terrain, 'Heavy', homeX, y, 24, 32, T.health, 0xaa8967);
    this.state = 'approach';
  }
  get damage(): number { return T.damage; }

  update(delta: number, player: Phaser.Geom.Rectangle | null): void {
    if (!this.alive(delta)) return;
    switch (this.state) {
      case 'approach': {
        this.body.setVelocityX(0);
        if (!player || Math.abs(player.centerX - this.view.x) > T.detectionRange
          || Math.abs(player.centerY - this.view.y) > T.verticalDetection) break;
        const distance = player.centerX - this.view.x;
        this.facing = distance < 0 ? -1 : 1;
        if (Math.abs(distance) <= T.attackRange + (this.hurtbox.width + player.width) / 2) {
          this.state = 'startup'; this.remaining = T.startup; this.telegraph(true);
        } else if ((this.facing === -1 && this.view.x > this.homeX - T.roamDistance)
          || (this.facing === 1 && this.view.x < this.homeX + T.roamDistance)) {
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
        if (this.remaining <= 0) this.state = 'approach';
        break;
    }
  }
}
