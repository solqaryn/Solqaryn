# SOLQARYN V5 — C15-03.M01.04: riesgos, supuestos y decisiones de auditoría de seguridad/tenancy

**Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha local:** 2026-11-14. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Clase:** preparación documental M01.04, SIN pruebas de penetración, sin hallazgos runtime certificados ni cambios de permisos.

## Autoridad, antecedentes y separación de evidencia

Fuente V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. PLAN_MAESTRO fila 2237 PENDIENTE sin responsable al preflight; COLA G/O=SI, DAG_TECNICO fila 2198 F=VALIDADO/G=SI y CLAIM exclusivo. El HEAD de fuentes `8c8f962807f7aaa4befa0bc8094442ed2ec7e68c` incluye M01.01 (16 invariantes NR01–NR16), M01.02 (12 actores, 12 IO y 10 fronteras) y M01.03 (15 rutas Git con hashes verificados y 12 brechas G01–G12). Estos artefactos son contratos de la auditoría futura, no un reporte que certifique 0 vulnerabilidades.

El stack real declarado: Angular 22.2.1, ASP.NET Core net10.0, MySQL/EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9. Las fuentes de código usadas incluyen `TenantPermissionContext`, `IdentidadUniversalAuthorizationGuard`, `TenantQueryBypassGuard`, `CommercialCrossTenantWriteGuard`, `UnscopedCommercialTenantGuardMiddleware` y `V5WorkItemTenantAuthorizationGuard` de la matriz M01.03. El origen y la fuente son del repositorio autorizado, nunca un mirror o una copia.

## Catorce riesgos potenciales — NO_VERIFICADOS por pruebas activas

| ID | Modo de fallo que debe investigarse | Fuente/contrato para preflight | Oráculo técnico requerido |
| --- | --- | --- | --- |
| R01 | Token JWT válido pero membresía o rol actual revocado | IdentidadUniversalAuthorizationGuard S02 | A/B con rol revocado sin 2xx ni efecto |
| R02 | Header X-Empresa-Id usado como evidencia de autoridad | TenantPermissionContext S01 | Header A/B duplicado, inválido y spoof con rechazo |
| R03 | Selector sucursal ajena se propaga sin verificación | Guard S02 y TenantContext S10 | Bajo tenant A, sucursal B denegada |
| R04 | IgnoreQueryFilters sin marca de empresa depende de cuarentena externa | TenantQueryBypassGuard S05 + middleware S04 | Prueba integrada con 2 empresas y contexto ausente |
| R05 | Consulta por ID de recurso permite enumeración cross-tenant | CommercialTenantReadFilterModel S12 | List/detail/export A/B sin filas de B |
| R06 | FK venta/compra/producto cruza empresas | CommercialCrossTenantWriteGuard S06 | MySQL efímero rechaza relaciones cruzadas |
| R07 | Legacy EmpresaId NULL se adjudica silenciosamente | CommercialCrossTenantWriteGuard S06 | Sin reatribución y con auditoría de excepciones |
| R08 | Host público arbitrario enlaza datos privados | Middleware S04 + storefront test S14 | Host adulterado no concede permisos |
| R09 | Soporte o plataforma excede su grant y su objeto | PlataformaEmpresasController S11 + test S15 | Token support falso/revocado rechaza |
| R10 | Medios/export carecen de propietario tenant | Interfaces IO10/NR13; verificación pendiente | URL B / expirado no descarga A |
| R11 | Cola futura acepta identidad tenant del payload del cliente | V5WorkItemTenantAuthorizationGuard S07 | Scope/tenant/actor desconfiable se deniega |
| R12 | Logs o errores revelan datos A/B, PII o credenciales | SecurityBoundaryContractTests S09 | Failure logs redactados, no datos tenant B |
| R13 | CI de SHA equivocado o skipped se interpreta como aprobado | GitHub check-runs / A4 | Exact head_sha success o VALIDANDO |
| R14 | A5 propio o Task retirada legitima promoción | PLAN/COLA/BITACORA y allowlist Javier6 | Revisor TASK_ID distinto, no QA por PASS dev |

La presencia de una fuente que contiene un guard, o una suite de tests que declara un negativo, no demuestra su aplicación en cada ruta efectiva. **R04 es una dependencia de seguridad que requiere reproducción en entorno efímero; NO es una vulnerabilidad observada**. R01–R14 no llevan estatus FAIL/PASS de sistema vivo. Las atribuciones de severidad P0/P1 requieren reproducción, impacto probado y evento institucional independiente.

## Ocho supuestos sujetos a verificación

