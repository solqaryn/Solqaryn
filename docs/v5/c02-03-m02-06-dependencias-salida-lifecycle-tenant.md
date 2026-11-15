# SOLQARYN V5 FINAL — C02-03.M02.06: dependencias y contrato de salida del ciclo de vida del tenant

**Autor:** PRIMARY DEV 01, JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-15. **Repositorio/ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Resultado contractual y no-alcance

Esta microtarea consolida cinco contratos M02 documentales y fija la puerta de salida para una futura implementación verificable del lifecycle de empresas. **No crea endpoints, servicios .NET, DTO reales, entidades EF, migraciones, cambio de estado real de empresa, automatizaciones, outbox, event dispatcher, webhooks, jobs ni flags activas.** Tampoco implica backup/restore real, cambios de proveedores, permisos, suscripciones ni publicación QA/main/PROD.

La línea base actual contiene operaciones de empresas en API ASP.NET Core net10.0 y backend Domain–Application–Infrastructure, MySQL/EF Core y membresía `UsuarioEmpresa`. El frontend Angular 22.2.1 solamente ofrece experiencia; la autoridad de plataforma, JWT, RBAC, membership y tenant debe resolverse en el servidor. El atributo legado de activación o marcado no constituye por sí mismo una máquina de estados certificada.

## 2. Predecesores LISTO + A5 independiente

| ID | Evidencia conceptual revisada | Límite no delegado |
| --- | --- | --- |
| C02-03.M01.06 | Contrato terminal de preparación, bloqueos y reversión | Un diagrama no realiza transiciones ni soft-delete |
| C02-03.M02.01 | E01–E14 e INV01–INV16, titularidad de empresa, actores, estado y evidencia | No instala EF ni cambia entidades |
| C02-03.M02.02 | Rutas futuras de transición global, DTO conceptual, validaciones, errores y 8 pares P/N | No registra endpoints ni confiere rol global |
| C02-03.M02.03 | Persistencia propuesta, cinco conceptos, ocho índices, seis gates de migración y 12 fixtures futuros | No ejecuta SQL, seeds o restore |
| C02-03.M02.04 | 18 opciones de configuración futura, 12 reglas de precedencia y 8 pares P/N | Las flags siguen sin activación real |
| C02-03.M02.05 | 10 señales futuras no emitidas, 16 campos de sobre y 14 reglas de idempotencia | No existe dispatcher desplegado por este contrato |

Cada M02.01–05 está LISTO_DEV y tiene A5 independiente verificado en PLAN_MAESTRO/ BITACORA antes de habilitar M02.06 por DAG F=VALIDADO/G=SI y COLA G/O=SI. Los A5 acreditan **coherencia documental**, no pruebas de integración de lifecycle real.

## 3. Dieciséis condiciones de salida (OUT01–OUT16)

| Gate | Criterio antes de cualquier cambio ejecutable |
| --- | --- |
| OUT01 | Enumerar estados de tenant y transiciones permitidas con matriz explícita de origen/destino |
| OUT02 | Toda transición requiere actor global autenticado en backend, permiso por acción y auditoría |
| OUT03 | JWT vigente, membresía/atribución comprobada y revocación deben fallar cerrado |
| OUT04 | Un operador de tenant A no puede cambiar ni inferir estado de tenant B |
| OUT05 | Rutas globales futuras no deben confluir con PATCH/activar/desactivar de clientes legados |
| OUT06 | Versionado esperado y CAS de estado efectivo rechazan actualizaciones obsoletas o concurrentes |
| OUT07 | Idempotencia de operación/actor/empresa/revisión impide reintentos contradictorios |
| OUT08 | Retención, baja lógica, restauración y reversibilidad exigen autoridad y evidencia de datos separadas |
| OUT09 | Configuración futura tipada y flags deben iniciar apagadas para acciones no implementadas |
| OUT10 | Estados desconocidos, claims o flags desde navegador no crean privilegios de plataforma |
| OUT11 | Evidencias de migración, reconciliación y copia/restore deberán probarse en MySQL efímero |
| OUT12 | La consistencia de membresías, sesiones, jobs y datos tras suspensión/activación deberá comprobarse |
| OUT13 | Errores saneados y correlación no filtran tokens, DSN, PII ni existencia de terceros |
| OUT14 | Las señales futuras precisan límites, deduplicación, orden y control de permisos; no se emiten ahora |
| OUT15 | CI GitHub Actions por SHA exacto comprobará pruebas positivas/negativas .NET/API/MySQL/seguridad cuando el diff lo requiera |
| OUT16 | Sólo un owner exclusivo y pruebas A0–A4 reales dan LISTO_DEV; A5 de otra Task Javier y autorizaciones separadas para otros entornos |

