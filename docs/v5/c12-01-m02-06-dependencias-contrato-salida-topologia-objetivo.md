# SOLQARYN V17 — C12-01.M02.06: dependencias técnicas y contrato de salida del lote M02

**ID:** C12-01.M02.06 · **PLAN_MAESTRO:** 1741 · **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Riesgo:** R0 documental · **Repositorio:** solqaryn/Solqaryn · **Rama:** dev · **PROJECT_SCOPE_LOCK:** STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO y documentos vivos de dev. Esta salida reúne los contratos M02.01–M02.05 de C12-01 sin activar ni sustituir proveedores. La operación efectiva continúa en Vercel, Render, Aiven y Cloudinary. Hostinger/VPS, redes destino, DNS, certificados, secretos, despliegues y cutover siguen NO_AUTORIZADOS sin una orden R3 específica.

## A0 — Entregable, impacto y fronteras de confianza

El objetivo es fijar **dependencias comprobables** y un **contrato de salida de M02** para que un futuro M03 pueda diseñar trabajo real tras su propia autorización y controles. Se revisan criterios, ownership, interfaces conceptuales, datos, seguridad, eventos, reversión y pruebas futuras. No se añaden código, endpoint, DTO compilado, migración EF/MySQL, mensaje, broker, job, cliente Angular, variable de entorno, proveedor, cuenta, DNS, infraestructura ni tráfico productivo.

- **Componentes:** únicamente este documento y una sección autocontenida de CHANGELOG_AI.md. Consumidores futuros: arquitectura, plataforma, IAM/RBAC, operación, datos, observabilidad, calidad y revisor A5.
- **Capas:** Domain/Application/Infrastructure/API permanecen intactas. Angular 22.2.1 y .NET 10 no cambian.
- **Datos y seguridad:** MySQL/EF, JWT/RBAC, tenant A/B, revocación, aislamiento DEV/QA/PROD, PII y secretos son controles **pendientes de prueba futura**, no certificaciones de este Markdown.
- **Rollback del changeset:** git revert normal sobre HEAD revalidado, preservando BITACORA y los cambios ajenos; prohibidos reset/force-push.

## D01–D14 — Dependencias de salida verificables

| Gate contractual | Entrada obligatoria | Evidencia y condición de rechazo para una etapa futura |
| --- | --- | --- |
| D01 — Plan y owner | C12-01.M01.01–06 con sus A5; G01–G16 del M01.06 | Mantener los 16 bloqueos operativos hasta evidencia R2/R3; ninguna aprobación documental los convierte en PASS productivo |
| D02 — Modelo | M02.01 E01–E16 e I01–I20 | Revisión con owner, ámbito y versión; rechazar relación entre ambiente/tenant incompatibles |
| D03 — Operaciones | M02.02 OP01–OP08 | Cada operación es **evaluación documental inactiva**; rechazo por ausencia de autorización, ownership o versión |
| D04 — Contratos | M02.02 D01–D12, ER01–ER12, V01–V16 | No crear API/DTO compilado sin scope autorizado, denegaciones negativas y validación de consumidores |
| D05 — Persistencia | M02.03 K01–K16 | Índices y claves son propuesta, no tablas existentes; MySQL 8.4 efímero con tests de integración real antes de declarar compatibilidad |
| D06 — Migración reversible | M02.03 MX01–MX08, F01–F12 | Sin rollback ensayado, integridad tenant A/B y fixtures sintéticos ejecutados, la migración queda NO_GO |
| D07 — Configuración | M02.04 CFG01–CFG16 | Flags tipados por ambiente/tenant/owner; cualquier entrada desconocida, ausente o sin firma rechaza |
| D08 — Activación | M02.04 F01–F12 | F12 cutoverEnabled permanece false; un documento LISTO no permite activarlo |
| D09 — Eventos | M02.05 EV01–EV12, ENV01–ENV12 | Son eventos futuros no emitidos; esquema, actor y correlationId no permiten escapar del scope |
| D10 — Idempotencia real | M02.05 I01–I12, corrección f47784e8 | logicalRequestId estable por solicitud y scoped, HMAC canónico completo antes de redacción, unicidad durable y cero doble efecto requieren pruebas R2 |
| D11 — Observabilidad y límites | M02.05 LIM01–LIM08 y ER-EV01–08 | Cuotas, latencia, retención, SLO, RTO/RPO quedan NO_MEDIDO sin numerador/denominador; rechazar replay, PII y bucles |
| D12 — Seguridad | M01.06 G07/G10/G14; M02.01–05 | JWT/RBAC/revocación, tenant A/B, IDOR, ACL, claves externas y medios requieren integración negativa independiente; guard Angular no sustituye backend |
| D13 — Interfaces entre bloques | C10-04.M01.06 y C11-01.M01.06 con A5, según DAG vigente | No crear dependencia circular ni asumir acceso al proveedor; leer HEAD, PLAN H/I, COLA G/O y DAG F/G por ID exacto |
| D14 — Revisión/rollback | V15 A0–A5 y M01.06 R01–R08 | CI/Scope Lock exact-SHA, autoría y A5 de otra Task Javier; pruebas/rollback efectivos **no realizados** en M02 |

**Causalidad:** M02.06 fue elegible solo después de que PLAN mostrara PENDIENTE/owner vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI, con los antecedentes dinámicos. Esto autoriza exclusivamente su trabajo documental. No demuestra que G01–G16, proveedor, DNS, bases destino o IAM productivos estén liberados. El A5 de M02.05 validó la reparación conceptual del contrato, **no** las seis pruebas de runtime de idempotencia.

