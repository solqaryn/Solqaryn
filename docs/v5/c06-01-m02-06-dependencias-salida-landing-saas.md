# SOLQARYN V16 — C06-01.M02.06: dependencias y salida de landing SaaS

**Microtarea:** C06-01.M02.06, PLAN_MAESTRO fila 877. **Autor:** PRIMARY DEV 06 Javier; TASK_ID 6af16bbad26881918c283abe06e1f32a. **Alcance:** solqaryn/Solqaryn dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL, ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Riesgo V15:** R0 documental sin runtime.

## A0. Alcance, antecedentes y autoridades

La salida de M02 es un contrato técnico para landing institucional: propuesta de valor, planes, sectores, demo, contacto, legal, SEO, sitemap y formularios. No significa sitio funcional, configuración, aprobación comercial, despliegue ni permisos nuevos. El claim exige PLAN H877=PENDIENTE/I vacío, COLA G/O=SI, DAG F838=VALIDADO/G838=SI; todos son dinámicos y requieren readback.

| Documento precedente | Contrato verificado, sin ejecución runtime |
| --- | --- |
| C06-01.M02.01 | 14 entidades conceptuales, 18 invariantes y 10 pares P/N futuros: ownership y tenant |
| C06-01.M02.02 | 8 operaciones API/DTO, 12 errores, 16 validaciones y 10 pares P/N futuros |
| C06-01.M02.03 | 12 familias de persistencia/índices/migraciones propuestas y 10 fixtures futuros |
| C06-01.M02.04 | 14 opciones tipadas, 16 validaciones y 8 pares P/N futuros |
| C06-01.M02.05 | 12 eventos, 14 invariantes de idempotencia, 10 pares P/N futuros, NO_IMPLEMENTADOS |

Los cinco antecedentes deben estar en LISTO con A5 independiente de Task distinta de su autor. Las decisiones comerciales/legales D01–D08 continúan PENDIENTES: no inventar precio, planes, promoción, condiciones, SEO público, consentimiento o legal. No se crean Angular, endpoints HTTP, JWT, tablas EF/MySQL, SMTP, colas, workers, webhooks, secretos, CDN, DNS, QA/main/PROD, cuentas de pago ni infraestructura.

## G01–G16: puertas dinámicas y criterios NO_GO para M03+

| Gate | Prueba causal requerida | NO_GO |
| --- | --- | --- |
| G01 | M02.01–05 LISTO + A5 independientes | A5_FAIL o aprobación propia |
| G02 | D01–D08 comercial/legal formalmente aprobadas | Precios, legal o claims no autorizados |
| G03 | Contratos de entidades, ownership y tenant correctos | Cruce de datos A/B por slug |
| G04 | DTO, errores e inputs backend versionados | UI suplanta JWT/RBAC |
| G05 | Consentimiento y retención PII mínimas | Lead/email sin consentimiento |
| G06 | SEO allowlisted, canonical y sitemap públicos | ERP/factura privada indexada |
| G07 | Plan y sector del catálogo real con evidencia | Plan/sector inventado |
| G08 | Rate limit, idempotencia, antiabuso | Replay envía doble correo |
| G09 | Config tipada y flags por fuente server-side | Flag altera seguridad o roles |
| G10 | EF/MySQL Oracle probados en CI MySQL 8.4 efímero cuando se implementen | SQL sin migración/rollback probado |
| G11 | JWT, membresía, RBAC y tenant A/B reales en API | Guard Angular sustituye backend |
| G12 | Angular 22.2.1, responsive, teclado, foco y Playwright real | WCAG por documentación sin navegador |
| G13 | Eventos M02.05 permanecen inertes sin autorización | Crear dispatcher/webhook/SMTP automáticamente |
| G14 | Positivos, negativos, frontera/error ejecutables por componente | FUTURO_NO_EJECUTADO contabilizado PASS |
| G15 | Scope Lock, V5 y CI obligatoria SUCCESS SHA exacto | CANCELLED/SKIPPED/QUEUED/FAIL contados PASS |
| G16 | PLAN/COLA/DAG reales, A0–A4 y A5 distinta Task | LISTO_DEV autoriza QA/main/PROD |

