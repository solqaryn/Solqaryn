# C03-01.M02.06 — Dependencias y contrato de salida M02: UX_LOGIN_INTELIGENTE_V1

**SOLQARYN V5 FINAL · `solqaryn/Solqaryn@dev` · PROJECT_SCOPE_LOCK=STRICT.** Ejecutor documental: DEV01 (TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`). Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, PLAN_MAESTRO!D445/E445, COLA!G445/O445 y DAG_TECNICO!A421:G421. Esta unidad **define y verifica la salida documental de M02**: no implementa ni certifica login, JWT, RBAC, MFA, HTTP, migraciones, persistencia, un nuevo selector o producción. El texto acumulativo de E445 no convierte a esta unidad en la certificación de M03–M06.

## 1. Gate real de entrada y autoridades verificadas

Snapshot previo al cambio: PLAN!H445=`PENDIENTE`, PLAN!I445 vacío, COLA!G/O445=`SI`, DAG!F421=`VALIDADO`, DAG!G421=`SI`. V17 namedRange `SQV17_C03_01_M02_06` sobre PLAN!H445:I445, ID `2000000445`, y CAS exactamente una ocurrencia para EN_PROGRESO, con owner DEV01 y evento idempotente. Commit base observado: `15af903da233785eb1a9e73cdf8b97a980014c93`. La elegibilidad **no** autoriza editar COLA/DAG ni tocar entornos fuera de dev.

El DAG existente exige `C03-01.M01.01–06+A5`, `C03-01.M02.01–05+A5` y `C02-04.M01.06+A5`. El readback acredita las 5 M02 previas como LISTO con A5 independiente, M01.06 y C02-04.M01.06 en LISTO+A5; la habilitación del DAG es una fórmula, no una certificación de API. Estos hechos se deben revalidar después de cada transición.

| Contrato antecedente | Fuente ya versionada | Qué entrega y qué **no** acredita |
| --- | --- | --- |
| C03-01.M01.06 | `docs/v5/c03-01-m01-06-bloqueo-rollback-ux-login-v1.md` | L01–L10, B01–B06, compatibilidad y rollback de diseño; no ejecución HTTP |
| C03-01.M02.01 | `docs/v5/c03-01-m02-01-entities-ownership-invariants-ux-login-v1.md` | ownership Usuario/UsuarioEmpresa/Empresa/Rol e invariantes 0/1/N; no grant real |
| C03-01.M02.02 | `docs/v5/c03-01-m02-02-api-dto-errors-compat-ux-login-v1.md` | DTO/errores y rutas hoy existentes (login, renovar, tenant-context); no endpoint nuevo |
| C03-01.M02.03 | `docs/v5/c03-01-m02-03-preferencia-usuario-persistencia-indices-migracion.md` | modelo de preferencia propuesta, índices, CAS y fixtures; **sin** migración ejecutada |
| C03-01.M02.04 | `docs/v5/c03-01-m02-04-login-config-flags.md` | flags tipados **sólo visuales** con 13/13 Vitest sobre SHA 15af903d; no MFA ni RBAC en servidor |
| C03-01.M02.05 | `docs/v5/c03-01-m02-05-eventos-idempotencia-correlacion-limites-ux-login-v1.md` | eventos/correlación sin PII y singleflight propuestos; no nuevo worker |
| C02-04.M01.06 | PLAN!H403/K403, bitácora A5 independiente | Puerta previa ya aprobada para documentación; no credenciales productivas |

Los contratos transversales existentes C01-01.M02.06, C02-01.M02.06 y C03-02.M02.06 figuran LISTO con A5 documental en PLAN; se preservan como **referencias**, no se añaden como nuevos bloqueos DAG. En el censo actual C01-01 tiene 36/36 LISTO, C02-01 23/36 y C03-02 12/36; las partes restantes de estos últimos bloques **no están certificadas** y su propia elegibilidad queda exclusivamente bajo sus fórmulas. No se anuncia un cierre de seguridad global.

## 2. Contrato de salida 0/1/N — casos falsables para implementación posterior

| Caso | Entrada y resultado esperado | Denegación / adversarial bloqueante |
| --- | --- | --- |
| X01 — 0 | Actor sin membresías activas: ninguna empresa seleccionada | No inferir tenant de host, ruta, localStorage, rol heredado ni preferencia |
| X02 — 1 | Una sola membresía vigente de A: autenticar una vez, validar A y mostrar panel directo | Preferencia previa B, rol legado o credencial revocada no conceden B |
| X03 — N | Dos o más membresías vigentes: selector accesible y pista autorizada | Nunca enumerar empresas ajenas ni seleccionar por índice sin GET de contexto |
| X04 — concurrencia | Revalidar al confirmar empresa y antes de operación de recurso | Revocación entre lista y selección deniega; snapshot viejo no autoriza |
| X05 — JWT | Credencial firmada, issuer/audience/exp válidos y autorización del servidor | Token vencido, manipulado, credencial débil o cuenta bloqueada: 401/403 |
| X06 — roles | Grant explícito de plataforma separado del rol de empresa | Admin de empresa nunca se convierte en superusuario de plataforma |
| X07 — sesión | Renovación autenticada con actor actual; revocación prevalece | JWT anterior no reanima membresía, rol o sesión cancelada |
| X08 — MFA | Política del servidor exige desafío; flags sólo cambian presentación | `bypassMfa`/config local nunca apaga desafío requerido |
| X09 — preferencia | Hint opcional aislado por UsuarioId, revalidado por membresía | TOCTOU, FK cruzada o versión CAS obsoleta no escriben ni eligen otro tenant |
| X10 — repetición | Un POST de login por intento y correlación acotada | Doble clic/Enter, reintento tras timeout ambiguo o replay no duplican efecto |
| X11 — error | 400/401/403/429/503 se distinguen sin fugas | 503 por integración ausente no cuenta como 403 probado; no filtrar PII/JWT |
| X12 — rollback | Revertir una implementación futura sin pérdida ni grants residuales | Migración Down destructiva, cache cross-tenant o restablecer sesión revocada: NO_GO |

Se trata de **oráculos documentados, NO resultados de pruebas ejecutadas**. La implementación M03–M05 deberá probar casos positivos/negativos en .NET 10 y API JWT firmada, RBAC/tenant A/B y revocación con MySQL 8.4 efímero cuando cambie EF o persistencia; Playwright/Chromium si se modifica UI/routing. No sustituir integración por simulaciones, build verde o una prueba puramente tipada.

## 3. Condiciones de bloqueo, liberación y rollback

La salida atómica de **esta** M02.06 requiere: (a) conservar las referencias documentales reales y el DAG existente; (b) matriz 0/1/N y riesgos contradictorios visibles; (c) diff de este documento + sección autocontenida de CHANGELOG; (d) Scope Lock y validador V5 exitosos en SHA exacto, PLAN/COLA/BITACORA con readback idempotente; (e) revisión A5 **posterior** por Task Javier distinta de DEV01. Hasta esa A5, la salida documental queda LISTO_A0_A4/A5_PENDIENTE sin forzar sucesores.

Los bloqueos técnicos siguen siendo independientes y exigibles: B01 credenciales/selección incorrectas; B02 JWT/RBAC/tenant A/B y propiedad; B03 persistencia/TOCTOU/idempotencia MySQL; B04 UX accesible, singleflight y errores; B05 pruebas causales y calidad; B06 rollback/observabilidad/PII. P0/P1 abiertos o CI obligatoria fallida/in_progress bloquean el **scope afectado**. Nunca declarar PASS de SKIPPED/CANCELLED. A5 documental no autoriza QA/main/PROD, pagos, secretos, migraciones reales ni una próxima microtarea si el DAG propio permanece NO.

**Impacto:** sólo `docs/v5/` y `CHANGELOG_AI.md`; no cambian Domain, Application, Infrastructure, API, Angular, DTO, esquema, políticas, endpoints, eventos, integraciones o cuentas. Consumidores: revisores del contrato UX_LOGIN_INTELIGENTE_V1 y etapas C03-01.M03–M06, únicamente al abrirse su DAG individual. Riesgo R0 por diff documental; criticidad R2 se retiene para las futuras implementaciones de autenticación. **Rollback de este changeset:** git revert ordinario de las dos entradas, bajo HEAD/lease nuevos y sin force-push ni borrar BITACORA. Rollback de persistencia/seguridad futuro necesita ensayo causal separado, no se realizó aquí.

## 4. Evidencia V15 A0–A5 y traspaso verificable

- **A0:** PLAN D445 y E445 (meta acumulativa), contratos de M01/M02, dependencias exactas del DAG y código de referencia cotejados; matriz de doce oráculos y límites explícitos.
- **A1:** consistencia documental de 0/1/N, positivo/negativo, errores/fronteras y checklist de regresión; los tests firmados HTTP/MySQL/Chromium no se ejecutaron en este diff.
- **A2:** separación de autoridad servidor/flags visuales, roles, revocación, IDOR/BOLA, PII, TOCTOU e idempotencia; **diseño, no certificación viva**.
- **A3:** NO_APLICA a test MySQL/HTTP/browser porque el diff no modifica runtime; futuros M03–M05 deben acreditar su integración.
- **A4:** pendiente de verificar contra el commit publicado: Scope Lock exact-SHA completed/success, CHANGELOG V5 completed/success, no regressiones causales, CAS y readback PLAN/COLA/BITACORA; trabajos obligatorios no completados mantienen VALIDANDO.
- **A5:** PENDIENTE — revisor Javier distinto del autor DEV01, revisión real de doc, dependencias, SHA, riesgos y pruebas pertinentes. No autocertificación.

**Handoff:** verificar Source SHA publicado, los checks de Scope Lock/V5, confirmar PLAN H/I, BITACORA event_id y que DAG/cola sigan siendo exclusivamente fórmulas. Cuando A0–A4 realmente aprueben, pasar VALIDANDO→LISTO con V17/CAS y registrar A5_PENDIENTE. Sólo la A5 independiente de esta unidad puede satisfacer el gate siguiente si la fórmula exige su evidencia.
