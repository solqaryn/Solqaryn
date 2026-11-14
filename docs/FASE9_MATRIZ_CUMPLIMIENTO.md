# Fase 9 — Matriz final de cumplimiento

Fecha: 2026-07-29  
Rama: `Desarrollo`

## Criterios de estado

- **Aprobado:** existe evidencia automatizada o documental suficiente.
- **Aprobado aislado:** funciona en infraestructura temporal, pero falta proveedor real.
- **Pendiente externo:** requiere acceso, credencial, servicio o dispositivo del propietario.
- **No autorizado:** no debe ejecutarse sin autorización expresa.
- **No apto:** bloquea una liberación productiva.

## Matriz principal

| Área | Estado | Evidencia principal | Pendiente |
|---|---|---|---|
| Backend ASP.NET Core | Aprobado | Run `30474905738` | Ninguno automatizado |
| Pruebas unitarias backend | Aprobado | Run `30474905738` | Ninguno automatizado |
| Frontend Angular productivo | Aprobado | Run `30474905738` | Revisión visual del propietario |
| Lint y calidad estática | Aprobado | Runs de compilación y Fase 8 | Ninguno automatizado |
| Docker | Aprobado | Run `30474905738` | Despliegue real no autorizado |
| MySQL 8.4 temporal | Aprobado | Runs `30474905738`, `30474905564`, `30474905679` | Aiven Desarrollo |
| Migraciones EF Core | Aprobado en temporal | Run `30474905738` | Aplicación controlada en Aiven Desarrollo |
| Snapshot EF | Aprobado | Run `30474905738` | Ninguno automatizado |
| SQL forward no destructivo | Aprobado | Run `30474905738` | Revisión previa a futura liberación |
| Autenticación JWT | Aprobado | Aceptación integral | Prueba en Render Desarrollo |
| Rate limiting | Aprobado | Auditoría `30474905571` | Prueba desde red externa |
| Health/readiness | Aprobado | Fase 8 `30474905679` | Confirmación en Render Desarrollo |
| Permisos por rol | Aprobado | Fases 6 y 8 | Aceptación funcional del propietario |
| Aislamiento por usuario | Aprobado | Fases 7 y 8 | Ninguno automatizado |
| Usuarios y roles | Aprobado | Regresión integral | Revisión con datos reales de Desarrollo |
| Productos y catálogos | Aprobado | Regresión integral | Revisión visual del propietario |
| Variantes por color/SKU | Aprobado | Fases 4, 7 y 8 | Datos reales de Desarrollo |
| Inventario consolidado | Aprobado | Matriz 4/3/3 y regresión | Conteo físico real |
| Compras | Aprobado | Regresión integral | Operación controlada en Desarrollo |
| Ventas | Aprobado | Regresión integral | Operación controlada en Desarrollo |
| Anulaciones y reversión | Aprobado | Fase 7 | Operación controlada en Desarrollo |
| Sobreventa por variante | Aprobado | Fase 7 | Ninguno automatizado |
| Facturación | Aprobado | Fases 6, 7 y 8 | Revisión comercial final |
| Cálculo fiscal | Aprobado | Caso L. 280.00 | Aprobación contable/tributaria del propietario |
| Pagos parciales/totales | Aprobado | Fase 7 | Prueba con operación real de Desarrollo |
| Costos de envío | Aprobado | Fase 7 | Confirmar tarifas reales |
| Descuentos e impuestos | Aprobado | Fases 7 y 8 | Confirmar políticas comerciales |
| PDF A4 | Aprobado | SMTP/PDF aislado | Impresión física |
| Perfiles de impresión | Aprobado simulado | Fase 6 | Impresoras físicas |
| POS 58/80 mm | Aprobado simulado | Fase 6 | Densidad, corte y avance reales |
| Correo SMTP | Aprobado aislado | Fase 8 `30474905679` | Buzón y proveedor reales |
| Idempotencia de correo | Aprobado local | Fases 7 y 8 | Persistencia distribuida futura |
| WhatsApp | Pendiente externo | No existe prueba física certificada | Teléfono real |
| Cargas masivas CSV/XLSX | Aprobado | Fases 5, 7 y 8 | Archivos reales del propietario |
| Seguridad XLSX | Aprobado | Fase 5 | Ninguno automatizado |
| Auditoría | Aprobado | Fases 6 y 8 | Revisión operativa del propietario |
| Reportes administrativos | Aprobado | Fase 6 | Validar utilidad para operación real |
| Exportaciones CSV/XLSX | Aprobado | Fases 5 y 6 | Ninguno automatizado |
| Accesibilidad semántica | Aprobado automatizado | Fase 8 | Revisión con tecnología asistiva real |
| Navegación por teclado | Aprobado | Fase 8 | Revisión manual opcional |
| Responsive 320 × 568 | Aprobado | Fase 8 | Dispositivo físico |
| Responsive 3840 × 2160 | Aprobado | Fase 8 | Pantalla física opcional |
| Tema claro/oscuro | Aprobado | Regresión integral | Preferencia visual del propietario |
| Imágenes y fallback | Aprobado | Fase 5 | Cloudinary Desarrollo |
| Cloudinary Desarrollo | Pendiente externo | Aislamiento documental | Carga y eliminación controladas |
| Render Desarrollo | Pendiente externo | Configuración validada | Despliegue y logs reales |
| Vercel Desarrollo | Pendiente externo | Configuración validada | Despliegue y navegación reales |
| Aiven Desarrollo | Pendiente externo | MySQL temporal aprobado | Migración y consulta reales |
| Consola del navegador | Aprobado automatizado | Fase 8 | Navegadores/dispositivos reales |
| Logs sin secretos | Aprobado automatizado | Fase 8 | Revisión de Render Desarrollo |
| Dependencias .NET | Aprobado | `30474905571`, `30474905679` | Reevaluar antes de liberar |
| Dependencias npm | Aprobado | `30474905571`, `30474905679` | Reevaluar antes de liberar |
| Rendimiento controlado | Aprobado | Fase 8 | Latencia y carga en servicios reales |
| Respaldo productivo | No apto | No ejecutado | Respaldo verificable |
| Restauración productiva | No apto | No ensayada | Simulación y evidencia |
| Plan de rollback | Documentado | `FASE9_PLAN_LIBERACION_Y_ROLLBACK.md` | Aprobación y responsables |
| Merge a main | No autorizado | Regla del proyecto | Autorización expresa |
| Despliegue productivo | No autorizado | Regla del proyecto | Autorización expresa |

## Resumen cuantitativo

### Aprobado técnica o automáticamente

- compilación backend y frontend;
- pruebas unitarias;
- 81 pruebas Playwright integrales;
- 7 pruebas especializadas de Fase 8;
- migraciones en MySQL descartable;
- seguridad, accesibilidad, responsive y runtime;
- SMTP y PDF aislados;
- dependencias y logs.

### Pendiente externo

- correo real;
- Render Desarrollo;
- Vercel Desarrollo;
- Aiven Desarrollo;
- Cloudinary Desarrollo;
- WhatsApp;
- dispositivos físicos;
- impresión física;
- aceptación comercial, visual y operativa.

### Bloqueos de Producción

- respaldo no verificado;
- restauración no ensayada;
- validaciones externas incompletas;
- riesgos no aceptados formalmente;
- merge no autorizado;
- despliegue no autorizado.

## Dictamen

```text
ESTADO TÉCNICO: APROBADO EN ENTORNOS AUTOMATIZADOS
ESTADO EXTERNO: PENDIENTE
PRODUCCIÓN: NO APTO / NO AUTORIZADO
```