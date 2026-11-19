# C14-02.M06.01 — auditoría de realidad de pruebas frente a dobles (V22)

PLATFORM=SOLQARYN | REPOSITORY=solqaryn/Solqaryn | BRANCH=dev | PROJECT_SCOPE_LOCK=STRICT | MICROTAREA_ID=C14-02.M06.01 | AUTHOR_TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a | RISK=R0_DOCUMENTAL | A5=PENDIENTE_OTRA_TASK_JAVIER

## A0 — alcance atómico, dependencias y arquitectura

La acción **exacta** de `PLAN_MAESTRO!D2084` es «Revisar que las pruebas correspondan al alcance real y no a mocks insuficientes». La columna E2084 contiene el objetivo acumulativo C14-02 (BOLA/IDOR, host, archivos, jobs, caché, backup y sesiones, cero P0/P1 no mitigados); **no autoriza convertir una auditoría documental de seis pruebas en un certificado global de seguridad**, ni equivale a evidencia R3 de producción. Este diff agrega exclusivamente este manifiesto y una sección aditiva y autocontenida al CHANGELOG; no cambia Angular, .NET, MySQL, JWT/RBAC, endpoints, migraciones, proveedores, checks, prompts ni fórmulas DAG/COLA. Los consumidores son las demás M06.02–06, cuyos gates se evalúan por ID.

Antes del claim se verificó `PLAN H2084=PENDIENTE`, `I2084=vacio`, `COLA G/O=SI/SI`, `DAG F/G=VALIDADO/SI`; las seis C14-02.M05.01–06 estaban **LISTO** con evidencia **A5 independiente** en PLAN/BITACORA. Se aplicó el namedRange V17 `SQV17_C14_02_M06_01`, ID `2000002084` y CAS de una celda H, conservando columna I como atribución del autor de la unidad nueva. No se alteran owners históricos.

## A1 — seis SHA fuente, pruebas verdaderas y límites falsables

