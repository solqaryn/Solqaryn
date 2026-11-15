# C08-04.M03.03 — Authoritative tenant module authorization fence

Riesgo **R2**: módulos sectoriales, JWT, RBAC, tenant, revisiones y concurrencia.
Implementación Application **real y de solo lectura**; separa fuentes inyectables de actor y módulo,
revalida identidad backend actual, membresía, permiso relacional, catálogo, owner persistido,
SHA, version, estado y denylist, y reusa reglas Domain y preflight Application M03.01/M03.02.

A0: nuevo fence, lectores de evidencia, tests xUnit, este documento y CHANGELOG;
no cambios de DI/Infrastructure/EF ni del backend HTTP. El tenant afirmado por cliente no
selecciona la fuente: sólo el tenant autenticado decide la lectura.

A1: xUnit positivos, negativos tenant A/B, revocación, grant faltante, allowlist/denylist,
revision obsoleta, estados invalidos, fuentes fallidas y cancelacion. Los tests únicamente
se contabilizan PASS después de evidencia GitHub-hosted del SHA.

A2: JWT, membresía y RBAC deben provenir de backend; dueño tenant, módulo, SHA y revisión
deben coincidir en igualdad ordinal. La respuesta exitosa sólo solicita un futuro adaptador CAS
autenticado, **nunca** concede acceso, activación, escritura SQL, permiso de despliegue ni A5.

A3: NO_APLICA conexión real MySQL/EF, API HTTP, Angular/browser/proveedores en este diff:
ambas fuentes están definidas como contratos aislados, sin implementaciones/DI ni datos reales.
Pruebas con readers sintéticos no certifican integración de infraestructura.

A4: Scope Lock, procedencia V5, build Release, xUnit, seguridad y autorización completos/success
en SHA exacto o equivalente con blobs comprobados; PLAN/COLA/DAG/BITACORA readback.
Si algún check requerido está queued/in_progress dejar VALIDANDO y owner.

A5: revisión independiente de otra Task Javier. Reversión: git revert normal, sin force-push,
borrar BITACORA, trabajar QA/main/PROD ni cambiar secreto o cuenta.
