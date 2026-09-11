import Phaser from 'phaser';
import { DevelopmentScene } from './scenes/DevelopmentScene';
import './style.css';

function displayZoom(): number {
  return Math.max(1, Math.floor(Math.min(window.innerWidth / 320, (window.innerHeight - 36) / 180)));
}

const game = new Phaser.Game({
  type: Phaser.AUTO,
  parent: 'game',
  width: 320,
  height: 180,
  backgroundColor: '#171e2b',
  pixelArt: true,
  antialias: false,
  roundPixels: true,
  scale: { mode: Phaser.Scale.NONE, zoom: displayZoom() },
  physics: { default: 'arcade', arcade: { gravity: { x: 0, y: 600 } } },
  scene: [DevelopmentScene],
});

// Whole-number CSS scaling keeps every internal pixel the same size.
function resize(): void {
  if (game.isBooted) game.scale.setZoom(displayZoom());
}
window.addEventListener('resize', resize);
