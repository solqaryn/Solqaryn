# C15-05.M06.03 — límites, riesgos abiertos y decisiones técnicas (V22)

**Unidad institucional:** `C15-05.M06.03`, PLAN_MAESTRO!D2338 «Documentar limitaciones, riesgos abiertos y decisiones tomadas». **Responsable:** `6af16bbad26881918c283abe06e1f32a` (DEV06). **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo de este incremento:** R0 documental. No se cambia runtime, esquema, migraciones, tests, CI ni autorización.

## 1. Frontera del requisito, fuentes y estado no inferido

Este documento agrega decisiones e incertidumbres de las unidades C15-05.M05.01–06 y M06.01–02. No equivale a la salida de fase «Completa M06» de PLAN!E, ni resuelve C15-05.M06.04–06, A5 pendientes o gates de otros bloques.

Fuentes contrastables:
- `docs/v5/c15-05-m06-01-auditoria-mocks-vs-reales.md` (commit `06fa2a8390ac34ceaa52d2ea2584b060362e6437`): auditoría de xUnit, TestServer, GitHub GET y Chromium reales frente a mocks/sesiones sintéticas. La unidad M06.01 fue cerrada a LISTO por rescate transversal con autoría histórica DEV03; su A5 sigue independiente y **pendiente** al publicar este archivo.
- `docs/v5/c15-05-m06-02-version-diff-artefactos.md` (commit `47e3195a86f68275abf7ee7f5a596d4dfbe83c4a`): tabla por ID de SHA, run, blob y reproducción; al crear esta unidad M06.02 estaba **VALIDANDO**, esperando checks exact-SHA de backend/seguridad. Se debe releer antes de usarla como dependencia final.
- PLAN_MAESTRO H/I/J/K/L/M, COLA G/O, DAG_TECNICO F/G, BITACORA A:Z y [GitHub Actions](https://github.com/solqaryn/Solqaryn/actions). Las fechas, estados y checks citados aquí son una fotografía de sus respectivos SHAs, **no** una certificación global permanente.

## 2. Registro de riesgos — condiciones verificables

| Riesgo / limitación | Evidencia disponible | Prueba o condición todavía NO acreditada | Tratamiento seguro / decisión |
| --- | --- | --- | --- |
| **R2: concurrencia y reservas** | M05.01 xUnit 10/10, CAS/idempotencia **in-memory**, fuente `61b9716e...` | Equivalencia con namedRange V17 de Google Sheets, exclusión entre seis Tasks y fallas de red/distribuidas | Mantener lock V17 + CAS H/owner + BITACORA + readback en ejecución institucional; no sustituir por test en memoria |
| **R2: HTTP/auth/tenancy** | M05.02 TestServer ASP.NET Core con JWT firmado 30/30 y límites DTO, fuente `07550553...` | Proveedor real de permisos, revocación empresarial en BD vivo, claims JWT de todos los módulos, aislamiento A/B integral | Tratar pruebas de fixture como contrato, no conceder permisos por UI ni cerrar déficits P0/P1 con `NO_APLICA` engañoso |
| **R2: proveedor y red** | M05.03 Node 7/7, GET HTTP real a API GitHub, fuente `f4827013...` | Escrituras de terceros, webhook autenticado, colas externas, permisos de producción, idempotencia de entrega | Conservar GET read-only; no instalar despacho ni simular un flujo de escritura |
| **R1/R2: navegador y accesibilidad** | M05.04 Chromium 3/3 y M05.06 Chromium 4/4 en GitHub-hosted, responsive y teclado, fuentes `ae1d0de5...` y `88da235e...` | JWT de sesión real para M05.04/M05.06, dispositivos físicos, auditoría WCAG completa, p75 de LCP/CLS/INP de campo | Identificar `page.route` y token sintético; los tiempos DCL son smoke local, no Core Web Vitals certificados |
| **R2: abusos y recurso/propiedad** | M05.05 xUnit 12/12 adversarial sobre lógica en memoria, fuente `43fd19b1...` | BOLA/IDOR con HTTP + JWT + MySQL real y controles granulares de sucursal, archivo, job y propiedad en toda la plataforma | Mantener obligación de pruebas negativas reales para cada superficie que se modifique; una prueba aislada no equivale a pentest |
| **R2: gates y estado histórico** | M05.01–06 figuran LISTO/A5 independientes; M06.01 LISTO+A5 pendiente y M06.02 VALIDANDO al preflight | M06.01/M06.02 A5 independientes, M06.04–06, cierre final de 2.339 predecesoras y dependencias de otros capítulos | No forzar COLA/DAG a `SI`; releer estados y evidencias por SHA antes de promoción |
| **R3: producción/proveedores reales** | Solo DEV autorizado | Migración Hostinger/Cloudflare, datos/sistemas reales, pagos, secretos, QA/main/PROD | **NO_GO** sin autorización adicional; no operar ni prometer despliegue |

Esta matriz registra **incertidumbres**; no asegura que exista una vulnerabilidad explotada. La falta de prueba causal es `NO_MEDIDO` o `NO_CERTIFICADO`, nunca `PASS`. P1/P2 abiertos de otras VALIDANDO deben conservarse en su ID y BITACORA original.

## 3. Decisiones y acciones de continuidad

1. **D1 — Calidad proporcional al requisito:** para esta unidad de texto R0, A0 es validación de alcance/diff y procedencia; A1 comprobar correspondencia entre las siete fuentes y la matriz de riesgo; A2/A3 `NO_APLICA` exclusivamente a ejecutar nuevos HTTP/JWT/MySQL/Chromium, porque no modifica código. Estas exenciones no se trasladan a los incrementos R1/R2 referidos.
2. **D2 — Evidencia sin inflación:** `completed/success` exact-SHA para Scope Lock y V5 es obligatorio. `SKIPPED/CANCELLED/FAIL/QUEUED/IN_PROGRESS` requerido no es PASS. Un check global fallido ajeno no se altera ni oculta; separar alcance del test dirigido de certificación global.
3. **D3 — Responsabilidad histórica y colaboración:** PLAN!I registra autor histórico; la Task reparadora/cerradora queda identificada como actor en BITACORA. Antes de escribir usar lock V17 del ID, CAS condicionado, relectura y ausencia de escritor; ninguna VALIDANDO es propiedad exclusiva permanente.
4. **D4 — Independencia de A5:** una Task Javier distinta del autor y, cuando corresponda, del reparador revisa realmente fuente, diff y riesgos antes de dar A5_PASS. No crear A5 retroactiva ni borrar FAIL.
5. **D5 — Rollback:** esta entrega solo agrega este documento y una sección autocontenida a CHANGELOG. Ante defecto documental, aplicar un `git revert` normal solo después de releer HEAD; conservar historial, BITACORA y autoría. No reset, force-push ni escrituras productivas.

## 4. Reproducción y criterio atómico de salida

- Tomar SHA del commit publicado, consultar `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100`; verificar `head_sha` exacto de `verify-solqaryn-project-scope`, `V5 - Sin encabezados vacios ni atribucion cruzada` y cualquier check pertinente disparado.
- Revisar con `git diff <PARENT>..<SOURCE_SHA> --name-only` que solo este documento y `CHANGELOG_AI.md` cambian; demostrar que el CHANGELOG anterior es sufijo byte a byte intacto, sin encabezados vacíos ni intercalado.
- Releer PLAN fila 2338, COLA G/O, DAG fila 2299 y BITACORA por EVENT_ID. Mientras CI pertinente esté ejecutándose dejar `VALIDANDO` con SHA/run, liberar únicamente el namedRange propio y permitir avance de otra unidad independiente.
- Cuando A0–A4 de este documento estén completos, cerrar `VALIDANDO→LISTO` por V17/CAS preservando I, registrar evento idempotente y readback; A5 independiente **después**. Ninguna de estas condiciones implica certificación de fase, QA/main/PROD, secreto o pago.

**Conclusión:** limitaciones explícitas y decisiones auditables para el requisito documental M06.03; no se adjudica seguridad ni integración que solo fue simulada. Mantener seguimiento técnico separado de los gaps R2 hasta prueba causal real.
