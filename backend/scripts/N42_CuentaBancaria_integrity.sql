-- N4.2.C DATA_INTEGRITY — QA_TAKEOVER
-- Fail-closed validation for CuentasBancarias on dev.
-- Read-only with respect to application schema/data: only a session-scoped TEMPORARY table is used.
-- No historical Banco relationship is invented; backfill is N/A unless real legacy data exists.
-- This script does not execute rollback/reconciliation or any Production change.

DROP TEMPORARY TABLE IF EXISTS _vaep_n42_integrity_assert;
CREATE TEMPORARY TABLE _vaep_n42_integrity_assert
(
    CheckName varchar(80) NOT NULL,
    Passed tinyint NOT NULL
);

-- Missing CuentasBancarias/Bancos fails naturally before any PASS can be reported.

-- 1) No Banco orphans.
INSERT INTO _vaep_n42_integrity_assert (CheckName, Passed)
SELECT
    'NO_BANCO_ORPHANS',
    CASE WHEN COUNT(*) = 0 THEN 1 ELSE NULL END
FROM CuentasBancarias cb
LEFT JOIN Bancos b ON b.Id = cb.BancoId
WHERE b.Id IS NULL;

-- 2) Estado must remain inside the persisted domain contract.
INSERT INTO _vaep_n42_integrity_assert (CheckName, Passed)
SELECT
    'ESTADO_IN_1_2',
    CASE WHEN COUNT(*) = 0 THEN 1 ELSE NULL END
FROM CuentasBancarias
WHERE Estado NOT IN (1, 2) OR Estado IS NULL;

-- 3) SaldoInicial cannot be negative or null.
INSERT INTO _vaep_n42_integrity_assert (CheckName, Passed)
SELECT
    'SALDO_INICIAL_NON_NEGATIVE',
    CASE WHEN COUNT(*) = 0 THEN 1 ELSE NULL END
FROM CuentasBancarias
WHERE SaldoInicial < 0 OR SaldoInicial IS NULL;

-- 4) BancoId + NumeroCuenta must be unique and materially populated.
INSERT INTO _vaep_n42_integrity_assert (CheckName, Passed)
SELECT
    'BANCO_NUMERO_UNIQUE',
    CASE WHEN COUNT(*) = 0 THEN 1 ELSE NULL END
FROM
(
    SELECT BancoId, NumeroCuenta
    FROM CuentasBancarias
    GROUP BY BancoId, NumeroCuenta
    HAVING COUNT(*) > 1
) duplicates;

INSERT INTO _vaep_n42_integrity_assert (CheckName, Passed)
SELECT
    'BANCO_NUMERO_REQUIRED',
    CASE WHEN COUNT(*) = 0 THEN 1 ELSE NULL END
FROM CuentasBancarias
WHERE BancoId <= 0
   OR NumeroCuenta IS NULL
   OR TRIM(NumeroCuenta) = '';

SELECT CheckName, Passed
FROM _vaep_n42_integrity_assert
ORDER BY CheckName;

DROP TEMPORARY TABLE _vaep_n42_integrity_assert;

-- Explicit reconciliation/rollback policy:
-- * No destructive SQL is executed by this gate.
-- * If invalid legacy rows are detected, remediation must be based on verified business history.
-- * Never guess a BancoId, delete duplicates, or rewrite balances automatically.
-- * Any future repair must be a separately reviewed dev-only migration/runbook.
