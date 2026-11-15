# SOLQARYN V5 — C01-01.M06.03: límites, riesgos y decisiones de identidad y membresía

**Autor de esta unidad:** PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Ámbito:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** Standard de Implementación de Automatizaciones V5 FINAL, PLAN_MAESTRO, COLA, DAG_TECNICO y GitHub Actions de los SHA indicados. **Tipo:** auditoría documental, no cambio de autenticación, datos ni despliegue.

## Precedencia y objeto de auditoría

El contrato exige C01-01.M05.01–06 en LISTO con A5 independiente y C00-03.M06.06 LISTO+A5. En el preflight se constató PLAN H178=PENDIENTE, I178 vacío, DAG F=VALIDADO/G=SI y COLA G/O=SI; se reclamó exclusivamente C01-01.M06.03 con CAS de H178 y readback I178. M06.01 y M06.02 son unidades documentales distintas, con otros autores y archivos propios; ninguna se toma como un PASS de pruebas runtime. El objetivo de esta nota es describir riesgos y decisiones sin falsificar integraciones o niveles de servicio.

| Alcance previo | Prueba y evidencia de origen inspeccionadas | Frontera que NO se acredita |
| --- | --- | --- |
| M05.01, 29f0b9b9001a595db9f3bfc8b2ac99e9e6d89ce9 | Reglas de dominio IdentidadUniversalReglasTests, tenant A/B, rol legacy, membresía revocada e ID inválido; blob e62ed4ef7c1fe4b39f27c43bb4853eeba32a2d17 | JWT firmado, HTTP y persistencia |
| M05.02, 7a1c78a733d1de1e72c7433f4558903019e1f96f | Cinco casos de contrato TenantContextController reales con ScopeStub y errores/DTO seguros; blob 018abf7b57185b98e7fb0d8b5cbb65935eeb37bc | Middleware, DI HTTP real y carga EF |
| M05.03, 11c6e30b4512b47e6b22f8a17c9fd03bbebac883 | Integration MySQL 8.4 Oracle EF, dos empresas, roles, membresías y revocación persistidos; test blob 8d9bc82d42ffcab202f46ad3c501564cd71b5980. Job 132816757016 ejecutó Category=Integration y reportó **47 PASS, 0 FAIL, 0 SKIPPED en SHA fuente exacto** | MySQL remoto, JWT API end-to-end, aislamiento de cada tabla y datos de producción |
| M05.04, 8f005c593a4b76e62cffc88ea3f6215bfbf832f0 | Tres xUnit positivos Application/Domain, identidades A/B, sucursal y relectura por solicitud; blob 39fcec70e590434ba6574164669aecd901f4db04 | Fuente de snapshots real, estado compartido distribuido |
| M05.05, 71eac84d8eee2e23731d0504ac4d371fb85b1c7b | Seis xUnit negativos Application, empresa B, usuario ajeno, revocación, rol antiguo y sucursal B; blob c512771ee2580c861b1888c24fd3d03665ca7701 | Penetración de endpoint, autenticación JWT, filtro EF global |
| M05.06, 38a7798cddcec6198ed8125246cc7c97347bd897 | Cinco xUnit de regresión/relecturas y dos Chromium de foco y ruta privada anónima; browser en fuente, backend en descendiente 4b0e1ac3c51a9bf5f35bfe076cf5ec2154c3abdb con blobs conservados | Benchmark p95, auditoría WCAG integral, navegador con login JWT real |

**Corrección de causalidad crítica:** el SHA 71eac84d8eee2e23731d0504ac4d371fb85b1c7b que figuraba como CI de M05.03 es ANCESTRO de 11c6e30 y no contiene ese test MySQL nuevo. No puede usarse para probar M05.03. La ejecución genuina de Category=Integration se verificó en el propio SHA 11c6e30: 47 aprobadas. No presentar el registro histórico de 46 pruebas en el ancestro como certificación del nuevo test. Los checks CANCELLED/SKIPPED tampoco cuentan PASS.

## Registro concreto de riesgos abiertos y decisiones

