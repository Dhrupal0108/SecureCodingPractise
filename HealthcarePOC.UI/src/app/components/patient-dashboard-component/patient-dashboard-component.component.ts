import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Patient } from '../../models/patient';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './patient-dashboard-component.component.html',
  styleUrls: ['./patient-dashboard-component.component.scss']
})
export class PatientDashboardComponent implements OnInit {
  patientData: Patient | null = null;

  constructor(private apiService: ApiService) {}

  async ngOnInit(): Promise<void> {
    const patientId = localStorage.getItem('userId'); // Assuming userId is stored in localStorage

    if (patientId) {
      (await this.apiService.getPatient(+patientId)).subscribe({
        next: (res) => {
          this.patientData = res;
        },
        error: (err) => console.error('Error fetching patient data:', err)
      });
    }
  }
}
