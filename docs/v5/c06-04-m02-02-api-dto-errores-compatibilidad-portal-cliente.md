# SOLQARYN V5 FINAL — C06-04.M02.02: API/DTO, errores y compatibilidad del portal de cliente

**Entrega documental, sin ejecución de runtime.** Autor PRIMARY DEV 02 JAVIER_CHATGPT (`6af16bb9492c8191b8e8c7d606f11507`), rama dev, PROJECT_SCOPE_LOCK=STRICT. Fuente única STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO fila 981, COLA G/O=SI y DAG_TECNICO fila 942 F=VALIDADO/G=SI al claim.

## 1. Precedentes, límites y autoridad de datos

C06-04.M01.06 llegó a LISTO con A5 ajena y SHA `12ed2af6682679fb371f67e3fbefda68839d6ea1`. C06-04.M02.01 define ENT01–ENT16, I01–I16 y PN01–PN12 en `docs/v5/c06-04-m02-01-entidades-ownership-invariantes-portal-cliente.md`, SHA `3742c0153a8266d1b1f2f8ff7b82873114cad407`. Este artefacto no altera ese contrato, ni presupone que una propuesta es ruta implementada. Portal actual de cuenta/pedidos NO acredita todavía reservas, membresías, documentos y recuperación universal.

La identidad, el tenant y el CustomerId efectivo proceden del backend autenticado y la membresía vigente, nunca del Host, slug, EmpresaId, header, parámetro, cookie o body del navegador. JWT de cliente no equivale a JWT de operador ERP. A y B son empresas distintas; A1 y A2 son clientes distintos de la misma empresa: **ambos límites se comprueban antes de leer, ordenar o paginar**. Desconocido = denegación, sin enumeración de recursos.

## 2. Ocho operaciones candidatas FUTURAS_NO_IMPLEMENTADAS

| Código | Contrato propuesto, no ruta actual acreditada | Autorización y comportamiento |
| --- | --- | --- |
| API01 | `GET /customer-portal/me` | JWT CUSTOMER + tenant+CustomerId server-side, DTO minimizado, no datos ERP |
| API02 | `PATCH /customer-portal/profile` | Campos allowlisted, dueño activo y `expectedVersion`, 409 ante carrera |
| API03 | `POST /customer-portal/recovery/challenge` | Respuesta indistinguible para cuenta existente/inexistente, rate limit, TTL |
| API04 | `POST /customer-portal/recovery/confirm` | Token hash ligado a actor/tenant, único uso, revocación al confirmar |
| API05 | `GET /customer-portal/orders` | Consulta filtrada por tenant+cliente antes de cursor, tamaño acotado |
| API06 | `GET /customer-portal/reservations` | Lectura de reserva propia y estado de servidor, no creación ni garantía de cupo |
| API07 | `GET /customer-portal/documents/{opaqueId}` | Grant privado propio, TTL, URL temporal no indexable ni públicamente cacheable |
| API08 | `PUT /customer-portal/preferences` | Consentimiento tipado/versión, rechazo de códigos no allowlisted |

No crear controladores, clases C#, nuevas rutas HTTP, banderas operativas, migraciones, endpoints de pago, workers, SMTP ni webhooks a partir de esta tabla. Al ejecutar M03 se hará inventario de rutas/DTO existentes y se decidirá versionado aditivo o mapeo sin romper contratos consumidos.

## 3. DTO propuesto y fuente de confianza

| Campo | Proveniencia o regla |
| --- | --- |
| `customerRef` | ID opaco de principal autenticado servidor; nunca confiar en un body con otro CustomerId |
| `tenantRef` | Resuelto en backend + membresía vigente; no desde `X-Empresa-Id` |
| `principalKind` | CUSTOMER separado de ERP_ADMIN; sin escalamiento RBAC |
| `displayName` | Texto validado y con límite; escapar HTML antes de renderizar |
| `verifiedContactRef` | Proyección privada mínima, sin email de terceros |
| `expectedVersion` | Versión persistida para CAS, denegación 409 si obsoleta |
| `challengeRef` | Identificador opaco, hash del token almacenado, un solo uso/TTL |
| `orderRef`, `reservationRef` | Recursos opacos siempre filtrados por tenant+CustomerId |
| `documentRef` | Referencia a grant temporal; no Cloudinary público ni credenciales |
| `preferenceCode` | Enum tipado/allowlisted, no texto que se interprete como permiso |
| `pageCursor`, `pageSize` | Cursor ligado a tenant, cliente, filtro y versión; máximo backend |
| `correlationId` | Diagnóstico saneado sin autoridad, no clave de idempotencia |
| `occurredAtUtc`, `statusCode` | UTC servidor y código de estado neutro, sin PII |

Campos futuros de contrato; no afirmar clases, columnas ni políticas de expiración presentes en el código.

## 4. Doce errores propuestos con respuesta segura

