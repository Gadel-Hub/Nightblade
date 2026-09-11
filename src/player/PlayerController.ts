import type Phaser from 'phaser';
import { MOVEMENT } from './tuning';

export class PlayerController {
  constructor(private readonly player: Phaser.Physics.Arcade.Image) {}

  get body(): Phaser.Physics.Arcade.Body {
    return this.player.body as Phaser.Physics.Arcade.Body;
  }

  get grounded(): boolean { return this.body.blocked.down; }
  get touchingLeftWall(): boolean { return this.body.blocked.left; }
  get touchingRightWall(): boolean { return this.body.blocked.right; }

  get state(): string {
    if (this.grounded) return this.body.velocity.x === 0 ? 'idle' : 'run';
    return this.body.velocity.y < 0 ? 'jump' : 'fall';
  }

  update(direction: number, jumpPressed: boolean): void {
    this.player.setVelocityX(direction * MOVEMENT.maxHorizontalSpeed);
    if (jumpPressed && this.grounded) this.player.setVelocityY(MOVEMENT.jumpVelocity);
  }

  reset(x: number, y: number): void {
    this.body.reset(x, y);
  }
}
