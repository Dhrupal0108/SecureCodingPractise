import { Component, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { MaterialModule } from '../../material/material.module';
import { ApiService } from '../../services/api.service';
import { EncryptionService } from '../../services/encryption.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MaterialModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements AfterViewInit {
  loginForm: FormGroup;
  loginError: string = '';

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService,
    private router: Router,
    private encryptionService: EncryptionService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  ngAfterViewInit(): void {
    // Clear autofilled values
    setTimeout(() => {
      (document.querySelector('input[formControlName="email"]') as HTMLInputElement).value = '';
      (document.querySelector('input[formControlName="password"]') as HTMLInputElement).value = '';
    }, 100);
  }

  async login() {
    if (this.loginForm.invalid) return;
    
    await this.encryptionService.fetchPublicKey();
    // ✅ Encrypt login data properly
    const encryptedPayload = this.encryptionService.encryptData({
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    });
  
    // ✅ Ensure payload is correctly structured
    const requestBody = {
      EncryptedData: (await encryptedPayload).encryptedData,
      EncryptedKey: (await encryptedPayload).encryptedKey,
      IV: (await encryptedPayload).iv  // ✅ Send IV to backend
    };
  
    this.apiService.login(requestBody).subscribe({
      next: (res) => {
        const encryptedData = res.encryptedData;
        const aesKey = res.aesKey;
        const iv = res.iv;  // ✅ Backend must return IV with response
        // ✅ Decrypt Token using AES Key and IV
        const decryptedToken = this.encryptionService.decryptData(encryptedData, aesKey, iv);
  
        localStorage.setItem('jwtToken', decryptedToken);
        const decodedToken: any = jwtDecode(decryptedToken);
  
        const userRole = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  
        if (userRole === 'Admin') {
          this.router.navigate(['/admin-dashboard']);
        } else if (userRole === 'Doctor') {
          this.router.navigate(['/doctor-dashboard']);
        } else {
          this.router.navigate(['/patient-dashboard']);
        }
      },
      error: (error) => {
        console.error("Login API Error:", error);
        this.loginError = 'Invalid email or password.';
      }
    });
  }
}
