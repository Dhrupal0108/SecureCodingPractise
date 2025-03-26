import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Patient } from '../../models/patient';
import { ApiService } from '../../services/api.service';
import { MaterialModule } from '../../material/material.module';
import { EncryptionService } from '../../services/encryption.service';

@Component({
  selector: 'app-doctor-dashboard',
  standalone: true,
  imports: [CommonModule,MaterialModule],
  templateUrl: './doctor-dashboard-component.component.html',
  styleUrls: ['./doctor-dashboard-component.component.scss'],
})
export class DoctorDashboardComponent implements OnInit {
  patients: Patient[] = [];
  selectedPatient: Patient | null = null;
  constructor(private apiService: ApiService,private encryptionService:EncryptionService) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.apiService.getAllPatients().subscribe({
      next: (res) => {
        const encryptedData = res.encryptedData;
        const aesKey = res.aesKey;
        const iv = res.iv;  
        const decryptedJson = this.encryptionService.decryptData(encryptedData,aesKey, iv);
        const patients = JSON.parse(decryptedJson);
        this.patients = patients;
      },
      error: (err) => {
        console.error("❌ Failed to fetch patients", err);
      }
    });
    
  }

  async viewPatientDetails(patientId: number | undefined): Promise<void> {
    if (patientId) {
      (await this.apiService.getPatient(patientId)).subscribe({
        next: (res) => {
          this.selectedPatient = res;
        },
        error: (err) => console.error('Error fetching patient details:', err),
      });
    }
  }

}
