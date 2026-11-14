# P0-001.B — Recuperación de paridad DEV exact-SHA (26-10-2026)

**Dictamen acotado: `PARIDAD_DEV_EXACT_SHA=PASS` observada; `CIERRE_PR_Y_READBACK=PENDIENTE`.** Este documento no fusiona ni declara `LISTO` por sí solo. Es un seguimiento positivo posterior al acta negativa histórica del 25-10-2026, que permanece intacta.

## Alcance y autoridad
- Repositorio `solqaryn/Solqaryn`, rama protegida `dev`, corte `35c67e330f876ee66f8015e889ffa151d6d4643a`.
- Microtarea `P0-001.B` del plan comercial inicial, Task propietaria `PRIMARY_DEV_03` / `6adc2e55f47c81919f6374e561687076`; operación humana Javier mediante PR protegido.
- Resultado exigido de la ficha: fijar HEAD y mapa exacto; **NEGATIVO obligatorio**: rechazar cuando GitHub/Render/Vercel no comparten el SHA. La UI no reemplaza las comprobaciones de backend y seguridad.
- Fuente rectora: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; hoja `STANDARD DE AUTOMATIZACIONES EN SOLQARYN` sólo para conciliación administrativa.
- La evaluación integral de todos los módulos ERP constituye trabajo independiente, **no** requisito de esta certificación de identidad/paridad. No se altera aplicación, credenciales ni datos empresariales.

## Evidencia nativa actual sobre el SHA exacto
| Control | Observación verificable | Dictamen |
| --- | --- | --- |
| GitHub `dev` | `35c67e330f876ee66f8015e889ffa151d6d4643a` | PASS |
| Checks obligatorios | Scope Lock, Seguridad multiempresa y Backend: 3/3 SUCCESS, mismo commit | PASS |
| Render DEV | Servicio `solqaryn-api-dev`, deployment `dep-dbfotee417fc73defu30`, estado `live`, mismo SHA | PASS |
| Vercel DEV | Proyecto `solqaryn-dev`, deployment `dpl_9wHGTwW1YNGucpmG58EqtYXtWsGs` `READY`, alias oficial `solqaryn-dev.vercel.app`, mismo SHA | PASS |
| Health y MySQL | Auditoría institucional `BITACORA!A191:Z191` y `PLAN_MAESTRO!L3`: `/health=status:ok`, `/health/ready=status:ready/database:connected`, mismo SHA | PASS en corte auditado |
| JWT/RBAC/tenancy A/B/C | [MF3 #40377756848](https://github.com/solqaryn/Solqaryn/actions/runs/40377756848): SUCCESS exact SHA, 3 JWT, 12 rechazos de otra empresa, 36 rechazos comerciales, 18 lecturas propias | PASS del subconjunto probado |
| Release | [Workflow consolidado #40373973264](https://github.com/solqaryn/Solqaryn/actions/runs/40373973264) intento 2 SUCCESS `ALREADY_READY_SHA` | PASS |

## CORS, pantalla autenticada y límites
- El propietario obtuvo dos resultados reales `OPTIONS /roles` el 26-10-2026 en el corte anterior `003cceea22bbd85a8f3f542c46816c6befbe5ee3`: origen oficial autorizado (`204` + `Access-Control-Allow-Origin` correcto), origen externo `example.org` sin permiso CORS.
- [Comparación GitHub](https://github.com/solqaryn/Solqaryn/compare/003cceea22bbd85a8f3f542c46816c6befbe5ee3...35c67e330f876ee66f8015e889ffa151d6d4643a): **sólo cuatro archivos de documentación, changelog, test Node y workflow**. Sin cambios frontend, backend, rutas ni CORS. Se preserva la prueba anterior como equivalencia de runtime documentada, **no** se afirma un OPTIONS nuevo en el SHA de corte.
- Captura del propietario: consola `/plataforma` en dominio DEV con sesión y rol de administrador verificados en interfaz. Acredita esa vista manual; no prueba todas las rutas ni es un E2E general.
- Una consulta no autenticada hecha desde un proxy externo devolvió `503` a `/health`, `/health/ready` y `/roles`; **no** se oculta ni se atribuye automáticamente a una caída persistente. El `PASS` de salud arriba es el del corte auditado, no un sondeo nuevo desde ese proxy.
- La certificación es **del control delimitado de paridad y de las regresiones dirigidas**. No afirma cobertura integral de todos los módulos ERP ni disponibilidad sin interrupciones.

## Snapshot y regresiones
- Snapshot positivo nuevo: `docs/baselines/P0-001-B-DEV-20261026-PARITY-VERIFIED.json` (contiene sólo metadatos públicos de proveedor, SHAs y enlaces de CI).
- `scripts/verify-p0-001-b-parity.mjs` admite el snapshot 2026 y conserva catorce mutaciones negativas y el corte antiguo `NO_GO`, sin reescribir los históricos.
- El workflow `.github/workflows/p0-001-b-head-parity.yml` comprueba ambos cortes. Un snapshot positivo no sustituye lecturas nativas de proveedores.

## Cierre protegido pendiente
1. CI exact-SHA **del PR final**, incluidos tres checks obligatorios y regresión P0-001.B.
2. Aprobación formal GitHub de revisor distinto al autor y a los contribuyentes del cambio.
3. Merge protegido hacia `dev`; verificar HEAD y ausencia de cambios ajenos al alcance.
4. Conciliar `PLAN_MAESTRO!H3:M3`, dejar `COLA!E3` por fórmula y agregar evento único `BITACORA` con readback; sólo entonces declarar `P0-001.B = LISTO`.

No hay autorización para convertir la mera aparición de este documento o de un commit en un cierre. Rollback por revert de PR protegido si se detecta regresión.
