# C15-02.M06.01 — Auditoria causal de pruebas reales frente a mocks (V22)

## Contrato del PLAN D, estado y riesgo

**Accion exclusiva:** «Revisar que las pruebas correspondan al alcance real y no a mocks insuficientes.» PLAN_MAESTRO fila 2228, `C15-02.M06.01`. El resultado es una matriz **auditada de fuentes, pruebas ejecutadas y limitaciones abiertas**, no una certificacion global de integraciones Hostinger/Cloudflare/R2/MySQL productivo/DNS ni una autorizacion de despliegue.

- Fecha de lectura UTC: 2026-11-20T02:20:24.291Z; HEAD de referencia `90e89e2801e6b86c4da8371d5fc005cb34d840b5` en `solqaryn/Solqaryn@dev`.
- Autor de esta auditoria: JAVIER DEV05, `6adc2e55f47c81919f6374e561687076`; A5 independiente debe hacerla otra Task Javier.
- Riesgo del **diff**: R0 documental; riesgo de las unidades auditadas: R1/R2, sin reducir controles JWT/RBAC/tenant ni riesgo R3 de recursos reales.
- Predecessores M05.01–M05.06: **6/6 PLAN H=LISTO con A5_PASS en K** (no se suprime ningun FAIL historico).

## A0 — Trazabilidad por microtarea

| Unidad | PLAN H / K | Accion autorizada por PLAN D |
|---|---|---|
| `C15-02.M05.01` | LISTO + A5_PASS K | `Ejecutar prueba unitaria o de regla con resultado reproducible.` |
| `C15-02.M05.02` | LISTO + A5_PASS K | `Ejecutar contrato API/DTO y validar errores seguros.` |
| `C15-02.M05.03` | LISTO + A5_PASS K | `Ejecutar integración de persistencia o proveedor cuando corresponda.` |
| `C15-02.M05.04` | LISTO + A5_PASS K | `Ejecutar caso positivo de aceptación del alcance.` |
| `C15-02.M05.05` | LISTO + A5_PASS K | `Ejecutar caso negativo de seguridad, aislamiento o abuso.` |
| `C15-02.M05.06` | LISTO + A5_PASS K | `Ejecutar regresión, rendimiento y accesibilidad proporcionales.` |

## A1/A2/A3 — Clasificacion causal por pruebas y alcance verdadero

