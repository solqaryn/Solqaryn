import { ANCHOS_MODAL, MODAL_DEFAULTS, modalInstitucional } from './modal-institucional';

describe('Estándar institucional de modales', () => {
  it('define cuatro tamaños y protege contra cierre accidental y desborde', () => {
    expect(Object.keys(ANCHOS_MODAL)).toEqual(['s', 'm', 'l', 'xl']);
    for (const size of Object.keys(ANCHOS_MODAL) as (keyof typeof ANCHOS_MODAL)[]) {
      const config = modalInstitucional(size, { disableClose: false, width: '1px', maxWidth: '200vw', restoreFocus: false });
      expect(config.width).toBe(ANCHOS_MODAL[size]);
      expect(config.disableClose).toBe(true);
      expect(config.restoreFocus).toBe(true);
      expect(config.maxWidth).toBe('calc(100vw - 24px)');
      expect(config.maxHeight).toBe('calc(100dvh - 24px)');
      expect(config.panelClass).toContain('solqaryn-modal--' + size);
    }
    expect(MODAL_DEFAULTS.disableClose).toBe(true);
    expect(MODAL_DEFAULTS.autoFocus).toBe('dialog');
    expect(modalInstitucional('s', { autoFocus: 'first-tabbable' }).autoFocus).toBe('first-tabbable');
  });
  it('conserva datos y clases del consumidor', () => {
    const data = { id: 42 };
    expect(modalInstitucional('m', { data, panelClass: 'custom' })).toEqual(expect.objectContaining({ data, panelClass: ['custom', 'solqaryn-modal', 'solqaryn-modal--m'] }));
  });
});
