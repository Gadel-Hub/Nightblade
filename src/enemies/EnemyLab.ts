import Phaser from 'phaser';
import { Guard } from './Guard';
import type { EnemyBody } from './EnemyBody';
import type { PlayerCombat } from '../combat/PlayerCombat';

export class EnemyLab {
  enemies: EnemyBody[] = [];
  selected: number | null = null;
  readonly bayStarts = [1984];

  constructor(private readonly scene: Phaser.Scene, private readonly terrain: Phaser.Physics.Arcade.StaticGroup) {
    const wall = (x: number, y: number, width: number, height: number): void => {
      terrain.add(scene.add.rectangle(x, y, width, height, 0x536479).setOrigin(0));
    };
    for (const x of this.bayStarts) {
      wall(x, 448, 480, 16); wall(x, 0, 16, 540); wall(x + 464, 0, 16, 540);
      scene.add.text(x + 32, 380, '1 GUARD / NUMBER KEY RESETS', { fontFamily: 'monospace', fontSize: '8px' });
    }
  }

  get spawn(): { x: number; y: number } | null {
    return this.selected === null ? null : { x: this.bayStarts[this.selected] + 64, y: 438 };
  }

  reset(): void {
    for (const enemy of this.enemies) enemy.destroy();
    this.enemies = [];
    if (this.selected === 0) this.enemies.push(new Guard(this.scene, this.terrain, 2256, 436));
  }

  clear(): void { this.selected = null; this.reset(); }

  update(delta: number, combat: PlayerCombat, playerAlive: boolean,
    receiveDamage: (amount: number, sourceX: number) => boolean): void {
    for (const enemy of this.enemies) {
      // Lethal player hits take priority over that enemy's attack this frame.
      enemy.syncHurtbox();
      if (combat.hitTarget(enemy)) enemy.afterHit();
      enemy.update(delta, playerAlive ? combat.hurtbox : null);
      if (playerAlive && enemy.attackHitbox && !enemy.attackSpent
        && Phaser.Geom.Intersects.RectangleToRectangle(enemy.attackHitbox, combat.hurtbox)) {
        enemy.attackSpent = true; // Rejected invulnerable hits are consumed too.
        receiveDamage(enemy.damage, enemy.view.x);
      }
    }
    this.enemies = this.enemies.filter(enemy => !enemy.removed);
  }

  drawDebug(graphics: Phaser.GameObjects.Graphics, visible: boolean): void {
    for (const enemy of this.enemies) enemy.drawDebug(graphics, visible);
  }
}
