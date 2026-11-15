# SOLQARYN V17 — C11-04.M03.03 autorización y tenant en observabilidad

**Unidad:** C11-04.M03.03, PLAN fila 1708. **Autor:** PRIMARY DEV 01 JAVIER, Task \`6adbb9fff1548191a1bd5b30d9e9984a\`. **Riesgo V15:** R2 JWT/RBAC/tenant. **Rama exclusiva:** \`solqaryn/Solqaryn@dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`.

## A0 — Código auténtico, capas y alcance

Implementa \`C1104ObservabilityAuthorizationGate\` en Application con puertos \`IC1104VerifiedPrincipalSource\` (identidad JWT, membresía y RBAC actuales, sin valores del cliente) y \`IC1104TenantPartitionedEvidenceSource\` (lectura particionada por el tenant de ese principal). Reutiliza por composición el servicio Application M03.02 y las reglas Domain M03.01. Aporta xUnit, este documento y sección CHANGELOG propia. No registra controladores API/DI, no consulta DB/EF/MySQL ni ejecuta collectors o exporters, no concede permisos de runtime, no activa alertas ni certifica SLO.

El Request tiene únicamente SignalName, CorrelationId y SHA. No acepta tenant, rol ni privilegio desde el frontend. Sólo después de validar identidad y pertenencia/rol/RBAC/revocación/fence de auditoría se permite consultar la fuente \`ReadAsync(verifiedTenantScope,...)\`, y se coteja el tenant devuelto con el del principal por Ordinal. El servicio anterior vuelve a revisar SHA, señal, correlación, fuente confiable, agregado y dominio.

## A1 — Pruebas ejecutables

\`C1104ObservabilityAuthorizationGateTests\` cubre flujo de preflight positivo **no autorizante**, entrada/SHA/correlación falsos antes de I/O, JWT/membresía/rol/RBAC revocados o ausentes, actor inválido, tenant A/B desde principal, recursos o identidad del lector ajenos, evidence JWT/membresía/RBAC/auditoría caduca, fuente inexistente, SHA/señal alterada, etiquetas con PII, denominador cero NO_MEDIDO, cancelación y constructor sin dependencias. A1 sólo PASS con xUnit .NET 10 en GitHub-hosted sobre SHA causal y 0 FAIL.

## A2 — Seguridad

\`GrantsAccess\`, \`ExposesTelemetry\`, \`WritesData\`, \`CertifiesSlo\` y \`MayOverrideTenant\` son SIEMPRE false. Un resultado \`AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED\` exige un futuro adaptador HTTP autenticado con verificación real de JWT y permisos, no equivale a autorización API, escritura, revisión A5 o ejecución de proveedor. Se rechaza tenant B, revocación, PII como label, origen SHA ajeno y políticas no verificadas, sin devolver datos privados. Nada del código amplía tenant, roles o grants.

## A3 — Integración real y limitaciones

La composición Application + Domain se prueba mediante xUnit real, con lectores sintéticos que permiten verificar orden de consultas y aislamiento. **No** se implementan JWT middleware, API HTTP, EF/MySQL, workers, exporters, Playwright, telemetría ni provisión de secretos; son **NO_APLICA** a este diff, no PASS. La validación auténtica contra proveedor/DB, métricas y SLO continúan \`NO_MEDIDO\` hasta etapa explícita.

## A4, A5 y reversión

Publicación exclusivamente mediante commit fast-forward y expected-SHA sobre dev, Scope Lock y V5 sin intercalado, \`Backend Release y pruebas\`, \`SOLQARYN - Build y pruebas backend\`, \`SOLQARYN - Seguridad multiempresa\`, \`Authorization, files, secrets and tenant gate\` SUCCESS por SHA, y PLAN/COLA/DAG/BITACORA readback V17. Si la CI requerida está QUEUED/IN_PROGRESS no cerrar LISTO, mantener VALIDANDO y owner con RUN/SHA. A5 independiente por otra Task Javier; no autoprobación. Rollback mediante \`git revert\` normal, sin reescribir historia, sin QA/main/PROD o cambios productivos.
