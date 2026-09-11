import Phaser from 'phaser';

// Temporary foundation controls; the full controller belongs to Milestone 2.
const PLACEHOLDER = { speed: 100, jumpVelocity: -260, spawnX: 48, spawnY: 280 };
const WORLD = { width: 1920, height: 540 };

export class DevelopmentScene extends Phaser.Scene {
  private player!: Phaser.Physics.Arcade.Image;
  private cursors!: Phaser.Types.Input.Keyboard.CursorKeys;
  private keys!: Record<'left' | 'right' | 'jump' | 'reset' | 'debug', Phaser.Input.Keyboard.Key>;
  private debugText!: Phaser.GameObjects.Text;
  private debugVisible = false;

  constructor() { super('development'); }

  create(): void {
    const graphics = this.make.graphics({ x: 0, y: 0 });
    graphics.fillStyle(0xffffff).fillRect(0, 0, 1, 1);
    graphics.generateTexture('block', 1, 1);
    graphics.destroy();

    this.physics.world.setBounds(0, 0, WORLD.width, WORLD.height);
    const terrain = this.physics.add.staticGroup();
    const block = (x: number, y: number, width: number, height: number): void => {
      const tile = this.add.rectangle(x, y, width, height, 0x536479).setOrigin(0);
      terrain.add(tile);
    };

    block(0, 320, 1280, 32); // Long flat floor.
    block(0, 0, 16, 540); // Left boundary wall.
    block(1904, 0, 16, 540); // Right boundary wall.
    block(0, 0, 1920, 16); // World ceiling.
    block(0, 524, 1920, 16); // Lower safety floor beneath the drop.
    block(200, 288, 64, 16);
    block(300, 256, 64, 16);
    block(400, 224, 80, 16);
    block(560, 272, 176, 16); // Low ceiling: 32-pixel corridor.
    block(800, 160, 16, 112); // Wall-jump shaft, entered underneath.
    block(848, 128, 16, 144);
    block(784, 112, 96, 16); // Shaft ceiling.
    block(928, 256, 64, 16);
    block(1040, 208, 80, 16);
    block(1168, 272, 64, 16);
    block(1440, 448, 464, 16); // Reserved combat floor, after drop.

    const label = (x: number, y: number, text: string): void => {
      this.add.text(x, y, text, { fontFamily: 'monospace', fontSize: '8px', color: '#aebcd0' });
    };
    label(32, 300, 'FLAT / START');
    label(208, 204, 'HEIGHTS');
    label(560, 250, 'CORRIDOR');
    label(776, 92, 'WALL TEST');
    label(1248, 294, 'DROP >');
    this.add.rectangle(1664, 408, 416, 80).setStrokeStyle(1, 0x809070);
    label(1496, 380, 'RESERVED COMBAT AREA');

    this.player = this.physics.add.image(PLACEHOLDER.spawnX, PLACEHOLDER.spawnY, 'block');
    this.player.setDisplaySize(12, 20).setTint(0xe6c66a);
    this.player.setCollideWorldBounds(true);
    this.physics.add.collider(this.player, terrain);
    this.cameras.main.setBounds(0, 0, WORLD.width, WORLD.height);
    this.cameras.main.startFollow(this.player, true);

    const keyboard = this.input.keyboard!;
    this.cursors = keyboard.createCursorKeys();
    this.keys = keyboard.addKeys({ left: 'A', right: 'D', jump: 'SPACE', reset: 'R', debug: 'F1' }) as typeof this.keys;
    this.physics.world.createDebugGraphic();
    this.physics.world.drawDebug = false;
    this.physics.world.debugGraphic.setVisible(false);
    this.debugText = this.add.text(4, 4, '', {
      fontFamily: 'monospace', fontSize: '8px', color: '#ffffff', backgroundColor: '#11151f', padding: { x: 3, y: 3 },
    }).setScrollFactor(0).setDepth(100).setVisible(false);
  }

  update(): void {
    const body = this.player.body as Phaser.Physics.Arcade.Body;
    const direction = Number(this.cursors.right.isDown || this.keys.right.isDown)
      - Number(this.cursors.left.isDown || this.keys.left.isDown);
    this.player.setVelocityX(direction * PLACEHOLDER.speed);
    if (Phaser.Input.Keyboard.JustDown(this.keys.jump) && body.blocked.down) {
      this.player.setVelocityY(PLACEHOLDER.jumpVelocity);
    }
    if (Phaser.Input.Keyboard.JustDown(this.keys.reset)) {
      body.reset(PLACEHOLDER.spawnX, PLACEHOLDER.spawnY);
    }
    if (Phaser.Input.Keyboard.JustDown(this.keys.debug)) {
      this.debugVisible = !this.debugVisible;
      this.debugText.setVisible(this.debugVisible);
      this.physics.world.drawDebug = this.debugVisible;
      this.physics.world.debugGraphic.clear().setVisible(this.debugVisible);
    }
    if (this.debugVisible) {
      this.debugText.setText([
        'DEVELOPMENT / PLACEHOLDER',
        `X ${body.x.toFixed(1)} Y ${body.y.toFixed(1)}`,
        `VX ${body.velocity.x.toFixed(1)} VY ${body.velocity.y.toFixed(1)}`,
        `Ground ${body.blocked.down} L ${body.blocked.left} R ${body.blocked.right}`,
        `State ${body.blocked.down ? (direction ? 'run' : 'idle') : 'air'}`,
      ]);
    }
  }
}