## 4. Diez parejas positivas/negativas futuras (PN01–PN10): NO EJECUTADAS

| Caso | Positivo previsto | Negativo obligatorio |
| --- | --- | --- |
| PN01 | Operador global auténtico pide transición permitida | Operador tenant A intenta actuar sobre tenant B |
| PN02 | JWT válido con permiso de plataforma | Token ausente, revocado o rol de tenant común |
| PN03 | Revisión actual y CAS único registran cambio | Peticiones concurrentes intentan saltarse revisión |
| PN04 | Reintento idéntico conserva resultado idempotente | Misma clave cambia empresa, payload o destino |
| PN05 | Transición ACTIVO→SUSPENDIDO conserva trazabilidad | Suspensión sin prueba afecta sesión de otra empresa |
| PN06 | Reactivación autorizada verifica precondiciones | Flag de UI activa tenant sin autorización backend |
| PN07 | Baja lógica con política de retención aprobada | Marcar propiedad legacy se trata como borrado irreversible |
| PN08 | Restore efímero e integridad verificados soportan rollback | Documento o git revert se presenta como restauración MySQL |
| PN09 | Evento futuro incluye correlación saneada | Log filtra JWT, DSN o datos de tenant ajeno |
| PN10 | CI exact-SHA completa con éxito en etapa M03 | CANCELLED/SKIPPED/IN_PROGRESS se declara PASS |

**Ninguno de esos veinte escenarios se ejecutó** en xUnit, HTTP, EF/MySQL, Playwright, Docker o producción. M03 exigirá código real y pruebas causales; M02.06 no reemplaza esas obligaciones.

## 5. Dependencias de M03 y decisiones pendientes

El DAG consultado para C02-03.M03.01 exige C02-03.M02.01–06 **LISTO + A5**, además de C02-02.M06.06 + A5. El cierre de este documento deja **A5_PENDIENTE** y no satisface C02-02.M06.06; por ello M03 permanece bloqueado, sin forzar G/O. Las sucesoras de C02-03.M03 tendrán prerequisitos adicionales propios que deberán revalidarse al ejecutarse.

La etapa M03 requiere implementar Domain (transiciones y validaciones), Application (casos de uso con autoridad), autorización backend, persistencia/compensación cuando se autorice, integraciones y auditoría. Se deben mantener las fronteras tenant A/B, JWT/RBAC, ausencia de secretos en repositorio y exclusividad de owner. No hay permiso para data migrations, cambio de esquema, cuentas o dominio sin gate específico.

## 6. Riesgos y rollback

- R01: carrera de estado/revisión — operación atómica real o rechazo; nunca confundir lectura+escritura con CAS.
- R02: elevación global — autorización explícita verificada del lado servidor, con revocación.
- R03: interferencia entre empresas — tests tenant A/B y verificaciones negativas antes de cambios runtime.
- R04: persistencia parcial — estrategia de rollback y compensación probadas en entorno efímero.
- R05: retención y baja — preservar datos y obligaciones legales de conservación, sin asumir borrado efectivo.
- R06: señales — no implementar webhooks, dispatchers ni mensajes entre Tasks mediante este documento.
- R07: regresión — controles actuales de API, sesión y estado no pueden degradarse para facilitar migración.
- R08: control de release — DEV nunca equivale a QA/main/PROD.

**Rollback aplicable a este changeset:** revert Git normal de Markdown y CHANGELOG preservando historia, no restauración de datos. Las operaciones de DB/servicio futuras requerirán respaldos, prueba de restore y autorización propia.

## 7. A0–A5 y entrega

**A0** revisa diff de dos Markdown, encabezado CHANGELOG autocontenido y fuentes actuales. **A1** comprueba OUT01–16 y PN01–10 como criterios documentales (no suites ejecutadas). **A2** analiza JWT, RBAC, seguridad de plataforma, tenant A/B, datos sensibles y denegación por defecto. **A3** contrasta documentos M01.06, M02.01–05, arquitectura y DAG, sin afirmar que M03 esté listo. **A4** exige publicación fast-forward/CAS sobre HEAD actual, controles `verify-solqaryn-project-scope` y procedencia V5 `completed/success` en SHA exacto, readbacks PLAN/COLA/DAG/BITACORA; compilaciones runtime de diff Markdown-only son NO_APLICA y **nunca PASS**. **A5** deberá hacerse por otra de las cinco Tasks Javier distinta de la autora, con SHA/blobs/CI y readback, sin activar QA/main/PROD.
