# P0-008.B — Certificación negativa de metadatos de secretos DEV

**Autoridad:** Plan Comercial Inicial Unificado / PLAN_MAESTRO y COLA fila 17, DEV solo, `PROJECT_SCOPE_LOCK=STRICT`. Dependencias verificadas al reclamar: P0-008.A LISTO y P0-012.B LISTO.

**Selector WIP:** primer scope de esta Task P0-004.B ya VALIDANDO con PR #3799 y CI 3/3, sin edición activa; no existen scopes VALIDANDO ajenos para REVIEW_FIRST. Se ocupan dos scopes distintos, el máximo permitido para esta Task. Rama exclusiva `work/solqaryn-p0-008-b`; no se edita la rama #3799.

## Contrato negativo

- Reutiliza el snapshot histórico **metadata-only** `docs/baselines/P0-008-A-DEV-METADATA-20261029.json` y el verificador `scripts/verify-p0-008-a-secret-metadata.mjs --self-test`. La evidencia base está vinculada a P0-008.A PR #3765 fusionado: no sustituye comprobación actual de Render, Vercel ni Aiven.
- La validación heredada comprueba estructura y 12 mutaciones negativas; esta entrega añade 10 mutaciones sintéticas: campos `value`, `password`, `connectionString`, `accessToken`, y valores ficticios con patrón de tokens GitHub/OpenAI, bloque PEM, URI con usuario:clave, Bearer y `password=...`.
- Todos los datos de prueba son cadenas **sintéticas, no credenciales reales**, y se almacenan solo dentro del código del test. El test nunca registra valores del baseline, nunca descifra variables, no abre conexiones, no consulta proveedores, no modifica secretos ni lee datos de clientes.
- Un rechazo positivo de estos casos demuestra que **este instrumento de evidencia** impide esos formatos de filtración. **No es** una búsqueda global de secretos en todo el repositorio ni demuestra la completitud, vigencia o rotación de credenciales reales.

## Reproducibilidad y controles

`node scripts/verify-p0-008-b-secret-evidence.mjs` (Node 24). El workflow `.github/workflows/p0-008-b-secret-evidence.yml` usa permisos `contents:read` y HEAD exacto del PR. Deben resultar PASS el test dirigido, los 3 checks obligatorios sobre el SHA final, aprobación formal independiente y merge protegido dev; después readback PLAN_MAESTRO/COLA/BITACORA. No reclamar LISTO antes de estos controles.

No se alteran arquitectura, APIs, UI, RBAC, MySQL, providers, dominios, valores de entorno ni QA/main/PROD. Rollback por PR protegido revertiendo el script, workflow, acta y changelog; sin impacto en secretos ni datos.

**Caveat de calidad:** regex y estructuras solo cubren patrones explícitos. No usar este test para concluir que no existen secretos con formatos desconocidos.
