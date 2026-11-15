# SOLQARYN V5 FINAL — C15-02.M02.04: configuración tipada, ámbitos y flags seguros para auditoría de migración

**Corte:** 2026-11-14 (America/Tegucigalpa). **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Ámbito:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT.

## 1. Alcance, autoridad y dependencias

Contrato documental del plan STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL. Consultar PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA actuales. C15-02.M01.06 posee LISTO y A5 independiente; M02.01 y M02.02 ya presentan contratos lógicos de entidades y API/DTO, pero ninguno habilita cambios de runtime. Esta tarea independiente M02.04 define ámbitos y valores tipados conceptuales, no una configuración activa.

Stack de referencia: Angular 22.2.1, ASP.NET Core 10, EF Core 10 y MySQL proveedor Oracle declarado, JWT/RBAC/tenancy en backend. Vercel/Render/Aiven/Cloudinary se declaran actuales sin verificar infraestructura viva; Hostinger/Cloudflare/R2 son objetivos, no activaciones. No se cambian clientes, código, bases, archivos, secretos, DNS, roles, flujos de producción, Tasks, calendarios, branches, QA/main/PROD ni integraciones.

## 2. Ámbitos de configuración documentales S01–S06

| ID | Ámbito tipado lógico | Alcance de lectura | Restricción |
| --- | --- | --- | --- |
| S01 | PUBLIC_DOCUMENT | Información pública redactada | Sólo allowlist de campos públicos |
| S02 | TENANT_PRIVATE | Datos específicos del tenant | Contexto JWT/membresía y backend |
| S03 | SERVICE_INTERNAL | Parámetros internos de un servicio | Sólo aplicación autenticada |
| S04 | ENV_DEV | Entorno de preparación dev | No implica QA/main/PROD |
| S05 | MIGRATION_PLAN | Diseño futuro de migración | Nunca activa proveedor |
| S06 | SECURITY_POLICY | Políticas JWT/RBAC e aislamiento | No editable mediante UI común |

Un valor recibido por UI nunca concede permisos. La decisión real de pertenencia tenant proviene de identidad autenticada y verificación backend. SERVICE_INTERNAL y SECURITY_POLICY requieren revisión del código/autoridad real antes de cualquier implementación. Ningún flag representa credencial o secreto.

## 3. Registro conceptual de valores CFG01–CFG14

Las claves de la tabla son **contratos futuros**, no nombres de settings que ya existan en el SaaS. Los defaults son intencionalmente fail-closed y no se escriben en archivos de despliegue. Una fila sin readback del recurso real permanece NO_ACREDITADO.

| ID | Campo conceptual | Tipo/allowlist | Ámbito lógico | Default de diseño | Garantía de seguridad |
| --- | --- | --- | --- | --- | --- |
| CFG01 | providerTarget | enum {current,planned} | SERVICE_INTERNAL | current | No habilitar proveedor planned |
| CFG02 | tenantVisibility | enum {public,private} | TENANT_PRIVATE | private | No aceptar autoridad del cliente |
| CFG03 | dataStoreMode | enum {current,isolated-test} | ENV_DEV | current | No conectar DB futura real |
| CFG04 | backupVerification | enum {unknown,verified} | SERVICE_INTERNAL | unknown | verified sólo con checksum+restore |
| CFG05 | objectCountStatus | enum {unknown,measured} | SERVICE_INTERNAL | unknown | unknown distinto de 0 |
| CFG06 | hostBindingPolicy | enum {strict,blocked} | SECURITY_POLICY | blocked | No host spoofing ni defaults libres |
| CFG07 | tlsValidationMode | enum {required,blocked} | SECURITY_POLICY | blocked | No autoaceptar certificados |
| CFG08 | migrationEligibility | enum {denied,review} | MIGRATION_PLAN | denied | review no ejecuta corte |
| CFG09 | rollbackRequirement | bool | MIGRATION_PLAN | true | No permitir false sin autorización |
| CFG10 | evidenceRevision | sha40|unknown | SERVICE_INTERNAL | unknown | SHA no nativo no se inventa |
| CFG11 | auditReadMode | enum {readonly} | ENV_DEV | readonly | No escrituras a proveedores |
| CFG12 | featureDraftEnabled | bool | ENV_DEV | false | true sólo validación documental |
| CFG13 | tenantQuota | int >=0, capped | TENANT_PRIVATE | 0 | 0 deniega por defecto |
| CFG14 | notificationMode | enum {none,manual} | SERVICE_INTERNAL | none | No crear webhooks/eventos |

