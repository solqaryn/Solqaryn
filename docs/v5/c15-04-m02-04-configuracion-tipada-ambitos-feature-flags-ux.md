# SOLQARYN V5 — C15-04.M02.04: configuración tipada, ámbitos y flags seguros para auditoría UX

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`; fecha local 2026-11-14. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clasificación:** contrato documental M02; no se crean ni activan flags en la aplicación.

## 1. Fuentes reales, dependencias y alcance

Fuente de ejecución: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Al reclamar `C15-04.M02.04`: `PLAN_MAESTRO!H2279=PENDIENTE`, I vacío, `COLA!G/O2279=SI`, `DAG_TECNICO!A2240=C15-04.M02.04`, F=VALIDADO, G=SI. Los seis documentos preparatorios `C15-04.M01.01–06` estaban LISTO con A5 independiente de otra Task Javier. M02.01 y M02.02 se encuentran LISTO_DEV desarrollados por esta Task, pendientes de su A5 independiente; NO se requieren para liberar M02.04 por el DAG actual, ni ello significa que M02.06 esté habilitada.

Archivos y blobs Git leídos en HEAD autorizado:
- `frontend/src/environments/environment.ts`: `fcc3144df39328605e7d7b561d4024b930fdfd59`; literal actual `production:false`, `apiUrl:'http://localhost:5005'`, solo desarrollo.
- `frontend/src/environments/environment.prod.ts`: `8512c57918c3f9bc061e83ea89f7549106c45fe7`; `production:true` y `apiUrl:'/api'` para proxy same-origin de Vercel. Esta lectura **no autoriza** modificar producción.
- `frontend/src/app/core/auth/tenant-context.service.ts`: `63a711355495d2f80efba35bffb2b8727401154a`; `TenantContextService` materializa `TenantContextoVerificado` tras backend, localStorage solo expresa una empresa solicitada sin autoridad.
- `frontend/src/app/core/auth/permisos-runtime.service.ts`: `b72cf568c9fdd294d98433db238644796cb75994`; existen `RUTAS_PROTEGIDAS` y permisos de módulo/acción a partir de contexto verificado, no de un flag visual.
- `frontend/src/app/app.routes.ts`: `2f560c14693455b79ac4546e9333fb74586ef234`; rutas públicas y privadas separadas por guards.
- `frontend/src/styles/_enterprise-design-system.scss`: `6a188590af9a958bff2e3e0375c0c46cdb2b14e6`; tokens y breakpoints UX actuales.
- `frontend/src/styles/_m10-accessibility.scss`: `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f`; foco, skip-link y estados accesibles existentes.

La autoridad arquitectónica continúa en Angular 22.2.1, .NET 10, JWT/RBAC y MySQL/EF Core. Este alcance **no** cambia archivos .ts/.scss, políticas de ruta, almacenes, permisos, worker o infraestructura; define propuestas tipadas para una futura implementación específicamente aprobada.

## 2. Modelo de configuración tipada propuesto (NO_IMPLEMENTADO)

| Clave | Tipo conceptual | Ámbito permitido | Default cerrado | Decisión de seguridad |
| --- | --- | --- | --- | --- |
| CFG01 `auditUx.enabled` | boolean | dev / cuenta autorizada | false | Auditoría no visible al visitante ni ejecutada automáticamente |
| CFG02 `auditUx.scopeKeys` | lista de IDs validados | scope del Standard V5 | [] | No admitir ID inventado ni reutilizado |
| CFG03 `auditUx.tenantContextMode` | enum `backend_verified` | empresa autenticada | backend_verified | localStorage no eleva acceso |
| CFG04 `auditUx.captureMode` | enum `disabled,redacted` | entorno y tenant autorizado | disabled | Evitar capturas con PII sin minimización |
| CFG05 `auditUx.viewportProfiles` | lista tipada de ancho/alto | fixture de prueba | [] | No inferir cobertura móvil si no se ejecuta |
| CFG06 `auditUx.keyboardSuite` | boolean | fixture local/CI dirigido | false | No afirmar navegación accesible por habilitarlo |
| CFG07 `auditUx.motionSuite` | enum `off,on` | fixture de preferencias | off | Mantener contenido visible con reduced-motion |
| CFG08 `auditUx.contrastSuite` | boolean | fixture auditado | false | Token presente no significa contraste medido |
| CFG09 `auditUx.performanceProfile` | referencia a baseline/red | suite CI | null | Sin fixture no hay cifras ni presupuestos certificados |
| CFG10 `auditUx.evidenceRetention` | referencia a política aprobada | evidencia por tenant/entorno | null | Prohibido afirmar retención o subir a proveedor no autorizado |
| CFG11 `auditUx.testDatasetRef` | referencia efímera | CI/fixture autorizado | null | Jamás señalar datos reales de producción por defecto |
| CFG12 `auditUx.reviewGate` | enum `independent_required` | cada scope | independent_required | A5 no puede firmarlo su autor |
| CFG13 `auditUx.publishScope` | enum `dev_only` | proyecto | dev_only | Flags no autorizan QA/main/PROD |
| CFG14 `auditUx.retryPolicy` | objeto límites finitos | prueba / corrida | `maxAttempts:0` | Nada de reintentos automáticos en bucle |

Las claves CFG01–CFG14 son **propuestas documentales**, no variables existentes en environment ni secretos. Los defaults de prevención reflejan un diseño fail-closed y no describen comportamiento runtime actual. M02.05 y otros scopes decidirán eventos y correlación; no se implementa ningún dispatcher.

## 3. Ámbitos y precedencia de autoridad

| Prioridad | Fuente verificada | Puede decidir | Nunca debe decidir |
| --- | --- | --- | --- |
| 1 | Gobierno SOLQARYN, Scope Lock y autorización del propietario | Repositorio, rama, identidades, ambientes | Ser anulada por flags de UI |
| 2 | Backend/JWT/RBAC y membresía tenant | Acceso a recurso y acción real | Confiar en enterprise ID del navegador como autoridad |
| 3 | PLAN H/I + COLA G/O + DAG F/G | Elegibilidad por microtarea bajo CAS | Conceder permisos de infraestructura o identidad |
| 4 | Configuración tipada verificada | Controlar visibilidad o ejecución de test ya autorizado | Conceder APIs, tenants, secretos o nuevas Tasks |
| 5 | Parámetros UI y preferencias de usuario | Render, motion y ergonomía sin pérdida de derechos | Habilitar ruta privada ni saltarse backend |

La precedencia es un contrato de **diseño**; las fuentes de gobierno siguen siendo las autoritativas. Publicación de un setting desde interfaz cliente no cambia autoría ni estado de PLAN. Los temas visuales de empresas diferentes se aislan por identidad backend y no se comparten cachés de recursos sensibles.

## 4. Reglas de validación y fallos seguros

| Regla | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| VAL01 | Config tipada rechaza claves desconocidas | Campo extra se convierte en permiso |
| VAL02 | Boolean/enum/lista se validan antes de aplicar | String accidental habilita feature |
| VAL03 | Entorno distinto de dev requiere autorización real | Flag publicado se usa para desplegar QA/PROD |
| VAL04 | Actor sin RBAC no ve ni accede a recurso privado | Toggle de UI evita autorización server-side |
| VAL05 | Captura sensible queda deshabilitada o anonimizada | PII/secretos en screenshots o logs |
| VAL06 | Tenant A/B usa contextos y cachés separados | Estado visual de B se filtra a A |
| VAL07 | Test a11y marca `NOT_RUN` sin ejecución | Activar flag se considera WCAG PASS |
| VAL08 | Métrica sin baseline queda `NO_MEDIDO` | LCP/CLS/INP inventado por configuración |
| VAL09 | Evento y retry sólo al recibir permiso nuevo y límites | `run_now` produce infinito server-side |
| VAL10 | Actualización de setting posee rollback reproducible | Config desconocida queda activa tras falla |
| VAL11 | CI corresponde al SHA exacto y conclusión success | queued/skipped/cancelled se atribuye PASS |
| VAL12 | A5 realizado por otro Task Javier con SHA original | Autor se autocertifica por flag de review |

Las reglas son **FUTURAS_NO_EJECUTADAS**, no se realizaron test suite de Angular, browser, MySQL o cross-tenant durante esta entrega Markdown. VAL09 no habilita eventos ni cambios de programación.

## 5. Compatibilidad, pruebas y rollback

**Compatibilidad:** `environment.ts` conserva su URL local; `environment.prod.ts` conserva proxy `/api`; TenantContextService mantiene señal de empresa solicitada separada de `TenantContextoVerificado`; PermisosRuntimeService conserva autorización por módulo/acción y rutas. Una implementación futura de flags no puede sobreescribir las guards, romper el modo zoned, ampliar permisos RBAC, modificar secrets o sustituir endpoints existentes sin contratos y CI. No existe nuevo flag real al terminar M02.04.

**Pruebas futuras dirigidas:** tests unitarios de validación del objeto tipado y defaults; tests negativos de tenant A/B y permisos en backend; Playwright con teclado/reduced-motion/viewport 320 px solo sobre fixture autorizado; pruebas de ausencia de PII y baseline de rendimiento; verificación de que QA/main/PROD no cambian. Preservar evidencia positiva/negativa exact SHA por caso y rollback de configuración.

**A0–A5 de este changeset:** A0 verifica las catorce claves propuestas y doce reglas negativas; A1 verifica estructura de criterios futuros, no su ejecución; A2 conserva JWT/RBAC/tenant A/B y no introduce privilegios; A3 compara modelos previos y fuentes reales de environment, tenant-context, permisos y estilos, sin runtime atribuido; A4 requiere solo nuevo Markdown + CHANGELOG con sección completa, fast-forward por CAS, Scope Lock y verificador V5 `completed/success` del SHA publicado, readback PLAN/COLA/BITACORA; A5 sigue pendiente de una Task Javier diferente.

**Rollback documental:** revert Git normal del changeset y evento aditivo, sin force-push, reset ni borrado de evidencia histórica. No se modifican cuentas, usuarios, permisos, deployment, DNS, datos reales o planes de pago.
