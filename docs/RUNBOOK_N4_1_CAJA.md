# N4.1 — Caja — Runbook operativo

## Objetivo y alcance

Este runbook documenta la operación segura del flujo N4.1 Caja en `Desarrollo`: Caja, CajaSesion y CajaMovimiento, cubriendo Apertura → Operaciones → Arqueo → Cierre. No autoriza cambios en `main`, Producción, secretos, infraestructura ni despliegues.

## Precondiciones obligatorias

1. Trabajar exclusivamente sobre `Desarrollo` y mantener el PR #2 en Draft.
2. Verificar que la identidad operadora tenga los permisos relacionales correspondientes a la acción solicitada; una mutación autorizada no debe depender secundariamente del permiso `Ver`.
3. Confirmar que la caja objetivo exista, esté en un estado compatible con la operación y que la sesión activa corresponda a la caja/usuario esperado.
4. Mantener trazabilidad de auditoría y correlación para las mutaciones de Caja.
5. Antes de una validación causal, confirmar que el HEAD funcional que se está certificando sea explícito y separarlo de commits de control VAEP/Jules.

## Flujo operativo

### 1. Apertura

- Verificar que no exista una sesión abierta incompatible para la caja/operador.
- Registrar el fondo inicial con el valor autorizado.
- Confirmar que la sesión resultante quede asociada a la caja correcta y en estado operativo.
- Validar que la acción genere la trazabilidad/auditoría esperada.
- Ante rechazo de autorización, estado inválido o inconsistencia de sesión, detener el flujo y no intentar compensaciones manuales sobre datos.

### 2. Operaciones y movimientos

- Registrar únicamente movimientos soportados por el dominio y por el permiso específico de la mutación.
- Conservar tipo, monto, referencia/concepto y sesión/caja de origen conforme al contrato vigente.
- Verificar que ingresos, retiros, depósitos y demás movimientos aplicables queden vinculados a la sesión activa correcta.
- No alterar saldos o registros directamente en base de datos para corregir una operación fallida; usar el flujo de negocio o el procedimiento de recuperación autorizado.
- Confirmar auditoría/correlación después de cada mutación relevante.

### 3. Arqueo

- Ejecutar el arqueo únicamente sobre una sesión compatible y antes del cierre cuando el flujo lo requiera.
- Comparar el efectivo/valor contado con el valor esperado por el sistema.
- Registrar y revisar diferencias sin ocultarlas ni normalizarlas artificialmente.
- Si existe una diferencia no explicada, conservar la evidencia y bloquear el cierre operativo hasta que el criterio funcional vigente permita resolverla.

### 4. Cierre

- Confirmar que la sesión que se cerrará sea la sesión activa correcta.
- Verificar que los pasos obligatorios previos, incluido el arqueo cuando aplique, estén satisfechos.
- Ejecutar el cierre mediante el servicio/API de negocio, nunca mediante edición directa de persistencia.
- Confirmar el estado terminal de la sesión, la consistencia de los movimientos y la evidencia de auditoría.
- Una sesión cerrada no debe reutilizarse para nuevas mutaciones.

## Autorización, auditoría y seguridad

- Todas las operaciones sensibles deben permanecer fail-closed ante falta de permiso, identidad o estado válido.
- Los permisos deben evaluarse por la acción específica de Caja según el catálogo/contrato vigente.
- La evidencia de auditoría debe permitir identificar la acción, el actor y el objeto/sesión afectados de acuerdo con la implementación existente.
- Nunca registrar secretos, tokens o credenciales en logs, documentación o artifacts.
- Un resultado `COMPLETED` de Jules, un manifest o un workflow de control no constituye por sí mismo evidencia de funcionalidad ni autorización de cierre.

## Validación técnica proporcional

Para una recertificación causal del módulo, ejecutar los gates aplicables sobre el SHA funcional exacto. Como mínimo, cuando corresponda al cambio:

```bash
cd backend
dotnet build Solqaryn.sln --configuration Release
dotnet test Solqaryn.sln --configuration Release --no-build
```

Las validaciones que dependan de MySQL deben ejecutarse únicamente en el entorno de pruebas autorizado/configurado. Una base local ausente en una auditoría documental no debe reinterpretarse como PASS de integración; debe registrarse como validación no ejecutada/no causal según corresponda.

## Checklist de verificación del operador

- [ ] Rama `Desarrollo` confirmada.
- [ ] Caja y sesión correctas identificadas.
- [ ] Permiso específico de la acción confirmado.
- [ ] Apertura/fondo inicial consistente.
- [ ] Movimientos asociados a la sesión correcta.
- [ ] Arqueo y diferencias revisados cuando aplica.
- [ ] Cierre realizado mediante el flujo de negocio.
- [ ] Auditoría/correlación comprobada.
- [ ] No hubo edición directa de datos, secretos, Producción ni deploy.
- [ ] Evidencia del SHA funcional y gates aplicables conservada.

## Manejo de fallos

Ante fallo funcional, autorización denegada inesperada, inconsistencia de sesión/saldo, error de persistencia o gate causal rojo:

1. detener nuevas mutaciones sobre la sesión afectada;
2. conservar logs/evidencia sin secretos;
3. identificar el SHA funcional exacto y el paso causal que falló;
4. no declarar `LISTO` mientras exista P0/P1 reproducible;
5. usar `docs/ROLLBACK_N4_1_CAJA.md` para recuperación/rollback y validación posterior.

## Criterio de salida

La operación se considera verificada únicamente cuando el flujo aplicable termina con estado consistente, permisos y auditoría comprobados, y los gates causales exigibles están verdes o explícitamente clasificados como N/A con evidencia. Prerrequisito de cierre N4.1.H: no LISTO_REAL ni promocion N4.2 sin evidencia required-current de backup cifrado REAL M11 operativo de Desarrollo. La evidencia debe identificar el artifact exacto más su `.sha256` coincidente y `.meta.json` coincidente, y cualquier reclamo de restore/drill debe estar correlacionado causalmente a ese mismo artifact; evidencia de CI histórica, planificada, definida por workflow o desechable no satisface el cierre. Este runbook no autoriza promoción, merge ni despliegue.
