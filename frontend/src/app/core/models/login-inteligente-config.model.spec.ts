import { describe, it, expect } from 'vitest';
import {
  DEFAULT_LOGIN_DISPLAY_FLAGS, parseLoginDisplayFlags, selectLoginDisplayVisibility,
  type ServerConfirmedLoginFacts,
} from './login-inteligente-config.model';
const flags = { version: 1, companyPicker: true, rememberedCompanyHint: true,
  recoveryLink: true, mfaLayout: 'compact' };
const facts = (delta: Partial<ServerConfirmedLoginFacts> = {}): ServerConfirmedLoginFacts => ({
  authenticated: true, revoked: false, activeCompanyCount: 2,
  rememberedCompanyAuthorized: true, mfaChallengeRequired: false, ...delta,
});
describe('C03-01.M02.04 typed presentation contract (not real HTTP/JWT)', () => {
  it('fails closed for null, array, wrong version, absent version', () => {
    for (const x of [null, [], {version: 2}, {companyPicker: true}, 'true']) {
      expect(parseLoginDisplayFlags(x)).toEqual(DEFAULT_LOGIN_DISPLAY_FLAGS);
    }
  });
  it('refuses truthy strings, numbers, object flags and invalid MFA layout', () => {
    expect(parseLoginDisplayFlags({version: 1, companyPicker: 'true',
      rememberedCompanyHint: 1, recoveryLink: {yes:true}, mfaLayout:'off'}))
      .toEqual(DEFAULT_LOGIN_DISPLAY_FLAGS);
  });
  it('ignores forged privilege fields and keeps exactly five UI fields', () => {
    expect(parseLoginDisplayFlags({...flags, isPlatformAdmin: true, tenantId: 'B',
      bypassMfa: true, jwt: 'forged', rol: 'SuperAdmin'})).toEqual(flags);
  });
  it('zero memberships never select enterprise', () => {
    expect(selectLoginDisplayVisibility(flags, facts({activeCompanyCount: 0})))
      .toMatchObject({companyPicker:false, rememberedCompanyHint:false});
  });
  it('one valid company avoids a selector (backend still verifies context)', () => {
    expect(selectLoginDisplayVisibility(flags, facts({activeCompanyCount: 1})))
      .toMatchObject({companyPicker:false, rememberedCompanyHint:false});
  });
  it('two valid companies may show a visual selector', () => {
    expect(selectLoginDisplayVisibility(flags, facts()))
      .toMatchObject({companyPicker:true, rememberedCompanyHint:true});
  });
  it('invalid remembered-company hint is ignored even with N memberships', () => {
    expect(selectLoginDisplayVisibility(flags, facts({rememberedCompanyAuthorized:false})))
      .toMatchObject({companyPicker:true, rememberedCompanyHint:false});
  });
  it('revocation immediately clears tenant UI hints', () => {
    expect(selectLoginDisplayVisibility(flags, facts({revoked:true})))
      .toMatchObject({companyPicker:false, rememberedCompanyHint:false});
  });
  it('unauthenticated visitor cannot show tenant controls', () => {
    expect(selectLoginDisplayVisibility(flags, facts({authenticated:false})))
      .toMatchObject({companyPicker:false, rememberedCompanyHint:false});
  });
  it('negative, fractional, NaN and infinite counts are fail-closed', () => {
    for (const count of [-1, 1.3, Number.NaN, Number.POSITIVE_INFINITY]) {
      expect(selectLoginDisplayVisibility(flags, facts({activeCompanyCount:count})))
        .toMatchObject({companyPicker:false, rememberedCompanyHint:false});
    }
  });
  it('injected bypassMfa cannot hide real server-required MFA', () => {
    expect(selectLoginDisplayVisibility({version:1,bypassMfa:true},facts({mfaChallengeRequired:true})))
      .toMatchObject({mfaChallenge:true,mfaLayout:'default'});
  });
  it('revoked session never reuses an obsolete MFA challenge', () => {
    expect(selectLoginDisplayVisibility(flags,facts({revoked:true,mfaChallengeRequired:true})))
      .toMatchObject({mfaChallenge:false});
  });
  it('recovery link remains UI-only and cannot grant a tenant', () => {
    expect(selectLoginDisplayVisibility(flags,facts({authenticated:false,activeCompanyCount:0})))
      .toMatchObject({recoveryLink:true,companyPicker:false});
  });
});