| ID | Supuesto no certificado | Evidencia que falta |
| --- | --- | --- |
| S01 | La cuarentena middleware permanece en la ruta requerida antes de consultas históricas | Validar pipeline efectivo de API y excepción |
| S02 | Las fuentes Git leídas corresponden a dev exacto y no un snapshot de otra rama | Comparar blobs y HEAD de commit |
| S03 | Las pruebas unitarias existentes incluyen negativos pero no cubren todas las superficies | Inventario de suite y tests HTTP/middleware dirigidos |
| S04 | Los datos heredados sin EmpresaId siguen en cuarentena sin owner inventado | Lectura DB autorizada separada, no en esta microtarea |
| S05 | Host tenant resolver y sesión pública aplican validaciones server-side | Prueba pública con host legítimo/falsificado |
| S06 | Las rutas de soporte y control plane no heredan RBAC comercial por omisión | Test de grants reales y token/replay |
| S07 | Proveedor Cloudinary actual no prueba futura seguridad R2 | Inventario real de objetos y URL TTL tras autorización futura |
| S08 | El estado A5 en un campo puede diferir del historial | Readback de K/L y último evento BITACORA por ID |

**S01–S08: ESTADO NO_VERIFICADO.** No se deduce INEXISTENCIA de riesgos de la ausencia de registros.

## Ocho decisiones abiertas (no implican autorización)

| ID | Decisión pendiente | Límite actual |
| --- | --- | --- |
| D01 | Aprobar fixture aislado tenant A/B, usuarios/roles y sucursales | Pendiente, sin tocar Aiven productivo |
| D02 | Precisar catálogo exhaustivo de endpoints, exports y medios alcanzables | Pendiente de inventory causal |
| D03 | Diseñar test fail-closed de IgnoreQueryFilters sin marca con cuarentena exterior | Pendiente, exige test integrado |
| D04 | Definir contrato para filas legacy NULL con backfill reversible y autorizado | Pendiente, ninguna migración por este doc |
| D05 | Definir política operativa de soporte y grant/expiry por plataforma | Pendiente, no ampliar roles |
| D06 | Aprobar metodología de pruebas contra host público y URL firmada | Pendiente, no activar R2/Hostinger |
| D07 | Definir runner CI efímero MySQL y evidencia de fallos negativos | Pendiente; no simular PASS |
| D08 | Requerir A5 independiente, priorización de P0/P1 y rollback antes de promoción | Pendiente para siguiente fase |

**D01–D08: PENDIENTE.** No se autoriza migración, cambio de JWT/RBAC, DB/EF, ambiente externo, DNS, secretos, pruebas en producción ni instalación de event dispatcher por rellenar esta matriz.

## Ocho pares positivos/negativos de aceptación futura

| Caso | Positivo observable si se ejecuta | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Usuario A autorizado puede leer sólo A | JWT A + recurso B denegado sin filtración |
| P02/N02 | Selector A validado tras membresía | Headers múltiples/inválidos/revocación rechazados |
| P03/N03 | Sucursal A validada para recurso A | Sucursal B con usuario A denegada |
| P04/N04 | Consulta histórica bajo owner A filtra correctamente | Ausencia de auth read scope no expone B |
| P05/N05 | FK escrita sólo con owner del mismo tenant | FK B o legacy NULL reasignado no persiste |
| P06/N06 | Soporte legítimo observa recursos permitidos | Soporte falso o fuera scope denegado |
| P07/N07 | Host público registrado sirve tenant propio | Host modificado/URL B no eleva privilegios |
| P08/N08 | A5 distinto del autor y CI exact SHA | Autorrevisión, CI skipped o SHA anterior rechazados |

Cada par requiere un futuro test real con autor, tenant A/B efímero, endpoint/capa, rol/sucursal, SHA exacto, `head_sha`, entrada, esperado/observado, hora UTC, logs redactados y rollback. **P01–P08/N01–N08 = FUTUROS_NO_EJECUTADOS** en esta unidad. Ni GitHub Actions general completado ni presencia de una suite xUnit sustituye reproducción causal para el hallazgo a certificar.

## A0–A5 documentales, pruebas causales futuras y rollback

- **A0:** revisión de 14 modos de fallo, 8 supuestos y 8 decisiones; cada riesgo enlazado a S01–S15 de M01.03 sin inventar métricas.
- **A1:** coherencia de 8 pares positivos/negativos y criterios de evidencia, sin ejecutar HTTP/MySQL.
- **A2:** diseño de frontera de seguridad JWT, RBAC, tenant A/B, sucursal, medios, soporte y jobs. No se modifican controles y no hay pentest PASS.
- **A3:** trazabilidad de NR01–NR16, IO01–IO12, T01–T10 y G01–G12 a pruebas por hacer; casos de integración NO_APLICA al diff Markdown, no atribuir PASS.
- **A4:** commit dev fast-forward con este archivo y una sección autocontenida del CHANGELOG; Scope Lock y verificador V5 en `completed/success` del SHA exacto; readback PLAN/COLA/BITACORA.
- **A5:** revisión independiente por una de las otras cinco Tasks Javier, sin autorrevisión. No habilita M01.06, QA, main o PROD por inferencia.

**Rollback:** `git revert` del changeset documental, reconciliación aditiva en BITACORA, sin force-push ni cambiar historial. Componentes API/Application/Infrastructure/DB/frontend y servicios externos no se editan.
