# SOLQARYN V5 FINAL — C06-04.M02.04: configuración tipada y flags del portal de cliente

**Entrega DOCUMENTAL, sin implementación de configuración o feature flags en runtime.** Autor PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507, rama dev, Scope Lock STRICT. Fuente única STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN fila 983, COLA G/O=SI, DAG fila 944 F=VALIDADO/G=SI; M01.06 LISTO+A5, M02.01 define entidad/política, M02.02 contrato de API, M02.03 esquema futuro. La disponibilidad DAG de esta M02 es documental y no autoriza IAM, JWT, DB, deploy ni acciones sobre clientes reales.

## 1. Fronteras y precedentes

El portal de cliente comprende registro, recuperación, pedidos, reservas, membresías, documentos y preferencias. Una flag no concede autorización; el backend sigue resolviendo tenant, principal CUSTOMER, CustomerId y membresía vigente con JWT y RBAC. No confiar en EmpresaId, Host, slug, header, cookie o body. Dos empresas A/B y dos clientes A1/A2 de A deben quedar separados en configuración, sesiones, caché y respuestas.

**Ninguno** de los siguientes nombres es una opción C# existente o una variable de entorno activa. Antes de implementarlas inventariar las settings actuales y consumidores de frontend/API. Rechazar valores desconocidos, degradación de seguridad y overrides de cliente; nunca aceptar código, HTML, SQL, expresiones, URL arbitraria ni secretos mediante configuración editable.

## 2. Catorce opciones tipadas PROPUESTAS

| Código | Opción candidata | Tipo / ámbito / default seguro |
| --- | --- | --- |
| CFG01 | PortalCustomerEnabled | bool por tenant, default false; no cambia grants |
| CFG02 | PortalRecoveryEnabled | bool backend por tenant, default false hasta tests de recuperación |
| CFG03 | RecoveryChallengeLifetimeMinutes | entero 5–30, global de seguridad, sin override cliente |
| CFG04 | RecoveryMaxAttempts | entero 1–10 por política backend, fail-closed |
| CFG05 | RecoveryCooldownSeconds | entero 30–3600, global, sin desactivar rate limit |
| CFG06 | CustomerProfileEditableFields | conjunto allowlisted de campos, nunca ID de tenant ni rol |
| CFG07 | CustomerSessionMaxAgeMinutes | entero acotado, TTL servidor, revocación prevalece |
| CFG08 | CustomerOrderPageSizeMax | entero 1–100, solo lectura y filtro por ownership |
| CFG09 | CustomerReservationsReadEnabled | bool por tenant, false si falta validación de estado/cupo |
| CFG10 | CustomerMembershipViewEnabled | bool por tenant, no concede beneficios vencidos |
| CFG11 | CustomerDocumentsEnabled | bool por tenant, false hasta grants privados expiran y prueba A/B |
| CFG12 | PrivateDocumentGrantTtlSeconds | entero 30–300, política backend, sin URL pública |
| CFG13 | CustomerPreferencesEnabled | bool por tenant, false hasta consentimiento auditado/CAS |
| CFG14 | CustomerPortalCorrelationEnabled | bool de diagnóstico, no autoriza ni incluye PII |

Tipos/intervalos son **hipótesis de contrato** sujetas a revisión de seguridad y configuraciones reales; no son settings implantadas. Flag false nunca debe romper recursos ERP preexistentes. Flag true sólo permite mostrar una capacidad cuando todas sus comprobaciones backend individuales siguen vigentes, no amplía roles ni permisos.

## 3. Doce reglas de validación y precedencia segura

| ID | Regla futura |
| --- | --- |
| R01 | Cualquier clave desconocida se rechaza; no se interpreta como texto ejecutable |
| R02 | Override desde cliente, Host, query, cookie o header no puede definir tenant/CustomerId |
| R03 | Tipos enteros y rangos fuera de límites se rechazan, no se truncan en silencio |
| R04 | Valores globales de seguridad prevalecen sobre preferencias de tenant más permisivas |
| R05 | Flags visibles no conceden RBAC ni acceso a recurso oculto en backend |
| R06 | Cambios de tenant A→B invalidan configuración, caché y respuestas tardías de A |
| R07 | Principal CUSTOMER jamás obtiene grants ERP por configuración de UI |
| R08 | Recovery disabled/expirado/fallido no concede sesión ni confirma correo enumerado |
| R09 | Document grants y acceso a PII requieren chequeo ownership por solicitud, no flag previa |
| R10 | Versionado y CAS de settings para evitar overwrite; auditoría mínima por cambio |
| R11 | Config pública está separada de secretos y sólo presenta claves allowlisted |
| R12 | Ausencia de fuente, fallo de parseo o servicio => default seguro/denegación |

