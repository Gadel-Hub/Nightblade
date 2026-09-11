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
} as const;
