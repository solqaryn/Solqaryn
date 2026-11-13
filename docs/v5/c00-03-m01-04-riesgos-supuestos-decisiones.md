# C00-03.M01.04 — Registro de riesgos, supuestos y decisiones de la matriz V5 integrada

**Alcance autorizado:** una microtarea documental del bloque C00-03, M01. Fuente única: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Repositorio `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Ejecutor: PRIMARY DEV 03 — JAVIER, `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`.

**RESULTADO_EXIGIDO de la fase:** integrar cada requisito anterior como mejora, contrato, prueba y criterio de cierre dentro del orden V5, con una sola matriz de cobertura y sin cola histórica separada. Este documento **registra** los riesgos, supuestos y decisiones de esa integración; no afirma que toda la matriz esté terminada ni que se haya probado funcionalidad no implementada. Los IDs ejecutables son exactamente los 2.340 de `C00-01.M01.01` a `C15-05.M06.06`.

## Riesgos operativos y criterio observable

| ID | Riesgo específico de integración V5 | Detección/evidencia exigible | Acción fail-closed | Responsable de tratamiento |
| --- | --- | --- | --- | --- |
| RI01 | Requisito histórico convertido dos veces en microtareas V5 | Mismo resultado, consumidor y aceptación aparecen en dos IDs de PLAN_MAESTRO | Consolidar trazabilidad en el ID V5 autorizado; no abrir otra cola ni un ID 2341 | Propietario del requisito/plan |
| RI02 | Requisito del documento V5 sin cobertura verificable | Falta asociación requisito → ID → artefacto → prueba → evidencia → rollback | Marcar brecha `PENDIENTE`, no declarar cobertura completa por texto decorativo | Responsable de matriz |
| RI03 | Una mejora se considera implementada únicamente porque existe su diseño | No hay diff ejecutable, pruebas pertinentes o readback de resultados cuando el RESULTADO_EXIGIDO pide runtime | No cerrar como LISTO; separar diseño de implementación y certificación | Task autora del ID |
| RI04 | Pérdida de autoría o colisión de claims mientras crece la matriz | `PLAN_MAESTRO!H/I` difiere de `COLA!E`, o dos Scopes intentan mismo ID | Releer estado y G/O; no sobrescribir responsables ajenos ni valores fórmula | Task solicitante de claim |
| RI05 | Fase previa inferida de cercanía de filas | `COLA!G/O` o dependencias DAG contradicen la lectura de PLAN | Usar puertas reales G/O y preflight de artefacto; nunca declarar disponible por orden aparente | Task selectora |
| RI06 | Cruce de empresa o privilegios entre superficies públicas y privadas | Un consumidor Angular permite operación sin control backend JWT/RBAC/tenant, o caso A→B se acepta | Bloquear promoción; exigir negativo server-side pertinente, revisión de PII y A5 independiente | Autor y revisor seguridad |
| RI07 | La arquitectura objetivo se confunde con infraestructura ya migrada | Documento menciona Hostinger/Cloudflare/R2 como si hubiera despliegue, DNS, datos o proveedor activo | Clasificar `FUTURO_NO_EJECUTADO`; no tocar producción ni secretos sin autorización aparte | Propietario de infraestructura |
| RI08 | CI no proporcional o evidencia atribuida a SHA equivocado | Job `SKIPPED`, `CANCELLED`, fallo requerido, o ejecución sobre ancestor con cambio relevante | Registrar `NO_PASS`; validar check causal del SHA exacto, sólo esperar jobs requeridos | Autor de cambio y revisor |
| RI09 | Activo histórico inexistente se presenta como migrado | Inventario de imágenes históricas es cero, pero reporte asegura copia a almacenamiento nuevo | Registrar `MIGRACION_HISTORICA=NO_APLICA`; conservar controles para archivos futuros | Responsable de archivos |
| RI10 | Registro de auditoría duplicado o insuficiente | `BITACORA` tiene `EVENT_ID` duplicado, origen sin TASK_ID, o menos de 21 campos | No certificar cierre; conservar evidencia existente, corregir por operación autorizada y lectura posterior | Task autora / auditor institucional |

## Supuestos controlados (no certificaciones)

| ID | Supuesto sujeto a confirmación por unidad | Verificación y condición de invalidación |
| --- | --- | --- |
| S01 | El V5 FINAL y PLAN_MAESTRO son las únicas fuentes ejecutables | Contrastar versión y secuencia 2.340; cualquier otra cola es no autorizada |
| S02 | Las puertas `COLA!G` y `COLA!O` representan dependencias vigentes | Leer fórmulas/valor actual y DAG_TECNICO; no reutilizar snapshots anteriores |
| S03 | El stack vivo sigue Angular 22, API .NET 10 y MySQL EF/Oracle | Validar código canónico, paquete y CI según componente; nunca inferir cobertura por nombre de workflow |
| S04 | Las evidencias de A0–A4 son proporcionales al artefacto | Para documento: diff, fuentes, seguridad documental y Scope Lock; para runtime: pruebas reales de consumidor, tenancy, integración y SHA |
| S05 | La incorporación de archivos futuros no constituye migración de imágenes históricas | Inventario cero significa NO_APLICA; nuevas cargas exigirán permisos, aislamiento y controles de ciclo de vida |

## Decisiones registradas y pendientes

| ID | Estado | Decisión, no-alcance y condición de resolución | Autoridad |
| --- | --- | --- | --- |
| D01 | FIJADA POR V5 | Un único catálogo de 2.340 IDs; requisitos anteriores se representan en el ID vigente, no en tickets heredados | Plan V5 y propietario |
| D02 | FIJADA POR V13 | `PLAN_MAESTRO!H` es estado mutable y `COLA` deriva fórmulas; A5 exige Task distinta antes de QA/main/PROD | Gobernanza canónica |
| D03 | PENDIENTE POR ID | Determinar qué consumidor concreto y qué caso positivo/negativo prueban cada requisito aún no mapeado | Responsable de la unidad futura |
| D04 | PENDIENTE DE AUTORIZACIÓN | Elegir/activar proveedor de archivos, migración Hostinger/Cloudflare/R2 y cambios de dominios, DNS, secretos o infraestructura | Propietario, fuera de esta microtarea |
| D05 | PENDIENTE POR ID | Definir necesidad real de .NET, Angular, MySQL, Docker, E2E o seguridad basada en archivos/resultados afectados | Task implementadora tras preflight |
| D06 | PENDIENTE DE VALIDACIÓN | Completar la matriz requisito → ID V5 → componente/contrato → prueba → evidencia → rollback para las 2.340 unidades sin marcar cobertura anticipada | Futuras unidades C00-03 |

## Casos verificables de aceptación y rechazo

**Positivo P1:** al registrar un requisito proveniente del V5, se identifica exactamente un ID existente, se preserva el RESULTADO_EXIGIDO, se distingue diseño de implementación, se registra el riesgo de seguridad/tenant y se define evidencia y rollback. **Positivo P2:** un cambio documental sin runtime evalúa fuente, diffs, integridad y Scope Lock cuando proceda, sin esperar MySQL/.NET irrelevantes.

**Negativos obligatorios:** N1 rechazar un ID fuera del catálogo; N2 rechazar un requisito duplicado como nueva ejecución; N3 rechazar cerrar implementación con sólo documentación; N4 rechazar afirmar migración histórica de imágenes cuando inventario=0; N5 rechazar `SKIPPED` como `PASS` de job requerido; N6 rechazar promoción QA/main/PROD sin A5 independiente y autorización explícita; N7 rechazar el acceso entre tenants aunque UI lo permita; N8 rechazar un claim sobre ID con responsable vigente. Estos son **criterios documentales**, no afirmaciones de tests HTTP/DB/browser ya ejecutados.

## Impacto, validaciones y reversión

**TIPO 1, documento sin cambio ejecutable.** `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI` (integridad, tenant y permisos como fronteras contractuales); `requiere_scope_lock=SI` (commit dev). No se editan API, DTO, Angular, EF, migraciones, integraciones, variables secretas, datos, workflows ni proveedores. A0: diff, sintaxis documental, secretos añadidos y alcance; A1: P1/P2 y N1–N8; A2: frontera tenant/PII/permiso y no autorización productiva; A3: referencias V5 ↔ PLAN/COLA/DAG coherentes; A4: SHA y árbol exacto, Scope Lock exigible, readback institucional. A5 será ejecutada por una Task diferente antes de cualquier promoción.

**Rollback:** `git revert <SHA_PUBLICADO>` no destructivo en `dev`, previa verificación de dependencias; las evidencias de PLAN/COLA/BITACORA se rectifican aditivamente, nunca se borran historiales. Este documento **no** certifica la fase C00-03 completa, sus unidades posteriores, A5_PASS o la migración de infraestructura.
