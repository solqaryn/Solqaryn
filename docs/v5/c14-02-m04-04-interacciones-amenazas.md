# SOLQARYN V17 — C14-02.M04.04 navegación, mensajes y doble acción

Riesgo: R2 por verificación negativa de una superficie privada de seguridad. Autor: DEV02 (TASK_ID 6af16bb9492c8191b8e8c7d606f11507). Plan fila 2075. El alcance son pruebas y la etiqueta de su workflow; no se modifica el componente productivo, la ruta, los guards ni permisos de backend.

## A0: alcance y criterios
- Navegación por enlace interno autorizado hacia /plataforma, sin formularios ni acciones de pentest, backup o cambio de política.
- Protección single-flight de la revalidación: dos invocaciones simultáneas deben producir una solicitud; una posterior a la respuesta puede volver a consultar.
- Mensaje de error 429 no debe revelar statusText, diagnósticos internos ni mostrar la lista de amenazas.
- Casos positivos, negativos, frontera de concurrencia y error con aserciones que fallan si aparece una solicitud duplicada o se publica un secreto.
- Dependencias: C14-02.M04.01-.03; rollback mediante git revert ordinario de este changeset sobre dev.

## A1/A2/A3
- Tres Vitest nuevos (navegación segura; single-flight y fin de ciclo; error 429) sobre la suite Angular existente; dos Chromium adicionales prueban navegación interna y error 429 con servicio de sesión simulado.
- Las rutas privadas existentes conservan plataformaGuard; pruebas de sesión sintética NO certifican JWT HTTP real, tenant A/B remoto, pentest ni revisión de proveedor.
- Prueba de integración pertinente: Vitest/Angular build y Chromium real en GitHub-hosted. No aplican MySQL, EF, migraciones o proveedor externo: los archivos ejecutables de esas capas no cambian.
- Aserciones de navegación y error no crean ningún endpoint o DTO nuevo.

## A4/A5
- Exigir checks completed/success del SHA publicado: workflow dirigido, frontend, seguridad, Scope Lock, V5 y aceptación integral aplicable; no contar queued, in_progress, skipped o cancelled.
- PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA requieren readback idempotente de CLAIM, VALIDANDO y LISTO.
- A5 debe ser revisión independiente de otra Task Javier, sin autocertificación.
- No cambiar QA/main/PROD, secretos ni datos reales.
