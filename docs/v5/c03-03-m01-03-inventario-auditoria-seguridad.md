# SOLQARYN V5 FINAL — C03-03.M01.03: inventario de contratos, código y evidencia de auditoría de seguridad

**Resultado exigido:** leer contratos, código y evidencia sin asumir certificación. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT. **Fuente única:** documento V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, `PLAN_MAESTRO/COLA/DAG_TECNICO`. Preparación M01, DAG `VALIDADO` y diez antecedentes `C02-04.M01.01–05` y `C03-01.M01.01–05` comprobados LISTO. Este documento no implementa ni acredita nuevas funciones de auditoría, no habilita M01.06/M02, QA/main/PROD ni acceso privilegiado.

## 1. Inventario de fuentes vivas y certeza limitada

| Archivo inspeccionado en dev | Git blob SHA | Contrato que puede inferirse de lectura | No acreditado por ese archivo |
| --- | --- | --- | --- |
| `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | `[Authorize]`, `RequierePermiso(Auditoria,Ver)`, rol administrativo y `GET /auditoria` | Aislamiento A/B HTTP y retención de registros |
| `backend/src/Application/Services/AuditoriaService.cs` | `def2c1f7bc58e23f98a1065fcdf0963322f3705c` | `RegistrarAsync` best-effort captura excepción; `RegistrarEstrictoAsync` propaga; tenant desde marcas de confianza, correlación de TraceIdentifier | Durabilidad en todas las operaciones, redacción universal de valores serializados |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Campos de actor/usuario, EmpresaId opcional, modulo/acción/resultado/error, IP y correlación | Cifrado, TTL/retención ni filtro seguro por el modelo solo |
| `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` | `8fc9417b88da91ad1a5777ea77d9e31756230384` | Persiste y pagina filtros por usuario/modulo/fecha/resultado y texto | Este método no añade filtro explícito de empresa; revisar `AppDbContext` y ámbito real antes de certificar multiempresa |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Limita `X-Correlation-ID` a 64 caracteres ASCII seguros; crea GUID si es inválido; lo refleja en TraceIdentifier | No equivale a clave idempotente ni correlación persistida después de fallo DB |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | ArgumentException recibe mensaje de error neutral y log estructurado sin texto arbitrario | No demuestra redacción de cada excepción/DTO o error de proveedor |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Login exitoso y rechazo llaman auditoría; renovar requiere token válido | Un evento login best-effort no certifica persistencia ni recuperación ni revocación global |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Prueba con mock respuesta/auditoría sin marcadores sintéticos | No prueba HTTP real, DB, SMTP o logs de terceros |

Los SHA son **blobs de código** inspeccionados, no resultados de ejecución. Se deben releer hashes y pruebas contra el SHA candidato de cualquier implementación posterior. El hecho de que `RegistroAuditoria.EmpresaId` sea nullable o que un método no filtre empresa explícitamente es un **riesgo a evaluar**, no por sí solo prueba de fuga: pueden existir filtros globales y autorizaciones aguas arriba; no afirmar lo contrario sin evidencia.

## 2. Matriz de cobertura y faltantes (positivos/negativos)

| ID | Caso y fuente | Qué muestra la inspección | Prueba aún necesaria |
| --- | --- | --- | --- |
| INV01 | + `GET /auditoria` con administrador y permiso | Guarda adicional `EsAdministrador` y atributo de permiso | Prueba HTTP positiva, resultados A del tenant A |
| INV02 | - Usuario no administrador consulta auditoría | `AuditoriaController` llama `Forbid()` en esa condición | Prueba HTTP 403 sin registros ni PII |
| INV03 | - Auditor de A solicita tenant B con header/consulta | Repositorio no muestra filtro `EmpresaId` explícito | Test MySQL A/B + query filters/consumidores; no acreditar aislamiento |
| INV04 | + Alta de evento de cambio crítico | Servicio arma RegistroAuditoria, llama AddAsync y SaveChangesAsync | Integración transaccional real en MySQL efímero |
| INV05 | - Falla al guardar auditoría obligatoria | Variante best-effort captura excepción mientras estricta la propaga | Test fallo DB, rollback/fail-closed en flujo crítico |
| INV06 | - Header malicioso con CRLF o longitud 65+ | `CorrelationIdMiddleware` delimita caracteres y reemplaza | Prueba HTTP que impida log injection |
| INV07 | + Correlación válida en request/response/registro | Valor saneado transferido a TraceIdentifier | Caso HTTP + persistencia y correlación coherente |
| INV08 | - Error con dato privado en argumentos | Middleware evita texto arbitrario ArgumentException en respuesta/log | Casos otras excepciones y redacción en DB + proveedor |
| INV09 | - Contraseña, JWT o PII en cambios antes/después | Servicio serializa `valoresAnteriores` y `valoresNuevos` como JSON sin redacción genérica demostrada | Inspección de eventos persistidos y políticas de minimización |
| INV10 | - Retención vencida/exportación no autorizada | Entidad y repositorio leídos no acreditan TTL/política final | Contrato de retención, pruebas de purga autorizada y ACL export |
| INV11 | - Acceso excepcional sin MFA, motivo o TTL | Fuentes leídas no acreditan workflow de elevación temporal | Diseño autorizado + casos negativos de escalamiento y expiración |
| INV12 | control | A5 independiente, Scope Lock exacto, BITACORA A:Z | El mecanismo institucional exige 26 campos y Task revisor distinto | Readback institucional y CI causal por commit |

Las filas INV01–INV12 establecen cobertura del inventario y **futuras verificaciones**; no son pruebas que se hayan corrido aquí. El alcance de la fase C03-03 exige en el futuro autorización/denegación, cambios críticos y accesos excepcionales sin secretos, logs estructurados, correlación, retención y no exposición. Este M01.03 solo identifica la distancia entre requisitos y evidencia existente.

## 3. Riesgos que quedan pendientes de resolución técnica

**R1 — aislamiento en lectura de auditoría:** comprobar si un filtro EF global acota `RegistrosAuditoria` y cómo el controller establece ámbito; posible exposición transversal a investigar sin afirmar vulnerabilidad demostrada. **R2 — semántica de auditoría best-effort:** no confundir llamada a `RegistrarAsync` con registro durable; para operaciones obligatorias verificar el uso de `RegistrarEstrictoAsync`, límites transaccionales y comportamiento ante error. **R3 — manejo de valores sensibles:** revisar seriaciones `ValoresAnteriores/ValoresNuevos/Motivo/Error/UserAgent` y redacción antes de guardar, logging y exportar. **R4 — correlación:** ID técnico no es prueba de permiso ni deduplicación; validar longitud y origen. **R5 — gobierno de retención/accesos excepcionales:** no se certifica a partir de los archivos leídos; exigir política, proveedor, autorización y pruebas explícitas antes de implementar.

Un hallazgo documental no permite alterar automáticamente autorizaciones, secretos, migraciones, proveedores ni datos productivos. Las tareas futuras deben diseñar pruebas que distingan positivo real, negativo real, mocks, in-memory y MySQL/HTTP verificables.

## 4. Auditorías y entrega M01.03

| Gate | Evidencia proporcional a este inventario |
| --- | --- |
| A0 | Control de Markdown, ocho rutas y SHA, 12 casos distintos, cero credenciales literales, sin código ejecutable ni cambio de seguridad |
| A1 | INV01 y INV04 positivos, INV02/INV03/INV05/INV06 negativos, todos con oráculo y faltante concreto; no fingir tests |
| A2 | JWT/RBAC/tenancy A/B, PII, auditoría estricta, correlación, grants y accesos excepcionales quedan delimitados como riesgos, sin bypass |
| A3 | Contratos entre controller/servicio/repository/modelo/middleware y diez DAG predecesores LISTO; runtime NO_APLICA |
| A4 | Commit FF `dev`, SHA exacto, diff documental, Scope Lock `completed/success`, PLAN/COLA/BITACORA y rollback |
| A5 | `PENDIENTE` de otra Task Javier autorizada distinta de DEV02, con un positivo y un negativo de inventario |

`requiere_dotnet=NO_APLICA`; `requiere_node_angular=NO_APLICA`; `requiere_mysql=NO_APLICA`; `requiere_docker=NO_APLICA`; `requiere_e2e=NO_APLICA` porque este diff únicamente documenta. `requiere_security=SI_INSPECCION_DOCUMENTAL`; `requiere_scope_lock=SI`.

**Rollback:** revertir únicamente el commit documental mediante `git revert`, conservando historia y eventos idempotentes de BITACORA 26 columnas. No hay ramas nuevas, PR, cambios de usuarios, JWT, ACL, base de datos, QA/main/PROD o despliegue. M01.06 y M02 siguen bajo sus propias puertas y no se liberan por esta preparación.
