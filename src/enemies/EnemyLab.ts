import Phaser from 'phaser';
import { Guard } from './Guard';
import { RangedAttacker } from './RangedAttacker';
import { Pursuer } from './Pursuer';
import { Heavy } from './Heavy';
import type { EnemyBody } from './EnemyBody';
import type { PlayerCombat } from '../combat/PlayerCombat';

export class EnemyLab {
  enemies: EnemyBody[] = [];
  selected: number | null = null;
  readonly bayStarts = [1984, 2464, 2944, 3424, 3904, 4384, 4864];

  constructor(private readonly scene: Phaser.Scene, private readonly terrain: Phaser.Physics.Arcade.StaticGroup) {
    const wall = (x: number, y: number, width: number, height: number): void => {
      terrain.add(scene.add.rectangle(x, y, width, height, 0x536479).setOrigin(0));
    };
    for (const [index, x] of this.bayStarts.entries()) {
      wall(x, 448, 480, 16); wall(x, 0, 16, 540); wall(x + 464, 0, 16, 540);
      const names = ['GUARD', 'RANGED', 'PURSUER', 'HEAVY', 'GUARD + RANGED', 'PURSUER + RANGED', 'GUARD + HEAVY'];
      scene.add.text(x + 32, 380, `${index + 1} ${names[index]} / NUMBER KEY RESETS`, { fontFamily: 'monospace', fontSize: '8px' });
    }
  }

  get spawn(): { x: number; y: number } | null {
    return this.selected === null ? null : { x: this.bayStarts[this.selected] + 64, y: 438 };
  }

  reset(): void {
    for (const enemy of this.enemies) enemy.destroy();
    this.enemies = [];
    // Hand-placed test encounters, not a spawn/wave system or enemy factory.
    switch (this.selected) {
      case 0: this.enemies.push(new Guard(this.scene, this.terrain, 2256, 436)); break;
      case 1: this.enemies.push(new RangedAttacker(this.scene, this.terrain, 2768, 436,
        new Phaser.Geom.Rectangle(2480, 0, 448, 540))); break;
      case 2: this.enemies.push(new Pursuer(this.scene, this.terrain, 3216, 438)); break;
      case 3: this.enemies.push(new Heavy(this.scene, this.terrain, 3696, 432)); break;
      case 4:
        this.enemies.push(new Guard(this.scene, this.terrain, 4128, 436),
          new RangedAttacker(this.scene, this.terrain, 4280, 436, new Phaser.Geom.Rectangle(3920, 0, 448, 540)));
        break;
      case 5:
        this.enemies.push(new Pursuer(this.scene, this.terrain, 4634, 438),
          new RangedAttacker(this.scene, this.terrain, 4760, 436, new Phaser.Geom.Rectangle(4400, 0, 448, 540)));
        break;
      case 6:
        this.enemies.push(new Guard(this.scene, this.terrain, 5088, 436), new Heavy(this.scene, this.terrain, 5204, 432));
        break;
    }
  }

  clear(): void { this.selected = null; this.reset(); }

  update(delta: number, combat: PlayerCombat, playerAlive: boolean,
    receiveDamage: (amount: number, sourceX: number) => boolean): void {
    for (const enemy of this.enemies) {
      // Lethal player hits take priority over that enemy's attack this frame.
      enemy.syncHurtbox();
      if (combat.hitTarget(enemy)) enemy.afterHit();
      enemy.update(delta, playerAlive ? combat.hurtbox : null);
      if (enemy instanceof RangedAttacker) {
        for (const projectile of enemy.projectiles) projectile.update(delta, playerAlive ? combat.hurtbox : null, receiveDamage);
        enemy.projectiles = enemy.projectiles.filter(projectile => !projectile.removed);
      }
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
