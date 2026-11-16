# SOLQARYN V17 — C13-01.M02.06: contrato terminal de privacidad y consentimiento

**ID:** C13-01.M02.06; **PLAN_MAESTRO:** fila 1885; **DAG:** fila 1846; **autor:** PRIMARY DEV 02, TASK_ID 6af16bb9492c8191b8e8c7d606f11507; **riesgo:** R0 documental; **repo/rama:** solqaryn/Solqaryn@dev; **PROJECT_SCOPE_LOCK:** STRICT.

## A0. Objetivo, limites e impacto real

Se fija el contrato de salida del lote M02 de privacidad, términos, finalidades, consentimiento, revocación, retención y derechos, sujeto al STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y a los estados y dependencias institucionales vivos. Se exige C13-01.M01.01–06+A5, C13-01.M02.01–05+A5, C06-01.M01.06+A5 y C09-03.M01.06+A5 según DAG. M02.01 propuso entidades E01–E16; M02.02 operaciones/DTO; M02.03 claves/índices y migraciones futuras; M02.04 opciones/flags por defecto FALSE; M02.05 eventos, nonce y digest. Esos diseños NO certifican componentes ejecutados ni su cumplimiento legal.

**Cambios efectivos:** este Markdown y una sección indivisible en CHANGELOG_AI.md. No se modifica Domain, Application, Infrastructure, API, Angular, datos MySQL, despliegues, DNS, cookies, proveedores, secretos o condiciones legales. Los consumidores futuros serían API/autenticación, Domain/Application, Oracle EF Core 10/MySQL 8.4, interfaz Angular 22.2.1, auditoría y responsables jurídicos. **Rollback documental:** nuevo commit git revert luego de comparar HEAD y conservar BITACORA, nunca force-push. No hay transacción ni política productiva activada.

## G01–G16. Puertas de aceptación futura — ninguna certificada en runtime

| Gate | Precondición objetiva | Negativo o NO_GO |
| --- | --- | --- |
| G01 | Todos los predecesores DAG realmente LISTO y A5 independiente cuando aplique | VALIDANDO, A5_FAIL o owner incierto bloquea |
| G02 | Finalidad y versión de política revisadas por responsable jurídico | Texto no revisado no habilita consentimiento ni publicación |
| G03 | Tenant y sujeto resueltos en backend con ownership comprobado | Header, subdominio y selector cliente no son autoridad |
| G04 | JWT válido y RBAC, membresía y revocación actuales en cada operación | Token aún vigente con membresía revocada se rechaza |
| G05 | Propósito aprobado, versión inmutable y digest canónico completo | Clave desconocida, colisión ordinal o versión stale se rechaza |
| G06 | Consentimiento explícito verificable por finalidad y sujeto | Silencio, preselección, timeout o error de UI no conceden |
| G07 | Revocación CAS persistida e invalidación de snapshots | Replay antiguo nunca revive aceptación revocada |
| G08 | Nonce estable solo para reintentos de una intención lógica | Mismo nonce, payload canónico completo distinto = CONFLICT |
| G09 | Clave/FK tenant-compuesta con pruebas aisladas A/B | FK global o lectura cross-tenant son NO_GO |
| G10 | Migración reversible probada en MySQL 8.4 efímero con Category=Integration real | Build verde o mocks no validan EF/MySQL |
| G11 | Auditoría minimizada con referencias opacas y correlación acotada | JWT, cookie, email, token o PII libres en logs/DLQ se rechazan |
| G12 | Retención, legal hold y excepciones revisados separadamente | TTL no aprobado no ejecuta DELETE irreversible |
| G13 | Proveedor, región, finalidad y contrato autorizados expresamente | Flag o documento no activan SDK, cookies ni exportación |
| G14 | Accesibilidad efectiva: teclado, foco, contraste, móvil y rechazo | 403/503 y foco inválido no generan aceptación |
| G15 | A0–A4 causales con Scope Lock/V5 SUCCESS y A5 por Task distinta | SKIPPED, CANCELLED, FAILED, IN_PROGRESS o auto-A5 no son PASS |
| G16 | Autorización independiente para QA/main/PROD y cambios irreversibles | LISTO DEV documental no promueve entornos ni servicios |

**Política fail-closed:** ningún gate documentado equivale a permiso de runtime. Los valores calculados DAG F/G y COLA G/O se leen, jamás se ponen en SI manualmente. No inferir una base jurídica, duración de retención o decisión personal desde un contrato técnico.

