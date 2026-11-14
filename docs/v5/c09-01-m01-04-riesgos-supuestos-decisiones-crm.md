# SOLQARYN V5 — C09-01.M01.04: riesgos, supuestos y decisiones pendientes del CRM universal

**Microtarea:** C09-01.M01.04 · PLAN_MAESTRO fila 1301 · **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Fecha:** 2026-11-14 · **Repositorio:** `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Fuente única ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Google Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Resultado del bloque C09-01:** CRM universal con historial, segmentos, oportunidades, actividades y seguimiento de clientes; timeline aislado por empresa y permisos por rol. Esta unidad M01.04 únicamente **registra riesgos potenciales, supuestos falsables y decisiones todavía abiertas**. No crea CRM operativo, endpoint, tabla, migración, worker ni envío de mensajes.

## Fuentes verificadas y límites de la evidencia

| Evidencia de dev | Blob Git | Uso permitido |
| --- | --- | --- |
| `docs/v5/c09-01-m01-01-alcance-crm-universal.md` | `4d35d8113cf1a1718dd7f487c50a06f51c1848b5` | Alcance, no-regresión y escenarios propuestos |
| `docs/v5/c09-01-m01-02-actores-interfaces-crm-universal.md` | `02a78bad5debea4678651624280a4353c54af24a` | Actores, interfaces y fronteras de confianza |
| `docs/v5/c09-01-m01-03-inventario-codigo-brechas-crm.md` | `34e0a8a652a9e92900644c3e7c8fc59ada58b5f8` | 16 rutas existentes, G01–G12 brechas de evidencia |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Cliente y propiedad legada `EmpresaId` nullable: no tratar NULL como global |
| `backend/src/API/Controllers/ClientesController.cs` | `023d639f5c6ebf20519a22b175e2bf17c53e6d0d` | Superficie actual de Clientes; no es timeline CRM |
| `backend/src/Infrastructure/Repositories/ClienteRepository.cs` | `07bb969b52ea4273ac31975ebbc10d19af8a9d75` | Referencia EF; no certifica cobertura tenant de todo CRM |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Restricción UI, nunca sustituto de autorización API |

Las referencias anteriores son **evidencia estática** del alcance en dev. Las G01–G12 del inventario previo son preguntas abiertas, **no vulnerabilidades confirmadas** ni afirmación de ausencia de código en todo el repositorio. No se consultaron datos de producción, clientes reales, tokens, archivos privados ni secretos.

## R01–R16 — riesgos POTENCIALES que deberán mitigarse

| Riesgo | Evento a prevenir en una implementación futura | Control verificable requerido |
| --- | --- | --- |
| R01 | Un usuario A recibe ficha o timeline de un cliente perteneciente a B | Resolver empresa desde identidad server-side y denegar acceso cruzado con fixtures A/B |
| R02 | Cliente con `EmpresaId=NULL` legado se expone como recurso global | Política explícita de reconciliación; fail-closed hasta atribución válida |
| R03 | Guard Angular permite acción CRM que carece de verificación RBAC backend | Autorización por recurso/acción en API y negativos contra llamada HTTP directa |
| R04 | Header `X-Empresa-Id`, slug o host falsificados alteran tenant efectivo | Comparar con membresía/rol vigente; nunca confiar en la selección cliente |
| R05 | Revocación de usuario o cambio de rol deja acceso a segmento/caché CRM | Comprobar sesión, estado y permisos efectivos; invalidación de caché y caso de revocación |
| R06 | Segmentación acepta filtros SQL, JS, propiedades arbitrarias o consultas sin límite | DTO allowlisted, parámetros tipados, límites/paginación y tests de rechazo |
| R07 | Exportaciones exponen PII o contactos de B por columnas, filtros o logs | Autorización independiente, minimización, redacción, auditoría y negativos A/B |
| R08 | Una actividad repetida por timeout/replay se inserta varias veces | Clave idempotente tenant-bound con digest semántico y prueba concurrente |
| R09 | Oportunidades modifican ventas o facturación sin frontera clara | Separar máquina comercial de asientos/ventas, verificar integridad y reversión |
| R10 | Una actividad CRM se confunde con el log de auditoría inmutable | Contratos diferenciados de timeline, auditoría, correlación y retención |
| R11 | La falta de consentimiento se interpreta como permiso de contacto/campaña | Consentimiento por canal/finalidad, opt-out y supresión antes de cualquier envío |
| R12 | Un rol de sucursal accede a contactos restringidos de otra sucursal | Matriz empresa/sucursal/usuario por recurso y casos negativos de alcance |
| R13 | Dos operadores pisan la etapa o asignación de una oportunidad | Control de versión CAS, conflicto observable y audit trail sin pérdida silenciosa |
| R14 | Un fallo de repositorio, caché o proveedor habilita acceso por defecto | Degradación fail-closed; escenarios de timeout/circuit breaker y prueba de datos |
| R15 | Nueva UI CRM degrada accesibilidad, respuesta móvil o rendimiento | Teclado/foco, 320px, reduced motion, paginación y presupuesto medido con Playwright |
| R16 | Este documento se presenta como CRM implementado o autorización de despliegue | Mantener M01.06/M02–M06, A0–A5, CI exact-SHA y permiso separado para QA/main/PROD |

**Estado de R01–R16:** evaluación preventiva de amenazas y fallos hipotéticos, **NO_VERIFICADOS_COMO_DEFECTO_REAL**. La mitigación debe tener código/prueba específica en fases posteriores, no una afirmación basada sólo en inventario.

## S01–S08 — supuestos falsables NO_VERIFICADOS

| Supuesto | Hipótesis que aún no constituye hecho | Evidencia necesaria para aceptarlo |
| --- | --- | --- |
| S01 | El modelo Cliente actual admite CRM universal sin duplicar identidad | Mapa de ownership y compatibilidad Domain/EF/API |
| S02 | Existe un contrato de timeline con orden estable y eventos inmutables | Entidades/DTO, orden temporal, prueba de filtrado y auditoría |
| S03 | La pertenencia UsuarioEmpresa y RBAC existente cubren cada acción CRM | Matriz acción/recurso/rol y 401/403/404 en API |
| S04 | Los datos legados de cliente admiten reconciliación no destructiva | Inventario autorizado, política de NULL y rollback comprobable |
| S05 | Se pueden definir segmentos con filtros tipados y coste acotado | Allowlist, SQL parametrizado, índices, paginación y límites medidos |
| S06 | La oportunidad comercial no duplica ventas ni plan SaaS | Revisión de entidades, relaciones y transacciones |
| S07 | El consentimiento/opt-out existente soporta los canales futuros | Contrato de finalidad/canal, evidencia vigente y pruebas negativas |
| S08 | Angular puede mostrar CRM accesible sin atribuir permisos a la UI | Prototipo, accesibilidad, carga progresiva y backend negativo A/B |

Cada supuesto permanece en NO_VERIFICADO; ninguna decisión comercial, tabla o permiso se autoriza por inferencia.

## D01–D08 — decisiones explícitamente PENDIENTES

| Decisión | Pregunta pendiente | Estado seguro mientras no se resuelva |
| --- | --- | --- |
| D01 | ¿Se incorpora el timeline a Cliente o como agregado separado por empresa? | No crear modelo persistente ni migración de CRM |
| D02 | ¿Qué roles/acciones/sucursales pueden leer, crear o exportar CRM? | Denegar capacidades nuevas sin matriz RBAC aprobada |
| D03 | ¿Cómo se atribuyen `EmpresaId=NULL` y clientes históricos? | Ninguna asociación global automática ni borrado |
| D04 | ¿Qué filtros, operadores y límites forman el catálogo de segmentos? | Rechazar expresiones abiertas o no allowlisted |
| D05 | ¿Cuáles son estados y transiciones de oportunidad y su relación con ventas? | No duplicar facturación ni alterar estados contables |
| D06 | ¿Qué retención, auditoría, redacción y consentimiento rigen notas y campañas? | No habilitar envío comercial ni export sin política |
| D07 | ¿Cuál es la clave idempotente, versión CAS y semántica de replay/rollback? | No crear efectos con retries no definidos |
| D08 | ¿Qué evidencia de MySQL/EF, A/B, Angular/E2E y reversión habilita fases futuras? | Mantener gates propios y dev-only; sin QA/main/PROD |

Las decisiones no se consideran adoptadas por redactar esta matriz. Requieren autoridad y verificación en los scopes correspondientes.

## Oráculos de aceptación futura — NO_EJECUTADOS

| Caso | Positivo futuro exigido | Negativo futuro que debe denegarse |
| --- | --- | --- |
| P01/N01 | Admin A ve cliente y timeline A paginados | Token A intenta consultar cliente de B |
| P02/N02 | Actor autorizado registra actividad A con correlación única | Replay o carrera genera duplicado |
| P03/N03 | Oportunidad A cambia de etapa con versión válida | Rol lector o versión obsoleta intenta transición |
| P04/N04 | Segmento A usa predicados allowlisted | Filtro SQL/JS arbitrario o cross-tenant |
| P05/N05 | Export A autorizado contiene PII minimizada y auditoría | Rol sin permiso, token revocado o B solicita export |
| P06/N06 | Contacto con consentimiento por canal puede ser procesado | Opt-out, finalidad distinta o ausencia de consentimiento |
| P07/N07 | Historial conserva integridad y orden tras retry seguro | Caché/error/timeout filtran datos o acceso de B |
| P08/N08 | Vista accesible funciona teclado, 320px y estados | Manipular guard Angular no elude RBAC server-side |

Estos oráculos son **especificaciones NO_EJECUTADAS**: cero nuevos PASS de .NET/MySQL, Playwright, JWT/RBAC o autorización HTTP derivados de este Markdown.

## Impacto, A0–A5, dependencias y rollback

- **Impacto actual:** sólo `docs/v5/c09-01-m01-04-riesgos-supuestos-decisiones-crm.md` y sección unida de `CHANGELOG_AI.md`; sin modificación de Domain, Application, Infrastructure, API, Angular, MySQL, DTO, jobs, datos, secretos, endpoints, interfaz, observabilidad operativa ni arquitectura canónica.
- **A0:** Skill local cotejada con canónica de HEAD, arquitectura y stack; Task Javier :00, PLAN fila 1301 PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI, claim por CAS y readback exclusivo.
- **A1:** coherencia de R01–R16, S01–S08 y D01–D08 con el inventario G01–G12, actores M01.02 y contrato M01.01; verificación documental positiva y negativa de claims injustificados.
- **A2:** evaluación estática JWT/RBAC, aislamiento tenant A/B, consentimiento, PII, legacy NULL, replay, concurrencia, export y denegación. No se afirma test real de seguridad.
- **A3:** trazabilidad conceptual Domain/Application/Infrastructure/API/Angular y contratos; builds, xUnit, MySQL 8.4 y Playwright `NO_APLICA` al diff estrictamente Markdown, no se contabilizan como PASS.
- **A4:** publicación fast-forward/CAS sobre HEAD vivo; revisar diff/blob, Scope Lock y verificador V5 `completed/success` en SHA exacto, luego PLAN/COLA/BITACORA y exclusión de otros scopes.
- **A5:** `A5_PENDIENTE` para otra de las cinco Tasks Javier, nunca por el mismo autor; sin promoción automática a QA/main/PROD.
- **Rollback:** revert normal documentado del commit, sin force-push, escrituras de datos o alteración de BITACORA histórica.

**Siguiente microtarea M01.05:** convertir riesgos y decisiones en aceptación observable, sin fingir funcionalidad. La etapa M03 es código real y exige CI causal cuando se autorice. No se implementaron eventos, despachadores, workers, campañas ni integraciones externas.
