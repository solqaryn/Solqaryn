# C13-04.M05.02 - Contrato API/DTO y errores seguros (DEV)

PROJECT_SCOPE_LOCK=STRICT | REPOSITORY=solqaryn/Solqaryn | BRANCH=dev
Autora: DEV05 / 6adc2e55f47c81919f6374e561687076. Riesgo conservador R2: fronteras de autorizacion global cliente.

## A0: alcance real, dependencia y rollback
Contrato existente en PlataformaSesionService.obtener(), GET /plataforma/sesion y ApiResponse<PlataformaSesion>. Sin endpoint, backend, grants, permisos, schema, migracion, secreto, schedulers ni despliegue. La vista C13-04 muestra solo datos autorizados por el servicio; no certifica inventario ni accesos del backend.
El cliente exige success=true, ambito literal Plataforma, esAdministrador=true y nombres no vacios. Una sesion previa se limpia antes de revalidar. Fuente: servicio Angular y PLAN_MAESTRO C13-04.M05.02; M04.01-06 preceden segun DAG V17.

## A1 y A2: oraculos positivos, negativos, frontera y error
Suite Angular HttpClientTesting: GET observable frio, respuesta positiva, envelope falso/ambiguo/nulo, administrador Empresa/flag false, nombre y rol invalidos, revocacion 403, HTTP 401/403/500/503, JSON malformado y limpieza idempotente. Son aserciones ejecutables reales sobre el consumidor, pero el transporte y las respuestas son sinteticos. NO constituyen JWT/RBAC/tenant A/B reales de servidor.

## A3 y A4: evidencias exigidas
Workflow v19-c13-04-m05-02-contrato-api.yml: Node 24/npm 11, npm ci, prueba Angular dirigida, lint y build produccion en GitHub-hosted. Revisar logs por SHA publicado y verificar tests reales 8/8, Scope Lock y V5/procedencia (completed/success). No contar queued, skipped, canceled ni in_progress como PASS. A4 requiere readback de PLAN/COLA/BITACORA. En el presente diff se agregan pruebas y documentos, no cambia UI ni backend; Playwright, MySQL y proveedor HTTP vivo NO_APLICA a ejecución de cambios, pero la certificacion end-to-end del contrato queda NO_ACREDITADA.

## A5, limites y revert
Revision independiente obligatoria por Task Javier distinta de DEV05, con posibilidad de A5_FAIL. No afirmar LISTO_DEV mientras falten controles exigibles. No activar QA/main/PROD, no forzar DAG ni alterar estados de otra Task. Rollback: git revert normal del commit en dev sin force-push, conservar BITACORA.
