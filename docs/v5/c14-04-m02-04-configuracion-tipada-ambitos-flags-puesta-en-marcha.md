# SOLQARYN V5 FINAL — C14-04.M02.04: configuración tipada, ámbitos y flags de puesta en marcha

**Autor:** PRIMARY DEV 03 JAVIER_CHATGPT, `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Tipo:** contrato DOCUMENTAL M02.04, sin modificar opciones reales ni activar proveedores.

## 1. Autoridades y delimitación

El [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) gobiernan este contrato. Conservar `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, la skill canónica y arquitectura de Angular 22.2.1 / .NET 10 / EF Core MySQL / JWT / RBAC.

El gate `C14-04.M01.01–06 + A5` se comprobó para este M02.04; el resto de M02 son diseños independientes, no autorizaciones de runtime. La elegibilidad inicial fue PLAN H=PENDIENTE/I vacía, COLA G/O=SI y DAG F=VALIDADO/G=SI. Los valores futuros **son propuestas**, no flags existentes ni comprobaciones operativas.

## 2. Propiedad y jerarquía de ámbitos

La selección futura de valor deberá distinguir: (i) parámetros invariables de seguridad y gobierno global; (ii) entorno explícitamente autorizado; (iii) servicio o componente; (iv) tenant validado exclusivamente por backend cuando la clave admita variación; (v) operador autenticado con rol, sólo para preferencias no sensibles. **No** conceder permisos, tenant, despliegue o acceso a DNS mediante un flag aportado por el navegador, una cookie, host, `EmpresaId` libre o un fallback.

Una resolución segura exige `configKey` allowlisted + versión de esquema + ámbito autorizado + identidad de solicitante + lectura auditada. Si no existen todos los datos, devuelve `RECHAZADO/NO_CONFIGURADO` (fail-closed). No usar defaults permisivos para auth, almacenamiento, datos o publicación. Las claves sensibles sólo se leen de secretos administrados por ambiente con permiso; el valor secreto no aparece en Git, bitácora, telemetría ni DTO.

## 3. Catálogo de configuración conceptual

| CFG | Tipo futuro y ámbito | Valor seguro previsto o bloqueo |
| --- | --- | --- |
| CFG01 | `release.mode` enum, entorno | Sólo modo autorizado; nunca PROD implícito |
| CFG02 | `release.manifestDigest` digest, release | Hash exacto verificado |
| CFG03 | `release.window` interval UTC, entorno | Sin ventana válida no desplegar |
| CFG04 | `release.stopCondition` enum, entorno | Falla => detener superficie específica |
| CFG05 | `release.rollbackRef` versión, release | Runbook aprobado, no ejecución automática |
| CFG06 | `observability.healthInterval` duración, servicio | Requiere mediciones reales y límites |
| CFG07 | `observability.alertSeverity` enum, servicio | Allowlist; no filtrar PII |
| CFG08 | `support.escalationPolicy` versión, servicio | Roles y destinatarios verificados |
| CFG09 | `support.sessionMaxMinutes` entero, soporte | Respetar contrato actual 1–30 min; no extender por configuración |
| CFG10 | `storage.providerStage` enum, entorno | Cloudinary vigente; R2 sólo futuro con autorización |
| CFG11 | `backup.retentionPolicy` versión, origen | Sin backup probado no prometer retención efectiva |
| CFG12 | `restore.trialEnvironment` enum, ensayo | Solo entorno aislado autorizado |
| CFG13 | `tenant.auditVisibility` enum, tenant | Denegación por defecto; no amplía RBAC |
| CFG14 | `ux.accessibilityTarget` versión, cliente UI | Especificación sin afirmar cumplimiento |
| CFG15 | `performance.budget` cantidad+unidad, cliente UI | Presupuesto propuesto, no Core Web Vitals medidos |
| CFG16 | `release.enableSwitch` bool, entorno | Desactivado sin grant; nunca salto de A5 o CI |

