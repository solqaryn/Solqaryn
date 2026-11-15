# SOLQARYN V5 — C06-02.M02.06: dependencias técnicas y contrato de salida M02 de landing por tenant

**Tipo:** cierre de contratos DOCUMENTALES, no cierre funcional de landing real. **Autor:** SOLQARYN PRIMARY DEV 05 JAVIER_CHATGPT, TASK_ID 6adc2e55f47c81919f6374e561687076. **Ambito:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

## 1. Propósito, fuentes y límites

Fuente ejecutable: Standard de Implementación de Automatizaciones V5 FINAL (Drive ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA). La hoja institucional 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU determina PLAN_MAESTRO H/I/K, COLA G/O y DAG_TECNICO F/G. En preflight, C06-02.M02.06 tenía PLAN PENDIENTE, responsable libre, COLA SI/SI, DAG VALIDADO/SI, y cinco M02.01–05 LISTO con A5 independientes comprobadas; se obtuvo claim exclusivo por cambio condicional. El prerrequisito M01.01–06 con A5 independiente y los requisitos transversales calculados también se preservan. La disponibilidad del DAG no es una autorización para cambiar runtime ni producción.

Este documento NO crea host binding, landing CMS, preview, publicación, SEO/CDN, tablas MySQL, migraciones, endpoints, workers, dispatcher, webhook, red de eventos, DNS/TLS, Cloudflare/R2, nuevas tareas ni QA/main/PROD. La futura M03 requiere implementación de código y pruebas verdaderas, nunca sustituidas por texto.

## 2. Cinco contratos M02 y trazabilidad

| Unidad | SHA publicado y blob del documento | Contrato y frontera |
| --- | --- | --- |
| C06-02.M02.01 | 95bde031fd7a3e52f8af8704f377467b960207c2 / 59be82bf5cd7041abbdfcbb009ed686c34d76b97 | 16 entidades y 16 invariantes; no tablas |
| C06-02.M02.02 | 8f093a426bde86e88c8ac8edc866c1d786b790fb / c409ecd9869674106d6be663f0de18aabefd4368 | 10 operaciones, 9 DTO, 16 errores; no rutas HTTP |
| C06-02.M02.03 | 4533d6540f8b5b0bad68a65bfd00931c3fb803b7 / fe77415319bb79892729366864cbc7e12f32642d | 13 estructuras, 11 índices, seis fases de migración y 12 fixtures FUTUROS_NO_EJECUTADOS |
| C06-02.M02.04 | 2ef376f8b0392f118492ff13d26994e5757b59ee / b4fe9f1438a155d661f699259ba35e29f8da17fc | 18 opciones tipadas, 12 pares de pruebas futuras; no configuración instalada |
| C06-02.M02.05 | 9482782704dea75d9fd579ee9c20b1f363131f0c / 9eec648f896447cb5fe81d65462c51f724a56eb2 | 12 intenciones de eventos, 12 reglas de idempotencia y 12 pares futuros; NO EMITIR EVENTOS |

Cada SHA documental fue sometido a Scope Lock y procedencia V5 exact-SHA exitosos según evidencia de los respectivos cierres. Sus aprobaciones A5 requieren Task distinta del autor, evidenciada en PLAN K y BITACORA; son revisiones de especificaciones, no pentest, SQL ni certificación operativa. La cuenta GitHub común no sustituye la independencia de los TASK_ID.

El M01.06 origen conserva 16 gates G01–G16 y 8 rutas de rollback RB01–RB08 (documento blob 1292b204ae5eda6f26813634b77940a7697d44ac). Este contrato solo conserva esas barreras; ninguna se considera satisfecha en el runtime.

## 3. Matriz de dieciséis gates que siguen abiertos para M03

| Gate | Criterio de evidencia futura requerido |
| --- | --- |
| G01 | Host normalizado y propiedad verificada en backend; Forwarded/Host no confiable rechazado |
| G02 | Tenancy A/B real segregado en entidades, cache, rutas, SEO y assets |
| G03 | JWT y membresía vigente con RBAC separado para editor/publicador, sin privilegios por frontend |
| G04 | Host binding y DNS/TLS únicamente mediante autorización independiente y propiedad acreditada |
| G05 | Preview con grants expiran/revocan, no-store/noindex; borradores fuera de sitemap |
| G06 | Publicación con revisión inmutable, aprobador distinto y CAS real |
| G07 | Canonicalización de host/ruta/slug y rechazo de colisiones/traversal |
| G08 | Bloques tipados y sanitizados, sin JavaScript/HTML/SQL libre |
| G09 | Canonical/robots/sitemap solo de publicación aprobada |
| G10 | Cache particionada por tenant+host+versión, no mezcla A/B |
| G11 | Assets públicos con ownership verificado, sin URL privada o datos ajenos |
| G12 | EF Core/MySQL 8.4 efímero, FK/índices/backup/reversión demostrados |
| G13 | Errores fail-closed de proveedor, sin marca o secreto de otro tenant |
| G14 | UI ligera y accesible con keyboard/320px/reduced-motion, browser tests |
| G15 | Auditoría y correlación minimizadas; sin activar dispatcher ni eventos por esta especificación |
| G16 | QA/main/PROD, dominios y proveedores reales solo con orden nueva, smoke y rollback |

