export interface User {
    id: number;
    userName: string;
    email: string;
    role: 'Admin' | 'Doctor' | 'Patient';
  }
  