# SOLQARYN V5 FINAL — C02-03.M02.02: contrato API, DTO, errores y compatibilidad del ciclo de vida de tenant

**Autor:** PRIMARY DEV 03 JAVIER; TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** STANDARD V5 FINAL, PLAN_MAESTRO fila 369 y antecedentes C02-03.M01.01–06 con A5 independiente.
**Tipo:** diseño M02 exclusivamente documental, sin API nueva, sin modificación de DTO compilado, EF/MySQL, R2, worker, IAM/JWT/RBAC, proveedor, migración ni entornos QA/main/PROD.

## 1. Inventario de interfaces existentes comprobadas en Git

| Superficie existente | Contrato vigente observado | Límite de autoridad |
| --- | --- | --- |
| `EmpresasController`, ruta `empresas` | GET lista/id, POST alta, PUT edición, PATCH `{id}/activar`, PATCH `{id}/desactivar` | `[Authorize]`, `RequierePermiso(Configuracion, Acción)`, contexto de empresa autorizado |
| `PlataformaEmpresasController`, ruta `plataforma/empresas` | GET directorio, POST `borrador`, POST `alta-controlada` | Permisos de operador global; alta controlada puede denegar si falta habilitación |
| `PlataformaGestionMvpController`, ruta `plataforma/gestion` | PUT `empresas/{empresaId}/estado`; gestión acotada de usuarios y membresías | No sustituye ciclo de vida universal ni revoca jobs/sesiones por sí misma |

Estos contratos **no** implican que existan endpoints de archivar, baja lógica, transiciones idempotentes, sagas, outbox o restore. `Empresa.Eliminado` NotMapped no demuestra soft-delete implementado. Se conservan las rutas legadas y sus protecciones; no se reinterpretan `PATCH desactivar` ni `PUT estado` como archivo o baja lógica.

## 2. Contrato candidato FUTURO, no implementado

La futura superficie propuesta, **sin registrar ruta ahora**, es `POST /plataforma/empresas/{empresaId}/transiciones` y `GET /plataforma/empresas/{empresaId}/transiciones/{operacionId}`. La ruta debe ser global verificada, no accesible por un tenant ordinario. El cliente no puede enviar ni autoconceder claims de plataforma, tenant efectivo, rol administrador o decisiones de retención.

`TransicionTenantSolicitudV1` conceptual:

| Campo | Tipo conceptual | Validación futura |
| --- | --- | --- |
| `operacionId` | UUID v4 | Obligatorio, único por actor/empresa y entorno; no incluir datos personales |
| `estadoDestino` | `ACTIVO/SUSPENDIDO/ARCHIVADO/BAJA_LOGICA` | Estado futuro permitido por matriz de transición, nunca por texto arbitrario |
| `versionEsperada` | entero positivo | CAS en servidor; mismatch es conflicto sin cambio |
| `motivoCodigo` | catálogo administrado | Sin texto libre en telemetría ni secretos |
| `correlationId` | UUID opcional | Sólo trazabilidad; jamás autoridad |
| `solicitudRevision` | identificador de evidencia | Debe corresponder a autorización institucional verificable |

`TransicionTenantResultadoV1` conceptual: `operacionId`, `empresaId`, `estadoAnterior`, `estadoResultante`, `versionActual`, `resultado` (`APLICADO`, `YA_APLICADO`, `RECHAZADO`, `PENDIENTE_RECONCILIACION`), `evidenciaId`, `correlationId`. Jamás devolver token JWT, DSN, secretos, PII, snapshot de terceros ni instrucciones de respaldo.

No almacenar ni aceptar `empresaId` del body como autoridad: el identificador de ruta se coteja con permiso efectivo de operador y titularidad validada en backend. La operación exige idempotencia transaccional con hash canonizado de payload, actor, empresa y ambiente; mismo key + mismo payload retorna el mismo resultado durable y distinto payload con la misma key se rechaza.

## 3. Estados propuestos y tabla de compatibilidad

| Desde | Hacia | Requisitos y comportamiento |
| --- | --- | --- |
| SOLICITADO | PROVISIONANDO | Permiso global, versión, plantilla y mecanismo de compensación certificados |
| PROVISIONANDO | ACTIVO | Membresías, DB, logs, sesión, jobs y protección tenant comprobados |
| ACTIVO | SUSPENDIDO | Revocación de permisos/cachés/sesiones y suspensión de jobs confirmada |
| SUSPENDIDO | ACTIVO | Revalidación de membresías/roles, nueva versión y autorización global |
| SUSPENDIDO | ARCHIVADO | Retención, respaldo restaurable, bloqueo de escritura y auditoría aprobados |
| ARCHIVADO | BAJA_LOGICA | Política legal/contable y estrategia de restauración autorizadas; sin `DELETE` físico |
| cualquier transición | FALLA_REPARABLE | Error parcial registrado sin habilitar operación residual; reanudación dirigida |
| ARCHIVADO/BAJA_LOGICA | ACTIVO | NO directo: reconciliación y autorización independiente previa |