Todos siguen PENDIENTES de implementación/pruebas reales; LISTO_DEV en la fase documental no los abre automáticamente.

## 4. Handoff por capas y criterios de aceptación posteriores

1. **Domain:** validar claves tenant-bound, estados de draft/revisión/aprobación/publicación y CAS; casos negativos de versión obsoleta, tenant B y contenido hostil.
2. **Application:** editor y publicador con membresía activa, autorización y auditoría fail-closed; denegar autoaprobación y JWT vencido antes de efectos.
3. **Infrastructure:** EF Core 10/MySQL 8.4 en CI efímera, unicidad de host y rutas, transacciones, caché tenant-bound, backup/restauración y filtros A/B; no usar Aiven real en esta fase.
4. **API:** trusted proxy, host aprobado, proyección pública y preview autorizado, errores neutros; rechazar forwarded spoofed, host sin validar y datos de B.
5. **Angular:** controles visuales sin autoridad, branding solo de proyección pública saneada, estados error/loading/denegado, teclado/320px y doble envío bloqueado; autorización siempre en servidor.
6. **Operación:** trazas sin secretos/PII, correlationId opaco, cuota/reintentos limitados, rollback demostrado e independencia A5. Las intenciones de eventos M02.05 permanecen NO_EMITIDAS y no constituyen un bucle de Tasks.

Los resultados positivos/negativos listados son oráculos FUTUROS_NO_EJECUTADOS; no atribuyen PASS a xUnit, Playwright, SQL, HTTP ni pruebas A/B en ejecución. El storefront actual no constituye prueba de binding seguro de un host arbitrario a un tenant.

## 5. Rollback RB01–RB08 futuro, NO EJECUTADO

| Código | Tratamiento de fallo previsto |
| --- | --- |
| RB01 | Host incorrecto: denegar binding, devolver solo mapeo aprobado, no reasignar a B |
| RB02 | Permiso inválido: revocar grant y preservar auditoría sin elevar RBAC |
| RB03 | Preview indexado: revocar token, noindex/no-store y corregir sitemap |
| RB04 | Publicación equivocada: CAS a versión aprobada previa, conservar borradores y ventas |
| RB05 | Cache contaminada: invalidar tenant+host+versión y verificar A/B |
| RB06 | Migración fallida: restaurar copia verificada en servicio MySQL efímero, no Aiven productivo |
| RB07 | Media caída: fallback neutro sin exponer URL firmada o PII |
| RB08 | DNS/TLS/CDN no aprobado: no ejecutar cambios ni tocar proveedores reales |

Rollback de este changeset solo documental: git revert normal después de validar HEAD actual, preservando commits concurrentes, sin force-push/reset ni reescribir BITACORA.

## 6. A0–A5 y condición de salida

**A0:** validar ID, owner exclusivo, PLAN/COLA/DAG, cinco blobs y cinco A5 independientes, 16 gates, ocho rollback y CHANGELOG autocontenido. **A1 documental:** verificar 5 contratos, 16 gates, 8 reversas y seis interfaces/consumidores, distinguiendo requisitos futuros de hechos. **A2 estática:** tenant A/B, Host no confiable, JWT/RBAC, privacidad de PII, preview noindex, cache y secretos; nada se ejecuta sobre runtime. **A3 documental:** cotejar Domain/Application/Infrastructure/API/UI y dependencias M01/M02 sin activar M03, migraciones ni eventos. **A4:** publicar en dev con fast-forward y CAS expected HEAD, comprobar diff de exactamente este Markdown y CHANGELOG, Scope Lock y check V5 completed/success del SHA exacto, readbacks PLAN/COLA/DAG/BITACORA. **A5:** otra de las cinco Tasks Javier, distinta del autor DEV05, con revisión real de diff/blob/SHA y evidencias; si está pendiente la sucesora técnica debe permanecer bloqueada por DAG.

**NO_APLICA causal:** backend, frontend, MySQL, Playwright, browser y pruebas de eventos no evalúan un diff exclusivamente documental; queued/cancelled/skipped no constituyen PASS. No activar QA/main/PROD, DNS, secretos, proveedores de pago, migraciones ni eventos reales por el cierre de un contrato.