## T01–T12. Pares de aceptación futuros — 0/12 ejecutados

| Caso | Positivo futuro | Negativo que debe fallar ante defecto |
| --- | --- | --- |
| T01 | Tenant A y sujeto verificado leen solo sus preferencias | B solicita ID de A y recibe dato o indicio de existencia |
| T02 | Roles y membresía actuales permiten operación autorizada | JWT de miembro revocado conserva efecto |
| T03 | Aceptación explícita registra finalidad+versión válida | Silencio, timeout o versión sin revisar se interpreta como sí |
| T04 | Mismo nonce lógico y digest completo devuelve único recibo | Payload distinto bajo mismo nonce se deduplica falsamente |
| T05 | Nueva intención con nonce nuevo produce decisión separada | Nonce reutilizado para otra intención se acepta sin conflicto |
| T06 | Revocación CAS invalida proyecciones y futuras acciones | Evento de aceptación viejo rehabilita preferencia |
| T07 | Evidencia auditada se minimiza en digest/referencia opaca | Log o DLQ contiene JWT, cookie, email o PII |
| T08 | Cambio de versión exige revisión de consentimiento | Aceptación v1 se aplica automáticamente a v2 |
| T09 | Derecho/exportación comprueba sujeto, rol, finalidad y tenant | IDOR/BOLA permite exportar expediente de otro sujeto |
| T10 | Legal hold aprobado impide eliminación | TTL supuesto borra factura, respaldo o evidencia |
| T11 | MySQL 8.4 aislado prueba claves compuestas y rollback | FK global, collation o migración parcial mezcla tenants |
| T12 | Chromium valida rechazo accesible, 403/503 y revocación | UI sin foco o error concede consentimiento implícito |

Estos casos son FUTUROS_NO_EJECUTADOS: 0/12 pares, 0 xUnit, 0 Playwright, 0 consultas HTTP, 0 migraciones EF/MySQL, 0 proveedores, 0 evidencia de cumplimiento jurídico. Cobertura de runtime, RTO/RPO y tasa de defectos = NO_MEDIDO. En cambios futuros R2 los asserts han de fallar ante IDOR, replay, revocación, divergencia de digest, colisión de clave y rollback corrupto; no sustituir pruebas de integración por mocks.

## RB01–RB08. Propuesta de reversión futura, sin ejecución

| Paso | Regla de seguridad |
| --- | --- |
| RB01 | Congelar alcance y conservar SOURCE_SHA, owner, versiones y evidencia |
| RB02 | Detener operación afectada sin modificar estado de otro tenant |
| RB03 | Desactivar flags optativos de manera controlada: default false |
| RB04 | Preservar revocaciones e historial de decisión; nunca recrear consentimiento |
| RB05 | Revertir migración solo tras ensayo en MySQL 8.4 efímero y backup válido |
| RB06 | Comparar hashes, claves tenant A/B, índices y transacciones contra baseline |
| RB07 | Documentar causa, severidad P0/P1, pruebas regresivas y revisor distinto |
| RB08 | Reabrir exclusivamente tras CI required SUCCESS, A5 y autorización de entorno |

**Rollback de esta unidad:** git revert aditivo del Markdown/CHANGELOG tras verificar HEAD. Ninguno de RB01–RB08 se ejecutó sobre infraestructura o datos.

## Evidencia A0–A5 y condiciones de continuidad

**A0:** alcance M02.06, predecesores y diff limitado a dos Markdown; criterios G01–G16 y riesgos/rollback. **A1:** revisión estática de 16 puertas, 12 pares adversariales y 8 reglas de reversión; NO afirmar tests runtime. **A2:** evaluación documental de tenant A/B, ownership, JWT/RBAC, revocación, consentimiento explícito, nonce estable, digest del payload canónico completo, privacidad/PII y legal hold. **A3:** MySQL, API, frontend, navegador, proveedores, cookies y hosting son NO_APLICA porque el diff R0 no toca código; las pruebas obligatorias existirán en otras unidades R1/R2. **A4:** commit fast-forward dev, Scope Lock STRICT, V5/procedencia SUCCESS en SHA exacto o equivalente verificado, PLAN/COLA/DAG/BITACORA readback y candado V17. **A5:** PENDIENTE de otra Task Javier con ID diferente del autor, con potestad de rechazar.

Cierre documental LISTO no implica que M03, QA/main/PROD, migraciones, obligaciones legales, cookies, proveedores, dominio o despliegues estén autorizados. No habilitar sucesores salvo cumplimiento real de DAG y A5.