Las acciones legadas seguirán disponibles **sin cambiar su semántica** hasta certificación de compatibilidad y migración. No se habilitarán adaptadores que conviertan automáticamente un `Desactivar` legado en `ARCHIVADO` o `BAJA_LOGICA`.

## 4. Matriz de validaciones y errores futuros

| Caso | Respuesta candidata | Invariante |
| --- | --- | --- |
| Sesión ausente o JWT inválido | 401 | No mutar; no filtrar detalles de usuario |
| Tenant ordinario solicita transición global | 403/404 según política de ocultación | Sin elevación ni consulta cross-tenant |
| Operador sin acción específica | 403 | Las capacidades son explícitas, fail-closed |
| Empresa inexistente o no visible | 404 | No enumeración de empresas de terceros |
| ID, estado o motivo no válido | 400/422 | ValidationProblem/ApiResponse sanitizado |
| `versionEsperada` obsoleta | 409/412 según contrato acordado | CAS sin efecto; devolver sólo referencia segura |
| Reintento idéntico con `operacionId` usado | 200 con mismo resultado confirmado | Idempotencia durable y sin doble mutación |
| Reuso del mismo key con payload distinto | 409 | Nunca reprocesar |
| Estado transitorio sin autorización de recuperación | 409 | No saltar pasos ni confirmar falso ACTIVO |
| DB/outbox/cache/backup no disponible | 503 o estado PENDIENTE_RECONCILIACION | Sin conceder permisos; rollback/compensación |
| Suspensión antes de invalidar jobs/cachés | rechazo/pending | No dejar procesamiento autorizado |
| Campo no reconocido/versión DTO futura | rechazo explícito o compatibilidad pactada | No tratar valores desconocidos como permisos |

No se fijan aquí códigos HTTP definitivos cuando depende de una decisión contractual 409 vs 412 o política 403/404; deben resolverse antes de implementar endpoints y probarse con cliente real. Estructura de error propuesta: `code` estable, `correlationId`, `message` público redactado; sin stack, SQL, DSN, token, email o identificadores de otras empresas.

## 5. Oráculos documentales positivos y negativos (NO pruebas ejecutadas)

- P-DOC01: contrato distingue POST de transición futura y PATCH legado actual; N-DOC01: no declarar las dos rutas equivalentes.
- P-DOC02: destino perteneciente a la matriz; N-DOC02: destino inventado/no catalogado se rechaza.
- P-DOC03: actor global autorizado + versión actual; N-DOC03: usuario tenant A intentando cambiar B sin autoridad.
- P-DOC04: mismo key y payload responde idempotente; N-DOC04: misma key con payload distinto no duplica ni altera.
- P-DOC05: suspensión revoca sesiones y trabajos sólo después de verificación; N-DOC05: revocación parcial nunca devuelve éxito final.
- P-DOC06: reactivación exige revalidar permisos; N-DOC06: no restaurar automáticamente grants previos.
- P-DOC07: archivo requiere retención/restore certificado; N-DOC07: no confundir soft-delete futuro con `Empresa.Eliminado` NotMapped.
- P-DOC08: error conserva `correlationId` redactado; N-DOC08: nunca incluir JWT/DSN/PII o error SQL.

Los 16 escenarios anteriores son **criterios futuros escritos, no tests xUnit, HTTP, MySQL ni Playwright ejecutados**. M03 deberá implementar código y pruebas reales en GitHub Actions con servicio MySQL efímero cuando corresponda.

## 6. Dependencias, rollback y A0–A5

- **Dependencias:** M01.01–06 con A5 independiente y requisitos del DAG; diseñar M02.02 no activa M02.06 ni M03, ni modifica COLA/DAG manualmente.
- **A0:** diff sólo Markdown + sección CHANGELOG autocontenida; contratos y propiedad fuera de runtime.
- **A1:** inspección positiva/negativa de los ocho pares de oráculos documentales y de los doce casos de validación; no adjudicarles pruebas de aplicación.
- **A2:** separación global/tenant, cross-tenant A/B, versiones, revocación, idempotencia, retención y redacción.
- **A3:** correspondencia con controladores existentes y contrato de M01, sin romper rutas o DTO reales.
- **A4:** fast-forward CAS en dev, SHA/blobs/diff, checks Scope Lock y V5 completos y readbacks PLAN/COLA/DAG/BITACORA antes de LISTO_DEV; pruebas runtime NO_APLICA causal.
- **A5:** revisión independiente por otra de las cinco Tasks Javier; autor no se revisa; ningún pase QA/main/PROD.
- **Rollback aplicable:** revert normal del documento/CHANGELOG. Restore de MySQL o incidentes runtime son escenarios FUTUROS y no se prometen.
