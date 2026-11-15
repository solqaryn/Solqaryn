# SOLQARYN V5 — C01-01.M04.03 branding contextual seguro

**Proyecto:** SOLQARYN, solo dev, PROJECT_SCOPE_LOCK=STRICT. **Responsable:** PRIMARY DEV 05 Javier, TASK_ID 6adc2e55f47c81919f6374e561687076. **Fuente:** STANDARD V5 FINAL + PLAN_MAESTRO fila 166 y DAG VALIDADO/SI.

## Entrega real
Extiende el componente Angular standalone `IdentidadSesionVerificadaComponent` ya existente, sin rutas, APIs ni permisos nuevos. Solo después de `TenantContextService.contextoVerificado()` presenta insignia del sistema SOLQARYN y el ID empresarial **confirmado por el backend**. La marca no aparece durante selección ni si se revoca la verificación. Los estilos consumen `--color-primary` y `--color-on-primary` del `ThemeApplierService` existente, que valida HEX y contraste; el tema cambia reactivamente sin introducir CSS configurable arbitrario ni nuevos componentes no inventariados. En forced-colors hay fallback accesible. No se muestra nombre/logo configurable de empresa cuya correspondencia con el tenant no pueda probarse.

## Arquitectura, seguridad y pruebas
- Angular 22.2.1 standalone; CSS tokens existentes, componente permitido en `/usuarios` con guards vigentes. No se modifica el ThemeApplierService, su política de contraste ni aplicación global de tenant.
- Seguridad fail-closed: no usa URL, localStorage, Empresa solicitada o header como autenticación; no altera JWT, RBAC, servidor ni base de datos.
- A0: diff limitado a HTML, SCSS y dos pruebas positivas/negativas nuevas, este documento y CHANGELOG; no secretos, ni cambios de arquitectura.
- A1: cinco pruebas Vitest/Angular (las 3 existentes + 2 nuevas) cubren ausente, A→B y HTML interpolado, marca revocada y tema permitido; resultados reales solo desde CI.
- A2: tenant verificado es fuente de autoridad; no hay grants nuevos ni datos cross-tenant, ni riesgo de CSS inyectable.
- A3: reusa signals y ThemeApplierService existente, no crea servicios de red; pruebas UI sobre HTML generado.
- A4: requiere GitHub-hosted exact SHA Vitest, lint, build, Scope Lock, V5, P0 catálogo y readbacks institucionales; cancelados/skipped no son PASS.
- A5: verificación independiente por Task Javier distinta del autor, sin QA/main/PROD.

**Límite honesto:** no se implementa branding editable por tenant ni una consulta de identidad adicional; esos cambios necesitarían correlación backend verificada y autorización. Rollback: git revert normal dev.
