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

## V21 — ampliación real de integración MySQL A/B y revocación de cuenta (19-11-2026)

Se añaden al MISMO ensayo `[Trait(Category,Integration)]` con Oracle EF Core y servicio MySQL 8.4 efímero, sin tocar runtime ni permisos: un rol y usuario empresarial B persistidos y aislados de A; las identidades A/B deben ser rechazadas como autoridad global incluso al suplantar el nombre de plataforma. El usuario de plataforma se bloquea, se consulta desde otra conexión (denegado), se marca eliminado lógicamente (denegado) y se restaura únicamente en la base de laboratorio para verificar que cada transición proviene de MySQL actual. Al final sigue exigiéndose revocación de rol global desde otra conexión. El job GitHub-hosted del nuevo SHA ejecuta los mismos tests Category=Integration y TRX con 0 fallos; los nuevos asserts no son simulación ni PASS hasta CI.

**Pendiente explícito:** este incremento NO implementa JWT firmado, autorización HTTP de archivos/jobs/host, BOLA/IDOR extremo a extremo, restauración comercial o cierre global P0/P1. H=VALIDANDO y owner histórico DEV06 permanecen intactos hasta completar A0–A4 del requisito institucional. A5 por otra Task Javier independiente. Rollback normal de Git; no QA/main/PROD ni datos productivos.
