import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as CryptoJS from 'crypto-js'; // Import CryptoJS for encryption/decryption
import { config } from '../config/config';
import { Patient } from '../models/patient';
import { map, Observable } from 'rxjs';
import { EncryptionService } from './encryption.service';

@Injectable({
  providedIn: 'root'
})
export class ApiService {

  private baseUrl = config.apiUrl;
  private secretKey = 'your-secret-key';  // Use a secure key here

  constructor(private http: HttpClient,private encryptionService : EncryptionService) { }

  // Encrypt data before sending it to the backend
  private encryptData(data: any): string {
    const ciphertext = CryptoJS.AES.encrypt(JSON.stringify(data), this.secretKey).toString();
    return ciphertext;
  }

  // Decrypt data after receiving it from the backend
  private decryptData(encryptedData: string): any {
    const bytes = CryptoJS.AES.decrypt(encryptedData, this.secretKey);
    const decryptedData = bytes.toString(CryptoJS.enc.Utf8);
    return JSON.parse(decryptedData);
  }

  // Register Patient - Encrypt payload before sending and Decrypt response
  registerPatient(patient: Patient) {
    const encryptedPayload = this.encryptData(patient);
    return this.http.post<{ data: string }>(`${this.baseUrl}/Patients`, { data: encryptedPayload })
      .pipe(map(response => this.decryptData(response.data)));
  }

  // // Get Patient - Encrypt payload (if needed) and Decrypt response
  async getPatient(id: number) {
    // Encrypt the patientId using AES (with RSA)
    const encryptedPatientId = this.encryptionService.encryptData({ id });
  
    // Prepare the encrypted data to send as query parameters
    const encryptedPayload = {
      EncryptedData: (await encryptedPatientId).encryptedData,
      EncryptedKey: (await encryptedPatientId).encryptedKey,
      IV: (await encryptedPatientId).iv,  // Send IV to backend
    };
  
    // Send encrypted data to backend
    return this.http.get<{ EncryptedData: string; AESKey: string; IV: string }>(`${this.baseUrl}/Patients/${id}`, {
      params: encryptedPayload,
    }).pipe(
      map(response => {
        // Decrypt the response data using AES key and IV
        const decryptedPatientDetails = this.encryptionService.decryptData(response.EncryptedData, response.AESKey, response.IV);
        return JSON.parse(decryptedPatientDetails);  // Parse and return the patient details
      })
    );
  }
  
  // Update Patient - Encrypt payload before sending and Decrypt response
  updatePatient(patient: Patient) {
    const encryptedPayload = this.encryptData(patient);
    return this.http.put<{ data: string }>(`${this.baseUrl}/Patients/${patient.Id}`, { data: encryptedPayload })
      .pipe(map(response => this.decryptData(response.data)));
  }

  // Delete Patient - (No need to encrypt payload, but decrypt response if needed)
  deletePatient(id: number) {
    return this.http.delete<{ data: string }>(`${this.baseUrl}/Patients/${id}`)
      .pipe(map(response => this.decryptData(response.data)));
  }

  // Login - Encrypt payload before sending and Decrypt response
  login(payload: { EncryptedData: string; EncryptedKey: string }) {
    return this.http.post<{ encryptedData: string; aesKey: string; iv : string }>(
      `${this.baseUrl}/Auth/login`,
      payload,
      { headers: { 'Content-Type': 'application/json' } } // ✅ Ensure correct Content-Type
    );
  }
  
  // Get All Patients - Encrypt payload (if needed) and Decrypt response
  getAllPatients(): Observable<{ encryptedData: string, aesKey: string, iv: string }> {
    return this.http.get<{ encryptedData: string, aesKey: string, iv: string }>(`${this.baseUrl}/patients`);
  }
  
  // Get All Users - Encrypt payload (if needed) and Decrypt response
  getAllUsers() {
    return this.http.get<{ data: string }>(`${this.baseUrl}/Users`)
      .pipe(map(response => this.decryptData(response.data)));
  }
}
