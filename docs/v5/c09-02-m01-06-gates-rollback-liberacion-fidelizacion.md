# SOLQARYN V17 — C09-02.M01.06: puertas, rollback y liberación de fidelización y reputación

**Microtarea:** `C09-02.M01.06`, PLAN_MAESTRO fila 1339. **Autor:** PRIMARY DEV 02, JAVIER_CHATGPT, `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R0, documentación sin runtime.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG y gobernanza V15–V17. Resultado solicitado: definir bloqueo, rollback y condición de liberación M01 de puntos, promociones, reseñas, respuestas y atención posventa, con antifraude, moderación y trazabilidad. **Este artefacto NO activa ni certifica beneficios, pagos, ledger, campañas, consentimiento, moderación automática o envío real.**

## 1. Prerrequisitos institucionales y evidencia documental exacta

La entrada se habilitó con `PLAN H1339=PENDIENTE/I vacío`, `COLA G1339/O1339=SI`, `DAG_TECNICO F1300=VALIDADO/G1300=SI` y reclamación exclusiva por candado V17 `SQV17_C09_02_M01_06` (namedRangeId `2000001339`). La fórmula de DAG vincula `C09-02.M01.01–05+A5`, `C07-02.M01.06+A5` y `C09-01.M01.06+A5`, además de referencias preparatorias del bloque. Esta elegibilidad **solo** autoriza el documento de preparación, no cambios comerciales live.

| Fuente M01 | Ruta de documento ya publicado | Blob de Git leído en dev | Alcance |
| --- | --- | --- | --- |
| M01.01 | `docs/v5/c09-02-m01-01-alcance-fidelizacion-reputacion.md` | `1030c70296220626c5341f58d99e68e47b8a6cb2` | NR01–NR14, ocho P/N futuros |
| M01.02 | `docs/v5/c09-02-m01-02-actores-interfaces-fidelizacion.md` | `7de37b4ccfa35cd07245975b97cf04dcca2b59a7` | A01–A14, I01–I12, F01–F09 |
| M01.03 | `docs/v5/c09-02-m01-03-inventario-codigo-brechas-fidelizacion.md` | `b5bbb9d2c8e85814390bd12c85e5f4d6bd3bfed7` | doce rutas, G01–G12 pendientes |
| M01.04 | `docs/v5/c09-02-m01-04-riesgos-supuestos-decisiones-fidelizacion.md` | `fb8079c397ad3e413d4a52c9f848744fff16bd3c` | R01–R14 riesgos potenciales, S01–S08, D01–D08 abiertas |
| M01.05 | `docs/v5/c09-02-m01-05-criterios-aceptacion-fidelizacion.md` | `714b1927b0db7b35a91931e3fdfd0c78643f67fe` | AC01–AC16 y RNF01–RNF10 futuros |

Los tipos existentes `Cliente`, `Venta`, `CreditoCliente`, `UsuarioEmpresa`, `RegistroAuditoria` y `TiendaCuentaCliente` son sustratos, no ledger de puntos ni permisos de moderación. `EmpresaId=NULL` histórico en Cliente/Venta **no acredita ownership**. Puntos, crédito comercial, facturación, descuentos y dinero se mantienen conceptos separados. No interpretar una hipótesis de M01.04 como defecto P0/P1 confirmado.

## 2. Dieciséis gates de bloqueo/aceptación

| Gate | Evidencia exigible de la fase pertinente | Rechazo obligatorio |
| --- | --- | --- |
| G01 | Skill canónica = local, autoridad Javier, dev/Scope Lock STRICT y SHA cierto | mismatch o escritor sin permiso |
| G02 | PLAN/COLA/DAG y A5 precedentes efectivos, sin gates editados a mano | VALIDANDO, A5 pendiente/fallido o G/O=NO |
| G03 | Candado V17 por fila, CAS H, owner I, idempotencia BITACORA y readback | conflicto, lock ocupado o lectura+escritura no atómica |
| G04 | Tenant A/B y propiedad de cliente/ledger verificados **en backend** | id de B o legado NULL expone saldo A |
| G05 | Ledger de puntos distinto de CréditoCliente/Venta y efectivo | bonus monetizado o ajuste contable inferido |
| G06 | Acreditación, reversión y expiración transaccional versionada | replay de venta o confirmación duplica puntos |
| G07 | Canje atómico con saldo suficiente, idempotencia y carrera real | doble canje, saldo negativo o efecto parcial |
| G08 | Promoción allowlisted por empresa, versión, UTC, cupo y caducidad | promo B, inválida o agotada aplicada |
| G09 | JWT/RBAC, membresía vigente, revocación y autorización por recurso | UI/slug/header concede permisos backend |
| G10 | Reseña con autor legítimo, sanitización, consentimiento y PII mínima | HTML/script o suplantación se publica |
| G11 | Moderación reversible y apelable con actor/motivo/versión | lector o moderador B cambia estado |
| G12 | Posventa vinculada a Cliente/Venta/Ticket del mismo tenant | IDOR/BOLA permite leer ticket B |
| G13 | Consentimiento y opt-out por canal, verificados también al retry | mensaje después de revocación o por caché |
| G14 | Exportación, errores, registros, caché y rate limit segmentados | PII/saldo/token de B aparece en A |
| G15 | xUnit+API, EF/MySQL 8.4 efímero si cambia DB, Angular/Playwright si UI, Scope Lock y V5 por SHA | FAIL/CANCELLED/SKIPPED/IN_PROGRESS como PASS |
| G16 | A5 por otra Task Javier con diff, pruebas, SHA y defectos revisados | autopase o P0/P1 abierta |

**Aplicabilidad:** G04–G15 son requisitos *futuros*, no PASS atribuidos al presente documento R0. Un build verde aislado no sustituye integración, transacción o autorización. Sin permiso específico, los cambios R3 productivos quedan NO_GO.

## 3. Doce casos positivos/negativos futuros (NO EJECUTADOS)

| Caso | Positivo que deberá probarse | Negativo fail-closed exigido |
| --- | --- | --- |
| P01/N01 | Cliente A ve su ledger A | Sesión B o cliente NULL no ve puntos A |
| P02/N02 | Venta confirmada acredita una vez | Replay/anulación no duplica |
| P03/N03 | Canje válido debita saldo una vez | Carrera, saldo insuficiente o versión vieja falla |
| P04/N04 | Promoción vigente por tenant respeta cupo | Promo B/expirada/arbitraria rechazada |
| P05/N05 | Ajuste con permiso y auditoría es reversible | Rol lector/JWT revocado no ajusta |
| P06/N06 | Reseña legítima saneada se publica | Script, PII o autor falso se bloquean |
| P07/N07 | Moderador A registra motivo y apelación | Moderador B o sin motivo no altera |
| P08/N08 | Posventa A une Cliente y Venta A | Ticket de B no filtra datos |
| P09/N09 | Opt-in vigente permite canal autorizado | Opt-out y retry no vuelven a enviar |
| P10/N10 | Hora UTC y versión controlan vigencia | Caducidad o reloj incorrecto no favorece |
| P11/N11 | UI accesible muestra estado real y foco | Guard salteado no concede API |
| P12/N12 | Deshabilitar conserva historial y ventas | Hard delete o saldo/crédito alterados fallan |

**Resultado de ejecución en esta unidad:** `0/12` casos runtime ejecutados, métricas `NO_MEDIDO`; estas filas son oráculos de CI futura, no pruebas pasadas.

## 4. Condiciones de liberación por nivel y riesgos de datos

**Nivel M01.06:** únicamente dos Markdown (este contrato y sección autocontenida de `CHANGELOG_AI.md`) mediante commits fast-forward sobre HEAD vigente, con evidencia de los cinco antecedentes, checks obligatorios `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA propio o equivalencia íntegra. Guardar eventos, H/I, BITACORA, COLA/DAG y liberar candado sólo tras transición auditada. `LISTO_DEV` no significa A5.