Ámbitos a separar: SECURITY_GLOBAL (no editable por empresa), TENANT_POLICY (solo administrador autorizado bajo límites globales), CUSTOMER_PREFERENCE (solo su propietario), PUBLIC_DISPLAY (proyección sin secretos). Ninguna capa de frontend puede elevar políticas backend.

## 4. Ocho escenarios positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| PN01 | Tenant A habilita portal con membresía y JWT validado | Flag A no activa portal B, usuario anónimo sigue denegado |
| PN02 | Recuperación A1 válida usa TTL y cuota permitidos | TTL inválido o múltiples intentos se rechazan sin enumeración |
| PN03 | Perfil A1 actualiza campo permitido con expectedVersion | Campo role/tenant/CustomerId y versión obsoleta rechazados |
| PN04 | Pedidos A1 paginan al tamaño máximo permitido | PageSize desmedido o cursor A2/B no fuga datos |
| PN05 | Reserva propia se consulta tras flag habilitada | Reserva A2/B o estado sin cupo no confirmado |
| PN06 | Documento A1 expira y se revoca correctamente | URL pública/TTL fuera de rango/flag B denegados |
| PN07 | Preferencia A1 persiste consentimiento tipado | Campo desconocido/código HTML/cliente A2 denegado |
| PN08 | Cambio de tenant limpia caché, configuración y sesión | Respuesta tardía A no repuebla UI/permiso de B |

Estos son requisitos de pruebas posteriores, **NO casos ejecutados**. M03–M05 requieren tests backend, JWT/RBAC reales, MySQL efímero, Angular y Playwright; no cuentan como PASS en esta fase documental.

## 5. Compatibilidad, impact analysis y rollback

Domain futuro impone invariantes de ownership; Application aplica defaults fail-closed y validación server-side; Infrastructure persiste settings versionadas de manera transaccional cuando proceda; API sólo expone proyección pública allowlisted; Angular consume señales y guarda/limpia estado por identidad. No modificar ahora paquetes, ENV, configuración de producción, JWT, MFA, EF, MySQL, redirects, DNS, Cloudflare ni secretos. No romper flags/contratos existentes: mapping explícito, versión de contrato y pruebas de regresión antes de código.

Observabilidad futura: registrar código de error, versión y correlationId saneados, sin JWT, PII, claves, tokens ni datos cross-tenant. Apagado seguro de cada flag debe mantener grants revocados e invalidar caché. Rollback de este changeset documental = git revert normal, sin force-push ni tocar BITACORA. Rollback futuro de settings debe ensayarse con versión, política y auditoría.

## 6. A0–A5 documentales

A0: Skill instalada = canónica, Scope Lock, identidad Javier, PLAN H/I, DAG F/G y COLA G/O, predecesores y claim exclusivo. A1: catorce opciones, cuatro ámbitos, doce reglas y ocho parejas PN definidas sin simular pruebas. A2: límites tenant A/B, A1/A2, JWT CUSTOMER/ERP, RBAC, PII, valores tipados/default denegar. A3: compatibilidad propuesta con capas actuales sin cambiar runtime; compilación/backend/Angular/MySQL/Playwright NO_APLICA a un diff Markdown y no son PASS. A4: publicación fast-forward expected SHA de este documento y sección completa de CHANGELOG, checks Scope Lock y V5 completed/success exact-SHA, BITACORA y readback. A5: Task Javier distinta del autor revisará evidencia antes de liberar sucesoras técnicas.

**No crear flag operativa, servicio, worker, migración, endpoint, cliente de proveedor, suscripción ni promoción QA/main/PROD.**