| Microtarea y fuente | Lo que la evidencia demuestra | Lo que **NO** demuestra |
| --- | --- | --- |
| M05.01 — [`6f44a0df`](https://github.com/solqaryn/Solqaryn/commit/6f44a0df7265e401f127dfc83632def0a06afcf6) | Angular TestBed/Vitest 5/5, sesión cliente y denegación/revocación bajo fixtures; A5 DEV06 sobre autor DEV04 | **No** JWT firmado servidor, multitenancy persistente, HTTP real ni MySQL |
| M05.02 — [`2cc9ef8f`](https://github.com/solqaryn/Solqaryn/commit/2cc9ef8f09718457defec1dda8e240691a7ce2bd) | Contrato DTO/HTTP real en TestServer ASP.NET .NET10, JWT firmado y rechazos 401/403, tenant A/B, integración MySQL 8.4 efímera 3/3, ejecución dirigida 15/15; A5 independiente posterior a reparación | **No** host/archivos/jobs/caché/backup global, PSP o Aiven productivo; A5_FAIL antiguo permanece histórico |
| M05.03 — [`4cbaab60`](https://github.com/solqaryn/Solqaryn/commit/4cbaab60070f0dc9d9d62814911decd5e647f6cb) | Oracle EF MySQL 8.4.13 efímero Category=Integration 3/3, A/B, identidad B, soft-delete y revocación persistidas; A5 independiente | **No** restaura Aiven ni cubre todas las rutas de archivos/jobs o proveedor externo |
| M05.04 — [`7d09ee99`](https://github.com/solqaryn/Solqaryn/commit/7d09ee9905d51be05dcb5d0d55c04f89ff6f8b87) | xUnit .NET Release positivo de reglas/evidencia de aceptación, ramas erróneas denegadas, gate de pruebas real; A5 independiente | **No** HTTP/JWT/DB real si el test concreto usa adaptadores/dobles en vez de integración |
| M05.05 — [`1c0785f0`](https://github.com/solqaryn/Solqaryn/commit/1c0785f07dce6b4f88c7e0095ef49793ce3866c0) | xUnit 20 PASS de negativos declarados para defensas, errores/tenancy y no-asignación de privilegios; A5 independiente | **No** prueba todo el BOLA/IDOR sobre recursos reales de archivos, jobs, caché o sucursales |
| M05.06 — [`73cfb858`](https://github.com/solqaryn/Solqaryn/commit/73cfb858acb7a6fdf0a90a51c4cfd53662ef8330) | **Chromium real**, 5/5: cuatro viewports, targets 44px, foco natural Tab/Enter, rechazo 403 y 503 sin secretos, mutación CSS adversarial y reversión; A5 DEV01 sobre autor DEV06 | Intercepción de sesión con token explícitamente **sintético** `NOT-JWT`: no prueba backend firmado, MySQL, hardware físico, WCAG integral, pentest ni métricas de campo |

Los totales de las primeras cinco filas se obtienen de sus registros PLAN/BITACORA, documentos y jobs dirigidos atribuidos al SHA individual: **no representan tests ejecutados de nuevo por esta microtarea R0**. M05.06 sí se inspeccionó en su spec `frontend/e2e/c14-02-m05-06-regression.spec.ts` y log de GitHub-hosted [job 135948164434](https://github.com/solqaryn/Solqaryn/actions/runs/44634130762/job/135948164434): `5 passed (7.7s)`, prueba adversarial CSS y `DOMContentLoaded=616 ms` local. Ese tiempo NO es LCP/INP/CLS de campo ni un SLA. Las pruebas API/Mysql verdaderas en M05.02/.03 **no pueden prestarse** para certificar que una sesión interceptada en M05.06 también haya usado JWT real.

## Negativos y discriminación causal de riesgo

1. Si existe `HttpTestingController`, interceptación `page.route` o `localStorage` sintético, etiquetar el resultado como prueba cliente; nunca como JWT firmado/servidor ni MySQL. Un error 403/503 simulado verifica presentación segura, no autorización de backend.
2. Si en una fuente hay `SKIPPED`, `CANCELLED`, `FAIL` o `IN_PROGRESS` en un check **pertinente**, no considerarlo PASS. Algunos retry auxiliares fueron `SKIPPED` no causales y NO son pruebas exitosas. Si existe un P0/P1 real abierto, conservarlo, y exigir su reparación en su propio scope.
3. Un job verde de seguridad global o de xUnit puro no sustituye negativa **real** de BOLA/IDOR sobre propietario de archivo/job/sucursal, host, caché o revocación a través de HTTP/JWT/DB. Seguir exigiendo la matriz de seguridad acumulativa antes de certificar fase M05/M06 y sin bypass de P0-007.
4. Las herramientas GitHub-hosted son la fuente válida de .NET10/Angular22/Chromium/MySQL84 efímero; **NO** son dispositivos físicos, entorno Aiven vivo, restore/cutover productivo ni permiso R3.
5. Un `A5_PASS` individual exige revisor Task-ID distinto de autor y reparador, revisión causal y preservación de FAIL anteriores. Las fórmulas DAG y COLA no se editan ni se fuerza una dependencia a SI por este documento.

## A2–A5, rollback y salida

- **A2/A3 de este diff:** `NO_APLICA` a ejecución nueva HTTP/JWT/RBAC/MySQL/Chromium porque esta unidad es documental pura y `PLAN D2084` exige evaluar suficiencia de evidencia, no cambiar runtime ni simular aceptación R2. Los huecos reales enumerados **siguen abiertos**; no son NO_APLICA del programa global.
- **A4:** publicación fast-forward/CAS sobre SHA real de `dev`, doc/CHANGELOG sin encabezados huérfanos, check exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`; transición `VALIDANDO→LISTO` sólo con V17 + CAS y readback PLAN/COLA/DAG/BITACORA, sin atribuir pruebas ajenas al SHA de este cambio.
- **A5:** revisor otra Task Javier distinta de DEV01, puede rechazar esta auditoría si una evidencia no existe, el resultado miente sobre mocks o alguna conclusión transfiere PASS a un subsistema no cubierto; nunca autoaprobación.
- **Rollback:** `git revert` normal del commit documental sobre HEAD actual y CAS institucional con BITACORA aditiva, sin force-push, destrucción de datos ni cambios QA/main/PROD.

**Conclusión limitada:** el lote tiene evidencia heterogénea verdadera (MySQL 8.4, JWT firmado y Chromium en ciertas fuentes), pero otras suites son dobles cliente o xUnit puro; no hay garantía global P0/P1=0, cobertura exhaustiva de BOLA/IDOR ni autorización productiva. El objetivo acumulativo de PLAN E y C14-02.M06.06 permanece separado y condicionado a pruebas posteriores genuinas.
