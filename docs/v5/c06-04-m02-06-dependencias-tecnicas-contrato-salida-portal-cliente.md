# SOLQARYN V5 FINAL — C06-04.M02.06: dependencias técnicas y contrato de salida M02 del portal de cliente

**Tipo de entrega: contrato documental de preparación, no implementación.** Repositorio `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autor técnico: PRIMARY DEV 03, `6adc2e4bd8a48191ac7d8641ed95a1a3`. Autoridad: *STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES* V5 FINAL, microtarea `C06-04.M02.06`, PLAN_MAESTRO fila 985. Alcance contractual: registro, recuperación, pedidos, reservas, membresías, documentos y preferencias del portal de cliente. **No se crean endpoints, DTO C#, EF entities, tablas, migraciones, JWT, flags, workers, eventos, webhooks ni dispatchers.**

## 1. Preflight y DAG de entrada comprobado

El claim individual comenzó desde PLAN `PENDIENTE` sin owner, COLA `G=SI/O=SI` y DAG `F=VALIDADO/G=SI`, con CAS sobre `H985` y readback de owner único. Los gates de documentación precedentes son específicos y no autorizan trabajo runtime:

| Predecesor | SHA de artefacto original | Alcance certificado únicamente para DEV |
| --- | --- | --- |
| C06-04.M01.06 | `12ed2af6682679fb371f67e3fbefda68839d6ea1` | Contrato de fase M01, LISTO y revisión A5 separada |
| C06-04.M02.01 | `3742c0153a8266d1b1f2f8ff7b82873114cad407` | ENT01–ENT16 y dieciséis invariantes conceptuales; A5 independiente |
| C06-04.M02.02 | `77ac1c3d830b3fed7cd099bd3f1b320d414fe950` | Ocho rutas API candidatas, DTO, errores seguros; A5 independiente |
| C06-04.M02.03 | `f519507dfebd142a34d2f7b72df930100a894702` | Doce modelos y diez restricciones de datos propuestos; A5 independiente |
| C06-04.M02.04 | `87a8ff9e23925e3b6d2d9998b81c9c941b67725a` | Configuración y flags tipados inertes; A5 independiente |
| C06-04.M02.05 | `ec0c7714fc96d375ee037a1d19a7f4ddb4d4f367` | Doce eventos conceptuales sin ejecución; A5 independiente |

La validez del DAG es dinámica: antes de reclamar cualquier M03, la próxima Task debe verificar otra vez sus fórmulas, PLAN H/I/K y la revisión **A5 independiente de esta M02.06**. Un LISTO_DEV sin A5 no es autorización para construir el siguiente scope técnico.

## 2. Mapa de dependencias de implementación M03–M06

| Capa / consumidor | Contrato de entrada M02 | Acción futura, solo después de gate propio |
| --- | --- | --- |
| Domain | Identidad de cliente, ownership e invariantes | Comprobar pertenencia tenant+cliente, versiones y denegaciones; no depender de HTTP/EF |
| Application | API01–08 y operaciones autorizadas | Casos de uso con validación positiva/negativa, cancelación, conflictos 409 y políticas de acceso |
| Infrastructure / MySQL | DB01–12, IDX01–10 y fixtures | Inventariar esquemas existentes, diseñar migración aditiva/rollback y pruebas MySQL 8.4 efímero sin tocar producción |
| API ASP.NET Core 10 | DTO mínimos y errores neutros | Autenticación JWT CUSTOMER separada de ERP, autorización server-side, rate limiting y contratos versionados |
| Angular 22.2.1 | UX privada y DTO tipados | Pantallas accesibles, estados vacíos/errores, guards solo de experiencia e invalidación de caché por identidad |
| Documentos / medios | Grants privados con TTL | Enlaces firmados cortos, revocación, no-store, restricción por tenant+CustomerId, sin URL pública |
| Recuperación | Challenge hasheado y único uso | Uniformidad de respuesta para cuenta existente/inexistente, protección de enumeración y revocación de sesiones |
| Eventos futuros | EVT01–12, outbox e idempotencia inertes | No instalar dispatcher, broker, worker, webhook ni envío; todo requiere diseño, cuotas y autorización independiente |
| Seguridad e integración | JWT/RBAC, A/B y A1/A2 | Test xUnit/HTTP negativo, integración efímera DB, pruebas Angular/Playwright y check exact-SHA |
| QA/main/PROD | Ninguna autorización en M02 | Bloqueados hasta orden explícita, A5 independiente y gates específicos |

## 3. Invariantes de seguridad no negociables

1. El servidor autentica principal CUSTOMER y comprueba membresía vigente antes de construir consultas; ERP_ADMIN no se hereda por un JWT cliente.
2. Tenant A nunca puede acceder a B y cliente A1 de A nunca puede consultar ni modificar recursos de A2.
3. EmpresaId, CustomerId, Host, slug, header, cookie, URL y body del navegador no sustituyen la identidad resoluble y verificable del servidor.
4. El ownership se comprueba **antes** de filtrado, paginación, caché, emisión de enlaces o eventos; referencias opacas por sí solas no son grants.
5. Acceso desconocido, expirado o revocado deniega por defecto; 401/403/404 se diseñan sin enumeración ni filtrado lateral.
6. Versiones de perfil/preferencia y escritura autorizada exigen CAS; una carrera no puede sobrescribir recursos ajenos.
7. Recuperación exige token hasheado ligado a sujeto, tenant, canal, TTL, límite de intentos y consumo único transaccional.
8. Las credenciales, JWT, PII y challenge nunca se guardan en URLs públicas, analytics, eventos, logs, archivos HTML ni respuestas de error.
9. Documentos se entregan por grant privado revocable, con expiración y controles de no-caché; Cloudinary no equivale a permiso.
10. Los flags tipados son condiciones de visibilidad/capacidad, jamás fuentes de autorización; defaults falsos y claves desconocidas denegadas.
11. Idempotencia liga clave+fingerprint+tenant+cliente+operación; replay divergente devuelve conflicto, sin doble efecto.
12. Los eventos futuros no transportarán secretos/PII; correlationId es sólo diagnóstico, nunca autoridad ni clave de deduplicación.
13. Toda transferencia entre backend, proveedor o base de datos exige tolerancia a fallo, auditoría y recuperación transaccional.
14. Consultas de pedidos, reservas y membresías no aseguran stock, disponibilidad, beneficios o cupos hasta confirmación real del servidor.
15. Las pruebas de UI no sustituyen controles backend y las pruebas de documentos no sustituyen MySQL/HTTP/JWT reales.
16. Todo rollback futuro de datos requiere ensayo, snapshot verificable y reversibilidad sin pérdida de evidencia histórica.

## 4. Matriz de aceptación futura: doce parejas positivas y negativas NO EJECUTADAS

| Caso | Camino positivo que debe probar una futura microtarea | Camino negativo / rechazo obligatorio |
| --- | --- | --- |
| PN01 | Cliente A1 autenticado obtiene sólo su perfil | A2 y tenant B reciben rechazo sin datos |
| PN02 | Actualización de perfil A1 con versión vigente | Versión obsoleta y CustomerId falsificado no alteran fila |
| PN03 | Orden propia de A1 paginada en tenant A | Cursor de B/A2 no filtra ni recupera pedidos ajenos |
| PN04 | Solicitud neutra de recuperación con cuota | Usuario inexistente recibe respuesta indistinguible |
| PN05 | Challenge válido se consume una sola vez | Reuso, expiración o cambio de tenant son rechazados |
| PN06 | Reserva propia muestra estado auténtico | Reserva ajena o cupo no confirmado nunca se promete |
| PN07 | Membresía vigente se presenta con permisos | Membresía revocada o de otra cuenta no concede beneficios |
| PN08 | Grant de documento propio con TTL vigente | Grant de A2/B, revocado o caducado no entrega bytes |
| PN09 | Preferencia tipada propia guarda con CAS | Flag/rol desconocido o versión vieja no amplía permisos |
| PN10 | Dos comandos idénticos se deduplican | Misma clave con distinto fingerprint produce conflicto |
| PN11 | Cambiar sesión A→B limpia cachés y vistas | Respuesta tardía de A no aparece dentro de B |
| PN12 | Error parcial conserva transacción y auditoría | Timeout proveedor/DB no emite éxito ni reintentos ilimitados |

Estos son **criterios de prueba propuestos**, no pruebas realizadas ni tasas de cobertura. M03 es código real y requiere CI dirigida de la tecnología efectivamente modificada. No confundir checks auxiliares cancelados/omitidos con PASS.

## 5. Contrato de salida M02 y gates para el siguiente scope

**Entrega documental verificable:** los cinco artefactos M02.01–05 permanecen inertes; la matriz anterior consolida consumidores, trust boundaries, planes de regresión, referencias y condiciones de rollback. Salida M02.06 sólo puede marcar LISTO_DEV con A0 análisis y diff, A1 coherencia de contratos/escenarios, A2 aislamiento de tenant/cliente y control de permisos a nivel estático, A3 trazabilidad de interfaces y pruebas futuras explícitamente NO_APLICA para Markdown, y A4 publicación fast-forward sobre HEAD, Scope Lock y check V5 de procedencia `completed/success` exact-SHA, registro idempotente y readback institucional.

**Gate A5 posterior:** otra de las cinco Tasks Javier —distinta de DEV03— inspeccionará SHA, diff, blobs, autoría, los checks causales y la no atribución de runtime antes de registrar A5_PASS o A5_FAIL. A5 no es certificación de seguridad productiva ni autorización para QA/main/PROD.

**No-alcance y rollback:** cero código ejecutable, endpoints, datos, migraciones, servicios externos, usuarios, secrets, DNS, proveedores, webhooks, pagos o despliegues. Reversión de este changeset sólo mediante `git revert` normal, sin force-push ni manipulación histórica de BITACORA. Cualquier corrección futura revalida HEAD, DAG, CAS de owner, pruebas exact-SHA y permiso específico.