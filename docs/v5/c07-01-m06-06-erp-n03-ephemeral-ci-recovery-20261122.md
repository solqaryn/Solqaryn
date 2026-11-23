# C07-01.M06.06 — correccion causal ERP-N0.3 GitHub-hosted (2026-11-22)

## Evidencia de fallo previa
Commit base: 37f13974f32c532fea0b96589411a353ccc895cb. La ejecucion de autoridad-variante 45081335159, job 137389941421, termino con 49 FAIL / 6494 PASS / 0 SKIPPED en 6543 pruebas. El log evidencia 29 configuraciones locales por microtarea que el workflow no exportaba; los fallos de tipo EPHEMERAL_MYSQL84_REQUIRED / MYSQL84_EPHEMERAL_TEST_CONNECTION_REQUIRED ocurren aunque el contenedor MySQL 8.4 exista. El test de falta de tenant tambien falla por esta misma precondicion ausente. El check dirigido C07-01.M06.06, job 137389941413, paso 1/1 y no habilita por si solo el cierre.

## Reparacion real
El workflow ERP-N0.3 mantiene dotnet restore, build Release -warnaserror y dotnet test de TODA Solqaryn.sln, sin quitar ninguna prueba o relajar asserts. Agrega 29 variables de conexiones apuntando EXCLUSIVAMENTE a 127.0.0.1:3306 MySQL 8.4 efimero en GitHub-hosted; crea bases bootstrap locales para las suites que requieren nombres exactos; aumenta redo temporal a 512 MiB; concede hasta 60 minutos para CI. Concurrency cambia a exact github.sha y cancel-in-progress=false para preservar pruebas de otros commits.

## Seguridad y alcance
Credenciales del servicio efimero de CI, no secretos productivos. No modifica JWT/RBAC real, API comercial, tablas de Aiven ni despliegues QA/main/PROD. No altera BITACORA historica ni libera gates DAG. Validar check-run exact-SHA despues de publicacion y leer logs, TRX y errores; WAIT_CI/FAIL no son PASS. R2: rollback operativo git revert normal y restitucion de version anterior del workflow bajo CAS. El propietario DEV01 conserva H1045 EN_PROGRESO, I historico y candado V17 2000001045; A0-A4 de fase comercial y A5 permanecen pendientes.
