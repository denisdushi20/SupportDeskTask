import { isGuidFormat } from './guid';

describe('isGuidFormat', () => {
  it('accepts seeded-style GUIDs that are not RFC version/variant UUIDs', () => {
    expect(isGuidFormat('22222222-2222-2222-2222-222222222222')).toBeTrue();
    expect(isGuidFormat('22222222-2222-2222-2222-222222220020')).toBeTrue();
  });

  it('accepts normal RFC-style UUIDs', () => {
    expect(isGuidFormat('876c0d8b-ba6b-463e-80cf-3fb0c17b5fa7')).toBeTrue();
    expect(isGuidFormat('0e6e8300-2061-46b2-a296-ce30fe72bd97')).toBeTrue();
  });

  it('rejects clearly malformed ids', () => {
    expect(isGuidFormat('')).toBeFalse();
    expect(isGuidFormat('not-a-guid')).toBeFalse();
    expect(isGuidFormat('22222222-2222-2222-2222-22222222222')).toBeFalse(); // too short
    expect(isGuidFormat('22222222-2222-2222-2222-2222222222222')).toBeFalse(); // too long
    expect(isGuidFormat('22222222_2222_2222_2222_222222222222')).toBeFalse();
    expect(isGuidFormat('GGGGGGGG-2222-2222-2222-222222222222')).toBeFalse();
  });
});
