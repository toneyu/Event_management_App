/// <reference types="vite/client" />

declare module 'node:test' {
  export function describe(name: string, fn: () => void | Promise<void>): void;
  export function it(name: string, fn: () => void | Promise<void>): void;
}

declare module 'node:assert/strict' {
  const assert: {
    equal<T>(actual: T, expected: T, message?: string): void;
    notEqual<T>(actual: T, expected: T, message?: string): void;
    ok(value: unknown, message?: string): void;
    match(string: string, regexp: RegExp, message?: string): void;
  };
  export default assert;
}
