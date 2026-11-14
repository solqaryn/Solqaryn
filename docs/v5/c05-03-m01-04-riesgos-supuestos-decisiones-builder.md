# SOLQARYN V5 — C05-03.M01.04: riesgos, supuestos y decisiones pendientes del builder

**Responsable de microtarea:** PRIMARY DEV 05 JAVIER / TASK_ID 6adc2e55f47c81919f6374e561687076. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, PLAN_MAESTRO fila 797, C05-03 Builder gobernado M01. **Repositorio:** solqaryn/Solqaryn dev. **PROJECT_SCOPE_LOCK:** STRICT. **Fecha:** 2026-11-14.

## Objeto limitado y estado de autoridad

Registro de riesgos **hipotéticos**, supuestos verificables y decisiones **aún no aprobadas** para un constructor futuro basado en componentes Angular precompilados y configuración tipada. Esta preparación M01.04 no implementa builder, define proveedores, crea permisos, cambia JWT/RBAC, crea tablas, activa runtime, publica páginas ni aprueba cuotas. No declara vulnerabilidades como hallazgos confirmados. Se apoya en M01.01 (alcance) y M01.02 (actores y superficies), diferenciando sus oráculos FUTUROS de pruebas reales.

## R01–R12: riesgos a comprobar, no incidencias verificadas

| ID | Riesgo potencial | Consecuencia / escenario | Control exigible después |
| --- | --- | --- | --- |
| R01 | XSS/render ejecutable | Markup, atributo evento, CSS o expresión importada como dato ejecuta código | Componente Angular precompilado, claves/campos tipados y codificación contextual; N01/N02 |
| R02 | SSRF por URL | Recurso o enlace a red interna o esquema peligroso se interpreta desde backend | Regla de esquema/destino y rechazo de URL privada; N04 |
| R03 | Cross-tenant A/B | ID de página conocido permite leer, editar o cachear página de otro tenant | Membresía/ownership server-side y cachés segmentadas; N09 |
| R04 | Publicación por usuario no habilitado | Guard UI o empresaId cliente se confunde con autorización | JWT/RBAC backend para publish y pruebas de denegación; N10 |
| R05 | Conflicto de revisiones | Editor obsoleto pisa borrador o publicación más reciente | Optimistic concurrency CAS y respuesta 409; N11 |
| R06 | Preview con efectos | Vista previa activa publicación, worker, webhook o archivo | Preview de solo lectura y sin efectos; N12 |
| R07 | Denegación de servicio | Payload excesivo, repetición o recursión agota recursos | Límites de bytes, secciones y profundidad; N05/N06 |
| R08 | Componente o campo desconocido | Clave o propiedad no permitida logra eludir catálogo tipado | Reject unknown type/extra field, validación en servidor; N07/N08 |
| R09 | Filtración de secretos en errores | Stacktrace o token expone PII y estructura SQL en logs | Errores mínimos, redacción y acceso restringido; A2 |
| R10 | Ruptura visual/accesibilidad | Diseño generado rompe foco, teclado o 320px/reduced-motion | Componentes accesibles auditados, pruebas responsive; P05 |
| R11 | Inconsistencia al revertir | Rollback destruye historial publicado o datos legítimos | Reversion versionada y auditoría; P04/N11 |
| R12 | Falsa equivalencia con tema actual | TemaVisual o GET público se interpreta como implementación builder | Mantener línea base existente y separar contratos futuros; A3 |

El tratamiento de riesgos no es una autorización. Especialmente, **N01–N12 son hipótesis de rechazo para pruebas posteriores**, nunca se contabilizan como suites ejecutadas en M01.04. Un texto inocuo con palabras HTML/SQL es dato: lo prohibido es su interpretación como código o esquema de privilegios.

## S01–S08: supuestos falsables y condición de bloqueo