CFG04 no acredita backup ni restore. CFG05 no declara conteo de archivos. CFG08 sólo permite evaluar elegibilidad del plan, nunca autoriza migración. CFG14 es exclusivamente diseño; no implementa dispatcher ni llamadas entre Tasks. No existe regla que permita cambiar fórmulas G/O ni abrir QA/main/PROD por la activación de un flag.

## 4. Precedencia y reglas de conflicto

- PR01: política de seguridad del backend se aplica antes que cualquier configuración editable de tenant.
- PR02: DEV nunca sobrescribe autorización de QA/main/PROD o secretos; todo entorno distinto requiere orden específica.
- PR03: a un ámbito privado se accede sólo con pertenencia tenant, autorización y revisión de roles vigentes.
- PR04: un valor de tenant sin owner verificado se rechaza, en vez de asociarse por slug, host, header o body.
- PR05: ante valores duplicados, tipo inválido o versión obsoleta, devolver error tipado y dejar configuración anterior sin cambios.
- PR06: no modificar parámetros compartidos si otro owner escribe o falta CAS legítimo.
- PR07: el default de una opción nueva debe ser denegación segura, no activación implícita.
- PR08: un campo de flags no puede habilitar migraciones, backups, DNS, JWT, RBAC, IAM, datos productivos, webhooks o servicios de pago.

## 5. Matriz negativa de validación N01–N12 (FUTUROS_NO_EJECUTADOS)

| ID | Caso negativo futuro | Resultado exigido |
| --- | --- | --- |
| N01 | scope público recibe propiedad privada | DENY_SCOPE_CROSSING |
| N02 | tenant B sobrescribe config de A | TENANT_MISMATCH |
| N03 | flag desconocida o typo | UNKNOWN_FLAG |
| N04 | valor fuera del enum/tipo | INVALID_TYPE |
| N05 | providerTarget planned se trata como activación | PROVIDER_NOT_AUTHORIZED |
| N06 | host derivado de slug no confiable | HOST_BINDING_DENIED |
| N07 | backup unknown se vuelve verified sin restore | EVIDENCE_NOT_VERIFIED |
| N08 | objectCountStatus unknown se interpreta 0 | OBJECT_COUNT_NOT_PROVEN |
| N09 | SHA de evidencia no coincide con HEAD | SOURCE_MISMATCH |
| N10 | featureDraftEnabled true habilita QA/PROD | ENVIRONMENT_NOT_AUTHORIZED |
| N11 | notificationMode manual crea dispatcher | EVENT_EXECUTION_NOT_AUTHORIZED |
| N12 | rollbackRequirement false sin nueva orden | ROLLBACK_REQUIRED |

Casos positivos futuros correspondientes: identidad/tenant correctos, valor de catálogo permitido y default seguro; versión de fuente idéntica al SHA esperado; sólo dev y sólo lectura; configuración validada sin persistencia; owner y A5 distintos donde sean exigibles. Ningún caso se ha ejecutado en backend, UI, MySQL, Playwright ni proveedores externos.

## 6. Evidencia, trazabilidad, rollback y A0–A5

El trabajo pendiente de implementación requerirá prueba positiva/negativa real, valor anterior/nuevo, owner/tenant, SHA exacto, autorización diferenciada, traza redactada y plan de rollback con lectura posterior. La prueba de este documento sólo certifica claridad de esquema y no servicio. M02.05 (eventos) y M02.06 (gate terminal) permanecen microtareas separadas con DAG propio.

**A0:** análisis estático de STANDARD, M01.06, modelos M02, arquitectura, PLAN/COLA/DAG. **A1:** 6 ámbitos, 14 claves tipadas y 12 negativos documentales consistentes; NO_APLICA a casos runtime no implementados. **A2:** revisión de JWT/RBAC/tenant A/B fail-closed y ausencia de secretos. **A3:** compatibilidad conceptual con API .NET/Angular/EF sin modificar interfaces. **A4:** Git fast-forward/CAS, diff sólo dos Markdown y checks Scope Lock y procedencia V5 completed/success exact SHA, PLAN/COLA/BITACORA readback. **A5:** revisión por otra Task Javier de distinto TASK_ID.

Rollback mediante git revert normal y evento BITACORA compensatorio; prohibido reescribir registros, force-push o modificar recursos externos.
