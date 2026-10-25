# P0-001.B - Certificacion negativa de paridad DEV (25-10-2026)

**Microtarea:** `P0-001.B`, Plan Comercial Inicial. **Estado de paridad:** `NO_GO`. Esta acta documenta una prueba de denegacion de paridad y una regresion local; no certifica despliegue equivalente ni autoriza promocion, cambios de infraestructura o cierre comercial automatico.

## Readback sobre el HEAD vivo de dev

- GitHub `solqaryn/Solqaryn`, rama `dev`, SHA `05e6a67a7bbbd2a94b4aeaf1322258ece90c730b`.
- CI exact-HEAD: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`, los tres `SUCCESS` sobre el SHA anterior.
- Render DEV: servicio `srv-daqvla49v7es738up9vg`, deployment `dep-dbf1kouq1p3s73eng1n0`, `status=live`, commit `05e6a67a7bbbd2a94b4aeaf1322258ece90c730b` (termino `2026-10-25T15:05:45.559Z`). Esta metadata no acredita HTTP health ni paridad con frontend.
- Vercel proyecto **DEV** `solqaryn-dev`, deployment `dpl_AxCj2iPM9NCPfgweeCa9254VL65q`, commit `05e6a67a7bbbd2a94b4aeaf1322258ece90c730b`, `state=CANCELED`. Un intento de ese mismo SHA cancelado **no** es un frontend READY. `target=production` describe el target interno del proyecto DEV de Vercel, no un permiso de PROD de SOLQARYN.
- Resultado: **`NO_GO`**: no se certifica Render + Vercel en el mismo HEAD READY/live, pese al CI 3/3 SUCCESS y Render `live`.

Fuentes consultadas: [commit exacto](https://github.com/solqaryn/Solqaryn/commit/05e6a67a7bbbd2a94b4aeaf1322258ece90c730b), [Render DEV](https://dashboard.render.com/web/srv-daqvla49v7es738up9vg), [Vercel DEV](https://vercel.com/solqaryn/solqaryn-dev/AxCj2iPM9NCPfgweeCa9254VL65q). Snapshot de los campos pertinentes en `docs/baselines/P0-001-B-DEV-20261025.json` (UTC original). Son lecturas, no despliegues provocados por esta microtarea.

## Negativo obligatorio y regresion reproducible

`node scripts/verify-p0-001-b-parity.mjs --self-test` ejecuta un gate local puro, sin llamadas de red ni secretos. Un resultado `MATCH` requiere **simultaneamente**: identidad `solqaryn/Solqaryn/dev`, SHA valido de 40 caracteres, tres checks obligatorios exitosos y vinculados exactamente a ese HEAD, metadata Render `live` con SHA exacto y Vercel **READY** con el mismo SHA. Cualquier ausencia, SHA desigual, check duplicado, check ajeno, URL de CI no canonica o estado incorrecto provoca `NO_GO`.

La suite incluye **10 mutaciones negativas**, una prueba contrafactual positiva (solo de algoritmo; no representa un despliegue real) y la comprobacion `NO_GO` del snapshot observado. Ninguna evidencia estatica reemplaza una consulta nueva a proveedores para una certificacion posterior. La certificacion positiva en entorno real exige nueva lectura de Render/Vercel y, cuando corresponda, prueba HTTP health/readiness y origen de frontend y API autenticados.

## Impacto, seguridad y continuidad

Impacto limitado a documento, JSON de auditoria y validador offline. No modifica Angular, APIs, DTOs, migraciones, MySQL, RBAC, JWT, Cloudinary, jobs, secretos, Render/Vercel, DNS, QA, main o PROD. No cambia parametros de autorizacion de las seis Tasks ni la autoridad de VAEP/Jules.

**Criterio de aceptacion de este PR:** los negativos del validador pasan; tres checks obligatorios del commit del PR pasan; hay revision independiente valida. **No declarar paridad real `MATCH` mientras Vercel DEV permanezca CANCELED**. Mantener el estado institucional de la microtarea segun PR/revision/merge/readback; cualquier excepcion de despliegue requiere autorizacion separada. Rollback: revert de estos archivos por PR protegido; los proveedores permanecen inalterados.
