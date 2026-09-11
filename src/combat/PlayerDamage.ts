import type Phaser from 'phaser';
import { COMBAT } from './tuning';

export class PlayerDamage {
  health: number = COMBAT.playerHealth;
  state: 'normal' | 'hit' | 'dead' = 'normal';
  invulnerabilityRemaining = 0;
  private stateRemaining = 0;

  constructor(private readonly player: Phaser.Physics.Arcade.Image) {}

  get readyToRespawn(): boolean { return this.state === 'dead' && this.stateRemaining === 0; }

  update(delta: number): void {
    this.invulnerabilityRemaining = Math.max(0, this.invulnerabilityRemaining - delta);
    this.stateRemaining = Math.max(0, this.stateRemaining - delta);
    if (this.state === 'hit' && this.stateRemaining === 0) this.state = 'normal';
  }

  receive(amount: number, sourceX: number, facing: -1 | 1): boolean {
    if (this.state !== 'normal' || this.invulnerabilityRemaining > 0 || amount <= 0) return false;
    this.health = Math.max(0, this.health - amount);
    const body = this.player.body as Phaser.Physics.Arcade.Body;
    body.maxVelocity.y = Infinity;
    if (this.health === 0) {
      this.state = 'dead';
      this.stateRemaining = COMBAT.respawnDelay;
      body.setVelocity(0, 0);
      body.enable = false;
      this.player.setVisible(false);
    } else {
      this.state = 'hit';
      this.stateRemaining = COMBAT.hitReactionDuration;
      this.invulnerabilityRemaining = COMBAT.postHitInvulnerability;
      const away = this.player.x === sourceX ? -facing : Math.sign(this.player.x - sourceX);
      body.setVelocity(away * COMBAT.receivedKnockbackHorizontal, COMBAT.receivedKnockbackVertical);
    }
    return true;
  }

  reset(): void {
    this.health = COMBAT.playerHealth;
    this.state = 'normal';
    this.stateRemaining = 0;
    this.invulnerabilityRemaining = 0;
    (this.player.body as Phaser.Physics.Arcade.Body).enable = true;
    this.player.setVisible(true);
  }
}
