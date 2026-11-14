import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PurchaseReportDisclosureComponent } from './purchase-report-disclosure.component';

describe('PurchaseReportDisclosureComponent', () => {
  let component: PurchaseReportDisclosureComponent;
  let fixture: ComponentFixture<PurchaseReportDisclosureComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseReportDisclosureComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseReportDisclosureComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
