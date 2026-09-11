// Internal pixels and seconds. Enemy damage uses the accepted player knockback.
export const ENEMY = {
  deathDuration: 0.18,
  guard: {
    health: 3, speed: 30, patrolDistance: 56, detectionRange: 48,
    verticalDetection: 24, attackRange: 28, attackHeight: 20,
    startup: 0.35, active: 0.12, recovery: 0.55, damage: 1,
  },
  ranged: {
    health: 2, detectionRange: 260, verticalDetection: 32,
    startup: 0.55, recovery: 1.1, damage: 1,
    projectileSpeed: 120, projectileLifetime: 2.5, maxProjectiles: 2,
  },
  pursuer: {
    health: 1, speed: 92, detectionRange: 240, verticalDetection: 48,
    attackRange: 16, attackHeight: 18, startup: 0.12, active: 0.10, recovery: 0.24, damage: 1,
  },
  heavy: {
    health: 6, speed: 20, detectionRange: 140, verticalDetection: 40, roamDistance: 80,
    attackRange: 52, attackHeight: 30, startup: 0.8, active: 0.22, recovery: 0.95, damage: 2,
  },
} as const;
