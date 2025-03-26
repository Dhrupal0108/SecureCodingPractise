import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PatientDashboardComponentComponent } from './patient-dashboard-component.component';

describe('PatientDashboardComponentComponent', () => {
  let component: PatientDashboardComponentComponent;
  let fixture: ComponentFixture<PatientDashboardComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientDashboardComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PatientDashboardComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
