# SOLQARYN V5 FINAL — C14-02.M01.04: riesgos, supuestos y decisiones de aceptación de seguridad

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** evaluación documental preliminar sin explotación comprobada ni pruebas runtime ejecutadas.

**Fuentes de autoridad:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.01 alcance](./c14-02-m01-01-alcance-no-regresion-aceptacion-seguridad.md), [M01.02 interfaces](./c14-02-m01-02-actores-interfaces-fronteras-seguridad.md), [M01.03 inventario](./c14-02-m01-03-inventario-codigo-evidencia-aceptacion-seguridad.md). Preflight: PLAN fila 2057 PENDIENTE sin owner, COLA G/O=SI y DAG_TECNICO fila 2018 F=VALIDADO/G=SI; claim exclusivo y readback. El DAG habilita este documento, no libera fase terminal M01.06 ni QA/main/PROD.

## 1. Semántica de riesgo y alcance

`RIESGO_POTENCIAL` es una hipótesis de daño a comprobar, **no** un hallazgo explotable. `SUPUESTO_NO_VERIFICADO` necesita evidencia antes de guiar cambios. `DECISION_ABIERTA` es una cuestión de diseño/aceptación pendiente, **no** autorización para implementar. La aceptación final del lote exige **cero P0/P1 sin mitigación aprobada**, pero la presente preparación no mide ni certifica ese requisito. Ningún riesgo se rotula P0/P1 efectivo sin reproducibilidad, impacto, severidad y revisión independiente.

En el código inspeccionado ya existen guardias JWT/RBAC/tenant y tests específicos; por ejemplo `TenantPermissionContext.cs` (blob `df7d49f3e185c538f3b3dafabb36c3a971a7257d`), `IdentidadUniversalAuthorizationGuard.cs` (`5c7a256ea9709f6bf4279f2ff89addf599e3a311`), `UnscopedCommercialTenantGuardMiddleware.cs` (`558dda1eb5be729b2bd9a6d9fefbccd5db94addc`) y `CommercialTenantMySqlIsolationIntegrationTests.cs` (`e1a41bf39e3a7161d921cdfc50b7c76c0d173a42`). Esa evidencia demuestra **existencia de implementación y archivos de prueba**, no ausencia universal de defectos, CI vigente aprobada o despliegue validado. `M01.03` identifica límites adicionales.

## 2. Riesgos potenciales R01–R16 y pruebas negativas futuras

| ID | Riesgo potencial / condición de fallo | Oráculo de comprobación futura; estado actual |
| --- | --- | --- |
| R01 | BOLA/IDOR por ID de otro tenant en ruta o query | A/B HTTP acceso permitido propio y rechazado ajeno; NO_EJECUTADO |
| R02 | Suplantación del tenant por header/cuerpo/host o proxy | Fuente confiable validada, spoofing no deriva propiedad; NO_EJECUTADO |
| R03 | Permiso obsoleto por JWT, rol o membresía revocada | Mutación denegada tras revocación, con snapshot vigente; NO_EJECUTADO |
| R04 | Acceso de sucursal A1 a recurso A2 sin membresía | Casos de sucursal/empresa con permiso exacto; NO_EJECUTADO |
| R05 | Endpoint sin gate backend aunque Angular oculte opción | Inventario endpoint→atributo→servicio y HTTP adversarial; NO_EJECUTADO |
| R06 | `IgnoreQueryFilters` sin tenant autorizado en alguna consulta | Barrido y pruebas SQL/MySQL efímero por llamador; NO_EJECUTADO |
| R07 | Propiedad cruzada en transacción/venta/inventario | Intentos de escritura A→B con rollback verificable; NO_EJECUTADO |
| R08 | Descarga, URL firmada o carga de archivo ajeno | Owner, TTL, tipo, tamaño y replay negativos; NO_EJECUTADO |
| R09 | PDF/exportación con PII o datos contables ajenos | Comparación export A vs B con fixture; NO_EJECUTADO |
| R10 | Job/cola con identidad caduca, payload adulterado o replay | Verificación de tenant/idempotencia en ejecución cuando aplique; NO_EJECUTADO |
| R11 | Caché compartida sin clave de tenant/permiso | A→B y revocación no dejan datos anteriores; NO_EJECUTADO |
| R12 | Restauración/backup contaminando tenant o exponiendo claves | Restauración sandbox aislada y secret scan; NO_EJECUTADO |
| R13 | Error, traza o auditoría con JWT, PII o URL sensible | Respuesta/trace redactado bajo error inducido; NO_EJECUTADO |
| R14 | Host/forwarded headers ambiguos aceptados al publicar tienda | Host exacto o StoreKey autorizado; malicioso rechazado; NO_EJECUTADO |
| R15 | Fallo o timeout de proveedor externo degrada autorización | Fail-closed y reintento sin exposición ni doble efecto; NO_EJECUTADO |
| R16 | CI o PASS documental confundido con cierre P0/P1 o autorización de release | Check causal completed/success exact-SHA, revisión A5 y permiso por entorno; NO_EJECUTADO |

