/** C03-01.M02.04. Presentation only; NEVER a source of JWT, RBAC, tenancy or MFA authority. */
export type MfaLayout = 'default' | 'compact';
export interface LoginDisplayFlags {
  readonly version: 1;
  readonly companyPicker: boolean;
  readonly rememberedCompanyHint: boolean;
  readonly recoveryLink: boolean;
  readonly mfaLayout: MfaLayout;
}
export interface ServerConfirmedLoginFacts {
  /** Requires fresh server-side confirmation. Never derive from localStorage/host/route. */
  readonly authenticated: boolean;
  readonly revoked: boolean;
  readonly activeCompanyCount: number;
  readonly rememberedCompanyAuthorized: boolean;
  readonly mfaChallengeRequired: boolean;
}
export interface LoginDisplayVisibility {
  readonly companyPicker: boolean;
  readonly rememberedCompanyHint: boolean;
  readonly recoveryLink: boolean;
  readonly mfaChallenge: boolean;
  readonly mfaLayout: MfaLayout;
}
export const DEFAULT_LOGIN_DISPLAY_FLAGS: Readonly<LoginDisplayFlags> = Object.freeze({
  version: 1, companyPicker: false, rememberedCompanyHint: false,
  recoveryLink: false, mfaLayout: 'default',
});
/** Version/shape mismatches and privilege-injection fields fail closed. */
export function parseLoginDisplayFlags(raw: unknown): LoginDisplayFlags {
  if (raw === null || typeof raw !== 'object' || Array.isArray(raw)) {
    return DEFAULT_LOGIN_DISPLAY_FLAGS;
  }
  const input = raw as Record<string, unknown>;
  if (input['version'] !== 1) return DEFAULT_LOGIN_DISPLAY_FLAGS;
  return {
    version: 1,
    companyPicker: input['companyPicker'] === true,
    rememberedCompanyHint: input['rememberedCompanyHint'] === true,
    recoveryLink: input['recoveryLink'] === true,
    mfaLayout: input['mfaLayout'] === 'compact' ? 'compact' : 'default',
  };
}
/** UI-only projection; do not call from permission guards, network auth or MFA enforcement. */
export function selectLoginDisplayVisibility(
  rawFlags: unknown, facts: ServerConfirmedLoginFacts,
): LoginDisplayVisibility {
  const flags = parseLoginDisplayFlags(rawFlags);
  const countValid = Number.isSafeInteger(facts.activeCompanyCount)
    && facts.activeCompanyCount >= 0;
  const confirmed = facts.authenticated === true && facts.revoked === false && countValid;
  return {
    companyPicker: confirmed && facts.activeCompanyCount > 1 && flags.companyPicker,
    rememberedCompanyHint: confirmed && facts.activeCompanyCount > 1
      && facts.rememberedCompanyAuthorized === true && flags.rememberedCompanyHint,
    recoveryLink: flags.recoveryLink,
    // The server demands MFA; an untrusted flag can NEVER suppress that challenge.
    mfaChallenge: facts.revoked === false && facts.mfaChallengeRequired === true,
    mfaLayout: flags.mfaLayout,
  };
}