## O01–O10 — Contrato de salida de M02 y handoff controlado

| Salida | Criterio de aceptación | Evidencia futura imprescindible |
| --- | --- | --- |
| O01 | Modelo de 16 entidades con ownership y dependencias explícitas | E01–E16/I01–I20 completos y revisión independiente |
| O02 | Operaciones, DTO, validaciones y errores conceptuales coherentes | OP01–08, D01–12, ER01–12, V01–16 sin permisividad implícita |
| O03 | Persistencia y ruta de migración únicamente diseñadas | K01–16, MX01–08 y F01–12; ningún cambio de esquema presente |
| O04 | Configuración por ámbito y deny-by-default | CFG01–16, F01–12; F12 desactivado |
| O05 | Eventos, correlación, errores y límites versionados | EV01–12, ENV01–12, I01–12, LIM01–08 y ER-EV01–08 |
| O06 | Idempotencia con igualdad semántica verificable | Misma solicitud/payload, payload diferente por PII y solicitud distinta/payload idéntico distinguidos; seis pruebas futuras pendientes |
| O07 | Rechazos de autorización, tenant y entorno | Casos negativos JWT/RBAC/revocación A/B, secretos y ACL en runtime futuro |
| O08 | Estado honesto de calidad | Pruebas R0 documentales revisadas; .NET/MySQL/browser/proveedores NO_APLICA en este cambio |
| O09 | Cierre institucional por ID y SHA | PLAN H/I y BITACORA A:Z, Scope Lock/V5 SUCCESS y A5 de otra Task Javier antes de dependencia posterior que lo exija |
| O10 | Handoff a M03 sin despliegue | Recalcular DAG/COLA, volver a clasificar R0–R3, obtener permiso R3 para cambios productivos; ningún flag ni servicio activado |

**Definición de salida:** M02.06 alcanza LISTO_DEV solo con artefacto y A0–A4 verificables; **M02 en conjunto** requiere que M02.01–06 acrediten sus estados y A5 independientes donde lo imponga el DAG. La documentación no otorga un PASS de M03, QA, main o PROD. Si la puerta de M03 permanece NO, conservarla sin escribir SI manualmente.

## P01/N01–P12/N12 — Matriz de aceptación FUTURA_NO_EJECUTADA

| Par | Positivo diseñado | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Revisión con owner, versión y ambiente explícitos | Revisión anónima habilita flujo |
| P02/N02 | Tenant A y B segregados por claves/ACL | Identificador compartido autoriza cruce A/B |
| P03/N03 | DEV/QA/PROD separados con IAM distinto | Credencial DEV sirve en PROD |
| P04/N04 | API privada aplica RBAC/revocación backend | Sesión revocada o rol ausente acepta operación |
| P05/N05 | Migración aditiva probada en MySQL 8.4 efímero | Build verde sustituye integración o índice errado |
| P06/N06 | Restore sintético con checksum y conciliación | Backup corrupto se anuncia restaurado |
| P07/N07 | Misma logicalRequestId/payload concurrente produce una decisión | Dos consumidores producen dos efectos |
| P08/N08 | Solicitudes distintas con payload igual conservan identidad separada | Deduplicación las fusiona por hash |
| P09/N09 | Misma solicitud con PII cambiada detecta conflicto por HMAC completo | Redacción igual oculta payload distinto |
| P10/N10 | Eventos y métricas solo tienen referencias opacas | JWT, DSN, PII o secreto acaba en log |
| P11/N11 | Frontend/API y TLS/routing preservan rechazo correcto | Proxy CORS o host sin prueba amplía confianza |
| P12/N12 | F12 queda false sin autorización R3, ventana y rollback | Documento o evento activa VPS/DNS/cutover |

**Estado real:** 0/12 pares ejecutados, 0/6 casos R2 de idempotencia ejecutados, 0 migraciones, 0 pruebas MySQL/API/browser, 0 eventos emitidos. No inventar denominadores de disponibilidad ni declarar SLO medido. El test futuro real se ejecuta solamente en un scope de implementación autorizado, con CI GitHub-hosted y evidencia causal SHA.

## A1–A5 — Calidad adaptativa y decisión

- **A0:** revisar este diff de dos Markdown, fuentes M01.06/M02.01–05, plan y DAG, impacto y rollback.
- **A1:** contraste documental de D01–14/O01–10 y 12 pares positivos/negativos, que deben detectar ausencias de owner, IAM, unicidad, integridad, revocación y rollback. No son tests ejecutados.
- **A2:** aislamiento conceptual tenant A/B, roles/JWT, secretos, revocación y ausencia de activación; controles reales pendientes de implementación R2.
- **A3:** MySQL/EF, API .NET, Angular/Playwright, broker, VPS, DNS y proveedor **NO_APLICA** al no modificarse runtime.
- **A4:** Scope Lock, V5/procedencia y CHANGELOG íntegro en completed/success sobre SHA exacto, PLAN H/I, COLA, DAG y BITACORA readback; QUEUED/CANCELLED/SKIPPED/FAILED no son PASS.
- **A5:** otra de las cinco Tasks Javier revisa en forma independiente requisitos, fuentes, diff, omisiones y SHA; puede A5_FAIL. La autora DEV06 nunca autoaprueba.

**Salida fail-closed:** cualquier falta de predecesor, A5, evidencia técnica o permiso R3 afecta solo al scope dependiente; no borrar historia ni inventar habilitaciones. Los dieciséis gates de infraestructura permanecen pendientes para una activación real.
