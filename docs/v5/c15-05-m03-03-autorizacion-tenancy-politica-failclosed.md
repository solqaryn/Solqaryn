# SOLQARYN V5 — C15-05.M03.03: política de autorización y aislamiento tenant para preflight operacional

Autor técnico: PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. Repo `solqaryn/Solqaryn`, exclusivamente `dev`. PROJECT_SCOPE_LOCK=STRICT.

## Alcance causal
Implementación **real .NET 10**, sin endpoint ni conexión externa: nuevo `V5OperationalAuditSecurityGate` en Domain, método integrado `PrepareWithTenantSecurityPreflight` en Application, y pruebas xUnit positivas y negativas en `V5OperationalAuditSecurityGateTests`. Predecesores autorizados: C15-05.M02.01–06 con A5 y C15-05.M03.01–02 con A5 independiente; verificar dinámicamente PLAN/COLA/DAG para el ID preciso.

La entrada a Domain es una proyección suministrada por el llamante, NO una autenticación. El resultado positivo no otorga sesión, acceso a filas, permisos GitHub/Drive ni capacidad de escribir: `AllowsInstitutionalWrite=false` para toda salida. Un adaptador futuro tendrá que autenticar JWT/identidad, consultar membresía y RBAC vigentes en backend, demostrar pertenencia del recurso, CI exact-SHA y la revisión independiente. No se instala tal adaptador aquí. `Prepare` existente permanece como preflight de solo lectura, tampoco concede escrituras.

## Decisiones y amenazas probadas en código
- Falta de principal autenticado o tenant resuelto por servidor → rechazo; `EmpresaId` NULL u ownership ambiguo no se atribuyen al cliente.
- A/B cruzado, membresía de otra empresa o recurso no poseído → rechazo determinista.
- Un tenant solicitado en cabecera/parámetro distinto al de fuente servidor → rechazo; aunque coincida, ese dato no otorga autoridad.
- Membresía inactiva, permiso revocado y ausencia del grant explícito de auditoría → rechazo.
- Actor servicio sin grant específico → rechazo. `IgnoreQueryFilters` u otra petición de bypass de filtros → rechazo incluso si el actor es servicio.
- Solicitud de escritura institucional → rechazo; checksum SHA no completo → rechazo.
- Datos coherentes → `AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED`, que es **pendiente de verificación externa**, no aprobación ni permiso real.

## Pruebas y límites
La suite xUnit incluye tenant A/B, NULL legacy, cliente spoofing, revocación, actor de servicio, filtro bypass, SHA y composición Domain→Application. La validación causal obligatoria es GitHub Actions backend build/tests y seguridad multiempresa, más Scope Lock y V5 exact-SHA sobre la publicación. CI en curso, cancelada o fallida no es PASS. No se atribuyen pentest, autorización HTTP/EF/MySQL real, R2, DNS, backup, migraciones, dispatcher, webhook, workers, QA/main/PROD ni certificación de 2.340 filas.

**Rollback:** git revert normal del commit (Domain/Application/tests/doc/CHANGELOG); no reescritura Git, no cancelación de CI y sin cambios a claves, cuentas o política de permisos de proveedores. A5 debe realizarla una de las otras cinco Tasks Javier, distinta del autor, con readback.
