export function setup(): void {}

export function teardown(): void {
  const exitCode = process.exitCode ?? 0;
  setTimeout(() => process.exit(exitCode), 10_000).unref();
}
