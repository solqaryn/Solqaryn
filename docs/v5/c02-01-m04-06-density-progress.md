# C02-01.M04.06 - densidad, carga progresiva y ligereza

Fecha 2026-11-20. Repositorio solqaryn/Solqaryn, rama dev exclusivamente. PROJECT_SCOPE_LOCK=STRICT. Task autora: Javier DEV03 (6adc2e4bd8a48191ac7d8641ed95a1a3). Riesgo R2 conservador por superficie tenant, pero cambio exclusivamente test/CI/documentacion.

## A0 - Alcance y aceptacion

PLAN_MAESTRO!D313 exige verificar densidad, progreso y ligereza. PLAN!E313 es meta acumulativa de M04, no exige que esta sola prueba certifique toda la fase de JWT/MySQL. No se altera Angular productivo, API, EF, MySQL, rol, sesion, permiso, esquema ni deploy. Dependencias DAG F=VALIDADO G=SI, COLA G/O=SI y CLAIM V17 exclusivo con CAS. Rollback mediante git revert no destructivo del changeset, y reversión de estado solo mediante CAS documentado si fuese necesaria.

## A1 - Pruebas adversariales de UI

Tres pruebas en Chromium real GitHub-hosted contra Angular compilado en loopback: (1) 320/375/768/1280: campos/botones dentro de viewport, overflow maximo 2px, presupuesto texto acotado y navigation timing medido sin fingir Web Vitals de produccion; (2) health GET retenido, feedback accesible, boton deshabilitado, doble submit sin segundo GET ni envio temprano de clave, fallo 503 opaco, spinner limpiado y ausencia de token; (3) tenant hint malicioso sin JWT no visualiza perfil ni selector privado. El HTTP de health/login en el caso 2 se intercepta para simular latencia y error de UI: NO es prueba de backend real, y ningun PASS aqui certifica autorizacion autentica.

## A2/A3 - Limites honestos

A2 NO_APLICA a nueva autorizacion JWT/RBAC por ausencia de cambio runtime. La negativa de acceso anonimo es asercion de guard Angular, NO certifica seguridad del servidor, pertenencia revocada, tenant A/B, host falso ni control plane. A3 requiere Angular/Chromium REAL en GitHub Actions del SHA del commit; no se afirma integrar MySQL, HTTP backend ni proveedor porque D313 solo exige UI de densidad. El resto de metas M04 y sus pruebas auténticas se preserva por separado.

## A4/A5 - Evidencia y cierre

Revisar job dirigido, tipado/build Angular, verify-solqaryn-project-scope y V5 provenance en GitHub Actions: todos los obligatorios completed/success sobre SHA propio o descendiente equivalente con blobs inalterados. Si CI aun corre, conservar VALIDANDO y owner original, sin LISTO ni A5 ficticios. A5 por otra Task Javier distinta tras revisiones genuinas de codigo y pruebas. No promueve QA/main/PROD. Prohibido aceptar SKIPPED, CANCELLED, FAIL o IN_PROGRESS en checks requeridos.
