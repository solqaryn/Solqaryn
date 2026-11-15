# SOLQARYN V5 FINAL — C02-04.M02.06: dependencias técnicas y contrato de salida de consola global

**Responsable:** PRIMARY DEV 01, JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT. **Fecha:** 2026-11-15.

## 1. Entrega real y alcance excluido

El alcance de M02.06 es consolidar contratos de diseño de consola global, enumerar riesgos y fijar criterios de salida comprobables para implementación posterior. **No crea consola privilegiada real, API o DTO compilados, módulos Angular, IAM/MFA, acceso global, autorización, migraciones, secretos, eventos, webhooks, jobs, cuentas, cambios de proveedor o despliegue.** Ningún contrato escrito sustituye una evaluación de seguridad, una prueba HTTP o una revisión de permisos efectivos.

La línea base usa ASP.NET Core 10 y Angular 22.2.1, EF Core/MySQL, JWT Bearer, RBAC relacional y contexto empresarial. La autoridad sobre plataforma y tenants pertenece al servidor autenticado; no se toma de roles textuales, parámetros Angular, storage cliente ni headers no validados.

## 2. Dependencias verificadas por PLAN, DAG y A5

| Prerrequisito | Evidencia documental existente y estado |
| --- | --- |
| C02-04.M01.06 | Contrato terminal de preparación LISTO con A5 independiente; no instala IAM |
| C02-04.M02.01 | Diseño de entidades, ownership e invariantes de consola global; LISTO+A5 |
| C02-04.M02.02 | API/DTO, errores y compatibilidad conceptual para administración global; LISTO+A5 |
| C02-04.M02.03 | Persistencia, índices, migraciones y fixtures proyectados; LISTO+A5, SQL no ejecutado |
| C02-04.M02.04 | Configuración, ámbitos y flags seguros futuros; LISTO+A5, sin flags activas |
| C02-04.M02.05 | Eventos, idempotencia, correlación y límites conceptuales; LISTO+A5, sin dispatcher |

La fórmula del DAG permitió M02.06 sólo después de que esos cinco contratos y la fase anterior acreditaron estado y revisión. Sucesoras M03 tienen gates propios; **VALIDANDO o LISTO sin A5 de M02.06 no habilitan código**. No editar fórmulas ni forzar SI.

## 3. Condiciones de salida (GATE01–GATE16)

| Control | Requisito futuro comprobable |
| --- | --- |
| GATE01 | Separar identidad de plataforma y operador de tenant mediante claims servidor con autoridad verificada |
| GATE02 | Autenticar JWT y comprobar RBAC/membresías vigentes por cada acción, nunca sólo una vez en UI |
| GATE03 | Denegar elevación por rol textual, query, cookie arbitraria, `localStorage` o cabecera manipulable |
| GATE04 | Aislar empresas A/B y ocultar existencia y datos de terceros en respuestas, búsqueda y auditoría |
| GATE05 | Exigir MFA real según política de privilegios, reautenticación sensible y revocación comprobable |
| GATE06 | Mantener permisos mínimos por acción y traza de aprobaciones, sin administrador universal implícito |
| GATE07 | Versionar contratos de API/DTO, errores saneados y compatibilidad con clientes existentes |
| GATE08 | Toda mutación administrativa usa versión/CAS y clave idempotente bajo autoridad del servidor |
| GATE09 | Prohibir que cliente seleccione secretos, DSN, proveedor, credenciales, tenant target o scopes ajenos |
| GATE10 | Configuración y flags futuras permanecen apagadas hasta implementación y revisión independiente |
| GATE11 | Cambios de persistencia requieren migración reversible, control FK y pruebas con MySQL efímero |
| GATE12 | Logs y eventos futuros ocultan PII, JWT, DSN, tokens y datos de empresas cruzadas |
| GATE13 | Cuotas, rate limiting, antifraude y fallos parciales deben denegar de forma predecible |
| GATE14 | Rollback de estado/datos implica prueba real de restauración, no git revert documental |
| GATE15 | CI dirigida por SHA exacto validará backend, seguridad, Angular, HTTP, base y permisos cuando corresponda |
| GATE16 | A0–A4 reales, A5 independiente, owner exclusivo y permiso de entorno separado antes de avance/deploy |

