# SOLQARYN V5 — C04-01.M01.04: registro de riesgos, supuestos y decisiones pendientes

**Microtarea:** `C04-01.M01.04`; **responsable:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14. **Repo/rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Clase:** evaluación documental M01; **no** alta, modificación o cobro real de suscripciones.

## 1. Fuente y alcance institucional

Documento ejecutable: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), plan `C04-01.M01.04` fila 581 y [Sheet operativo](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight de este ID: PLAN PENDIENTE sin responsable, COLA G/O=SI, DAG_TECNICO F542=VALIDADO/G542=SI; se requiere claim CAS propio para pasar a EN_PROGRESO.

Esta entrega registra condiciones desconocidas y decisiones a resolver antes de definir un catálogo SaaS **versionado** de planes, precios, periodicidad y upgrades. No confunde el catálogo `Plan`/suscripciones ya presente con un motor de facturación comercial certificado. Reutiliza inventarios `C04-01.M01.01–03` sin atribuirles nuevas pruebas.

## 2. Inventario de riesgo causal (riesgo, efecto, condición NO_GO y futura prueba)

| Riesgo | Impacto y vector | Medida preventiva / puerta obligatoria |
| --- | --- | --- |
| R01 — fuga tenant A/B | Alguien selecciona `empresaId` de otra empresa y obtiene datos, límites o suscripción | Autoridad server-side `IUsuarioScopeService`, membresía vigente, RBAC + tests negativos A/B, no validar solamente en Angular |
| R02 — entitlement como RBAC | Una capacidad de plan se interpreta como autorización de administración | Permiso independiente de módulo y acción; entitlements comerciales nunca suplen JWT ni roles; denegación por defecto |
| R03 — sobreescritura histórica | Cambio en `Plan` o precio afecta suscripciones previas sin aceptación | Versión inmutable por contrato, vigencia y grandfathering aprobados; rollback que preserve historia |
| R04 — cobros sin mandato | Se infieren precios/monedas/impuestos o se habilitan pagos sin autorización | Aprobar tabla de tarifas, impuestos, divisa, periodo y cobro fuera de esta fase; ninguna compra o integración |
| R05 — replay/concurrencia | Reintentos provocan altas duplicadas, cambios de plan repetidos o cargos dobles | Idempotencia por tenant y operación, conflictos de payload, restricciones únicas y pruebas MySQL reales cuando apliquen |
| R06 — desincronización límites | Domain/UI/API interpretan distinto capacidad ilimitada o vencimiento | Semántica única allowlisted, límites positivos o null explicitado, pruebas contractuales backend |
| R07 — upgrade/downgrade destructivo | Cambio retroactivo elimina acceso o altera datos contables legítimos | Reglas de transición temporal, compensación aditiva y rollback operativo con datos de prueba |
| R08 — permisos de operador global | Administrador tenant adquiere facultad de editar catálogo global | Separación RBAC/tenant, autorización expresa por caso de uso y denegación cross-tenant |
| R09 — rechazo de plataforma externa | Webhook tardío, replay, outage, timeout o callback falsificado impacta plan | No implementar webhooks ahora; futuro mecanismo validado, idempotente, con seguridad y reconciliación |
| R10 — evidencia falsa de CI | Se considera PASS una prueba queued, SKIPPED, ajena al SHA o un documento como prueba runtime | Checks exact-SHA, `completed/success` de pruebas pertinentes, logs causales, no elevar documentación a ejecución |
| R11 — rollback de datos productivos | Reversión documental o migración borra relaciones/suscripciones | Prohibición de cambios de producción, migraciones aditivas con reversibilidad y CI MySQL efímera |
| R12 — filtración de secretos/PII | Logging comercial expone JWT, tokens, datos de tarjeta u otros identificadores | Auditoría estructurada mínima, seguridad de logs, no registrar secretos ni datos de pago |

## 3. Supuestos etiquetados explícitamente (NO certificados)

| Supuesto | Situación de verificación | Acción si es falso |
| --- | --- | --- |
| S01 — catálogo global `Plan`, límites y suscripción ya existen | Observado en `backend/src/Domain/Entities/SuscripcionesSaaS.cs` blob `d9b5dbe19fb728293b00b748f10ac10336debd2d` | Releer HEAD y adaptar contratos antes de cambios |
| S02 — `SuscripcionesSaaSService` resuelve tenant antes de persistir | Observado en fuente blob `bb501285f11cd4b32709a58a2c51efa19c3664ea` | NO_GO a publicación backend hasta corregir seguridad |
| S03 — ledger idempotente pertenece al tenant | Fuente repositorio blob `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79`; NO certifica transacciones MySQL reales en esta tarea | Exigir integración de concurrencia con servicio efímero |
| S04 — catálogo trae versión de precio, moneda, periodicidad y descuentos | **No probado por los DTO/Domain inspeccionados; prohibido asumir** | Mantener diseño pendiente y no publicar cobros |
| S05 — responsables comerciales aprobaron tarifas, impuestos, prorrateo | **No hay aprobación encontrada en fuentes de esta microtarea** | No inventar ni crear datos comerciales operativos |
| S06 — la UI valida pertenencia de tenant de manera suficiente | **Supuesto inválido como fuente de seguridad**; Angular solo experiencia | Aplicar todas las condiciones server-side |
| S07 — migraciones de SaaS están aplicadas en Aiven/PROD | Snapshot/config EF observados, **estado físico no certificado** | No ejecutar migración ni inferir integridad de prod |
| S08 — todos los tests existentes pasan en SHA vivo | Archivos de test están presentes, **no ejecutados por este changeset documental** | Exigir CI dirigida para código posterior, preservar estados reales |

## 4. Registro de decisiones pendientes (NO inventar aprobación)

| Decisión | Alternativas posibles sin elección ejecutiva | Responsable de autorización y gate |
| --- | --- | --- |
| D01 — versionado | Catálogo por versión inmutable y asignación histórica vs. redefinición compatible controlada | Producto/arquitectura/seguridad; decisión explícita, test de migración |
| D02 — unidad comercial | Moneda, impuestos, periodicidad, región y fecha de tarifa vigente | Propietario comercial; sin importes implícitos |
| D03 — downgrade/upgrade | Aplicación inmediata, en renovación o mediante transición con aceptación | Producto y finanzas; compatibilidad, auditabilidad y no pérdida de datos |
| D04 — ilimitado y cuotas | `null` ilimitado o modelo de entitlements versionado, unidades y horizonte de medición | Arquitectura; conservar invariantes existentes y pruebas negativas |
| D05 — cobro/replay | Proveedor neutral futuro, periodicidad de reconciliación, timeouts y deduplicación | Autorización comercial + seguridad por separado; actualmente NO_IMPLEMENTAR |
| D06 — autoridad global | Roles y permisos específicos para administrar catálogo, separados de tenant | Gobierno de acceso; JWT/RBAC y A/B tests reales |
| D07 — datos/migración | Modelo de versiones/índices y transición aditiva con rollback probado | DB/arquitectura; CI con MySQL efímero |
| D08 — retención/auditoría | Eventos y motivos mínimos, protección de PII y secretos, retención | Seguridad/privacidad; sin inventar datos reales |

**Ninguna D01–D08 está marcada aquí como APROBADA.** Una Task no puede sustituir autorización humana comercial cuando la decisión exige nueva tarifa, proveedor, gasto, impuesto o datos de producción.

## 5. Escenarios de control documental, positivos y negativos

**P01**: identificar el catálogo `Plan` separado de `Suscripcion.EmpresaId`; **P02**: identificar clave idempotente y error de conflicto; **P03**: comprobar lectura de `[Authorize]` más `[RequierePermiso]`; **P04**: identificar semántica de límite `null` o valor positivo; **P05**: identificar que una tarifa futura es decisión pendiente, no constante codificada.

**N01**: un tenant B intenta el tenant A y es denegado; **N02**: un rol sin acción RBAC intenta cambio de plan y se deniega; **N03**: monto o moneda no aprobados se rechazan; **N04**: cuota 0 o negativa se rechaza; **N05**: duplicación de idempotency key con payload distinto es conflicto; **N06**: rollback no borra historia; **N07**: callback externo no implementado no se interpreta como operación exitosa; **N08**: un test SKIPPED o in_progress no es PASS; **N09**: un frontend validado no es autoridad de tenant; **N10**: documental M01 no libera por sí solo M02, QA o PROD.

Son **escenarios especificados**, no pruebas de runtime ejecutadas ni nuevas validaciones de una pasarela. A1–A3 de esta entrega son revisión de contenido, coherencia de riesgos y no escalamiento de privilegios.

## 6. Criterios de salida, dependencias y rollback

- **Impacto:** Markdown nuevo `docs/v5/c04-01-m01-04-riesgos-supuestos-decisiones-catalogo-planes.md` y nueva sección indivisible de `CHANGELOG_AI.md`. Sin editar Domain, Application, EF, API, Angular, secretos, configuración ni datos reales.
- **Dependencias de lectura:** fuentes canónicas V5 y tres documentos del mismo ámbito M01.01–03; no convertir ninguno en permiso para M01.06/M02. Las decisiones D01–D08 quedan abiertas, no autorizan tarifas ni operaciones.
- **A0–A4 documental:** revisar texto y diff, coherencia con límites tenant/JWT/RBAC, versión en dev, GitHub Scope Lock exact-SHA y check V5 de CHANGELOG completed/success. Backend/frontend/MySQL no son PASS si no se ejecutaron; pueden clasificarse NO_APLICA por diff documental.
- **A5:** pendiente de otra Task Javier distinta del autor, con EVENT_ID/IDEMPOTENCY_KEY y readback A:Z; tampoco es autorización de QA/main/PROD.
- **Rollback limitado:** `git revert` normal del commit documental manteniendo historia; nunca force-push, reset ni acciones sobre registros de clientes.

**Resultado:** registro de doce riesgos, ocho supuestos y ocho decisiones pendientes; ninguna operación comercial, evento, webhook, compra, pago, worker o cambio de autorizaciones fue implementada.
