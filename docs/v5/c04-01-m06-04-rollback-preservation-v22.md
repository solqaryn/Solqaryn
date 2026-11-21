# SOLQARYN V22 - C04-01.M06.04: ensayo real de rollback sin perdida
Requisito PLAN_MAESTRO D611: Ensayar rollback o desactivacion sin borrar datos legitimos. R2: MySQL/EF/tenant. Repo solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT; Task autora DEV03 6adc2e4bd8a48191ac7d8641ed95a1a3.

## Entrega y fronteras
Un test nuevo xUnit Category=Integration y un workflow GitHub-hosted con servicio mysql:8.4 efimero. Solo AppDbContext, entidades Empresa, Rol, Usuario y UsuarioEmpresa del stack vigente; sin alteracion de API, migracion productiva, backend runtime, Angular, facturacion, PSP ni secretos.

Ensayo: conservar dos empresas y membresias A/B, intentar desactivar B dentro de una transaccion y ejecutar rollback explicito; inspeccion SQL desde contexto nuevo para verificar ambas membresias activas. Crear tenant C provisional y descartar transaccion sin commit; demostrar ausencia en SQL. Finalmente desactivar membresia B con commit y leer desde contexto independiente: A activa, B inactiva, empresas A/B, usuario y ambas filas preservadas.

Tres negativos adicionales rechazan host remoto, puerto no permitido y usuario no-root antes de conectar. Crear esquema GUID aislado; solo eliminar esa DB efimera en cleanup. Comprobar SELECT VERSION() MySQL 8.4 real y TRX total=4 executed=4 passed=4 failed=0 notExecuted=0. No afirmar que el ensayo autoriza JWT firmado ni cuotas, precios, cobros reales o servicio productivo.

A0: diff puntual contra D611; E611 es meta acumulativa y NO certificado de M06. A1: test de rollback, desactivacion segura, abortos y tres entradas adversariales. A2: aislamiento de membresias A/B y preservacion, sin cambiar permisos ni afirmar JWT backend. A3: MySQL 8.4 efimero autentico y Oracle EF, no mocks; pruebas no terminadas no cuentan PASS. A4: commit FF/CAS y check-runs exact SHA completos SUCCESS para workflow dirigido, verify-solqaryn-project-scope, V5 provenance y checks pertinentes; readback PLAN/COLA/BITACORA V17. A5 requiere Task Javier distinta del autor/reparador; no autopase.

Rollback del changeset: git revert normal y revalidacion CI, sin force-push. La unica limpieza destructiva afecta al esquema temporal creado por el test. QA/main/PROD/PSP y datos reales no autorizados.