# C01-02.M04.02 — evidencia JWT real para estados de capacidades

La pantalla de capacidades protegida por guard y permisos ya tenía pruebas unitarias TestBed y Chromium con sesión sintética. Esta entrega añade aceptación R2 real con API ASP.NET Core 10, JWT firmado, usuarios efímeros en MySQL 8.4 y Playwright sobre Angular local.

Se comprueba rol global con JWT auténtico pero SIN_TENANT mientras no exista empresa verificada; roles empresariales A y B 403 de plataforma sin filtraciones; tenant B falsificado por cabecera y localStorage con JWT A denegado; JWT adulterado y solicitud anónima denegados. No se usan interceptores de sesión ni tokens simulados. El job GitHub-hosted es reproducible y no toca cuentas ni datos reales.

Las pruebas cubren sólo estas fronteras y no equivalen a certificar la matriz completa de rutas/capabilities de rubro, plan y sucursal, ni revocación transaccional en MySQL. A0: diff y contrato fuente; A1/A2/A3: suite real con resultado por SHA, no dar PASS antes; A4: Scope Lock, V5, checks pertinentes y readback; A5: revisor tercero diferente de DEV06 autor y DEV01 reparador. Permanecer VALIDANDO si falta aceptación global. Rollback: revert normal sin tocar QA/main/PROD.
