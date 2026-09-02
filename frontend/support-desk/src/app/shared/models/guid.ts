/**
 * Accepts any 8-4-4-4-12 hexadecimal GUID string.
 * Does not enforce RFC UUID version/variant nibbles — the API owns ID validity.
 */
const GUID_SYNTAX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function isGuidFormat(value: string): boolean {
  return GUID_SYNTAX.test(value);
}
