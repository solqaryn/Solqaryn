# C14-01.M05.01 — V22 prueba unitaria de reglas de identidad tenant A/B

## Requisito y riesgo
PLAN_MAESTRO D2042: ejecutar prueba unitaria o de regla con resultado reproducible. Riesgo R2 por branding multiempresa, manejo de respuestas asíncronas y datos privados. La meta de M05 en PLAN E es acumulativa: este ID no certifica la aceptación completa de la fase.

## Cambio ejecutable
Se añaden siete casos al spec Angular existente `frontend/src/app/services/empresa-identidad.c14-01-m04-03.spec.ts` sobre `EmpresaIdentidadService` SIN cambiar código de producción, API, DB ni UI. Se reutiliza el workflow `.github/workflows/v19-c14-01-m04-03-branding.yml`, que ejecuta la suite con Angular/Node en GitHub-hosted más lint y build real. Control de concurrencia por SHA; no cancela otra corrida.

## A0–A4, positivos, negativos y límites
- A0: revisión del diff test-only, sin cambio de API, contratos ni migraciones. El aislamiento real continúa siendo responsabilidad del backend.
- A1: ID correcto, marca y logo privados A/B, limpieza inmediata al cambiar de tenant, reintento tras 403, inválidos/fraccionarios/no finitos, carrera pública y respuestas obsoletas, nombre HTML malicioso.
- A2: los dobles de proveedor prueban rechazo de DTO con `empresaId` cruzado y no filtración de campos fiscales/correo; no se declaran pruebas reales de JWT/RBAC, permisos ni SQL en esta unidad.
- A3: NO_APLICA HTTP firmado, Oracle EF/MySQL 8.4 y Chromium a este test-only D2042; no cambió runtime. El proveedor está simulado en el test y se registra como LIMITACION de fase, nunca como integración real.
- A4: exigir en GitHub Actions exact-SHA suite Angular, lint/build, `verify-solqaryn-project-scope`, V5 procedencia y checks requeridos completados SUCCESS. SKIPPED/QUEUED/IN_PROGRESS/FAIL no son PASS.
- A5: revisión autónoma independiente de otra Task Javier distinta del autor/reparador, posterior al cierre A0–A4; conservar FAIL histórico.

## Rollback y seguridad
Revertir solo este changeset mediante commit nuevo fast-forward; no reset/force-push ni cambios en BITACORA histórica. No permisos R3 ni QA/main/PROD. Sin datos productivos, secretos ni pagos.

## Reparación CI P1 — sintaxis TypeScript de los dobles
El job GitHub-hosted V19 `136725493019` en SHA `30583ae730f7f6f359171f0118ac4fd53b4d1893` concluyó FAILURE: TS1434/TS2304/TS2693 en la declaración `const provider` de la primera prueba nueva, porque la aserción de tipo quedó en una nueva línea después del literal. Se une el `as unknown as EmpresaConfiguracionService` a la misma expresión; no se cambian asserts, casos, runtime ni autorización. Reejecutar los siete tests y regresiones Angular por el nuevo SHA; el fallo histórico se conserva, ningún PASS se anticipa.
