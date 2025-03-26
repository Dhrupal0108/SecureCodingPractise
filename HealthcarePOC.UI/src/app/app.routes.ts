import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { AdminDashboardComponent } from './components/admin-dashboard-component/admin-dashboard-component.component';
import { DoctorDashboardComponent } from './components/doctor-dashboard-component/doctor-dashboard-component.component';
import { PatientDashboardComponent } from './components/patient-dashboard-component/patient-dashboard-component.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'patient-dashboard', component: PatientDashboardComponent,canActivate:[AuthGuard], data: { role: 'Patient' }  },
  { path: 'doctor-dashboard', component: DoctorDashboardComponent,canActivate:[AuthGuard], data: { role: 'Doctor' } },
  { path: 'admin-dashboard', component: AdminDashboardComponent,canActivate:[AuthGuard], data: { role: 'Admin' } }
];
