# SOLQARYN V5 FINAL — C02-03.M01.02: actores, entradas, salidas y superficies de aprovisionamiento tenant

**Clase:** TIPO 1 documental preliminar; sólo identifica actores, contratos IO y fronteras de confianza para C02-03 (alta, suspensión, reactivación, archivado y baja lógica idempotentes). **No implementa un lifecycle manager, no crea/baja empresas ni migra datos.** Fuentes: Standard V5 FINAL Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`; `solqaryn/Solqaryn@dev`; Scope Lock STRICT; Skill canónica. Autoría de preparación: PRIMARY DEV 03 JAVIER, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`.

**Resultado exigido FUTURO de fase:** alta, suspensión, reactivación, archivado y baja lógica reproducibles e idempotentes; evidencia futura mínima de repetición, fallo parcial y recuperación. La fase M01.02 sólo especifica entradas/salidas/actores/superficies, nunca un PASS de operaciones live.

## 1. Actores y autoridad (no conceden permisos nuevos)

| Actor | Entrada propuesta | Salida posible futura, condicionada por auth real | Caso prohibido |
| --- | --- | --- | --- |
| Propietario/operador de plataforma autorizado | Solicitud firmada con grants y reason code | Propuesta de alta/estado tenant auditada | Usar un rol empresarial o un booleano del cliente como permiso plataforma |
| Administrador empresarial | Solicitud de operación en empresa propia | Ver estado no sensible/capacidades autorizadas | Suspender o borrar otra empresa o activar operaciones globales |
| Usuario miembro activo | JWT validado y membresía actual verificada | Operar sólo en tenant y recursos aprobados | Obtener acceso a empresa suspendida/revocada o cambiar EmpresaId |
| Worker de aprovisionamiento (FUTURO) | Comando validado, ID idempotente y scope firmado server-side | Aplicar paso transaccional autorizado y registrar avance | Usar tenant/DSN arbitrario recibido del cliente o de un evento sin procedencia |
| Registro de empresas (actual Domain) | Comando controlado más estado existente | Alta y transiciones de dominio propuestas | Duplicar empresa o restaurar estado ilegal silenciosamente |
| Servicio de autorización/contexto tenant | JWT y UsuarioEmpresa activa desde servidor | Decisión RBAC/tenant por operación y empresa | Confiar en host/slug/claim en lugar de membresía |
| Adaptador de persistencia (FUTURO) | Tenant verificado, versión de estado y transacción | Persistir cambio de estado con retry y auditoría | Borrar datos, reusar conexiones de otra empresa o mutar PROD desde esta fase |
| Revisor independiente | SHA documental/código y A0–A4 de autor distinto | Emitir A5_PASS/FAIL con evento idempotente A:Z | Revisar con el mismo TASK_ID, omitir evidencia o autopromover QA/main/PROD |

Las cuentas ChatGPT/GitHub y el usuario comercial jamás son el mismo principal; una Task no hereda permisos para crear empresas ni activar infraestructuras.

## 2. Modelo de estados propuesto, NO implementado

- **SOLICITADO → PROVISIONANDO → ACTIVO:** sólo si la autorización y unicidad se verifican; reintento con mismo ID no repite efectos.
- **ACTIVO → SUSPENDIDO:** bloquear escrituras y operaciones privadas según política, retener datos según normativa/contrato; denegación efectiva en backend.
- **SUSPENDIDO → ACTIVO:** nueva comprobación de autorización y membresía; invalidar sesiones/cachés previos cuando proceda.
- **ACTIVO/SUSPENDIDO → ARCHIVADO:** sólo con retención definida; el archivado no borra ni transfiere datos por sí mismo.
- **ARCHIVADO → BAJA_LOGICA:** autorización elevada, verificación de obligaciones/retención, reversión controlada cuando corresponda. La baja lógica no equivale a DROP DATABASE.
- Cualquier transición ilegal, incompleta, repetida o de estado desconocido debe rechazarse o quedar en reparación auditable. Ningún estado es afirmado como implementado aquí.

## 3. Matriz de IO: entradas, salidas, consumidores y riesgos

| ID | Entrada y fuente de autoridad | Salida / consumidor FUTURO | Límite y validación exigibles |
| --- | --- | --- | --- |
| IO01 | Identidad operador y grant plataforma | Contexto de solicitud de lifecycle | JWT/grant real en backend, sin elevación por cliente |
| IO02 | Tenant solicitado + identificador interno | Empresa objetivo verificada | EmpresaId positivo y ownership, sin confiar en slug/host |
| IO03 | Comando de transición + estado previo | Decisión válida/denegación | Tabla de transiciones y capacidad por estado |
| IO04 | IDEMPOTENCY_KEY y correlación estables | Respuesta única al reintentar | Idempotencia por operación, tenant y versión |
| IO05 | Versión de estado y token de concurrencia | Actualización compare-and-set | Rechazar carreras de suspensión/reactivación y replay |
| IO06 | UsuarioEmpresa y rol actual | RBAC y acceso ajustado al nuevo estado | Revocación, cache/rol por empresa y aislamiento A/B |
| IO07 | Comando autorizado de alta | Registro mínimo Empresa y pertenencia inicial | Unicidad y transacción, no duplicar empresa ni usuarios |
| IO08 | Suspensión o archivo autorizado | Bloqueo de acciones privadas, evento de auditoría | Sin fuga de PII, no descartar contabilidad ni documentos |
| IO09 | Reactivación autorizada | Revalidación de membresía, estado, caché | No restaurar JWT/sesiones antiguas automáticamente |
| IO10 | Baja lógica con confirmaciones/retención | Marca lógica y plan de retención o rollback | Nunca borrar datos físicos o DB por ruta de esta M01 |
| IO11 | Error parcial de worker o proveedor | Estado reparable, checkpoint y alerta segura | No ejecutar otra empresa ni reintentar sin límite |
| IO12 | Evento versionado/autorizado | Observabilidad y registro auditable | No exponer secrets/JWT/DSN, no crear duplicados en colas |

