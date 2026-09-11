// Run-local respawn position only; restarting a level constructs a fresh instance.
export class Checkpoint {
  active = false;
  constructor(readonly start: { x: number; y: number }, readonly destination: { x: number; y: number }) {}
  get spawn(): { x: number; y: number } { return this.active ? this.destination : this.start; }
  activate(): boolean {
    if (this.active) return false;
    this.active = true;
    return true;
  }
}
