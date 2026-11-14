# SOLQARYN V5 — C12-01.M01.04: riesgos, supuestos y decisiones pendientes de topología objetivo

**Unidad:** `C12-01.M01.04`, PLAN_MAESTRO fila 1733, C12-01 Topología objetivo, M01. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fuentes de autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG institucionales](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `PROJECT_SCOPE_LOCK=STRICT`. **Baseline causal de lectura:** `solqaryn/Solqaryn@dev` `55b1f5489189e8cc22ed2b66b5c9e5d72461ae2d`, 2026-11-14.

## Alcance, clasificación de evidencia y límites

Riesgos y decisiones de **preparación documental** para una topología futura que separe frontend Angular 22.2.1, API .NET 10, workers y MySQL, con red privada, secretos, backups, volúmenes, DNS/TLS y reversión. Se conservan como estado declarado los actuales Vercel, Render, Aiven y Cloudinary. Hostinger y Cloudflare son **destinos propuestos**, no contratos, cuentas conectadas, VPS creados ni infraestructura certificada. Ningún riesgo se considera mitigado por redactar la matriz. No se ejecuta migración, cambio DNS, reconfiguración MySQL, backup, pago, evento, webhook, build, prueba HTTP ni despliegue.

Antecedentes documentales verificados: `C12-01.M01.01` alcance/no-regresión (blob `808cb7d980c4c258a640cf5f78245b3dac78e421`), `C12-01.M01.02` actores/fronteras (blob `63a2698009d5d62499b4c81e160102a1db319310`), `C12-01.M01.03` inventario 14 fuentes/12 brechas (blob `d0e0bc12185d60f63a1e91f61bcd9a2c2db22884`). Son entradas, no autorizaciones de cutover.

## R01–R16 — matriz de riesgos causales abierta

| ID | Riesgo y evidencia del repositorio | Consecuencia a evitar | Mitigación futura / gate verificable |
| --- | --- | --- | --- |
| R01 | `render.yaml` declara `Database__ApplyMigrationsOnStartup=true` en DEV | Migración automática en instancia o base equivocada | Bloquear auto-migración de topología objetivo sin revisión EF, DB efímera y aprobación expresa |
| R02 | `EnvironmentDatabaseGuard.cs` fija host/puerto Aiven y usuario/base por ambiente | Desactivar guard o romper binding al migrar | Rediseñar binding conservando rechazo de host, user, DB, puerto y SSL inválidos; suites positivas/negativas |
| R03 | `RenderEnvironmentContractGuard.cs` exige 28 claves administradas | Servicios objetivo no arrancan o se ocultan faltantes | Contrato por entorno versionado, revisión de mínimos y secretos externos; no copiar valores |
| R04 | `frontend/vercel.json` redirige `/api/:path*` a backend-proxy | Rutas perdidas, bucle de proxy o SSR/SEO inconsistentes | Tests de rutas, querystrings, SEO, errores, headers y fallback con proxy futuro aislado |
| R05 | Contenedor .NET 10 expone 10000 y usa `USER $APP_UID` | Publicar API/puertos no previstos o perder privilegio mínimo | Test de usuario no-root, binding, puertos firewall y acceso interno; no exponer DB |
| R06 | No hay inventario certificado de cuentas/quota de proveedor objetivo | Gastos y dependencia de infraestructura inexistente | Autorización de cuenta y presupuesto, confirmación de acceso/roles antes de compra |
| R07 | No se acreditó topología privada entre API/workers/MySQL objetivo | Datos, credenciales o DB expuestos a Internet | Diseño deny-by-default con pruebas de rechazo de origen externo |
| R08 | Plan propone DNS/TLS/Cloudflare sin evidencia de cutover | Caída o suplantación por certificado/ruta defectuosa | Dry run controlado, validación TLS y rollback DNS con ventana aprobada |
| R09 | No consta backup/restore ensayado en MySQL objetivo | Pérdida irreversible o inconsistencia transaccional | Restore efímero, hashes y conteos, prueba de reversión sin tocar Aiven |
| R10 | Identidad tenant/JWT/RBAC atraviesa proxy y capas | Acceso cruzado empresa A/B o sesión falsificada | Pruebas negativas de cabecera/slug/tenant_id y verificación de autorización backend |
| R11 | Workers propuestos sin implementación/cola probada | Duplicación de cobros o operaciones sin idempotencia | Contrato de entrega única observable, reintentos acotados y DLQ futura |
| R12 | Cloudinary actual frente a R2 objetivo sin manifiesto de objetos | Referencias rotas y pérdida de medios | Inventario por hashes/propietario; no migrar imágenes inexistentes |
| R13 | No se acreditaron SLO, alertas y retención de logs de destino | Fallas silenciosas y PII filtrada en telemetría | Definir SLI/SLO, límites de retención y redacción, ensayos de alertas |
| R14 | Builds distintos entre Run actual y destino propuesto | Divergencia de dependencias e imagen insegura | Build reproducible, SBOM, CVE scan y comparativa de runtime/configuración |
| R15 | DEV/QA/PROD separados en manifiestos pero no auditados en destino | Fuga de secretos o datos entre ambientes | Revisar IAM por ambiente, usuarios DB y endpoints; negación cruzada antes de activar |
| R16 | Despliegue sin plan de retorno y tiempo máximo observable | Cutover irreversible y degradación de disponibilidad | ADR, runbook rollback verificado, propietarios, punto de no retorno y criterio abortar |

**Ningún R01–R16 está cerrado** por esta entrega. Cada riesgo representa un escenario potencial, no hallazgo confirmado en proveedor externo. Se mantienen controles de JWT, RBAC, aislamiento tenant y Scope Lock; no se altera seguridad para facilitar un despliegue hipotético.

## S01–S10 — supuestos falsables, no hechos acreditados

| ID | Supuesto por verificar | Falsación / fuente requerida |
| --- | --- | --- |
| S01 | El destino admitirá ejecución .NET 10 equivalente | Probar imagen real firmada, no inferir desde marketing |
| S02 | El destino dará red privada MySQL sin acceso público | Inspección de firewall/ACL y test negativo externo |
| S03 | Habrá capacidad persistente para backups/volúmenes | Documento de cuota y restore medido, sin cifras inventadas |
| S04 | La latencia API→DB será aceptable | Carga medible por ambiente/tenant, no estimación comercial |
| S05 | El proxy conservará auth, CORS, headers y rutas SEO | E2E positivo/negativo y rastreo de headers |
| S06 | Se podrán transportar secretos con mínimo privilegio | Revisión vault/KMS, expiración y rotación; nunca exportar credenciales |
| S07 | Workers podrán operar separadamente de API | Proof en CI efímera, sin eventos server-side ahora |
| S08 | RTO/RPO objetivo serán alcanzables | Ejercicios de falla/restore con observaciones, sin objetivos ficticios |
| S09 | Tenant A/B conservará aislamiento lógico en tránsito | Tests auth negativos, DB permisos y filtros de repository |
| S10 | La ruta de rollback preservará datos/URLs y archivos | Ensayo sin perder hashes, referencias ni operaciones confirmadas |

## D01–D10 — decisiones humanas/técnicas aún pendientes

| ID | Decisión requerida antes de la ejecución futura | Responsable / evidencia mínima |
| --- | --- | --- |
| D01 | Elegir proveedor/plan y confirmar autorización de gasto | Propietario; contrato/limitaciones y costo aprobado |
| D02 | Aprobar arquitectura de red y puertos privados | Arquitectura/seguridad; ADR y pruebas deny-by-default |
| D03 | Definir matriz de secretos, IAM y rotación por ambiente | Seguridad; esquema sin valores secretos y negativo |
| D04 | Aprobar manejo de migraciones EF y compatibilidad MySQL | DBA/backend; restore efímero, esquema y rollback |
| D05 | Resolver guard Render/Aiven sin degradar su aislamiento | Backend/seguridad; tests host/user/tenant maliciosos |
| D06 | Resolver proxy/routing Vercel y DNS/TLS de transición | Frontend/infra; E2E, certificados y reversión |
| D07 | Definir alcance real de workers/idempotencia o descartarlos | Backend; contratos y pruebas, sin habilitar dispatcher aquí |
| D08 | Definir Cloudinary→R2 solo para objetos existentes | Datos/archivos; inventario, permisos y hashes |
| D09 | Acordar observabilidad, cuotas y objetivos medibles | Operaciones; SLI/SLO/alertas y retención redactada |
| D10 | Dar GO/NO_GO de ejecución de cutover con rollback aprobado | Propietario y seguridad; gate formal en otro alcance |

## Verificaciones y cierre documental (futuro vs. ahora)

**A0:** Skill instalada/canónica coincide, seis Tasks Javier, origen GitHub Javier, Plan/Cola/DAG por ID y CAS; componentes afectados limitados a `docs/v5` y `CHANGELOG_AI.md`.

**A1 documental:** 16 riesgos distintos, 10 supuestos con prueba de falsación y 10 decisiones atribuibles. **Negativo:** no convertir R07 o S02 en prueba de red privada realizada.

**A2 seguridad documental:** rechazo explícito de copiar secretos, desactivar guards, confiar en cabeceras tenant, mover Aiven o exponer MySQL. **Negativo:** ninguna habilitación de IAM, firewall, DNS o prod sin autorización.

**A3 integración documental:** vínculos a evidencias Git del inventario M01.03 y predecesores M01.01/02, entre routing frontend, guards, Docker, base de datos y capas. Toda prueba real en proveedores se marca **NO_APLICA_EN_ESTA_ENTREGA/NO_EJECUTADA**.

**A4 publicación:** dos archivos Markdown por fast-forward y CAS directo a dev, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados success en SHA exacto; readback Git/PLAN/COLA/BITACORA. Builds runtime sin cambios son **NO_APLICA** para documento, nunca PASS ficticio.

**A5:** exclusivamente otra Task Javier con TASK_ID distinto. La salida documental no habilita M01.06, M03, migraciones, QA/main/PROD, DNS o servicios de pago. **Rollback:** nuevo commit `git revert` normal y trazable (no reset/force-push, no modificar BITACORA histórica).

**Resultado de esta unidad:** matriz de riesgos y decisiones lista para revisión, sin implantación ni aceptación de riesgo por terceros.
