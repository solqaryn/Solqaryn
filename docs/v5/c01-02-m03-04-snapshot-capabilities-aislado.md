# SOLQARYN V17 — C01-02.M03.04: snapshot aislado de capabilities y permisos

**Microtarea:** C01-02.M03.04, PLAN_MAESTRO fila 197, autor PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. Repositorio exclusivo `solqaryn/Solqaryn` rama `dev`, Scope Lock STRICT. Riesgo V15: **R2**, por capabilities, roles, tenants y privacidad.

## A0 — Cambio real por capa y límites
Se crea `C0102IsolatedCapabilityPolicySnapshotAdapter` en Infrastructure, sobre el contrato `IC0102VerifiedCapabilityPolicyReader` de Application (C01-02.M03.03). Se preserva Domain ← Application ← Infrastructure, sin modificar API, DTO HTTP, MySQL/EF, migración, JWT, roles reales, productos o proveedores. El cambio agrega también 9 hechos xUnit adversariales, este registro y una sección CHANGELOG autocontenida.

El adaptador lee una **captura sintética local**, tenant- y recurso-scoped, construida con valores del llamante. Las banderas que significarían fuente persistente confiable, catálogo backend actual y grant RBAC relacional se fuerzan a `false` al capturar. La política se copia a conjuntos ordinales para evitar que colecciones mutables/OrdinalIgnoreCase amplíen permisos. El resultado no es autoridad de backend, ni un cache ALLOW, ni capacidad de escritura. Un adaptador de persistencia real requiere una microtarea separada con autenticación, versionado, filtros A/B, transacción y pruebas MySQL 8.4 efímero.

## A1 y A2 — Pruebas e invariantes
9 pruebas xUnit verifican captura nominal *no autorizante*; rechazo tenant B; recurso y ownership incorrectos; comparación ordinal de claves; mutación de colección fuente; ausencia de política; constructor inválido; cancelación; y degradación de evidencias aparentemente positivas. El resultado no habilita endpoint, exportación, activación, modificación de plan, bypass RBAC o concesión tenant. No se registran secretos ni datos personales reales.

## A3, A4 y A5 — CI, publicación y rollback
A3 pertinente: .NET 10 Release y xUnit dirigido en GitHub Actions; seguridad multiempresa y autorización deben aprobar. **NO_APLICA** MySQL/Category=Integration, Chromium o proveedor live a este diff aislado sin EF/SQL/UI/HTTP. Esto no certifica integración real ni seguridad end-to-end. A4: Scope Lock exact-SHA, V5 procedencia, estado, bitácora y readback; checks pertinentes `completed/success` antes de LISTO_DEV. Mantener VALIDANDO si CI externa sigue queued/in_progress, owner conservado. A5 solo otra Task Javier distinta del autor. Reversión convencional `git revert` tras reconciliar HEAD, sin force-push ni borrado de BITACORA. No QA/main/PROD.

## V19 — Reparación causal del A5_FAIL P2 (2026-11-17)

La auditoría independiente DEV06 del SHA cb79ef0c detectó que `Freeze` copiaba en `HashSet<string>` pero lo exponía como `IReadOnlySet<string>`, permitiendo a un lector hacer downcast a `ISet<string>` y mutar la referencia retenida por `ReadCurrentAsync`. El defecto se corrige con `System.Collections.Frozen` y `ToFrozenSet(StringComparer.Ordinal)`, preservando NULL y la comparación ordinal. Se agregan dos pruebas adversariales que prohíben exponer `ISet`/`ICollection` mutables, comprueban relectura estable y mutación externa de permisos/denegaciones. No se modifican API, JWT, MySQL/EF, datos reales ni permisos productivos. Rollback normal `git revert`; A5_FAIL histórico permanece, y sólo una revisión nueva de otra Task Javier con CI Release/seguridad/Scope Lock/V5 exact-SHA permite corregir A5 y reactivar el DAG sin forzar fórmulas ni estados.


## Afinación de regresión A5: interfaz no equivale a mutabilidad
GitHub Actions detectó que los dos nuevos tests exigían erróneamente que FrozenSet no fuera asignable a ISet/ICollection; en .NET 10 puede exponerlas con operaciones de mutación no soportadas. Se sustituyó la aserción estructural por intentos reales de Add/Clear y comprobación del contenido íntegro. HashSet mutable falla este ensayo. Se mantienen el defecto A5_FAIL histórico, la reparación ToFrozenSet y la revisión independiente posterior. No se cambian sistemas productivos.