| Riesgo | Brecha real o fallo posible | Decisión de mitigación sin bypass |
| --- | --- | --- |
| R01 | ScopeStub del controlador sustituye autoridad de membresías | Probar middleware real JWT, servicio de membresías y rechazo HTTP 401/403 en microtarea autorizada |
| R02 | Contextos A/B probados con snapshots en memoria | Repetir integración negativa de filtros EF, API y propiedad persistida al introducir cambios de Infrastructure |
| R03 | Token/sesión revocados después de crear contexto | Revalidar permisos de servidor en cada petición sensible; la UI no concede roles ni reutiliza claims cliente como verdad |
| R04 | MySQL local efímero no representa Aiven vivo ni todas las tablas | No inferir conexión remota ni migración; requerir schema/fixture controlado y proveedor real únicamente en ambiente autorizado |
| R05 | Controlador unitario no comprueba fugas de mensajes HTTP | Añadir pruebas API TestServer con rutas reales, JWT y errores neutros antes de una certificación end-to-end |
| R06 | Conteo 128 lecturas es estructural, no latencia | Medir p50/p95, CPU, bytes y concurrentes con fuente, SHA y dispositivo; no declarar mejora de rendimiento por conteo |
| R07 | Dos Playwright Chromium no demuestran WCAG completa | Ampliar teclado, foco, lector, reducido movimiento, responsive y errores en escenarios propios con artefactos |
| R08 | Resultados CI de SHA ancestro se atribuyen a nuevo archivo | Inspeccionar árbol, cronología y blob de test; usar únicamente SUCCESS en SHA que contenga el test y job con filtro real |
| R09 | Pushing concurrente puede cancelar trabajos de seguridad | Aplicar CI_CANCEL_GUARD por github.ref; preservar VALIDANDO hasta SUCCESS causal, evitar pushes que cancelen checks |
| R10 | El estado LISTO puede confundirse con A5 o autorización de release | Exigir revisión por distinta Task Javier, evidencia independiente y recálculo DAG; nunca QA/main/PROD por LISTO |
| R11 | Roles legacy, usuario, sucursal y EmpresaId de cliente pueden escalar permisos | Mantener resolución de actor, tenant, membership, rol y propietario desde backend persistido; denegar B→A y A→B |
| R12 | Manifiesto desactualizado puede confundir fuente, blob y evidencia | Versionar manifest y hashes, conservar BITACORA append-only; revalidar tras cada cambio de dependencias o CI |

## Decisiones y restricciones

- D01: los tests de identidad y pertenencia actuales constituyen evidencia válida limitada a sus escenarios y tecnologías; **no** hay certificación de JWT/HTTP extremo a extremo, filtros globales EF en cada tabla o auditoría de seguridad productiva.
- D02: registrar origen exacto y blobs separados de los SHA de CI. Cuando el test MySQL fue introducido después del supuesto SHA verificado, emplear su propio job Category=Integration 47 PASS, no reciclar 46 PASS históricos.
- D03: conservar las puertas de validación y revisión independiente A5. Documentación M06.03 no libera M06.06 ni cambia fórmula DAG G/O.
- D04: requerir riesgo aceptado y evidencia adicional de API/HTTP real, pruebas integradas A/B y análisis de revocación antes de cualquier autorización de promoción.
- D05: no crear workers, webhooks, schedulers, nuevas Tasks, migraciones, credenciales ni endpoints por esta auditoría.
- D06: documentar límites de rollback: git revert normal de este doc/changelog, preserving other commits and BITACORA. No existe base real que restaurar por esta unidad.

## Comprobación institucional A0–A5

A0: revisión estática de alcance, archivos, dependencias, auth/tenant, HEAD, exclusividad y A5 de predecesores. A1: inspección de código positivo/negativo y correspondencia de escenarios; nuevos tests funcionales NO_APLICA al diff documental. A2: matriz de riesgos de aislamiento A/B, revocación, JWT/RBAC, DTO/errores y datos; sin afirmar prueba live nueva. A3: integración documental de versiones, test local MySQL y Chromium **históricos** con evidencias de origen; no se ejecutan estos runners por modificar Markdown. A4: únicamente se cierra si el commit documental dev fast-forward/CAS conserva CHANGELOG autocontenido y Scope Lock más control V5 procedencia completed/success en el SHA publicado, con BITACORA, PLAN y COLA readbacks. A5: revisión futura por otra Task Javier; ninguna autoaprobación.

Este documento es individual y no instala funcionalidad nueva. Cualquier cambio posterior en Domain, API, EF, JWT/RBAC, browser o la configuración de pruebas exige evaluación causal nueva. Rollback: git revert ordinario solo sobre el commit de esta unidad, sin force-push ni alterar datos o historial institucional.
