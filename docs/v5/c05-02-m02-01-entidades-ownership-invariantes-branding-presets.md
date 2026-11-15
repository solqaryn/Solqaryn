# SOLQARYN V5 FINAL — C05-02.M02.01: entidades, ownership, relaciones e invariantes de branding y presets

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Rama:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

**Fuentes:** Standard V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA; skill canónica y AGENTS.md; preparación C05-02.M01.01–06 y sus A5 independientes. Esta microtarea es **sólo modelo documental**: no se implementan presets, logos tenant-aware, versiones, APIs, temas, migraciones, pruebas HTTP/EF ni infraestructura.

## 1. Baseline verificable y problema causal

- backend/src/Domain/Entities/TemaVisual.cs define un tema GLOBAL de fila Id=1 con 15 campos de colores y referencias de actualización; no tiene EmpresaId. No presentar TemaVisual global como tema tenant.
- backend/src/Domain/Entities/ConfigEmpresa.cs sí vincula configuración operativa a EmpresaId, con Moneda, ZonaHoraria, valores JSON no secretos, Version y plantillas de correo; NO equivale a tema visual tenant ni provee revisión histórica de presets.
- frontend/src/app/services/tema-visual.service.ts usa GET/PUT tema-visual y POST restaurar; no demuestra snapshots versionados o rollback tenant.
- frontend/src/app/services/theme-applier.service.ts acepta sólo literales hexadecimales, calcula contraste y descarta respuestas visuales obsoletas al cambiar contexto. La limpieza de tokens no acredita autorización backend.
- frontend/src/app/services/empresa-identidad.service.ts mantiene fallback de plataforma y versión de respuestas para no reintroducir identidad A después de B. Identidad pública es presentación, no un permiso de gestión.
- La preparación C05-02.M01.06 documentó bloqueos B01–B14 y reversión ROL01–ROL08. La aceptación de un diseño posterior exige separar plataforma, branding empresarial, evidencias A/B y controles de acceso.

## 2. Objetos lógicos E01–E14 y límites de ownership

| ID | Objeto | Estado en dev | Propietario lógico y relación | Restricción |
| --- | --- | --- | --- | --- |
| E01 | Empresa | EXISTENTE | Identidad Empresa.Id | Raíz tenant, no tema visual por sí sola |
| E02 | ConfigEmpresa | EXISTENTE | EmpresaId 1:1 empresa operativa | Versión no reemplaza autorización de marca |
| E03 | TemaVisual | EXISTENTE GLOBAL | Fila global Id=1, sin EmpresaId | No asignar a otra empresa ni suponer aislamiento |
| E04 | UsuarioEmpresa | EXISTENTE | UsuarioId+EmpresaId+RolId | Rol activo solo autoriza acciones de su empresa |
| E05 | TenantBrandProfile | PROPUESTO, NO IMPLEMENTADO | EmpresaId, perfil inmutable por versión | Cada versión pertenece exclusivamente a una empresa |
| E06 | BrandColorPalette | PROPUESTO, NO IMPLEMENTADO | TenantBrandProfileId, valores tipados | Tokens definidos, no CSS arbitrario |
| E07 | BrandLogoAsset | PROPUESTO, NO IMPLEMENTADO | EmpresaId+assetId+versión | No reutilizar archivo privado o ruta de otra empresa |
| E08 | BrandPresetCatalog | PROPUESTO, NO IMPLEMENTADO | PresetId y ámbito explícito | Preset global de referencia no concede permiso tenant |
| E09 | TenantPresetAssignment | PROPUESTO, NO IMPLEMENTADO | EmpresaId+PresetId+revision | No heredar grants ni desplazar propiedades ajenas |
| E10 | BrandVersionRevision | PROPUESTO, NO IMPLEMENTADO | EmpresaId+secuencia+autor+timestamp | Revisión append-only, no overwrite silencioso |
| E11 | BrandPublicationDecision | PROPUESTO, NO IMPLEMENTADO | EmpresaId+versión esperada+actor | CAS y permiso backend antes de publicar |
| E12 | BrandChangeAudit | PROPUESTO, NO IMPLEMENTADO | EmpresaId+correlationId+operationId | PII y secretos redactados, tenant correcto |
| E13 | BrandPreviewSnapshot | PROPUESTO, NO IMPLEMENTADO | EmpresaId+revision no publicada | Vista privada, no configuración publicada |
| E14 | BrandRollbackRecord | PROPUESTO, NO IMPLEMENTADO | EmpresaId+versiónOrigen+versiónDestino | Reversión auditada distinta de reset global |

Los objetos E05–E14 son propuestas sujetas a M02.03 y fases M03, sin crear tablas, migraciones, servicios o campos. Una futura configuración visual tenant debe tener EmpresaId en el servidor y no confiar en claims del navegador o valores de localStorage.

## 3. Relaciones propuestas y autoridades

- Empresa 1:1 ConfigEmpresa actual, separada de TemaVisual GLOBAL; perfil futuro 1:N TenantBrandProfile por EmpresaId y revisiones.
- TenantBrandProfile 1:N BrandColorPalette, BrandLogoAsset y BrandVersionRevision, todos con tenant verificado; BrandPresetCatalog puede ser global como catálogo de plantillas, pero TenantPresetAssignment siempre es de una sola Empresa.
- BrandPublicationDecision 1:1 versión esperada y 1:N BrandChangeAudit; BrandPreviewSnapshot nunca se publica ni otorga capacidades automáticamente; BrandRollbackRecord referencia versiones auténticas de la misma empresa.
- UsuarioEmpresa efectiva se resuelve server-side para cada mutación; la UI puede mostrar un preset disponible, pero no garantiza autorización de seleccionar, editar, publicar o restaurar.

