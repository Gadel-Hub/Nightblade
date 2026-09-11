import Phaser from 'phaser';
import { COMBAT } from './tuning';

export type AttackPhase = 'idle' | 'startup' | 'active' | 'recovery';

export class PlayerCombat {
  phase: AttackPhase = 'idle';
  facing: -1 | 1 = 1;
  private swingFacing: -1 | 1 = 1;
  private elapsed = 0;
  readonly hitTargets = new Set<object>();
  readonly hurtbox = new Phaser.Geom.Rectangle();
  attackHitbox: Phaser.Geom.Rectangle | null = null;

  constructor(private readonly player: Phaser.Physics.Arcade.Image) {}

  startAttack(): boolean {
    if (this.phase !== 'idle') return false;
    this.phase = 'startup';
    this.elapsed = 0;
    this.swingFacing = this.facing;
    this.hitTargets.clear();
    return true;
  }

  update(delta: number): void {
    if (this.phase !== 'idle') {
      this.elapsed += delta;
      const activeEnd = COMBAT.startupDuration + COMBAT.activeDuration;
      if (this.elapsed >= activeEnd + COMBAT.recoveryDuration) this.phase = 'idle';
      else if (this.elapsed >= activeEnd) this.phase = 'recovery';
      else if (this.elapsed >= COMBAT.startupDuration) this.phase = 'active';
    }
    const body = this.player.body as Phaser.Physics.Arcade.Body;
    // A distinct combat rectangle; terrain collisions remain Arcade's job.
    this.hurtbox.setTo(body.x, body.y, body.width, body.height);
    if (this.phase === 'active') {
      this.attackHitbox ??= new Phaser.Geom.Rectangle();
      this.attackHitbox.setTo(
        this.swingFacing === 1 ? this.hurtbox.right + COMBAT.attackRange - COMBAT.attackHitboxWidth
          : this.hurtbox.left - COMBAT.attackRange,
        this.hurtbox.centerY - COMBAT.attackHitboxHeight / 2,
        COMBAT.attackHitboxWidth, COMBAT.attackHitboxHeight,
      );
    } else this.attackHitbox = null;
  }

  hitTarget(target: { health: number; hurtbox: Phaser.Geom.Rectangle }): boolean {
    if (!this.attackHitbox || target.health <= 0 || this.hitTargets.has(target)
      || !Phaser.Geom.Intersects.RectangleToRectangle(this.attackHitbox, target.hurtbox)) return false;
    target.health = Math.max(0, target.health - COMBAT.attackDamage);
    this.hitTargets.add(target);
    return true;
  }

  reset(): void {
    this.interruptAttack();
    this.facing = 1;
  }

  // Forced interruption by damage/death, never a player-controlled cancel.
  interruptAttack(): void {
    this.phase = 'idle';
    this.elapsed = 0;
    this.hitTargets.clear();
    this.attackHitbox = null;
  }
}
