// Hand-authored internal pixels. Solids and damage rectangles never come from art.
export const LEVEL = {
  width: 6144, height: 360, floor: 304,
  start: { x: 48, y: 294 }, checkpoint: { x: 3008, y: 294 },
  exit: { x: 6048, y: 256, width: 32, height: 48 },
} as const;
export const SOLIDS: readonly (readonly [number, number, number, number])[] = [
  [0,304,6144,56], [0,0,16,304], [6128,0,16,304],
  // Opening: low plates, then an optional upper rhythm above a safe floor.
  [192,288,64,16], [352,272,64,32], [480,256,64,48],
  // First Guard has an uninterrupted broad floor; hazard follows alone.
  [1360,272,48,32], [1568,272,48,32],
  // Enter under the left wall, alternate inside the 48px shaft, exit right.
  [1984,144,16,112], [2048,176,16,128],
  [2160,208,64,16], [2304,240,64,16], [2480,272,64,32],
  // Quiet checkpoint approach; no enemy detection reaches the respawn point.
  [2752,272,64,32],
  // Combined encounter: projectile cover, elevated firing position.
  [3280,272,48,32], [3472,256,128,48], [3776,272,48,32],
  [4064,272,64,32], [4288,256,128,48],
  // Climax shaft repeats the learned wall rhythm without a new mechanic.
  [4800,144,16,112], [4864,176,16,128],
  [4976,208,64,16], [5120,240,64,16],
  [5456,272,48,32], [5648,256,128,48],
];
export const HAZARDS: readonly (readonly [number, number, number])[] = [
  [1248,288,32], [1488,288,32], [1728,288,48],
  [2384,288,32], [3632,288,32], [3952,288,48],
  [4528,288,48], [5232,288,48], [5536,288,32],
];
export const GUARDS = [816, 1120, 2656, 3360, 3888, 4640, 5360, 5904] as const;
export const RANGED: readonly (readonly [number, number])[] = [[3552,244],[4368,244],[5728,244]];
export const PURSUERS = [4160, 5328] as const;
