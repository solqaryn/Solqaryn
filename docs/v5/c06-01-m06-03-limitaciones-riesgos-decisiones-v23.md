# C06-01.M06.03 — Limitaciones, riesgos abiertos y decisiones comprobables (V23)

Proyecto SOLQARYN, repositorio `solqaryn/Solqaryn`, rama `dev`. `PROJECT_SCOPE_LOCK=STRICT`. Documento de alcance **R0** para `PLAN_MAESTRO!D898`: documentar limitaciones, riesgos abiertos y decisiones. `PLAN!E898` expresa una meta acumulativa de M06 y **no** acredita cierre integral de M06, M05, publicación, QA ni PROD. Esta entrega no altera frontend, backend, migraciones, JWT, permisos, proveedores ni datos.

## Fuentes y alcance revisados (21-11-2026)

- `PLAN_MAESTRO!A890:S900`, `COLA` y `DAG_TECNICO` vigentes; M05.01–M05.06 figuraban `LISTO` y con A5 independiente documentada antes de este CLAIM. Cada `LISTO` se limita a su acción D correspondiente.
- Angular público `/conoce-solqaryn`: cinco anchos de Chromium (320/390/768/1280/1440), navegación por teclado, CTA `/login`, respuestas sanitizadas de estado, petición de salud sólo tras pulsación y capturas del entorno CI. `C06-01.M05.06` código/pruebas y workflow SHA `5c54571785ad0fc89f74d579b808a85c7d31c5d7`; ejecución dirigida [GitHub Actions](https://github.com/solqaryn/Solqaryn/actions/runs/44956939907/job/137007041604) `completed/success`, 5/5 pruebas Playwright reales, 0 fallidas, 0 omitidas, diez PNG no vacíos en [artifact 14727862529](https://github.com/solqaryn/Solqaryn/actions/runs/44956939907/artifacts/14727862529). Los umbrales `DCL < 15000ms` y `DOM < 4000 nodos` son **observaciones acotadas de laboratorio**, no valores de campo o presupuesto productivo.
- M05.02/M05.03 aportaron pruebas concretas de readiness y proveedor Oracle EF / MySQL 8.4 efímero según sus registros de la hoja, sin certificar por ello el flujo completo autenticado de usuarios de negocio. M05.04/M05.05 probaron rutas y errores de la landing con fixture HTTP local y navegador Chromium; no son pruebas de endpoints privados.
- A5 independientes de M05.01–.06 se preservan en `PLAN!K` y `BITACORA` por actor distinto; M05.06 A5 de DEV04 `SQV23-J30-47DD4611-C0601M0506-A5PASS-5C545717-20261121-R11`.

## Riesgos y límites abiertos; no convertirlos en PASS

| ID/ámbito | Evidencia disponible | Límite pendiente y tratamiento |
| --- | --- | --- |
| `AUDIT_UI_BRECHAS: UI-03` (P1, parcial) | Landing accesible por `/conoce-solqaryn`; test de acceso y CTA | La raíz pública sigue redirigiendo a `/login` según el reporte de M05.04. Mantener la brecha parcial y su severidad; tratarla en microtarea elegible y autorizada, sin editar rutas aquí ni marcarla resuelta. |
| Experiencia real de dispositivo y UX premium completa | Screenshot PNG y Chromium de CI con anchos y teclado; pruebas de reflow/overflow | No se ha verificado físicamente cada móvil/OS ni auditoría WCAG integral, zoom de sistema, accesibilidad con tecnologías asistivas o todas las rutas de la app. Capturas de laboratorio **no** certifican el diseño premium global. |
| Rendimiento y disponibilidad | Navegación DCL medida en CI bajo fixture local; estado explícito de error/429 | No existen métricas verificadas de Core Web Vitals en campo, tráfico real, SLA, DNS, hosting productivo ni latencia de proveedores externos; `NO_MEDIDO` hasta instrumentación y autorización pertinentes. |
| Privacidad, JWT/RBAC y aislamiento | Ruta pública no consulta endpoints privados en los tests; se valida que no refleje token/redirect de la query | Esto **no** certifica firmas JWT reales, revocación de membresía, IDOR/BOLA, aislamiento tenant A/B, pagos ni política de seguridad completa; requieren pruebas causales de rutas privadas en sus scopes correspondientes. |
| Datos y proveedores | Pruebas aisladas Oracle EF/MySQL 8.4 de microtareas M05 específicas | No equivalen a persistencia real completa, consentimientos, formularios externos de contacto, Cloudinary, campañas, transacciones ni rollback productivo. |
| SEO, legal, demo y oferta | Texto y navegación local de la landing, con alcance funcional limitado | No hay certificación de SEO indexado real, sitemap público publicado, textos legales revisados externamente, precios/planes comerciales publicados o conversión del formulario de contacto real. La descripción de fase E es acumulativa. |

## Decisiones vigentes de esta microtarea

1. Conservar intactas las rutas Angular y todos los controles del backend. El documento distingue la acción D898 de los resultados acumulativos M05/M06. No abrir host, QA, producción, pagos ni alterar DNS.
2. Mantener visible la condición parcial de `UI-03` y otras carencias. El protocolo de QA exige corregirlas en su scope, repetir CI exact-SHA y revisión independiente cuando corresponda; no introducir excepciones por esta nota.
3. Mantener estados históricos `A5_FAIL`, evidencias ya aprobadas, owners originales y fórmulas derivadas `COLA/DAG`; el estado de una microtarea posterior nunca sustituye esas dependencias.
4. Un `SKIPPED`, `CANCELLED`, `FAIL`, `QUEUED` o `IN_PROGRESS` de un check **causalmente requerido** no se convierte en PASS. Se evalúa pertinencia por diff y riesgo: para este documento R0 no se inventan validaciones HTTP/JWT/MySQL/Chromium nuevas.
5. La reversión autorizada de esta entrega es un `git revert` convencional de su commit documental, preservando la historia compartida. No borrar BITACORA ni modificar datos comerciales. El artefacto documenta límites; no desactiva funcionalidad.

## A0–A5, referencias y seguimiento

- **A0:** contrato D898, lista de evidencias anterior, tabla de límites y diffs documentales; sin cambios runtime o interfaces API.
- **A1:** comprobación documental por comparación de enlaces/SHAs/estados con la hoja y jobs reales; pruebas funcionales nuevas: `NO_APLICA` por no tocar runtime.
- **A2:** `NO_APLICA` a JWT/tenant privados en este diff R0; se preserva explícitamente el límite y no se simula aislamiento.
- **A3:** `NO_APLICA` a nuevas integraciones de navegador/MySQL; se distinguen las pruebas previas de los gaps aún abiertos.
- **A4:** requerir `verify-solqaryn-project-scope` y procedencia V5 pertinentes del SHA publicado, readback `PLAN/COLA/BITACORA`; sin fingir equivalencia de SHA.
- **A5:** `PENDIENTE` revisión genuina por otra Task Javier distinta de DEV04, sin autoaprobación.

Referencias originales consultadas por la revisión del área pública: SKREF-03 Impeccable, SKREF-05 Taste, SKREF-06 Humanizer y SKREF-10 Angular Developer, con pins y rutas de `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md`. Para el **diff documental** de este ID, se aplica SKREF-06 a terminología fiel; SKREF-03/05/10 informan el contexto pero no imponen nuevas pruebas UI. SKREF-04 motion, SKREF-11 threat model y SKREF-12 profiling .NET son `NO_APLICA`: ninguna de esas superficies cambia. No se instalaron ni copiaron skills externas.

Fuente de verdad del progreso: `PLAN_MAESTRO!H/I/J/K/L/M`, `BITACORA!A:Z`, `COLA!G/O` y `DAG_TECNICO!F/G`. Este archivo registra las decisiones y el handoff, no una certificación de M06 ni una autorización de QA/main/PROD.
