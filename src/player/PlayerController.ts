import type Phaser from 'phaser';
import { MOVEMENT } from './tuning';

export class PlayerController {
  private lastJumpWall: -1 | 0 | 1 = 0;
  private pushDirection = 0;
  private pushRemaining = 0;
  private sliding = false;
  constructor(private readonly player: Phaser.Physics.Arcade.Image) {}

  get body(): Phaser.Physics.Arcade.Body {
    return this.player.body as Phaser.Physics.Arcade.Body;
  }

  get grounded(): boolean { return this.body.blocked.down; }
  get touchingLeftWall(): boolean { return this.body.blocked.left; }
  get touchingRightWall(): boolean { return this.body.blocked.right; }

  get state(): string {
    if (this.grounded) return this.body.velocity.x === 0 ? 'idle' : 'run';
    if (this.pushRemaining > 0) return 'wall-jump';
    if (this.sliding) return 'wall-slide';
    return this.body.velocity.y < 0 ? 'jump' : 'fall';
  }

  update(direction: number, jumpPressed: boolean, deltaSeconds: number): void {
    const wall = this.touchingLeftWall ? -1 : this.touchingRightWall ? 1 : 0;
    this.pushRemaining = Math.max(0, this.pushRemaining - deltaSeconds);
    if (this.grounded) {
      this.lastJumpWall = 0;
      this.pushRemaining = 0;
    } else if (wall !== 0 && wall === this.pushDirection) {
      // Reaching the opposite wall ends the push even in a very narrow shaft.
      this.pushRemaining = 0;
    }

    this.sliding = !this.grounded && wall !== 0 && direction === wall
      && this.body.velocity.y >= 0 && this.pushRemaining === 0;
    // Clamp in physics as well as here so gravity cannot exceed the slide cap.
    this.body.maxVelocity.y = this.sliding ? MOVEMENT.wallSlideMaxDownwardSpeed : Infinity;
    if (this.sliding) {
      this.player.setVelocityY(Math.min(this.body.velocity.y, MOVEMENT.wallSlideMaxDownwardSpeed));
    }

    if (jumpPressed) {
      if (this.grounded) {
        this.player.setVelocityY(MOVEMENT.jumpVelocity);
      } else if (wall !== 0 && wall !== this.lastJumpWall) {
        this.lastJumpWall = wall;
        this.pushDirection = -wall;
        this.pushRemaining = MOVEMENT.wallJumpPushDuration;
        this.sliding = false;
        this.body.maxVelocity.y = Infinity;
        this.player.setVelocityY(MOVEMENT.wallJumpVerticalVelocity);
      }
    }
    this.player.setVelocityX(this.pushRemaining > 0
      ? this.pushDirection * MOVEMENT.wallJumpHorizontalVelocity
      : direction * MOVEMENT.maxHorizontalSpeed);
  }

  reset(x: number, y: number): void {
    this.body.reset(x, y);
    this.lastJumpWall = 0;
    this.pushRemaining = 0;
    this.pushDirection = 0;
    this.sliding = false;
    this.body.maxVelocity.y = Infinity;
  }
}