| ID | Supuesto sujeto a prueba | Evidencia necesaria | Si resulta falso |
| --- | --- | --- | --- |
| S01 | Tema y rutas actuales seguirán como componentes separados de un builder futuro | Cotejar blobs y revisar cambios arquitectónicos | No concluir integración sin código |
| S02 | El backend será autoridad de membresía/permiso por cada recurso | Diseñar matriz de roles y pruebas API A/B | NO_GO a publicación sin RBAC probado |
| S03 | Catálogo versionado representará componentes sin JS arbitrario | Versionar manifiesto de tipos y contrato | NO_GO a aceptar HTML/JS/SQL ejecutable |
| S04 | Se podrán acotar bytes, profundidad, cantidad y cardinalidad | Definir límites y medir pruebas de carga | NO_GO si no existen cotas seguras |
| S05 | Preview podrá operar sin persistir ni invocar terceros | Prueba de efectos y auditoría cero | NO_GO ante side effects |
| S06 | Una revisión podrá guardarse/publicarse con control CAS | Diseñar versión/ETag y pruebas de carrera | NO_GO si hay lost update |
| S07 | Host y slug públicos podrán resolverse con binding backend | Probar A/B, caché y proyección pública | NO_GO a datos privados en endpoint público |
| S08 | Diseño responsive y teclado podrán preservarse con catálogo de base | Prueba Playwright 320px, foco, motion | NO_GO si el editor rompe accesibilidad |

Todos los supuestos permanecen **POR_VERIFICAR** a nivel de builder. No se infiere autorización por el resultado de COLA o DAG para código, migraciones o despliegues que requieran una orden y gate causal separado.

## D01–D08: decisiones sin tomar

| ID | Decisión requerida | Rol propuesto para aprobar / revisar | Dato que falta | Estado |
| --- | --- | --- | --- | --- |
| D01 | Identidad/versionado de catálogo | Arquitecto frontend + backend con revisión seguridad | Elegir esquema tipado/version y migración compatible | PENDIENTE |
| D02 | Roles de editar, revisar y publicar | Propietario de producto + responsable seguridad | Separar permisos efectivos por tenant | PENDIENTE |
| D03 | Límites de bytes, secciones y profundidad | Líder técnico API + QA/performance | Establecer cuotas y umbrales tras mediciones | PENDIENTE |
| D04 | Protocolos/destinos de medios y links | Responsable seguridad + arquitectura | Definir allowlist de URL/medios, SSRF y CSP | PENDIENTE |
| D05 | Modelo de versión, conflicto y rollback | Arquitecto datos + Application | Definir revisión, CAS, auditoría y reversión | PENDIENTE |
| D06 | Política de preview y aislamiento | Responsable producto + seguridad | Fijar ausencia de efectos y permisos de preview | PENDIENTE |
| D07 | Caché, ruta pública y binding host/tenant | Arquitectura backend + seguridad | Aprobar clave de caché y resolución de tenant | PENDIENTE |
| D08 | Criterios de accesibilidad/UX del catálogo | Diseño UX + QA accesibilidad | Validar componentes, teclado, 320px, reduced-motion | PENDIENTE |

Ninguno de estos roles adquiere autoridad adicional al aparecer en un documento. Las aprobaciones son futuras decisiones humanas/técnicas según gobierno vigente; hasta entonces los valores críticos son NO_GO a implementación/publish. No inventar acuerdos comerciales ni límites de pago.

## Trazabilidad de fuentes realmente inspeccionadas

