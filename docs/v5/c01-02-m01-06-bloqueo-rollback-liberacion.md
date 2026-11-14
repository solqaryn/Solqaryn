# SOLQARYN V5 FINAL — C01-02.M01.06: bloqueos, rollback y salida de capabilities/RBAC

**Naturaleza:** contrato de salida **documental** para C01-02/M01; no certifica la matriz de capacidades ni crea autorización ejecutable. **Autor de esta unidad:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Destino:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14.

**Fuentes ejecutables únicas:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Nada de este documento añade filas, microtareas, permisos o fases fuera del plan.

## 1. Resultado requerido y frontera de ejecución

La fase C01-02 pretende una matriz de capabilities por **rubro, plan, rol, tenant, sucursal y recurso**, con denegación por defecto. **M01.06 solamente define los bloqueos, el rollback y las condiciones de liberación de la preparación M01**. No declara construida esa matriz, no registra nuevos grants, no integra facturación, no habilita un worker, webhook, dispatcher, rol de plataforma, endpoint o migración. Las decisiones de precedencia, expiración y revocación de capabilities siguen pendientes de M02.

**Autoridad de seguridad existente:** `ContextoTenantActual.DesdeMembresia` obtiene usuario/empresa/rol de membresía verificada; `PermisoService` usa roles y grants relacionales. Un entitlement de rubro/plan es una restricción adicional futura, **nunca** sustituto de grant ni de contexto tenant. `EsAdministrador`, headers, URL, IDs suministrados por el cliente o controles visuales Angular no son autoridad. Soporte plataforma DEV permanece únicamente en su allowlist de lectura.

## 2. Prerrequisitos específicos: no equivalen a habilitación automática

Al preflight de esta unidad, `PLAN_MAESTRO!H182:H186=LISTO` (cinco preparativos), `H151=LISTO` (C01-01.M01.06) y `H79=LISTO` (C00-03.M01.06). Estos son IDs exactos, no una autorización genérica por bloque o prefiltro de COLA. Los cinco preparativos disponen de revisión A5 independiente realizada por una de las seis Tasks Javier; para M01.05 se confirmó **A5 correctiva Javier DEV05** y la evidencia histórica de una Task retirada no se utilizó como permiso. Los dos terminales antecedentes conservan su A5 independiente. Verificar siempre de nuevo estos estados antes de cerrar.

| ID previo | Artefacto o contrato identificado en `dev` | SHA de blob verificado |
| --- | --- | --- |
| C01-02.M01.01 | `docs/v5/c01-02-m01-01-alcance-capabilities.md` | `5994e473830d93822e8d21c0ac649726085ae333` |
| C01-02.M01.02 | `docs/v5/c01-02-m01-02-actores-superficies.md` | `e40392d6c14307ecf794e97923dbbea2c3ccb69b` |
| C01-02.M01.03 | `docs/v5/c01-02-m01-03-fuentes-contratos-evidencias.md` | `fc18a7f3c2a014c7ae914f9ca8d615d79231f754` |
| C01-02.M01.04 | `docs/v5/c01-02-m01-04-riesgos-supuestos-decisiones.md` | `17fddc06684ea6e121569cb70fed1f77d1137dc4` |
| C01-02.M01.05 | `docs/v5/c01-02-m01-05-aceptacion-capabilities.md` | `7dd7109c2297f2292aa8e22bc2b9ccdd4ba0aefb` |
| C01-01.M01.06 | Contrato de identidad universal, PLAN y A5 independiente Javier | `009d412176429266eee6574ac632bca333f3b0b2` (commit revisado) |
| C00-03.M01.06 | Contrato transversal, PLAN y A5 independiente Javier | `89a9f0c681d163a8ca617523075249af9fb8a2c6` (commit revisado) |

El `DAG_TECNICO` de **C01-02.M01.06** exige precisamente esos siete anteriores: `F=VALIDADO, G=SI`. Antes del claim, `PLAN!H187=PENDIENTE`, `I187` vacío y `COLA!G187/O187=SI`. El claim exclusivo deja `H187=EN_PROGRESO`, `I187` con el TASK_ID propio y `O187=NO` por fórmula. Los cambios de estado y la aceptación posterior requieren readback de PLAN/COLA, BITACORA con 26 campos y Scope Lock del SHA exacto.

## 3. Registro de bloqueos y retorno seguro