**Nivel A5:** otra Task entre las cinco Javier distintas de la autora verifica el documento, las advertencias de seguridad, originales, SHA/diff y resultados. Un A5_FAIL bloquea sucesoras hasta corrección y revisión independiente. Nunca forzar `G/O=SI`.

**Nivel M02:** contratos y modelos conceptuales por ID independiente, nuevo lock, owner, A0–A4 y A5; no crear endpoints, migraciones o campañas por un documento preparatorio.

**Nivel M03–M06:** exigir cambios auténticos .NET 10/Angular 22.2.1/MySQL 8.4, pruebas xUnit positivas/negativas, autorización tenant A/B, idempotencia y transacción/carrera MySQL efímero cuando corresponda; JWT/RBAC revocable, privacidad y API/Playwright según superficie. Cualquier programa monetizable, tratamiento de datos o proveedor necesita autorización específica. Sin QA/main/PROD.

## 5. Rollback, errores y recuperación por causa

- **R0 documental o A5_FAIL:** corregir con commit aditivo y nueva evidencia, o `git revert` normal contra HEAD actual. No borrar BITACORA ni reescribir historia.
- **Scope Lock/CI fallida:** inspeccionar job y logs del SHA, corregir el archivo responsable sin pisar escritor activo; publicar corrección mínima y comprobar nuevo SUCCESS. CANCELLED/SKIPPED no son PASS.
- **Concurrencia V17:** con lock duplicado o CAS de H inconcluso, abortar exclusivamente fila, conservar dueño y prueba; no borrar candados ajenos por antigüedad.
- **Riesgos futuros ledger:** prevenir doble acreditación/overspend con transacción y verificación MySQL efímera, revertir por compensación y auditoría sin modificar ventas/créditos reales.
- **Reseñas, campañas y privacidad:** no enviar ni publicar ante permiso/consentimiento dudoso; mantener mecanismo reversible y trazabilidad mínima sin secretos.
- **UI/proveedor futuro:** no confundir un redirect, guard o mock con autorización/entrega; rollback con flags autorizados y prueba de error/teclado/320px cuando exista UI.

**Impacto presente:** no cambia Domain/Application/Infrastructure/API, rutas Angular, MySQL, flags, JWT/RBAC, secretos, proveedores, jobs, datos reales ni arquitectura. A0=diff/requisitos/antecedentes; A1=escenarios escritos sin ejecución; A2=amenazas de tenant/consentimiento y seguridad sólo a nivel de contrato; A3=integraciones runtime `NO_APLICA` por dos Markdown; A4=checks exact-SHA y readback; A5=`PENDIENTE` de revisor independiente.