Un gate documental no habilita automáticamente módulos técnicos ni M01.06/M06.06. Toda sucesora exige PLAN H=PENDIENTE, I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI, más su propio análisis R0–R3 y A5 exigida. JWT/RBAC/tenant son autoridad del backend; documento, host, header, precio, flag o slug jamás son credencial.

## RB01–RB08: rollback futuro y disparadores

| Código | Causa | Respuesta segura |
| --- | --- | --- |
| RB01 | Documento o contrato incoherente | git revert normal del changeset documental, sin borrar BITACORA |
| RB02 | Plan/precio/legal sin aprobar | Retirar borrador, NO publicar, consultar decisión D01–D08 |
| RB03 | Contenido de tenant A servido a B | Denegar, invalidar caché/epoch y probar backend A/B |
| RB04 | Consentimiento ausente o replay SMTP | Detener integración y conservar evidencia privada |
| RB05 | Robots/sitemap revela ruta privada | Corregir rutas, invalidar caché y auditar PII |
| RB06 | Migración EF/SQL no reversible | NO_GO, restore realmente probado en MySQL efímero |
| RB07 | Fallo de UI, foco, contraste o bundle | Revertir componente y repetir Playwright/medidas |
| RB08 | CI, Scope Lock o A5_FAIL | Mantener bloqueo, diagnóstico de logs y reparación causal |

Solo RB01 se aplica a este diff Markdown. RB02–RB08 son procedimientos FUTUROS_NO_EJECUTADOS, no ejecución ni evidencia de proveedor/DB.

## T01–T08: positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| T01 | Contenido aprobado público por ruta allowlisted | Anónimo ve recurso ERP privado |
| T02 | Plan verificado con aprobación comercial | Precio inventado se publica |
| T03 | Consentimiento y lead mínimo registrado una vez | Replay duplica lead/correo |
| T04 | Tenant A lee solo su contenido | Tenant B lee A por slug/header |
| T05 | Sitemap/robots sólo lista rutas públicas | Factura y datos personales indexados |
| T06 | Config segura conserva teclado/foco | CSS/JS externo o tab bloqueado |
| T07 | Migración/restore MySQL 8.4 efímera con Category=Integration ejecutada | Tests DB omitidos o rollback irrecuperable |
| T08 | CI obligatoria exact-SHA y A5 Task distinta | CI cancelada o autor se autopasa |

Ningún T01–T08 se ejecutó por este documento. LCP/INP/CLS, tasa de error, latencia, métricas de integración y cobertura HTTP = NO_MEDIDO.

## A0–A5, publicación y rollback de este changeset

**A0:** revisar los cinco contratos previos y A5; diff solo este documento y una sección autocontenida de CHANGELOG. **A1:** coherencia estática de 16 gates, ocho rollback y ocho pares P/N sin afirmar suites. **A2:** inspección documental de JWT/RBAC, PII/consentimiento, tenant A/B, SEO e idempotencia; pruebas de seguridad runtime NO_APLICA a R0. **A3:** xUnit, EF/MySQL, Angular/Playwright, API, SMTP y proveedores NO_APLICA al diff Markdown-only; seguirán obligatorios cuando se implementen. **A4:** commit FF/CAS a dev, verificar Scope Lock STRICT y V5 provenance completed/success por SHA, readback PLAN/COLA/DAG/BITACORA y registro A5_PENDIENTE. **A5:** revisión independiente por Task Javier distinta del autor, puede rechazar. No promover a QA/main/PROD.

Rollback real: git revert normal del commit de documento+CHANGELOG en dev, sin forzar rama ni reescribir BITACORA. M02.06 entrega el contrato documental, no la landing, no aprobación de precio/legal ni permiso de despliegue.
