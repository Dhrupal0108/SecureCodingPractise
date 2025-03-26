import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DoctorDashboardComponentComponent } from './doctor-dashboard-component.component';

describe('DoctorDashboardComponentComponent', () => {
  let component: DoctorDashboardComponentComponent;
  let fixture: ComponentFixture<DoctorDashboardComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DoctorDashboardComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DoctorDashboardComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
