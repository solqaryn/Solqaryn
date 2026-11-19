# C14-02.M05.03 — MySQL 8.4: autoridad de plataforma, revocación y rollback

ID: C14-02.M05.03 · Proyecto SOLQARYN · rama dev · PROJECT_SCOPE_LOCK=STRICT · owner DEV06 · Riesgo R2.

## A0 — Alcance, impacto, arquitectura y rollback

El STANDARD V5 exige una integración pertinente de persistencia/proveedor para la aceptación de seguridad de C14-02 (BOLA/IDOR, spoofing, host, archivos, jobs, caché, backup y sesiones). Este incremento pone a prueba el servicio productivo existente \`PlataformaScopeService\` en MySQL 8.4 efímero usando Oracle EF Core 10, sin cambiar runtime, API, DTO, interfaces, migraciones, seguridad, cliente Angular, secretos ni esquema productivo. Consumidores afectados: ninguno, solo verificación en CI. Rollback: git revert normal del commit.

## A1-A3 — Pruebas adversariales, transacciones y límites

El job dedicado impone imagen mysql:8.4 real, base aislada de nombre aleatorio, localhost:3306 obligatorio, inicio de esquema y borrado al final. Rechaza destinos remotos y puertos inesperados ANTES de conectar. Con roles de plataforma, usuario empresarial, empresas A/B y asignaciones persistidas, la suite prueba:
- Positivo: acceso global desde UsuarioRolPlataforma, aunque Usuario.RolId sea empresarial.
- Negativos: usuario empresarial no asciende a global, identidad obsoleta con ID verdadero se deniega, sesión no autenticada se deniega.
- Dos roles globales activos: la verificación debe denegar; al revertir la segunda asignación por transacción, conexión nueva restaura autoridad.
- Revocación persistida desde una conexión distinta impide conceder autoridad al leer después.
El filtro Category=Integration es real: TRX debe contener al menos 3 casos ejecutados y 0 FALLAS. Ningún mock sustituye MySQL; solo ICurrentUserService es fixture para identidad y NO una firma JWT.

**NO ACREDITADO**: JWT criptográficamente firmado, middleware HTTP real, IDOR/BOLA sobre documentos, autorización de archivos/jobs/host, finanzas, pentest, restauración productiva, cierre de P0/P1 globales. La microtarea completa continúa VALIDANDO hasta A0-A4 pertinentes de todo el requisito; el éxito de este incremento no implica LISTO.

## A4-A5

Revisar SHA exacto, GitHub Actions job .NET 10/MySQL efímero, Scope Lock, V5/procedencia, backend/security pertinente, readback PLAN/COLA/DAG y BITACORA. QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILED nunca son PASS de prueba requerida. A5 por una Task Javier distinta del autor DEV06 y cualquier reparador; no autoprobar. Sin QA/main/PROD, Aiven real, cuentas externas, cambios de RRULE, secretos ni deploy.
