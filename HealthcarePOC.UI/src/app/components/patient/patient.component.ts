import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Patient } from '../../models/patient';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-patient',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './patient.component.html',
  styleUrls: ['./patient.component.scss']
})
export class PatientComponent implements OnInit {

  patientResponse: Patient | null = null;

  constructor(private apiService: ApiService) { }

  ngOnInit(): void {
    const patient: Patient = {
      FirstName: 'Secure',
      LastName: 'Patient',
      dateOfBirth: '1995-01-01T00:00:00',
      Email: 'securepatient@example.com',
      password: 'Test@12345',
      medicalRecord: 'Sample medical history for test user.'
    };

    this.apiService.registerPatient(patient).subscribe({
      next: (res) => {
        this.patientResponse = res;
      },
      error: (err) => console.error('Registration Error:', err)
    });
  }
}