**CFG01–CFG16 son NOMENCLATURA_PROPUESTA, NO_IMPLEMENTADA.** No se añaden claves a `appsettings`, Angular environments, base MySQL ni panel de administración. La incorporación futura exige inspeccionar nombres realmente existentes, evitar shadow keys, crear esquema validable y proteger precedencia de parámetros.

## 4. Doce invariantes de seguridad y compatibilidad

| INV | Invariante futura |
| --- | --- |
| IV01 | Clave desconocida o tipo incorrecto se rechazan sin fallback inseguro |
| IV02 | `release.enableSwitch` no suplanta autorización de QA/main/PROD |
| IV03 | Seguridad JWT/RBAC no se desactiva por flag |
| IV04 | El tenant verificado en servidor prevalece sobre input cliente |
| IV05 | Secrets se referencian, jamás se serializan al navegador o repositorio |
| IV06 | Valor global no sobreescribe denegación por empresa específica |
| IV07 | Cada cambio queda correlacionado con identidad, UTC, versión y auditoría |
| IV08 | No desplegar cuando check exact SHA está failed/cancelled/skipped/in_progress |
| IV09 | Fallback de proveedor de archivos no produce acceso cross-tenant |
| IV10 | Los flags son reversibles con readback y comprobación de efecto |
| IV11 | Compilación no equivale a restore, auditoría runtime ni SLO certificado |
| IV12 | Flags experimentales no sustituyen gates del DAG, CAS o A5 independiente |

## 5. Diez pares de pruebas FUTURAS_NO_EJECUTADAS

| CASO | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FT01 | Tipo enum allowlisted se acepta | Texto arbitrario o null inseguro rechaza |
| FT02 | Config por entorno autorizado se lee | Valor de PROD sin grant se rechaza |
| FT03 | Tenant A ve sólo config A permitida | Tenant A no cambia config B |
| FT04 | Flags no sensibles llegan tipados a UI | Secretos no se incluyen en DTO |
| FT05 | CI exact SHA success permite considerar release | Check canceled/skipped/in_progress no PASS |
| FT06 | Cambio versionado preserva rollback | Valor sin versión no permite revertir |
| FT07 | Sesión soporte respeta límite documentado | Duración fuera de 1–30 no se acepta |
| FT08 | Sin provider R2 autorizado se usa integración vigente | Flag no migra archivos ni DNS |
| FT09 | Presupuesto con unidad declarada se comprueba después | Número no medido no se informa como logro |
| FT10 | Revocación de role/tenant bloquea evaluación | Cache desactualizada no conserva privilegio |

Los casos son **diseño**, no ejecución de unit tests, browser, .NET ni MySQL. En una implementación posterior se requieren casos positivos/negativos reales, telemetría, fixtures tenant A/B y GitHub Actions dirigidas. No sustituir tests con un cambio documental.

## 6. A0–A5 y reversión

- A0: comprobar CFG01–CFG16, IV01–IV12, FT01–FT10, referencias de gobierno, Markdown y ausencia de secretos.
- A1: escenarios positivos/negativos conceptuales; marcar `FUTUROS_NO_EJECUTADOS`.
- A2: inspección estática de fronteras tenant, JWT, RBAC, secretos, PII y provider; ningún runtime alterado.
- A3: coherencia con M02.01 (ownership), M02.02 (DTO/errores), M02.03 (persistencia/fixtures), M01.06 (no release sin gates) y contrato real de soporte `PlataformaSoporteController`.
- A4: Git commit fast-forward/CAS en dev, exactamente dos Markdown, Scope Lock y control de provenance V5 `completed/success` exact-SHA, BITACORA y readback de PLAN/COLA/DAG.
- A5: posterior e independiente por Task Javier distinta de autora, sobre SHA original, sin autoaprobación.

**Impacto presente:** documento y CHANGELOG, ningún código, API, opción live, tabla, secreto, DNS, host, Cloudflare/R2, QA/main/PROD. **Rollback**: `git revert` convencional previo control de HEAD y sin borrar historia institucional.