Se requerirán API/DTO, entidades, índices, jobs, pruebas de integración y transacciones cuando se implementen; no son endpoints reales hoy por esta unidad.

## 4. Evidencia de código inspeccionado (lectura; no PASS runtime)

| Fuente Git en dev | Blob SHA | Qué demuestra / qué NO demuestra |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Identidad y estado empresarial de dominio; no implementa lifecycle universal |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía y rol tenant; no provee permiso global |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto verificado desde membresía; no valida por sí mismo transición empresarial |
| `backend/src/Application/Interfaces/StorageTenantContext.cs` | `716e8e205502d6300f555947c79edc2852fd63ba` | Prefijo tenant controlado; no autoriza baja ni movimiento de archivos |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Resolución server-side de pertenencia; no certifica baja/reactivación de tenant |

No se emplean fixture sintéticos como evidencia de MySQL o autorización viva. Ningún secreto ni dato de empresas se lee en este documento.

## 5. Casos positivos y negativos especificados para fases FUTURAS

| ID | Tipo | Estímulo | Resultado exigible |
| --- | --- | --- | --- |
| P01 | POS | Operador con grant crea tenant nuevo con ID idempotente válido | Un registro activo tras workflow autorizado, audit trail y respuesta estable |
| P02 | POS | Operador suspende A y repite misma solicitud | Un estado SUSPENDIDO, sin segundo efecto ni pérdida de datos |
| P03 | POS | Operador reactiva A cumpliendo permisos y validación | Estado ACTIVO sólo tras recomprobar membresía y caché |
| P04 | POS | Falla paso intermedio; operador reinicia desde checkpoint válido | No dobles cambios ni efectos interempresariales |
| N01 | NEG | Administrador de A solicita suspensión de B | 403/denegación sin cambiar B |
| N02 | NEG | Usuario envía EmpresaId/host de tercero | No se convierte en autoridad ni elige tenant |
| N03 | NEG | Clave idempotente reutilizada con payload diferente | Conflicto seguro, sin sobrescribir solicitud anterior |
| N04 | NEG | Dos comandos concurrentes cambian estado de A | Control de versión/fencing, un orden trazable |
| N05 | NEG | Empresa archivada recibe escritura privada | Denegación server-side y sin bypass por API/worker |
| N06 | NEG | Migración/archivo falla a mitad de transacción | Rollback o recuperación verificable sin pérdida de datos |
| N07 | NEG | Se intenta baja física sin autorización/retención | Rechazo; no DROP/DELETE de datos contables |
| N08 | NEG | Membresía revocada reintenta reactivación | Denegación antes de operar recurso |
| N09 | NEG | Un evento de A ejecuta worker con contexto B | Rechazo y aislamiento A/B sin side effects |
| N10 | NEG | Excepción/log contiene JWT, DSN, password o PII | Redacción, mensaje genérico y auditoría segura |
| N11 | NEG | CI requerida del SHA resulta SKIPPED/FAILED/CANCELLED | No certificar A0–A4 como PASS |
| N12 | NEG | Documentación provisional intenta liberar M01.06/M02/QA/PROD | Bloquear hasta dependencias, tests y A5 causal |

**P=4; N=12.** Son contratos de aceptación futura, no tests live ejecutados.

## 6. DAG, clasificación CI y reglas de salida

DAG_TECNICO marca C02-03.M01.02 F=`VALIDADO`, G=`SI` para **preparación documental M01.01–05**; 19 prerequisitos de preparación: seis C00-01.M02, cinco C00-03.M01, cinco C01-01.M01 y tres C02-02.M01.01–03. PLAN_MAESTRO H/I, COLA G/O se revalidan antes del claim. Si una premisa queda bloqueada o A5 falla, no liberar implementación/M01.06/M02 por inercia.

- `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` — sólo documento y CHANGELOG, sin código ni datos modificados.
- `requiere_security=SI_DOCUMENTAL` — JWT/RBAC, estados, permisos plataforma vs empresa, aislamiento A/B, idempotencia y PII.
- `requiere_scope_lock=SI` — tras commit directo dev exigir `verify-solqaryn-project-scope` completed/success sobre el SHA exacto.
- **A0:** revisar diff, ausencia de secretos y referencias; **A1:** ocho actores, doce IO y casos P4/N12; **A2:** rechazos cruzados, revocación, JWT/PII, fallos transaccionales; **A3:** cinco blobs, DAG 19/19, V5 y Sheet (BITACORA A:Z, 26 columnas); **A4:** SHA, integridad post-commit, CI pertinente y readback; **A5:** otro TASK_ID revisa independientemente, sin autocertificación.

**No-alcance:** no cambiar Empresa/UsuarioEmpresa, JWT/RBAC, API, migraciones/EF, Angular, colas reales, Aiven, Render, proveedores, datos, IAM, DNS, Cloudflare, QA/main/PROD, PR, ramas, credenciales o automatizaciones. Inventario histórico de imágenes cero: migración NO_APLICA.

**Rollback:** reversión Git aditiva del commit documental después de revalidar HEAD, sin force-push. Al cerrar contratos C00/C01/C02-02 y antes de diseño ejecutable deben revalidarse los supuestos de estados, retención y autorización.
