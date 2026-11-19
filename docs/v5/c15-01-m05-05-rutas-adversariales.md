# C15-01.M05.05 — auditoría negativa de seguridad de rutas (V20 DEV04)

## Alcance y pruebas causales R2

Leemos fuentes actuales de dev: `app.routes.ts`, `auth.guard.ts`, `permiso.guard.ts`, `plataforma.guard.ts`. Sin crear rutas/roles ni cambiar runtime. El script valida que las rutas privadas de plataforma requieren `plataformaGuard`, seis rutas ERP críticas requieren authGuard+permisoGuard y módulo/acción exactos, que centro-reportes está autenticado y que los tres guards conservan comprobaciones de contexto tenant, rol/ruta y rechazo fail-closed de 403/error. Ocho mutaciones negativas independientes (ruta privada pública forjada, guard eliminado, rol alterado, bypass permiso, bypass tenant, 403 negado y error) deben ser rechazadas. CI GitHub-hosted Node 24 ejecuta la versión exact-SHA de este código.

**Límites:** Es un control estático y adversarial de las fuentes concretas, NO un test HTTP de JWT firmado, RBAC de backend, empresas A/B ni IDOR/BOLA reales, tampoco verifica acceso MySQL ni endpoint de producción. El contrato M05.05 completo exige considerar esas vías por separado antes de LISTO. Esta entrega no certifica A2/A3 globales; mantener VALIDANDO cuando queden puertas relevantes y exigir A5 por Task Javier distinta.

Impacto: script de pruebas, workflow independiente, documento y CHANGELOG; ninguna modificación a app, guard, API, contrato, DB, secretos o infraestructura. Rollback mediante git revert sobre dev con HEAD verificado. V17 claim/cierre de estado y BITACORA de 26 columnas con readback.
