import { Component } from '@angular/core';
import { AuthService, RegisterRequest } from '../../../services/auth';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent {
  model: RegisterRequest = {
    email: '',
    password: '',
    repeatPassword: '',
    firstName: '',
    lastName: ''
  };

  message = '';

  constructor(private authService: AuthService) {}

  register() {
    this.authService.registerUser(this.model)
      .then(msg => this.message = msg)
      .catch(err => this.message = 'Error: ' + err.error || 'Error desconocido');
  }
}
