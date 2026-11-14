# Runbook de Rollback y Recuperación: PeriodoContable (N4.9)

## 1. Alcance
Este runbook documenta los procedimientos de recuperación técnica y rollback para la fase N4.9 (Módulo de Contabilidad - PeriodoContable) en Solqaryn. Está diseñado para mitigar fallos funcionales, caídas de base de datos o regresiones de seguridad relacionadas con el manejo de periodos contables.

## 2. Niveles de Rollback

### 2.1 Rollback de Código (Git / Despliegue)
Si se identifica una regresión crítica en la lógica de aplicación o en los controladores API que no afecta la integridad de los datos persistidos (por ejemplo, validaciones DTO incorrectas o fugas en el contrato API):

*   **Identificar Commit Base Seguro:** El rollback se debe realizar al último tag estable o commit de integración (por ejemplo, previo a los merges de `N4.9.*`).
*   **Procedimiento (Hotfix/Revert):** Revertir los commits pertinentes en el repositorio de código y redesplegar. **Nota:** No se publican comandos `git push` aquí ya que el rollback en producción es ejecutado por CD.
*   **Criterio de éxito:** Los flujos operativos de módulos preexistentes operan con normalidad.

### 2.2 Rollback de Persistencia (Base de Datos)
La persistencia introdujo la tabla `PeriodosContables` vía la migración `20260905093000_N4_9_PeriodoContablePersistencia`. Si la migración corrompe datos colindantes, introduce bloqueos de rendimiento imprevistos por los índices `IX_PeriodosContables_Estado_Rango` / `UX_PeriodosContables_Rango`, o es defectuosa estructuralmente:

**Advertencia:** Nunca usar comandos destructivos de drop en producción de manera autónoma.

*   **Rollback de Migración vía EF Core (Ambientes controlados):**
    ```bash
    cd backend
    dotnet ef database update <Migracion_Anterior>
    ```
    *Esto ejecutará el método `Down` que contiene `migrationBuilder.DropTable(name: "PeriodosContables");`.*

*   **Desactivación Lógica (Producción / Alternativa Segura):**
    Si ya existen registros contables productivos en `PeriodosContables`, ejecutar un "Down" es destructivo e inaceptable. En su lugar:
    1.  Desplegar un hotfix de código que detenga todas las llamadas de escritura (`Crear`, `Cerrar`) hacia la tabla (cerrar funcionalmente el feature).
    2.  Parchar manualmente (vía operaciones de DBA supervisadas) el estado si hay periodos inconsistentes (ej. `Estado = 2` sin `CerradoEnUtc`), respetando siempre los Check Constraints (`CK_PeriodosContables_Cierre`).

### 2.3 Rollback de UI (Frontend Angular)
Si los componentes Angular (UI de Periodos Contables) bloquean la navegación general o rompen dependencias compartidas:

*   Revertir la importación del módulo de Periodo Contable desde el enrutador principal (`app-routing.module.ts` o equivalente).
*   Revertir cualquier modificación de estado global (NgRx / Contextos compartidos) relacionada, redesplegando el SPA.

## 3. Plan de Recuperación de Integridad de Datos

El dominio `PeriodoContable` es estricto (no admite borrado físico, el `Cierre` es fail-closed y genera timestamp).
Si un periodo contable fue creado incorrectamente por un bug o un vector de ataque:
1.  **No ejecutar DELETE SQL.** El modelo actual no implementa un flag de "Eliminado" porque un periodo es el ledger temporal maestro de Contabilidad.
2.  Si el periodo no tiene asientos asociados, se debe documentar y escalar a negocio.
3.  Si es imperativo corregir el estado por corrupción técnica, las actualizaciones directas en BD deben cumplir rigurosamente las constraints:
    *   `Estado` (1=Abierto, 2=Cerrado).
    *   Si `Estado=2`, `CerradoEnUtc` **debe** ser `NOT NULL`.
    *   Las fechas de inicio y fin deben mantener `FechaFin >= FechaInicio` (`CK_PeriodosContables_Rango`).

## 4. Pruebas de Verificación post-Rollback (QA)
Tras un rollback o recuperación en caliente, QA debe verificar:
1.  **Regresión general:** Módulos preexistentes (Inventario, Ventas, Cuentas por Pagar) operan sin alteraciones.
2.  **Verificación API:** Los endpoints de `/api/periodos-contables` retornan correctamente 404 (si el API fue revertida) o presentan bloqueo defensivo explícito (si se aplicó flag de contingencia).
3.  **Auditoría y Log:** La tabla de auditorías (`IAuditoriaService`) y el log de errores no presentan fugas de excepciones de sistema relativas al proceso de rollback.