| Código | Semántica de futura implementación |
| --- | --- |
| E01 UNAUTHENTICATED | 401 genérico sin existencia de cuenta |
| E02 CUSTOMER_FORBIDDEN | 403 por permiso/revocación sin revelar roles |
| E03 RESOURCE_NOT_VISIBLE | 403/404 neutro ante B o A2 ajenos |
| E04 TENANT_CONTEXT_INVALID | Denegar tenant del cliente incompatible con backend |
| E05 PROFILE_INPUT_INVALID | 400 validado por campo, sin eco de HTML/PII |
| E06 VERSION_CONFLICT | 409, no sobrescribir versión ganadora |
| E07 CHALLENGE_EXPIRED | Error neutro, no reutilizar desafío |
| E08 RECOVERY_RATE_LIMIT | Limitar intentos sin enumeración |
| E09 DOCUMENT_GRANT_DENIED | Rechazar enlace propio vencido o ajeno |
| E10 CURSOR_SCOPE_INVALID | Rechazar cursor B/filtros de otra sesión |
| E11 RESERVATION_STATE_UNAVAILABLE | Sin confirmación ficticia de disponibilidad |
| E12 CONSENT_INVALID | Rechazar preferencia no permitida sin activar eventos |

Estos códigos son conceptuales; la API futura debe mapearlos a HTTP compatible con clientes existentes y mensajes accesibles, nunca enviar stacktrace, JWT, token de recuperación, email privado, SQL ni conexión.

## 5. Ocho pares de escenarios de aceptación FUTUROS_NO_EJECUTADOS

| ID | Positivo requerido futuro | Negativo requerido futuro |
| --- | --- | --- |
| PN01 | A1 lee perfil propio del tenant A | B o A2 no ven datos de A1 con IDs manipulados |
| PN02 | A1 actualiza perfil con versión correcta | expectedVersion viejo devuelve conflicto sin escritura |
| PN03 | Recuperación envía token a contacto previamente verificado | Cuenta inexistente no puede enumerarse ni filtra email |
| PN04 | Token de un uso confirma y revoca challenge | Token vencido, repetido o de B se rechaza |
| PN05 | Paginación de pedidos A1 preserva orden/tenancy | Cursor B e `EmpresaId` falso no acceden a A |
| PN06 | Consulta de reserva propia refleja estado servidor | Reserva de A2/B y cupo inexistente no se confirman |
| PN07 | Grant temporal permite documento A1 | URL vencida/ajena o pública denegada, `no-store` |
| PN08 | Preferencia válida registra versión y consentimiento | Flag desconocido o JWT de otro dominio no concede permiso |

Estos 16 casos son **oráculos**, NO pruebas ejecutadas. M03 y auditorías posteriores deberán ejecutar xUnit, HTTP JWT/RBAC, MySQL 8.4 efímero y Playwright/Chromium por tenant A/B y cliente A1/A2. Ni el documento ni un build verde sustituyen integración real.

## 6. Arquitectura, compatibilidad, seguridad y rollback

Domain: propiedad y versiones; Application: casos de uso y autorización efectiva; Infrastructure: consultas tenant-bound, auditoría y transacciones futuras; API: contratos versionados y respuestas neutras; Angular: guards solo UX, limpiar caché al cambiar tenant/cliente y bloquear respuestas tardías. Revisar consumidores existentes y pruebas de no regresión antes de modificar DTO/rutas. No aceptar token de identidad de query, no compartir JWT cliente/ERP, no exponer PII ni secretos en logs, URL, DOM o errores. Integraciones actuales Cloudinary/SMTP requieren adaptación futura controlada, sin llamadas en este changeset.

Rollback de esta entrega **solo Markdown**: `git revert` normal sin force-push, preservando BITACORA histórica y HEAD de autores ajenos; migraciones y revocación runtime no aplican hoy. Cualquier despliegue, IAM live, MySQL real, datos de clientes, DNS, QA/main/PROD requiere permiso y gates separados.

## 7. Evidencia A0–A5 y continuación

- A0: identidad GitHub/Drive Javier, skill canónica, HEAD/SCOPE, predecesor M01.06 LISTO+A5, PLAN H981/I981 y CAS condicional, COLA y DAG exactos.
- A1 documental: ocho operaciones, DTO, doce errores, ocho pares PN cotejables; sin marcar ejecución funcional.
- A2 estática: rechazo A/B y A1/A2, principal CUSTOMER vs ERP, recuperación no enumerable y control CAS.
- A3 documental: trazabilidad a entidades/invariantes de M02.01 y fronteras Domain/Application/Infrastructure/API/UI; compilaciones y DB NO_APLICA al diff Markdown.
- A4: fast-forward con `expected_sha`, diff y blobs exactos, Scope Lock y procedencia V5 `completed/success` del SHA publicado, BITACORA A:Z y readback PLAN/COLA/DAG.
- A5: revisión independiente posterior por otra Task Javier, nunca por su autor, antes de liberar descendientes técnicos.

**No representa certificación de endpoints, MySQL, JWT real, pagos, reserva real, webhooks, Cloudflare o producción.**
