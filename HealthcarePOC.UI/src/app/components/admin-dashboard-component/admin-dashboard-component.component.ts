import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { User } from '../../models/user';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard-component.component.html',
  styleUrls: ['./admin-dashboard-component.component.scss']
})
export class AdminDashboardComponent implements OnInit {
  users: User[] = [];

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.apiService.getAllUsers().subscribe({
      next: (res) => {
        this.users = res;
      },
      error: (err) => console.error('Error fetching users:', err)
    });
  }
}
