# P1-006.A — Selector empresarial por membresías activas (DEV)

## Alcance y dependencia
- Microtarea institucional: `P1-006.A`, primera P1 pendiente con `DEPENDENCIAS_OK=SI`; `P0-012.B=LISTO`.
- Base de trabajo: `dev@d89806cbbf4fea18ff9b503df94445839e83c78b`.
- Task propietaria: `6adbb9fff1548191a1bd5b30d9e9984a`, conexión `jmejia31`.
- El selector del login deja de solicitar un ID numérico escrito libremente y consulta un catálogo privado por JWT.
- P1-006.B (pruebas de certificación/integración) es un scope separado, aún no autorizado a cerrar por este cambio.

## Contrato implementado
1. `GET /tenant-context/empresas` exige `[Authorize]`. No es un listado administrativo de `/empresas`.
2. `UsuarioScopeService.ListarEmpresasSeleccionablesAsync` reutiliza `ContextosTenantValidos`: JWT del actor, usuario activo/desbloqueado, membresía `UsuarioEmpresa` activa, empresa activa, rol activo/empresarial válido y rol de plantilla de sistema o propietario correcto. SQL filtra antes de retornar; el cliente recibe solo `{empresaId,nombre}`.
3. El frontend consulta esa lista antes de mostrar opciones; solo permite escoger un `empresaId` incluido. Nunca acepta el ID persistido como permiso. La selección elegida SIEMPRE se verifica otra vez contra `GET /tenant-context/{empresaId}`, que permanece fail-closed.
4. Si falla la consulta, llega un esquema inválido, no hay membresías vigentes o el JWT expira, no se concede contexto tenant. El usuario conserva la ruta independiente a la consola global, sujeta a su propio gate.
5. Los DTOs y los tests existentes que usan `IUsuarioScopeService` sin el nuevo método siguen fallando cerrado por implementación predeterminada vacía, sin concesión de empresas.
6. Este cambio no toca migraciones, datos de usuarios, roles reales, credenciales, QA/main/PROD ni secretos.

## Pruebas dirigidas agregadas (ejecución pendiente de CI)
- `P1006ASelectorEmpresarialTests`: A válida visible; B revocada, C de otro actor, D con rol de plataforma, E inactiva y sesión sin JWT quedan fuera.
- `tenant-context.service.spec.ts`: catálogo válido no da contexto por sí solo; estructura malformada/401 fallan cerrado.
- `tenant-selector.component.spec.ts`: selección de opción del servidor, rechazo de ID arbitrario y recordado ajeno, fallo 503 sin navegación.
- Gates exigidos sobre SHA final: Scope Lock, Seguridad multiempresa y Build/backend, frontend tipado/build, regresión dirigida y cualquier gate extra aplicable. No se afirma PASS hasta readback.

## Dependencias, compatibilidad y rollback
- Riesgo: UI y pruebas legacy que completaban el campo `#empresa-id` con un número deben seleccionar una opción del nuevo catálogo; la ruta API de contexto validado se conserva.
- Integración backend/frontend debe desplegarse de manera coordinada en DEV; sin endpoint nuevo el selector muestra error recuperable y nunca realiza un fallback inseguro.
- Rollback exclusivo por PR protegido revirtiendo el nuevo endpoint, servicio de catálogo, selector y tests asociados; no revocar el servicio de verificación `GET /tenant-context/{id}` ni el aislamiento ya certificado.
- Toda auditoría de prueba y entrega requiere SHA exacto. `P1-006.A` permanece EN_PROGRESO hasta CI y entrega real; no se marca LISTO ni se anticipa el cierre P1-006.B.
