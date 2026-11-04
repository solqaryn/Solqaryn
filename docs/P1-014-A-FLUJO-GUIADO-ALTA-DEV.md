# P1-014.A — Flujo guiado comercial de alta (DEV)

## Autoridad
- Plan Comercial Inicial P1-014.A, orden 51, PLAN_MAESTRO fila 52, P0-012.B LISTO, COLA G52=SI.
- Task autora Javier :00, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a, GitHub jmejia31, WIP 6/1/1.
- Claim idempotente SQ-P1-014-A-JAVIER00-CLAIM-89B3687D-20261104, BITACORA fila 580 con readback.
- Rama work/solqaryn-p1-014-a desde dev@89b3687d14336734788bda26e2e187f0eea763c2; sin force-push ni modificar otros scopes.

## Reuso, experiencia y seguridad
Componente Angular existente frontend/src/app/features/plataforma/plataforma-gestion-mvp.component.ts.
El asistente divide el alta en tres pasos accesibles:
1. Empresa: nombre válido entre 2 y 200 caracteres; borrador disponible únicamente inactivo.
2. Administrador: cuenta existente previamente localizada y verificada, o nueva identidad con contraseña fuerte; no permite introducir IDs arbitrarios.
3. Revisión y confirmación: datos de empresa y usuario, explicación de configuración inicial y membresía creada por servidor; nunca muestra la contraseña ni activa una tienda.

Conserva el endpoint original /plataforma/empresas/alta-controlada y PlataformaGestionMvpService. La UI no concede autoridad. El backend conserva validación de gates, JWT/RBAC, identidad global, tenant y transacción MySQL. La confirmación exige directorio.puedeCrear; rechazo o 403 mantiene protección existente. No hay cambios de DTO/API, datos, migraciones, flags, secretos ni QA/main/PROD. La configuración inicial es informativa: no se inventan opciones ni se activan banderas.

## Validación y aceptación
- Pruebas de componente P1014A: nombre inválido, cuenta bloqueada, navegación de regreso, contraseña débil/oculta, permiso comercial denegado y una única solicitud de alta original.
- Exigir evidencia de ejecución, compilación Angular, CI exact SHA, Scope Lock AUTO_CERT PASS, seguridad multiempresa, revisión técnica de otro TASK_ID habilitado, merge protegido y CI postmerge exact merge SHA.
- Las pruebas con dobles no certifican MySQL vivo ni HTTP remoto. No declarar resultados sin ejecución.
- Revertir solo por PR protegido, preservando historial y auditoría.
- Referencias consultadas en origen/pin: SKREF-10 Angular Developer (angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9) y SKREF-03 Impeccable (pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199).