## 4. Invariantes I01–I16 para implementación futura

| ID | Invariante obligatoria | Caso negativo a demostrar posteriormente |
| --- | --- | --- |
| I01 | EmpresaId se determina de sesión y membresía verificada | Cliente A envía ID/host de B |
| I02 | Perfil y sus hijos conservan EmpresaId único | Color o logo de B aparece en A |
| I03 | TemaVisual global Id=1 no se convierte implícitamente en tenant | Override global borra identidad A |
| I04 | Rol empresarial no equivale a operador plataforma | Administrador A publica branding global |
| I05 | Tokens CSS están limitados a catálogo y HEX seguro | Payload con CSS/URL arbitrario inyectado |
| I06 | Contraste y foco conservan accesibilidad | Fondo/primer plano ilegible en tema permitido |
| I07 | Contexto A se limpia antes de presentar B | Respuesta tardía A restituye marca al cambiar a B |
| I08 | Preview no escribe versión publicada | Vista previa accidentalmente afecta otros usuarios |
| I09 | Publicación exige CAS de versión de empresa | Dos editores sobrescriben sin 409 |
| I10 | Restauración usa snapshot de la MISMA empresa | Reset global presentado como rollback versionado |
| I11 | Presets no llevan datos o secretos de otra empresa | Copia preset B comparte tokens privados |
| I12 | Recursos logo requieren ownership/URL segura y permisos backend | Archivo B accesible mediante ID de A |
| I13 | Auditoría conserva autor/versión y redacción | Log muestra JWT/DSN/credenciales/PII |
| I14 | Rollback conserva referencias legales y versiones | Eliminación irreversible de revisiones |
| I15 | CI causal exact SHA requiere SUCCESS real | CANCELLED/IN_PROGRESS/SKIPPED como PASS |
| I16 | A5 independiente no promueve QA/main/PROD | Autor valida su propio cambio o activa producción |

## 5. Escenarios de aceptación P01–P04 y N01–N10, FUTUROS NO EJECUTADOS

| Caso | Tipo | Estímulo | Resultado observable exigido en fase posterior |
| --- | --- | --- | --- |
| P01 | positivo | Empresa A autorizada elige preset publicado | Perfil A versionado con CAS; B sin cambios |
| P02 | positivo | Vista previa de colores seguros | Contraste accesible sin mutar publicación |
| P03 | positivo | Reversión de A a snapshot previo válido | Nueva revisión A con auditoría, B intacta |
| P04 | positivo | Cambio de A a B en la sesión | B solo después de verificación del servidor |
| N01 | negativo | Usuario A envía EmpresaId de B | Denegación sin información B |
| N02 | negativo | Tema contiene CSS arbitrario o URL peligrosa | Rechazo y tokens de fallback seguros |
| N03 | negativo | Logo privado de B se usa como asset de A | Denegación en backend y storage |
| N04 | negativo | Edición de A usa versión stale | 409, ningún overwrite |
| N05 | negativo | Respuesta antigua A llega tras seleccionar B | Marca A no reaparece |
| N06 | negativo | Tenant sin grant intenta publicar | Denegación server-side |
| N07 | negativo | Reset global se anuncia como restauración A | Rechazo de certificación de rollback tenant |
| N08 | negativo | Preset global contiene secreto/PII | Redacción y no publicación |
| N09 | negativo | CI esencial sigue queued/cancelled | Permanecer VALIDANDO, no contar PASS |
| N10 | negativo | Documento intenta crear DB, API o activar QA/PROD | NO_GO fuera de alcance |

Estos 14 ejemplos son contratos de prueba futuros, no pruebas Angular, MySQL o API ejecutadas. M02 es contrato, no habilitación de feature flags, logos reales o versión operativa.

## 6. A0–A5, dependencias, rollback y handoff

**A0:** verificar sólo dos Markdown (este documento y CHANGELOG_AI.md) con encabezado y viñetas contiguas, sin secretos o cambios de runtime. **A1:** verificar E01–E14, I01–I16, P01–P04 y N01–N10 positivos/negativos futuros. **A2:** validar conceptual y estáticamente autorización de EmpresaId, RBAC, aislamiento A/B, salida segura de assets, XSS y fail-closed. **A3:** cotejar fuentes de TemaVisual, ConfigEmpresa, ThemeApplierService, EmpresaIdentidadService y precedentes C05-02.M01.01–06; reevaluar DAG F/G y COLA G/O por ID. **A4:** commit fast-forward CAS en dev, controles Scope Lock y procedencia V5 completed/success sobre SHA exacto, readback PLAN/COLA/DAG/BITACORA. Builds runtime no causales son NO_APLICA, no PASS. **A5:** revisión independiente obligatoria de otra Task Javier, no por este autor, antes de liberar sucesoras que exigen A5.

**No-alcance:** no escribir en DB, Domain, API, Angular, MySQL, buckets, DNS, QA, main o PROD; no habilitar branding tenant sin implementación y test A/B real. **Rollback documental:** git revert normal preservando historia y eventos. **Siguiente:** evaluar M02.02–05 sólo con PLAN PENDIENTE sin owner, COLA G/O=SI y DAG VALIDADO/SI propios, sin adelantar M02.06 o M03.
