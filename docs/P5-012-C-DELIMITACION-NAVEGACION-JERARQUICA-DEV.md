# P5-012.C - Delimitacion de navegacion jerarquica (DEV)

- Autoridad: Plan Comercial Inicial V3 P5-012.C (DELIMITAR), origen P5-012.A y dependencia P1-016.B LISTO. Esta microtarea no implementa jerarquia ilimitada.
- Fuente producto unica: `frontend/src/app/shared/navigation/navigation-contract.json`, blob original `7142082462d7bc53ff4f0cdf96f67f21405f3a44`. No se modifica JSON de producto.
- Contrato actual `solqaryn-navigation-contract/v1`: grupos de dominio (`groups[].id`) con hijos directos (`groups[].children[]`) de `route`, `modulo`, `accion`; algunas rutas poseen `requires_admin:true`. El frontend solo refleja permisos; backend autoriza verdaderamente.
- Limite demostrable: SOLO se observa profundidad padre -> hijo/ruta. Este contrato V1 NO acredita grupos arbitrarios, nietos, recursividad ilimitada ni navegación visual real. Cualquier desarrollo de profundidad mayor exigira contrato, implementacion y tests propios, no inference de este documento.
- Escenario dirigido: test `scripts/tests/p5-012-c-delimitacion-jerarquia-navigation.test.mjs`, Node 22.16.0 (`node --check` y `node --test`, 5 casos). Positivo de esquema actual; negativos para esquema y autoridad mutados, duplicados, permisos/admin guard faltantes y nietos no soportados.
- No se altera Angular, backend, DTOs, autentificacion, RBAC/JWT, aislamiento tenant, MySQL/migraciones, secretos o QA/main/PROD. El test es estructural y no certifica navegador, teclado, accesibilidad, ruta runtime, permisos reales ni profundidad ilimitada.
- Rollback aditivo: `git revert` selectivo normal del test, esta nota y su entrada CHANGELOG, preservando otros Scopes y evitando force push.
- A5 se realizara por otro TASK_ID independiente antes de cualquier promocion, sin permisos de produccion implicitos.