## 4. Casos positivos y negativos (PN01–PN10), FUTUROS NO EJECUTADOS

| Par | Escenario positivo futuro | Rechazo negativo obligatorio |
| --- | --- | --- |
| PN01 | Operador plataforma con permiso específico consulta un recurso autorizado | Tenant A consulta detalles de tenant B |
| PN02 | JWT/MFA vigentes permiten acción sensible | Token revocado o MFA ausente intenta misma acción |
| PN03 | Permiso por acción y membresía real autorizan | String `superadmin` o header forjado conceden privilegio |
| PN04 | Versión vigente ejecuta modificación idempotente | Dos peticiones con revisión obsoleta producen doble cambio |
| PN05 | API compatible devuelve error saneado y correlación | Endpoint revela stack SQL, token, PII o recurso tercero |
| PN06 | Configuración fuente servidor valida flag | Flag de frontend desactiva RBAC o aislamiento |
| PN07 | Tenant A recibe sólo agregados propios autorizados | Exportación/paginación mezcla datos de A y B |
| PN08 | Prueba MySQL efímera valida reversibilidad | DDL sin restore ni comprobación FK se considera listo |
| PN09 | Auditoría futura descarta secretos y limita eventos | Webhook duplica orden, excede cuota o envía datos ajenos |
| PN10 | CI requerida `completed/success` en SHA pertinente | `cancelled/skipped/queued/failure` se contabiliza PASS |

**Estas pruebas son oráculos de diseño escritos; ningún test .NET, HTTP, navegador, MySQL, MFA o integración real se ejecutó por este changeset.**

## 5. Riesgos, dependencias y autorización

- **R01 autoridad global:** crear consola antes de IAM/MFA independiente expone todas las empresas; se mantiene NO_GO.
- **R02 pertenencia y alcance:** revocación de permisos y operaciones cross-tenant requieren cobertura A/B positiva y negativa real.
- **R03 retención/PII:** búsquedas, exports y logs deberán limitar resultados y datos sensibles por política validada.
- **R04 compatibilidad:** DTO nuevos no podrán romper API comercial actual sin versión, regresión y rollback.
- **R05 persistencia:** cambios de esquema o administración masiva requieren transacción/compensación, backup y restore probados.
- **R06 idempotencia:** reintentos y cambios concurrentes no deberán duplicar efectos ni falsificar propietario.
- **R07 distribución:** no implementar Tasks, dispatcher o webhooks por autorización de documentación M02.
- **R08 promoción:** Scope Lock STRICT y A5 documental no autorizan QA, main, PROD, DNS, secretos o cuentas de pago.

La fase M03 requiere implementación auténtica en Domain/Application/API/Infrastructure (y UI cuando corresponda), con CI GitHub Actions de .NET 10, Angular 22.2.1, tests de seguridad, tenant A/B y servicios efímeros MySQL si el alcance lo exige. El gate C02-04.M03.01 se reconsulta tras A5; no puede liberarse por este texto.

## 6. A0–A5, evidencia y reversión

**A0:** revisar sólo dos Markdown y encabezado de CHANGELOG con viñetas contiguas. **A1:** consistencia de GATE01–16 y PN01–10, sin atribuir ejecución. **A2:** validar el contrato JWT/RBAC, MFA conceptual, aislamiento A/B, fail-closed y confidencialidad, sin certificar seguridad runtime. **A3:** cotejar los cinco M02 previos, M01.06 y DAG/PLAN/COLA por ID, sin crear dependencias falsas. **A4:** fast-forward con SHA esperado, check-runs Scope Lock y procedencia V5 `completed/success` en SHA exacto, diff/blobs y BITACORA/PLAN/COLA/DAG readback; compilaciones ajenas al diff documental se clasifican `NO_APLICA`, nunca PASS. **A5:** otra Task Javier distinta de la autora revisará evidencia real, SHA, blobs, seguridad y dependencia antes de habilitar sucesoras.

**Rollback documental:** `git revert` normal sobre el commit en dev con protección de cambios concurrentes; no restaura MySQL ni revoca accesos reales (ninguno fue creado). Prohibido force-push, QA/main/PROD y alterar Tasks o cuentas.