| Gate | Condición de aceptación para este contrato | NO_GO y rollback |
| --- | --- | --- |
| Autoría, concurrencia | Solo una Task Javier en PLAN I; revalidar HEAD, owner y archivo antes de editar | Owner distinto, carrera CAS o cambio de HEAD: no escribir encima; reconciliar o retirar reclamo de forma auditable |
| Tenant/identidad | Membresía activa, empresa coincidente, rol vigente, ownership de recurso verificable | Tenant A solicita B, sucursal ajena, membresía revocada o `EmpresaId` incierto: DENY sin revelar información |
| RBAC y capabilities | Grant relacional explícito **y** restricciones de rubro/plan satisfechas cuando exista su modelo | Plan sin grant, administrador sin grant, soporte fuera de allowlist o grant sin entitlement: DENY |
| Contratos pendientes M02 | Decidir precedencia rubro/plan/rol/sucursal/recurso, vigencia, caché, revocación y errores seguros | No afirmar ALLOW operativo ni liberar M03, pagos o migraciones con meras definiciones M01 |
| Calidad técnica | A0 documental, A1 oráculos positivos/negativos, A2 fronteras de seguridad, A3 integración con fuentes, A4 diff/commit fast-forward + checks exact-SHA + readback | SKIPPED/CANCELLED/FAIL/IN_PROGRESS de prueba **requerida** no son PASS. Compilaciones ajenas al diff Markdown = NO_APLICA motivado |
| Revisión independiente | A5 posterior por TASK_ID Javier distinto del autor, y autorizaciones separadas para promociones | No autoaprobar A5 ni trasladar a QA/main/PROD |
| Datos y entornos | Contrato estático sin modificación runtime ni datos | Prohibido cambiar grants, JWT, MySQL productivo, DNS, Cloudflare, Hostinger, archivos, licencias o cobros sin autorización nueva |

**Rollback de este changeset documental:** `git revert` ordinario condicionado a HEAD y consumidores; conservar la sección CHANGELOG y la BITACORA histórica mediante evento compensatorio cuando aplique, nunca editar eventos anteriores ni force-push. Un revert Git **no** deshace datos reales; esta entrega no los modifica.

## 4. Oráculos concretos de aceptación documental

| Caso | Signo | Resultado verificable |
| --- | --- | --- |
| P01 | Positivo | Los cinco preparativos M01.01–05 y los dos terminales externos están LISTO con revisión Javier autorizada, el DAG del ID exacto dice VALIDADO/SI y el owner es único |
| P02 | Positivo | La salida M01 describe restricciones acumulativas y criterios de reversión sin añadir autoridad paralela al RBAC |
| P03 | Positivo | Publicación exclusivamente documental fast-forward, Scope Lock y CHANGELOG V5 SUCCESS del SHA publicado, PLAN/COLA/BITACORA re-leídos; A5_PENDIENTE independiente |
| N01 | Negativo | Algún predecesor no LISTO, A5 inválida o DAG sin gate: NO_GO del cierre |
| N02 | Negativo | Plan pagado sin grant, `EsAdministrador` sin grant o rol sistema: DENY |
| N03 | Negativo | Tenant A consulta B, recurso/sucursal sin ownership, membresía revocada o legacy sin empresa verificable: DENY |
| N04 | Negativo | Token, header o UI manipulado como fuente de permiso: DENY |
| N05 | Negativo | CI requerida de otro SHA, SKIPPED, CANCELLED, FAIL o IN_PROGRESS: NO_PASS |
| N06 | Negativo | Revisión A5 propia, evento sin 26 celdas, doble writer, secretos en Git o promoción no autorizada: NO_GO |
| N07 | Negativo | Documento M01 se presenta como endpoints/matriz/DB/JWT/entitlements/worker ya implementados: NO_GO de certificación runtime |

**Alcance de pruebas de esta unidad:** `dotnet=NO_APLICA`, `Angular/Node runtime=NO_APLICA`, `MySQL/EF/Docker=NO_APLICA`, `Playwright=NO_APLICA` cuando el diff consta únicamente de este Markdown y `CHANGELOG_AI.md`. Requiere revisión de seguridad **documental** y gates GitHub Scope Lock + procedencia CHANGELOG sobre el SHA publicado. Ningún oráculo de este documento es ejecución de HTTP/JWT/MySQL en vivo.

## 5. Salida limitada de M01 y continuidad

Este contrato habilita **solo la evaluación individual** de los IDs posteriores M02 con lectura actual de PLAN H/I, COLA G/O, DAG F/G, dependencias, A5 causal, permisos, tooling y exclusión de archivos. No libera automáticamente M02/M03, C01-01.M06.06, C15-05.M06.06, workers/eventos, ni QA/main/PROD. Si hay A5_FAIL causal histórico, reparar y revisar independientemente antes de habilitar el scope afectado. La futura implementación real de matriz, persistencia, autorización server-side y aislamiento A/B requiere su propio diseño, pruebas .NET/API/MySQL/Angular/E2E proporcionales y rollback.
