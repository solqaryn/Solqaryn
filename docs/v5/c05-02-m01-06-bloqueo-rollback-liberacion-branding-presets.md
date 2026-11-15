# SOLQARYN V5 — C05-02.M01.06: bloqueo, rollback y liberación documental de branding y presets

**Alcance:** cierre de preparación documental M01 del bloque C05-02 (branding y presets); rama \`dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`. **Autor responsable:** PRIMARY DEV 03 / JAVIER_CHATGPT / TASK_ID \`6adc2e4bd8a48191ac7d8641ed95a1a3\`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`); PLAN_MAESTRO fila 763. **Fecha:** 2026-11-15.

## Alcance real y no-alcance

Esta unidad fija una **decisión de bloqueo reproducible y condiciones verificables** para preparar M02. Es un contrato de revisión y recuperación, **no** una implementación de presets, personalización por empresa, subida de logos, historial de versiones, API tenant, migración o despliegue. El único resultado nuevo es este documento y una sección autocontenida de \`CHANGELOG_AI.md\`.

El sistema actual cuenta con \`TemaVisual\` **global** (fila Id=1, 15 campos de colores), \`TemaVisualRepository.GetAsync\` sin filtro \`EmpresaId\` y aplicación Angular de tokens hex permitidos, con descarte de respuestas obsoletas. El reset actual a defaults **no es** restauración histórica versionada. No deducir aislamiento tenant de un tema global, ni seguridad de JWT/RBAC desde el aspecto visual. Las decisiones D01–D08 de M01.04 permanecen abiertas. La mera aprobación de un documento no las convierte en decisiones adoptadas.

## Evidencia, prerequisitos y frontera de responsabilidad

| Fuente / unidad | Evidencia mínima y uso | Qué **no** demuestra |
| --- | --- | --- |
| \`C05-02.M01.01\` y \`.02\` | Objetivo, actores y fronteras de confianza; estado LISTO con revisión A5 independiente en PLAN | Una marca tenant ya operativa |
| \`C05-02.M01.03\` | Inventario causal de Domain, Application, API, EF y servicios Angular; distinguir global/tenant | Pruebas de aislamiento HTTP/MySQL A/B |
| \`C05-02.M01.04\` | Registro R01–R12, S01–S08 y decisiones D01–D08 pendientes; A5 en PLAN | Vulnerabilidades comprobadas o permisos concedidos |
| \`C05-02.M01.05\` | Oráculos **futuros NO_EJECUTADOS** P01–P08 y N01–N16, revisión independiente | PASS runtime, implementación de preview, versionado o presets |
| \`C01-03.M01.06\` | Contrato de configuración dinámica con LISTO y A5 independientes en PLAN | Concesión automática de IAM, DB o promoción |
| \`AGENTS.md\`, \`docs/PROJECT_SCOPE_LOCK.md\` y CI SHA | Gobierno de \`dev\` y controles de integridad | Permiso a QA/main/PROD o migración productiva |

Antes de usar esta decisión, releer **por ID exacto** PLAN_MAESTRO H/I/K, COLA G/O y DAG_TECNICO F/G, no números de disponibles antiguos. La fase terminal M01.06 **exige** los cinco hermanos M01.01–05 LISTO+A5 y \`C01-03.M01.06\` LISTO+A5, además de las dependencias que calcule el DAG vigente. La lectura de esta unidad no suplanta una decisión humana de producto ni una validación técnica futura.

## Bloqueos B01–B14: regla fail-closed

| ID | Condición de bloqueo | Evidencia exigible para quitar **solo ese bloqueo** |
| --- | --- | --- |
| B01 | Falta estado LISTO o A5 independiente de algún antecedente exigido por DAG | PLAN H/K y evento BITACORA de autor/revisor diferentes, leído tras cambio |
| B02 | COLA G/O no son ambos SI o DAG F/G no son VALIDADO/SI para el scope siguiente | Recalcular fórmulas; diagnóstico causal, jamás escribir SI a mano |
| B03 | Owner existente, lease vigente o escritor simultáneo | Exclusividad con cambio condicional verificable; no apropiación por antigüedad |
| B04 | \`verify-solqaryn-project-scope\` no es completed/success en SHA pertinente | Resultado exact-SHA comprobado, sin deshabilitar reglas |
| B05 | Check obligatorio de la tecnología aplicable queued/in_progress/failed/cancelled/skipped | Suite pertinente SUCCESS del SHA, o equivalencia de blobs y CI explicada; jamás tratar cancelled como PASS |
| B06 | Identidad global confundida con identidad tenant o sucursal | D01 aprobada, contrato de propiedad y pruebas positivas/negativas A/B |
| B07 | Edición, publicación o rollback inferidos del cliente sin autorización server-side | D02, matriz JWT/RBAC/membresía/scope y denegación verificada |
| B08 | Preset permite CSS/HTML/JS arbitrario, URLs o tokens fuera de allowlist | D03, DTO tipado, validaciones de inyección y pruebas de rechazo |
| B09 | Publicaciones concurrentes sin revisión esperada o escritura no idempotente | D04, política de versión/CAS y ensayo de conflicto |
| B10 | Logo sin titularidad, validación MIME/tamaño, retención o política de URL | D05 y verificación efímera de autorización/propiedad del archivo |
| B11 | Fallo parcial deja estado visible mixto o rollback confundido con reset global | D06, invariantes transaccionales, compensación y prueba de retorno |
| B12 | Contraste, foco, teclado, responsive o reduced-motion incumplidos | D07, pruebas Angular/Playwright sobre los componentes afectados |
| B13 | Cambia tenancy, JWT/RBAC, EF/MySQL, proveedor, secretos o entorno sin autorización | Gate técnico específico, integración CI efímera, rollback y permiso explícito |
| B14 | Se intenta promover a QA/main/PROD por LISTO_DEV o A5 | D08 y autorización adicional vigente de promoción; nunca inferirla de DAG |

**Precedencia:** un bloqueo de seguridad/identidad/propiedad tiene prioridad sobre un resultado documental favorable. Las decisiones D01–D08 no se dan por aprobadas con M01.06. Un \`A5_FAIL\` anterior requiere reparación **y** revisión independiente causal posterior; el historial no se borra.

## Procedimientos de reversión ROL01–ROL08

| ID | Escenario | Estrategia de recuperación y prueba antes de producción |
| --- | --- | --- |
| ROL01 | Error en este documento o su CHANGELOG | \`git revert\` normal del commit de \`dev\`, sin force-push; preservar BITACORA e identificar rectificación |
| ROL02 | Cambios de tema global rompen legibilidad | En la **futura implementación**, restaurar tokens conocidos y comprobar contraste/teclado; no prometer snapshot actual |
| ROL03 | Respuesta tardía de empresa A sobre B | Invalidar contexto y descartar respuesta por versión; ejecutar carrera controlada Angular |
| ROL04 | Publicación simultánea de editores | Rechazar versión antigua, registrar conflicto y exigir relectura antes de reintentar |
| ROL05 | Logo de propietario incorrecto o contenido no fiable | Bloquear publicación, verificar dueño, MIME, tamaño y cache; no servir referencia cruzada |
| ROL06 | Migración o transacción incompleta | Ensayar rollback/restauración con datos **efímeros** y copia verificada, nunca probar contra MySQL productivo |
| ROL07 | Publicación visual inválida tras fallo intermedio | Aislar revisión, compensar según diseño futuro y probar que lectores no ven mezcla de revisiones |
| ROL08 | Regresión de autorización o aislamiento | Deshabilitar **solo la feature nueva mediante mecanismo aprobado** y volver a versión segura; conservar JWT/RBAC/tenant fail-closed y evidencia de incidente |

ROL02–ROL08 son **estrategias por implementar y ensayar** en futuras fases; no se declara que existan mecanismos operativos hoy. Para esta entrega documental únicamente aplica ROL01.

## Oráculos de aceptación de esta **documentación** (no tests runtime)

- **P-DOC01:** identifica correctamente \`TemaVisual\` global, sus límites de propiedad y la ausencia de presets tenant/versionado implementados.
- **P-DOC02:** enlaza los cinco hermanos M01.01–05, \`C01-03.M01.06\` y el gate dinámico sin reescribir fórmulas.
- **P-DOC03:** contiene B01–B14 con condición y salida comprobable, y ROL01–ROL08 con rollback distinto del reset a defaults.
- **P-DOC04:** distingue explícitamente A0–A4 propios de A5 independiente y de autorización de QA/main/PROD.
- **N-DOC01:** si falta algún A5, un LISTO, owner exclusivo o permiso, se mantiene NO_GO al scope correspondiente.
- **N-DOC02:** si CI requerida está cancelled/skipped/in_progress/failed, jamás se contabiliza PASS.
- **N-DOC03:** si una prueba o decisión de branding/tenant es solo futura, no se describe como implementada.
- **N-DOC04:** un \`DAG G=SI\` de documento no concede migración, IAM, secretos, endpoints, DNS ni despliegues.

A0: inspección de diff de solo dos Markdown y comprobación de fuentes/autorización. A1: comprobación estructural P-DOC01–04/N-DOC01–04. A2: revisión documental JWT/RBAC/tenant A/B, datos privados y no-promoción. A3: correspondencia con M01.01–05, D01–D08 y DAG dinámico. A4: publicación fast-forward en \`dev\`, Scope Lock y procedencia CHANGELOG exact-SHA \`completed/success\`, y readbacks PLAN/COLA/BITACORA. Compilaciones .NET/Angular/MySQL sin cambios runtime son **NO_APLICA por causa**; nunca se anotan como PASS propio. A5 **pendiente hasta revisión efectiva por otro TASK_ID Javier**.

## Condición de liberación y handoff a M02

Al quedar esta M01.06 LISTO_DEV con A0–A4 acreditadas, registrar A5_PENDIENTE. Solo después de un A5_PASS independiente, releer automáticamente el estado calculado de cada sucesora en PLAN/COLA/DAG. **Reclamar M02 documental únicamente** si está PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI, preflight y lock exclusivo. M02 continúa definiendo contratos; no autoriza implementar presets, imagen de marca por empresa, endpoints, almacenamiento, migraciones ni publicación real. Cualquier etapa de código posterior necesita especificación real, IAM/JWT/RBAC, aislamiento tenant, CI dirigida y rollback apropiado, y nunca obtiene permiso a producción de este documento.
