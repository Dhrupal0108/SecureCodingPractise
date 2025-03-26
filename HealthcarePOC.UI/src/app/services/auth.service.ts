import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  constructor() { }

  // Method to decode the JWT token and get user information
  getDecodedToken(): any {
    const token = localStorage.getItem('jwtToken');
    if (token) {
      return jwtDecode(token);
    }
    return null;
  }

  // Check if the user is authenticated (token exists)
  isAuthenticated(): boolean {
    const token = localStorage.getItem('jwtToken');
    return token != null;
  }
}
