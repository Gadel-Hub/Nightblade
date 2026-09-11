// Sheet registration lives here so replacement artwork doesn't alter gameplay.
export const PLAYER_FRAME = { idle: 0, run: 1, jump: 3, fall: 4, wall: 5, startup: 6, slash: 7, hurt: 8 } as const;
export const GUARD_FRAME = { idle: 0, walk: 1, startup: 3, attack: 4, dead: 5 } as const;
export const TILE = { top: 0, fill: 1, left: 2, right: 3, wall: 4, background: 5 } as const;
export const HEALTH = { full: 0, empty: 1, normal: 2, hit: 3, dead: 4 } as const;
