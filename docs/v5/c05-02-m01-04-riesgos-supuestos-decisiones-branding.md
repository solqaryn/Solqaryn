# SOLQARYN V5 — C05-02.M01.04: riesgos, supuestos y decisiones pendientes de branding

**Unidad:** C05-02.M01.04, M01 de preparación. **Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT / TASK_ID \`6adbb9fff1548191a1bd5b30d9e9984a\`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive ID \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`. **PLAN_MAESTRO:** fila 761. **Repo:** \`solqaryn/Solqaryn@dev\`. **Fecha:** 2026-11-14. **Scope Lock:** STRICT.

## Propósito y evidencia de partida

Este documento identifica los riesgos potenciales, supuestos falsables y decisiones pendientes para logos, temas, identidad visible y futuros presets por rubro. **Registrar un riesgo no demuestra que exista una vulnerabilidad**; marcar una decisión PENDIENTE no concede permiso para ejecutar cambios. M01.04 es **documental** y no altera API, JWT/RBAC, MySQL, estilos, archivos, workers o publicaciones.

Fuentes de evidencia verificadas previamente en este mismo alcance:
- \`backend/src/Domain/Entities/TemaVisual.cs\` blob \`c17fd1a3398ac1676f09a69e4c9b20f94aa20855\` define tema **global** con \`Id=1\`; no tenant por registro.
- \`backend/src/Infrastructure/Repositories/TemaVisualRepository.cs\` blob \`350604fe85fb5361d52f7c28cb61f3a36789f6c6\`: recupera primera entrada, sin filtro \`EmpresaId\`.
- \`backend/src/API/Controllers/TemaVisualController.cs\` blob \`a33991fbf941c1fc3048dd30425f6342a0b0c6a4\`: lectura de colores anónima deliberada, escritura y reset con autorización/permiso.
- \`backend/src/Application/Services/TemaVisualService.cs\` blob \`f92ee8e34e88ca8e611a81d7f00623e823f7b10b\`: hex, persistencia y auditoría; restaurar equivale a defaults, no a snapshot versionado.
- \`backend/src/Infrastructure/Persistence/Configurations/TemaVisualConfiguration.cs\` blob \`0700a9cecb85c6e3e91a011fbd44539107b64627\`: semilla \`Id=1\`, sin tabla de revisiones.
- \`frontend/src/app/services/theme-applier.service.ts\` blob \`cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3\`: allowlist, regex hex, limpieza y protección de respuesta tardía.
- \`frontend/src/app/services/empresa-identidad.service.ts\` blob \`88526c37ad3d50dc454e8e23ce7899818a0ada72\`: versión local de identidad y restauración de contexto.

Los contratos de M01.01 y M01.02 y el inventario de M01.03 establecen los límites; ninguno acredita un editor de presets, un sistema de publicaciones versionadas ni certificación tenant A/B. Todo hallazgo de seguridad requiere reproducción en el entorno pertinente antes de etiquetarse como fallo comprobado.

## Registro de riesgos R01–R12 (hipótesis, NO incidentes confirmados)

| ID | Escenario hipotético | Impacto / condición observable futura | Control y prueba requerida |
| --- | --- | --- | --- |
| R01 | Usar el tema GLOBAL como si fuera personalización aislada por tenant | Cambiar paleta A altera apariencia B | Distinguir modelo global de modelo tenant; prueba A/B y decisión explícita de diseño |
| R02 | Inferir autorización desde tema, logo, \`empresaId\` o \`storeKey\` | Acceso cruzado a edición | Validación JWT, membresía, RBAC y tenant server-side, denegación 403/404 pertinente |
| R03 | Permitir CSS/HTML/JS o URLs arbitrarias desde preset | XSS o exfiltración hipotética | allowlist tipada, rechazo de payload hostil e inspección de DOM |
| R04 | Exponer en GET público configuración empresarial o secretos | Fuga de datos privados en caché/CDN | respuesta pública mínima, separación de scope y pruebas sin JWT |
| R05 | Mantener caché o respuesta tardía de marca A al cambiar a B | Contaminación visual entre contextos | invalidación por tenant, carrera A→B, logout seguro |
| R06 | Publicar sin versión esperada ni concurrencia | Pérdida de cambios entre editores | CAS de revisión, auditoría y conflicto, no last-write-wins implícito |
| R07 | Confundir restauración a defaults con rollback de revisión histórica | Historial no recuperable | snapshots versionados futuro, restauración auditable y prueba de reversión |
| R08 | Aceptar logos con MIME falso, tamaño excesivo o titular ajeno | Fuga, abuso o recurso no fiable | validar firma, tamaño, propietario, ruta y URL; pruebas negativas |
| R09 | Elegir colores válidos hex pero ilegibles | Problema de contraste, foco/error invisible | matriz por estado, ratio real y alternativa no solo cromática |
| R10 | Preset por rubro cambiar estructura/permiso | Escalada desde personalización | esquema de presentación separado del IAM, tests de invariantes |
| R11 | Estado parcialmente aplicado tras caída API/almacenamiento | Publicación visual incoherente | transacción/compensación diseñada y rollback reproducible |
| R12 | Cambiar estilos globales sin regresiones responsive/teclado | Scroll global, pérdida de foco y bloqueos en 320px | CI angular/Playwright/a11y sobre consumidor real cuando corresponda |

Las categorías anteriores son **riesgos por evaluar**; no cuentan como FAIL de seguridad ni suponen que los casos hayan sido ejecutados.

## Supuestos S01–S08, todos sujetos a validación

| ID | Hipótesis de trabajo | Qué evidencia la valida o refuta |
| --- | --- | --- |
| S01 | La identidad visual necesita modalidad GLOBAL y/o por-tenant diferenciada | decisión de producto + matriz de consumidores/hosts |
| S02 | Tema global actual sigue requerido para login público | inventario HTTP y cache de pantallas sin JWT |
| S03 | La futura personalización no requiere nuevas acciones IAM si se reutilizan permisos válidos | matriz RBAC con denegaciones; de lo contrario permiso nuevo aprobado |
| S04 | El sistema de marca tolera versionado inmutable y CAS sin migrar datos históricos destructivamente | prueba de migración efímera + rollback |
| S05 | Presets serán solo configuración visual tipada | esquema de allowlist autorizado, no scripts ejecutables |
| S06 | Upload de logo puede reutilizar infraestructura actual sin introducir exposición cruzada | inventario backend/storage + pruebas MIME A/B |
| S07 | La estrategia de contraste puede cubrir colores y estados previstos | matriz de tokens/estados en Angular y test de accesibilidad |
| S08 | Preview puede separarse de publicación auditada sin escritura accidental | API/DTO y E2E negativo + cobertura de concurrencia |

Ni siquiera un supuesto plausible se convierte en hecho implementado por estar en esta tabla.

## Decisiones D01–D08: explícitamente PENDIENTE

| ID | Decisión necesaria antes de implementación | Autoridad futura/condición de desbloqueo |
| --- | --- | --- |
| D01 | Política global vs marca por empresa vs marca pública por tienda | responsable de producto + seguridad tenancy; diseño revisado |
| D02 | Permisos para editar/previsualizar/publicar/revertir | propietario autorizado y matriz IAM/RBAC |
| D03 | Esquema allowlisted y límites de presets por rubro | diseño técnico y pruebas de inputs prohibidos |
| D04 | Modelo de revisión, número de versión y regla CAS | arquitectura/persistencia y tests de concurrencia |
| D05 | Política de logo, almacenamiento, retención y URL | seguridad/storage, no contratar proveedor nuevo sin orden |
| D06 | Reglas de fallback y publicación fallida | UX + backend, pruebas A/B y compensación |
| D07 | Política de contraste, foco, responsive y reduced-motion | accesibilidad y pruebas automatizadas pertinentes |
| D08 | Criterio release/rollback, observabilidad y promoción | propietario del proyecto y gates A0–A5; QA/main/PROD diferenciados |

**Ninguna D01–D08 está aprobada por este documento.** La persona que implemente una etapa debe obtener autoridad real desde la fuente institucional y registrar decisión, impacto, evidencia y rollback, sin derivarla de un gate \`DAG=SI\`.

## Criterios de aceptación a implementar (NO se ejecutan en M01.04)

**P01–P05 (positivos futuros):** P01 tema global seguro en login público; P02 editor autorizado modifica solo su contexto; P03 tema con contraste accesible; P04 preview sin persistencia; P05 rollback versionado auditado cuando se implemente.

**N01–N12 (negativos futuros):** N01 PUT sin JWT; N02 rol sin permiso; N03 actualización cross-tenant; N04 CSS/JS malicioso; N05 URL logo ajena; N06 cache pública con datos privados; N07 respuesta tardía A→B; N08 validación hex sin contraste; N09 edición concurrente sin CAS; N10 restauración que destruye historial; N11 fallo parcial que publica un estado mixto; N12 preset que altera RBAC. Estos son oráculos, **no PASS/FAIL de runtime**.

## Control de dependencias, A0–A5 y rollback

Se habilitó la preparación documental solo si al momento de claim \`PLAN_MAESTRO!H761=PENDIENTE/I761\` vacío, \`COLA!G761/O761=SI\`, \`DAG_TECNICO!F722=VALIDADO/G722=SI\` y compare-and-swap exclusivo. Esto **no libera** M01.06, IAM, migraciones, publicación real ni producción.

**A0:** verificar alcance documental y fuente Git. **A1:** comprobar matriz de R/S/D y oráculos redactados, sin atribuir tests. **A2:** revisar límites JWT/RBAC/tenant A/B, acceso público y secretos solo como análisis. **A3:** rastrear los siete blobs y las interfaces de seguridad, sin afirmar HTTP/DB/browser ejecutados. **A4:** commit FF dev, SHA exacto, Scope Lock y V5 CHANGELOG \`completed/success\`, PLAN/COLA/BITACORA readback; CI runtime irrelevante a diff Markdown se etiqueta NO_APLICA, nunca PASS ficticio. **A5:** revisión independiente por otra Task Javier sobre SHA original.

Rollback documental mediante \`git revert\` normal, conservando el histórico. Nada de nuevos servicios, eventos, workers, pagos, Tasks, ramas, secretos, deployments, QA/main/PROD ni datos productivos.