**Clasificación:** R01–R16 son **potenciales y sujetos a confirmación**. No se declara que el código incurra en esos defectos, ni que el lote esté seguro por contener tests. Si una futura prueba reproduce el defecto, registrar SHA, fixture, severidad e impacto y bloquear únicamente el scope causal sin editar historia.

## 3. Supuestos falsables S01–S10, todos NO_VERIFICADOS

| ID | Supuesto que necesita evidencia | Cómo falsarlo o validarlo |
| --- | --- | --- |
| S01 | Toda API privada aplica autorización backend correcta | Enumerar controllers/actions y hacer negativas sin UI |
| S02 | El tenant efectivo siempre deriva de identidad/membresía vigente | Spoof `X-Empresa-Id`, body, host y sesión de B |
| S03 | No quedan lecturas multitenant con filtros omitidos | Analizar `IgnoreQueryFilters` y consultas EF/SQL |
| S04 | Roles revocados bloquean inmediatamente acciones sensibles | Fixture con renovación/revocación y HTTP |
| S05 | Objetos Cloudinary y exportaciones están siempre aislados | Inspección proveedor mock, propietario y URL expirada |
| S06 | Todo job existente preserva tenant e idempotencia | Inventario de productores/consumidores reales y replay |
| S07 | Cachés/tokens no transportan información A→B | Cambio activo de tenant/rol y purga de caché |
| S08 | Backups, restores y logs son redactados y gobernados | Política real + restauración/escaneo en entorno permitido |
| S09 | Los tests en repo corresponden a CI efectiva del mismo SHA | Check-runs exact-SHA, logs y fixtures efímeros |
| S10 | Cero P0/P1 están abiertos sin mitigación aprobada | Registro institucional y revisión independiente, no inferencia |

El test xUnit en archivo, incluso `[Trait("Category","Integration")]`, es evidencia de **intención y código de prueba**, no de ejecución. Los checks `cancelled`, `skipped`, `queued`, `in_progress` o `failure` no se califican PASS cuando son obligatorios.

## 4. Decisiones abiertas D01–D10, sin autorización implícita

| ID | Decisión futura y autoridad requerida | Gate no eludible |
| --- | --- | --- |
| D01 | Determinar matriz única endpoint→rol→permiso→tenant/sucursal | Validación backend, no menú Angular |
| D02 | Formalizar origen verificable de contexto tenant/host/proxy | Pruebas spoof, configuración de infraestructura autorizada |
| D03 | Definir cobertura de refresh/revocación y rol vigente | Prueba de pérdida de privilegio exact-SHA |
| D04 | Acordar cobertura de queries que eluden filtros | MySQL CI efímero, tenant A/B positivo y negativo |
| D05 | Definir alcance de validación de archivos y URLs | Datos sintéticos y sin proveedores productivos |
| D06 | Separar inventario de jobs existentes de propuestas de eventos | **NO_IMPLEMENTAR_EVENTOS** ni crear workers/webhooks |
| D07 | Establecer partición e invalidación de caché | Prueba de contención A/B sin introducir tecnología nueva por defecto |
| D08 | Seleccionar fixture y permiso de restauración/backup | Prohibido acceso a backups productivos sin autorización |
| D09 | Establecer formato de evidencia sanitizada/severidad P0/P1 | Autor y reviewer distintos, nunca secretos en logs |
| D10 | Determinar condiciones formales para C14-02.M01.06 y siguientes | PLAN/COLA/DAG vivos, A0–A4/A5, CI y permiso QA/PROD separado |

Ninguna decisión se aprueba por el hecho de documentarla. Un NO_GO en una superficie no autoriza a omitir pruebas ni despliegue por otra vía.

## 5. Aceptación documental, efecto y reversión

**A0:** verificar R01–R16, S01–S10 y D01–D10, referencias correctas, no secretos ni claims exagerados. **A1:** revisar correspondencia entre riesgos y oráculos positivos/negativos futuros, sin afirmar que se ejecutaron. **A2:** no inferir seguridad efectiva de JWT/RBAC/tenant; preservar fail-closed y propiedad. **A3:** correlación con tres entregas M01 anteriores, blobs reales y V5. **A4:** diff de únicamente este documento y `CHANGELOG_AI.md`, fast-forward/CAS, GitHub Scope Lock y verificador V5 en `completed/success` para SHA exacto, readback PLAN/COLA/BITACORA; CI runtime irrelevante sólo `NO_APLICA` con causa, nunca PASS inventado.

**A5 pendiente:** Task Javier diferente de la autora y lectura del documento SHA original. No se modifica frontend, backend, MySQL, JWT/RBAC, roles, migraciones, secrets, hosts, DNS, Cloudflare/R2, pagos, worker/dispatcher/eventos, QA/main/PROD, ni se crean Tasks/PR/branches. Rollback del documento y entrada de CHANGELOG por `git revert` normal tras verificar HEAD actual. BITACORA histórica permanece intacta.