| ID / SHA de entrega | Test dirigido ejecutado (GitHub-hosted) | Real frente a simulado, alcance demostrado | Evidencia **NO** demostrada / gate independiente |
|---|---|---|---|
| `M05.01` `a2dd7af7d6f1a437d1d886ad4596a4e47720d9c0` | Angular Vitest **4/4 PASS** en [job 136014234950](https://github.com/solqaryn/Solqaryn/actions/runs/44653594400/job/136014234950) | Pruebas unitarias ejecutadas, asserts de reglas de componente; fixture local **no es proveedor real** | No JWT firmado, HTTP backend, MySQL ni servicios externos; check global `release` y `Modulos...` **FAIL** en SHA (no PASS para etapa global) |
| `M05.02` `01e4131f03ffed3887ace004738c9018e2314211` | Angular Vitest de contrato HTTP cliente **6/6 PASS** en [job 136018264999](https://github.com/solqaryn/Solqaryn/actions/runs/44654801182/job/136018264999) | `HttpTestingController` comprueba DTO/errores de cliente, **no hace solicitud a servidor live** | Check V5 **FAIL** en SHA original; corregido con commit descendiente `ec765931f852fbb7e8f24e5761dd7e6b274b8540` con V5 SUCCESS sin cambiar fuentes de prueba; JAMAS contar el FAIL original como PASS |
| `M05.03` `b9466fc06b4c2c882948fe12dae0ccdaf46c70b8` | **6/6 xUnit PASS** con Docker `mysql:8.4`, Oracle EF Core, Category Integration, [job 136021869603](https://github.com/solqaryn/Solqaryn/actions/runs/44655887915/job/136021869603) | Base **efimera real** en runner, verificado aislamiento tenant A/B, IDOR, rollback/cancelacion; modelo EF de pruebas **aislado**, no AppDbContext live | Gate **global** de migraciones EF en mismo SHA **FAIL**, [job 136021980847](https://github.com/solqaryn/Solqaryn/actions/runs/44655888043/job/136021980847); no certificar migraciones globales ni Aiven real |
| `M05.04` `8865c6c483157e67598d77436d2df58584ca7865` | **2/2 Playwright Chromium PASS** Angular local en [job 136016023781](https://github.com/solqaryn/Solqaryn/actions/runs/44654113784/job/136016023781) | Navegador real + sesión/respuestas locales simuladas; valida UI de arquitectura y navegacion | No JWT firmado en API ni autenticidad de proveedor; check global `Modulos...` **FAIL** no cuenta PASS; migracion global **CANCELLED** tampoco |
| `M05.05` `ae7d06e00db9aa647792911f2648833af61775e5` | **5/5 Playwright Chromium PASS** con API real aislada y firma JWT, tenant A/B, 401/403, [job 136018083744](https://github.com/solqaryn/Solqaryn/actions/runs/44654745490/job/136018083744) | Prueba de seguridad HTTP/JWT + aislamiento A/B ejecutada en **laboratorio efimero**. No simulacion simple de token UI | No proveedores externos ni QA/PROD; `Modulos...` **FAIL** global pendiente en SHA, no confundir green dirigido con green universal |
| `M05.06` `2ca545eaa8eb9a75e0b909fff0f6404281dcc6ab` | **3/3 Playwright Chromium PASS**, 320/375/768/1280, [job 136028793145](https://github.com/solqaryn/Solqaryn/actions/runs/44657965081/job/136028793145) | Browser real en loopback, `403` **sintetico**, 5 tarjetas `NO_VERIFICADO`, foco Tab/Enter y cero peticiones mutantes; DCL/FCP **laboratorio** | No JWT firmado/DB/provider en esta prueba; LCP/CLS/INP de campo `NO_MEDIDO`; migraciones globales `IN_PROGRESS` al corte, no PASS |

**Regla positiva:** al menos una prueba real ejecutada y pertinente al diff particular, con `completed/success` en SHA causal, acredita **solo** el requisito que realmente asertó. **Regla negativa:** un fixture (`HttpTestingController`, sesión `localStorage`, DB model sintético) o gate `failed/cancelled/skipped/in_progress` **no** acredita integración productiva. La matriz aplica estas reglas a los seis casos y conserva los errores no mitigados como pendientes. Un PASS dirigido no reemplaza un gate global fallido ni un A5 de otro SHA.

## Límites, decisiones y siguiente gate

- Se verificaron commits, diffs, checks y logs de los seis orígenes; el registro de BITACORA/PLAN conserva authors/A5 originales. La fuente `M05.02` tuvo correccion V5 posterior; usar siempre evidencia del SHA probado y su descendiente con blobs identicos, nunca reinterpretar fallo histórico.
- Los proveedores Hostinger, Cloudflare, R2, DNS, dominios, credenciales, permisos de pago, continuidad y backup prod permanecen **NO_CERTIFICADOS/NO_AUTORIZADOS**. Se requiere alcance y permisos concretos antes de pruebas R3; no leer/escribir secretos.
- Los fallos globales de `release`, `Modulos...` y migraciones deben investigarse por sus dueños y SHA concreto. No elevar ninguno a PASS por esta revision R0 ni bloquear pruebas dirigidas no relacionadas si no son gates causales.
- M06.02 debera registrar diff/artefactos reproducibles; M06.03 riesgos; M06.04 rollback seguro; M06.05 solicitud de A5; M06.06 cierre terminal solo cuando pruebas y dependencias reales lo permitan. No se liberan por esta acta automaticamente: dejar formulas DAG/COLA intactas.

## A4 — Publicacion, CAS, CI, reversibilidad

Preclaim verificado: `PLAN!H2228=PENDIENTE`, `I2228` libre, `COLA!G2228=SI/O2228=SI`, `DAG_TECNICO!A2189=C15-02.M06.01/F2189=VALIDADO/G2189=SI`. Se obtuvo `SQV17_C15_02_M06_01` (`namedRangeId=2000002228`, H:I fila 2228), CAS H + owner unico y readback antes de tocar artefactos. Tras publicar solo DOC+CHANGELOG en dev, exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed/success exact SHA**, BITACORA evento unico, CAS VALIDANDO→LISTO y readback. A5 otra Task distinta, sin autoaprobar.

Rollback del doc: **git revert** normal, sin reset, force-push, reescribir historia ni borrar BITACORA. Correccion de PLAN solo mediante transicion V17/CAS, no forzar `DAG!G` ni `COLA!G/O`. Cualquier CI obligatoria no `completed/success` impide cerrar esa tarea.

**Cierre de criterio D2228:** clasificacion verdadera y reproducible de six casos, distinguiendo pruebas reales de mocks, detectando fallos globales y sin promover a certificaciones R2/R3 que no existen. No QA/main/PROD.
