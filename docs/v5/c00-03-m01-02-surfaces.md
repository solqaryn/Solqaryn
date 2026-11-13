# C00-03.M01.02 — Entradas, salidas, actores y superficies de la matriz V5 integrada

**Fuente única:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, V5 FINAL, Drive Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **ID:** `C00-03.M01.02`; **unidad:** `C00-03 Matriz integrada de alcance / M01`; **rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Ejecutor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`.

Este documento identifica las **entradas, salidas, actores y superficies** que la matriz deberá cubrir. Es preparación transversal documental, **no** la matriz exhaustiva de las 2.340 unidades, ni implementación/certificación de otras unidades.

## Entradas autorizadas y validación de procedencia

| Entrada | Fuente/verificación | Consumo autorizado | Falla que debe bloquear |
| --- | --- | --- | --- |
| Plan V5 FINAL y su secuencia de 2.340 IDs | Google Doc V5; extremos `C00-01.M01.01` y `C15-05.M06.06` | Identificar requisito, ID, resultado, prueba y no-alcance | Plan antiguo, requisitos duplicados o un ID 2.341 |
| Estado de una unidad | `PLAN_MAESTRO!A:O` con autoridad en `H` | Filtrar `PENDIENTE/EN_PROGRESO/VALIDANDO/LISTO` y responsable | Derivar estatus de notas, de un commit o de un mock |
| Elegibilidad y prerequisitos | `COLA!E/G/O`, fórmulas y `DAG_TECNICO` validado | Comprobar `G=SI`, `O=SI`, no-owner, prerequisitos por ID | Interpretar `SI` sin DAG o puertas de fase como autorización universal |
| Evidencia de auditoría | `BITACORA!A:Z` (26 campos; EVENT_ID en A e IDEMPOTENCY_KEY en R), eventos idempotentes y fuente de pruebas | Traza de claim, versión, SHA, A0–A5, revisores separados | Contar evento ausente, duplicado, inventado o `SKIPPED` como PASS |
| Código y dependencias actuales | `solqaryn/Solqaryn@dev`, Skill canónica, `AGENTS.md`, Scope Lock, `ARCHITECTURE.md`, componentes | Contrastar requisito contra implementación y tests, nunca asumir despliegue | Usar SHA antiguo, rama incorrecta o artefactos ajenos al scope |
| Pruebas remotas causales | Workflows GitHub Actions, jobs/steps/logs/artifacts por SHA exacto | A0–A4 proporcionales cuando el resultado realmente exige código/CI | Substituir test real requerido por fixture o CI de otro SHA |
| Restricciones de seguridad/operación | `docs/AUTOMATION_V13.md`, JWT/BCrypt/RBAC, membresía tenant, prohibiciones | Conservar fronteras de confianza, secretos y revisión independiente | Tenant enviado por navegador otorga permisos o saltar A5 |

## Actores y límites de autoridad

| Actor | Puede producir/validar | No puede inferir ni hacer |
| --- | --- | --- |
| Propietario del plan V5 | Definir alcance y autorizar cambios explícitos dentro de las políticas vigentes | No se presume autorización implícita de migración o PROD |
| PRIMARY DEV Javier y Alex, cada TASK_ID autenticado | Leer fuentes, reclamar solo ID elegible propio, producir A0–A4, dev directo y BITACORA | Suplantar Task ajena, modificar horarios/Alex desde Javier, crear PR o ramas V5 |
| Revisor independiente (TASK_ID distinto del autor) | A5 sobre SHA exacto y pruebas positivas/negativas pertinentes | Aprobar su propia autoría o desplegar QA/main/PROD por A5 solo |
| GitHub Actions runner efímero | Compilar/testear .NET 10, Node/Angular, MySQL 8.4, E2E o Docker **si un job real lo configura y ejecuta** | Operar como terminal persistente o probar un job no disparado |
| Aplicación backend autorizada | Decidir JWT, roles, membresías, tenant, persistencia y errores seguros | Confiar en `EmpresaId`, slug, URL, cabecera o cookie cliente como autoridad |
| UI/browser | Mostrar flujos, estados accesibles, navegación pública/privada; ejecutar pruebas front-end | Conceder por sí sola acceso a datos de empresa |

Las cuentas de Alex solo son administradas desde Alex. El inventario de seis Tasks Javier y dos Alex no autoriza actuar fuera de la cuenta y tarea acreditadas.

## Superficies afectadas y salidas de la futura matriz

| Superficie | Entradas / controles relevantes | Salida de la matriz a desarrollar posteriormente | Evidencia mínima antes de afirmar cumplimiento |
| --- | --- | --- | --- |
| Gobierno y asignación V5 | Plan/Sheet/DAG/Task owner | Relación requisito → unidad → responsable → estado | Lectura PLAN/COLA, claim sin colisiones, BITACORA y SHA |
| Dominio/aplicación ASP.NET | Validaciones y invariantes server-side | Contrato de reglas y casos de uso por ID | Tests .NET pertinentes, positivos/negativos y CI exact-SHA |
| API/seguridad/tenancy | JWT, RBAC, membresía, errores, rate limiting | Frontera y superficie HTTP con denegación por defecto | Positivo A y negativo B; sin PII ni autorización de browser |
| Infrastructure/EF/MySQL | Índices, transacciones, Oracle EF, migraciones | Contrato reversible con aislamiento de empresas | Job MySQL 8.4 real si exige DB, rollback/restore aislado |
| Angular/experiencia | Rutas, guards, formularios, estados, a11y, branding | Superficie de UI ligera y responsive | npm lint/build, Playwright/Chromium cuando aplique, no `SKIPPED` |
| Workers/proveedores/archivos | SMTP, Cloudinary, colas, límites y reintentos | Adaptadores idempotentes y reglas de archivos futuros | Tests integración reales según el proveedor, sin secretos ni datos PROD |
| Publicación/evidencia | commit FF `dev`, SHA, A0–A5, rollback | Mapeo reproducible entre artefacto y aceptación | Check Scope Lock, pruebas causales, BITACORA 26 campos A:Z y A5 ajeno |

Cada fila de esta tabla es una **superficie**, no una microtarea nueva ni una certificación transversal. La matriz requisito por requisito, sus contradicciones, riesgos y criterios de aceptación se desarrollan en `C00-03.M01.03–05` y los lotes posteriores.

## Dependencias, salida y no-regresión

El `DAG_TECNICO` habilita `C00-03.M01.01–05` como análisis documental independiente porque `C00-01.M02.01–06` están `LISTO`. **No** habilita `C00-03.M01.06`, `C00-03.M02–M06`, gates de fase ni migraciones; revalidar C00-01 final antes de implementar o promover. No modificar `COLA!A:O`, que permanece fórmula, ni añadir filas a auxiliares.

**Positivo verificable:** las siete entradas, seis tipos de actor y siete superficies pueden rastrearse a fuentes y pruebas esperadas; el documento distingue explícitamente lectura de estado, elegibilidad, ejecución, auditoría y revisión. **Negativos fail-closed:** nunca permitir un ID fuera de V5, una fuente histórica paralela, un claim duplicado, promoción sin A5_PASS, una autenticación basada en tenant cliente, MySQL/E2E no ejecutado o un evento ausente como éxito.

**Clasificación de esta entrega:** TIPO1_DOCUMENTACION. `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` (ningún ejecutable cambia); `requiere_security=SI` revisión de límites y referencias; `requiere_scope_lock=SI` para commit `dev`. A0 revisión estática de alcance/secretos, A1 entradas/salidas/positivos/negativos, A2 seguridad, A3 contraste con Plan/DAG/Skill, A4 diff/commit/bloques exact-SHA/readback y A5_PENDIENTE de otro TASK_ID. Estos controles no prueban runtime .NET, SQL o browser.

**Rollback:** `git revert` del commit específico tras inspeccionar HEAD, sin force-push ni borrar BITACORA. No alterar `qa`, `main`, PROD, DNS, R2, Hostinger, proveedores o recursos históricos. Imágenes históricas inexistentes: migración `NO_APLICA`; archivos futuros deben tener controles tenant y seguridad.


## Corrección causal A5_FAIL — esquema físico BITACORA A:Z/26 (2026-11-13)

El encabezado vivo de `BITACORA!A1:Z1` contiene **26 campos**; A=`EVENT_ID`, H=`RUN_ID`, R=`IDEMPOTENCY_KEY` y Z=`TIMESTAMP_UTC_ORIGINAL`. Los eventos nuevos usan 26 posiciones contiguas, EVENT_ID e IDEMPOTENCY_KEY únicos, autor y revisor acreditados y readback posterior. Si no se acredita RUN_ID nativo, H queda vacío. Los registros históricos de 21 campos y el A5_FAIL del SHA original se conservan sin borrado ni relleno ficticio.

**Positivo:** verificar 26 nombres y posiciones reales A:Z, correlación PLAN_MAESTRO/COLA, evento nuevo no duplicado y evidencia del SHA exacto. **Negativos:** rechazar vectores de 21 a 25 campos, EVENT_ID o IDEMPOTENCY_KEY repetidos, suplantación de TASK_ID/revisor, A5 autoprobado, un job SKIPPED como PASS exigido y promoción sin autorización a QA/main/PROD. La documentación no cambia runtime, MySQL, Angular, proveedores ni gates de otra fase; dotnet/Node/Angular/MySQL/Docker/E2E son NO_APLICA al diff documental, pero Scope Lock y seguridad documental sí aplican. Rollback de un commit sólo mediante `git revert` normal, sin force-push. Se solicita nueva A5 por Task distinta del autor sobre el commit correctivo.
