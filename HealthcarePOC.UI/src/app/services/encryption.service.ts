import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as forge from 'node-forge';
import { config } from '../config/config';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EncryptionService {
  private publicKey: string = '';  // Stores the RSA Public Key (PEM format)
  private baseUrl = config.apiUrl;

  constructor(private http: HttpClient) {}

  // ✅ Fetch & Store Public Key from Backend
  async fetchPublicKey(): Promise<void> {
      try {
        const response = await firstValueFrom(
          this.http.get<{ publicKey: string }>(`${this.baseUrl}/Auth/get-public-key`)
        );

        if (response && response.publicKey) {
          this.publicKey = response.publicKey; // Store the received public key
        } else {
          throw new Error('Public key response is undefined or invalid.');
        }
      } catch (error) {
        console.error('Error fetching public key:', error);
        throw new Error('Failed to fetch public key.');
      }
  }

  // ✅ Encrypt Data Using Public Key (Before Sending to Backend)
  async encryptData(data: any): Promise<{ encryptedData: string; encryptedKey: string; iv: string; }> {
    if (!this.publicKey) {
     await this.fetchPublicKey();
    }
  
    try {
      const rsa = forge.pki.publicKeyFromPem(this.publicKey);
      
      // ✅ Generate AES Key & IV
      const aesKey = forge.random.getBytesSync(32); // 256-bit AES key
      const iv = forge.random.getBytesSync(16); // 16-byte IV (AES CBC mode)
  
      // ✅ Encrypt Data with AES
      const cipher = forge.cipher.createCipher('AES-CBC', aesKey);
      cipher.start({ iv: iv });
      cipher.update(forge.util.createBuffer(JSON.stringify(data)));
      cipher.finish();
      const encryptedData = forge.util.encode64(cipher.output.getBytes());
  
      // ✅ Encrypt AES Key with RSA
      const encryptedKey = forge.util.encode64(rsa.encrypt(aesKey, 'RSA-OAEP', {
        md: forge.md.sha256.create()
      }));
  
      return { encryptedData, encryptedKey, iv: forge.util.encode64(iv) }; // ✅ Send IV along with data
    } catch (error) {
      console.error('Encryption failed:', error);
      throw new Error('Failed to encrypt data.');
    }
  }

  // ✅ Decrypt Data Using Private Key (If Needed in Angular)
  decryptData(encryptedText: string, aesKeyBase64: string, ivBase64: string): string {
    try {
      // ✅ Decode base64 strings into Forge-compatible binary strings
      const aesKey = forge.util.decode64(aesKeyBase64); // binary string
      const iv = forge.util.decode64(ivBase64);
      const encryptedBytes = forge.util.decode64(encryptedText);
      const decipher = forge.cipher.createDecipher('AES-CBC', aesKey);
      decipher.start({ iv: iv });
      decipher.update(forge.util.createBuffer(encryptedBytes));
      const success = decipher.finish();
  
  
      if (!success) throw new Error("AES decryption failed. Ensure correct key and IV.");
  
      return forge.util.decodeUtf8(decipher.output.getBytes());
    } catch (error) {
      console.error('Decryption failed:', error);
      throw new Error('Failed to decrypt data.');
    }
  }
  
}
