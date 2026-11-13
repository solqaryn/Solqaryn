# C00-02.M04.02 — estados de lectura V5 con autorización verificable

**Resultado:** estados reales de interfaz para la ruta protegida `/plataforma/flujo-v5`, sobre el componente Angular existente. No corresponde a un dashboard conectado a PLAN_MAESTRO ni a un sistema de despacho.

- **Carga:** el usuario revalida la sesión y se muestra comprobación en curso sin afirmar autorización antes del servidor.
- **Permiso denegado o no confirmado:** la vista de contrato queda oculta cuando `PlataformaSesionService.sesion` no acredita autoridad de plataforma. El guard requiere GET autenticado a `/plataforma/sesion`.
- **Error:** al fallar la comprobación se muestra un aviso genérico y no se expone `HttpErrorResponse.error.message`, credenciales ni PII.
- **Éxito:** después de la autoridad global confirmada, se distingue permiso para **lectura** del cumplimiento de microtareas.
- **Vacío:** se reconoce que no existe una fuente institucional de microtareas conectada a esta pantalla; no inventar registros ni A0–A5.

**Pruebas:** cuatro escenarios Vitest en `frontend/src/app/features/plataforma/plataforma-gestion-mvp.component.spec.ts`; cubren éxito/vacío, falta de permiso, carga/fallo seguro y confirmación posterior. `requiere_node_angular=SI`, `requiere_security=SI` (frontera plataforma), `requiere_scope_lock=SI`; `requiere_dotnet=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO_APLICA` para esta superficie sin datos dinámicos ni cambios de navegación. El backend sigue siendo la autoridad JWT/RBAC y las pruebas de UI no certifican API ni MySQL.

**Dependencias:** seis C00-02.M03.01–06 LISTO en PLAN, DAG F=VALIDADO/G=SI, COLA G/O=SI al reclamar. No habilita M04.06 ni otras fases por sí misma.

**Seguridad y retroceso:** no hay escritura de tareas, altas, API nueva, imágenes históricas, secrets ni cambios de proveedor. A0 revisión estática/secretos; A1 escenarios de estado; A2 permisos/errores seguros; A3 build/lint/pruebas Angular exact SHA; A4 Scope Lock, commit, código y Sheet readback; A5 pendiente de otra Task Javier. Si el commit requiere reversión, utilizar git revert normal sobre dev tras revalidar HEAD.
## Gate de pruebas remotas dirigido

La inspección del commit inicial `21f8aee907dd1d5976185fb1f1969ae3f4df8d7f` verificó lint y compilación Angular, pero no un job que ejecutara las cuatro nuevas pruebas Vitest. Por esa causa se añade `.github/workflows/v5-plataforma-ui-tests.yml` como CI dirigida con activación **solo en dev** y rutas limitadas, sin escritura, secretos o servicios. Este workflow es una herramienta de verificación, no un ejecutor comercial. Sus pruebas deben aprobar en el SHA que incorpore el gate; un workflow omitido no es PASS.