| Ruta | SHA blob leído en dev | Alcance observado |
| --- | --- | --- |
| docs/v5/c05-03-m01-01-objetivo-no-alcance-builder-gobernado.md | 35fc8299e3529bb42a5ee6913e325f83b84efc95 | Define NR01–NR12 y oráculos futuros, no implementación |
| docs/v5/c05-03-m01-02-actores-entradas-salidas-builder-gobernado.md | d175d24d5704bd62aa726b60cb012310a89b9552 | Delimita 12 actores, 12 I/O y P01–P05/N01–N12, no runtime |
| frontend/src/app/core/models/tema-visual.model.ts | 9d99a1da70c9c6da408d9a564e91512150b4aedd | CAMPOS_TEMA para colores, no builder |
| frontend/src/app/services/theme-applier.service.ts | cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3 | Limpieza y validación de hex, no sanitizador HTML global |
| frontend/src/app/services/theme-applier.service.spec.ts | 99529c4f26a075f58d1b0a08f6817c4d42003814 | Casos unitarios existentes de tokens A/B, inspeccionados no reejecutados |
| backend/src/API/Controllers/EmpresaConfiguracionController.cs | ea1bce800862d706387ccce18b368f401ce3ad3d | GET publico y PUT privado con RequierePermiso, no ACL del builder |
| backend/src/Application/DTOs/TemaVisualDto.cs | b159d86e58a1df246ccb7aa903e7938cf050b7d5 | DTO de colores, no esquema de componentes |
| frontend/src/app/services/empresa-configuracion.service.ts | 0e4f127bc2a7dce504a7318546231fb0776903fe | Clientes de identidad/tenant, sin servicio de páginas |
| frontend/src/styles/_enterprise-design-system.scss | 6a188590af9a958bff2e3e0375c0c46cdb2b14e6 | Tokens responsive existentes, no renderer de builder |

Las referencias demuestran el modelo actual de tema e identidad y documentos de preparación, no certifican HTML sanitizado global, editor, componentes configurables, CAS de páginas o migraciones. Existen pruebas Angular del tema en archivo fuente, **no se reejecutaron aquí**, y sus escenarios no sustituyen pruebas del builder.

## Controles de aceptación para fases posteriores, no tests ejecutados

**P01–P05 FUTUROS:** P01 catálogo reconoce clave permitida; P02 propiedades tipadas producen vista escapada; P03 actor A autorizado guarda solo su borrador; P04 versión vigente publica/revierte con CAS y auditoría; P05 keyboard/foco/320px/reduced-motion conservados.

**N01–N12 FUTUROS:** N01 script/handler bloqueado; N02 HTML/CSS libre bloqueado; N03 SQL nunca ejecutado; N04 javascript:/URL de red privada rechazada; N05 bytes excesivos rechazados; N06 profundidad extrema rechazada; N07 componente no catalogado rechazado; N08 propiedad/enum extra rechazada; N09 A→B acceso denegado; N10 usuario sin permiso no publica; N11 stale version no sobrescribe; N12 preview no dispara eventos/worker/publicación.

## Dependencias, impacto, A0–A5 y rollback

En reclamación individual: PLAN_MAESTRO!H797=PENDIENTE/I797 vacío; COLA!G797/O797=SI; DAG_TECNICO!A758=C05-03.M01.04/F758=VALIDADO/G758=SI, sometidos a CAS. Referencias C01/C02/C05-01/C05-02 son preparatorias. No liberar M01.06 ni M02–M06 por haber documentado riesgos.

**Impacto:** exclusivamente docs/v5 y CHANGELOG_AI.md. No se modifican frontend, Domain, Application, API, MySQL, integraciones, tenancy, políticas, operaciones financieras, permisos, jobs o QA/main/PROD. **A0:** diff/9 blobs y scan básico de secretos; **A1:** 12 riesgos, 8 supuestos, 8 decisiones y 5P/12N futuros coherentes; **A2:** servidor verifica JWT/RBAC/tenant, URL/código/payload/fuga de datos, no se alteran controles; **A3:** correspondencia con la fuente institucional y M01.01/02 y archivos reales; **A4:** fast-forward exact SHA, Scope Lock SUCCESS y V5 procedencia SUCCESS, readback PLAN/COLA/BITACORA; .NET/Angular/MySQL/Playwright de runtime NO_APLICA al diff Markdown, NO afirmar PASS. **A5:** revisor independiente entre cinco Tasks Javier restantes; autor no puede aprobar su propia entrega.

**Rollback:** git revert normal del commit documental, sin force-push. No habilita eventos/webhooks, R2, proveedores de pago, secretos, QA/main/PROD ni cambio de schedules.
