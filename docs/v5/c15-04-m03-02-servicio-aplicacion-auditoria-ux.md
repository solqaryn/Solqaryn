# SOLQARYN V5 — C15-04.M03.02: caso de uso de auditoría UX en Application (.NET 10)

Autor: PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). Repositorio `solqaryn/Solqaryn` rama `dev`. PROJECT_SCOPE_LOCK=STRICT. Alcance aprobado por el STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN/COLA/DAG de esta microtarea y antecedente C15-04.M03.01 LISTO con revisión A5 de distinta Task Javier.

## Entrega implementada
Nuevo `V5UxAuditApplicationService` en Application, que consume las reglas puras `V5UxAuditRules` del Domain existente. Evalúa observaciones individuales de UX/accesibilidad/performance contra catálogo de superficies/criterios, presupuesto MIN/MAX, SHA fuente, referencias de evidencia y tenant A/B. Impone límite de 500 observaciones por petición de preflight y deniega catálogo vacío, contexto tenant no confirmado, SHA obsoleto, recurso de otro tenant, prueba fuera de presupuesto y evidencia insegura. Devuelve índice procesado para reparación dirigida.

Una coincidencia estructural devuelve `EXTERNAL_EVIDENCE_AND_A5_REQUIRED`: **no** autentica datos del navegador, mide WCAG, renderiza Playwright ni certifica presupuesto. `AllowsInstitutionalWrite=false`, `CertifiesExternalMeasurement=false`, `GrantsUserAccess=false` para todo resultado. La bandera de contexto del request es una afirmación del llamante, que deberá verificarse en un adaptador autenticado posterior; tampoco se conecta a un endpoint aquí. No se exporta PII ni métricas reales de clientes.

## Seguridad y pruebas
Pruebas reales xUnit de Application incluyen positivos de catálogo, límites A/B, tenant ausente, SHA distinto, superficie/criterio desconocidos, presupuesto excedido, evidencia con caracteres de control, capacidad excesiva y error en segunda observación. Son pruebas del algoritmo de preflight, **no** pruebas E2E, browser, HTTP o métricas productivas.

A0: inspección del Domain y de los contratos M02/M03.01, sin modificar esquema ni API. A1/A2/A3: build y xUnit causales en GitHub Actions para SHA exacto; CI de seguridad multiempresa además de backend. A4: publicación Git fast-forward con compare-and-swap, diff limitado a Application/tests/doc/CHANGELOG, Scope Lock/V5 en SUCCESS exact-SHA y readbacks PLAN/BITACORA. A5: revisión independiente de otra Task Javier, nunca de su autor.

No crear Jobs, workers, migraciones MySQL, endpoints, permisos JWT/RBAC, flags, recursos externos ni nuevos ejecutores; sin QA/main/PROD. La UI ligera y accesible del sistema no cambia. Rollback seguro: git revert normal y registro compensatorio sin borrar BITACORA.
