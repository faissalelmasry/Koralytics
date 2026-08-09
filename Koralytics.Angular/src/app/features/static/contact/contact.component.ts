import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './contact.component.html',
  styleUrls: ['./contact.component.css']
})
export class ContactComponent {
  formData = {
    fullName: '',
    email: '',
    academyName: '',
    userRole: 'Manager',
    subject: '',
    message: ''
  };

  submitted = false;

  onSubmit() {
    if (this.formData.fullName && this.formData.email && this.formData.message) {
      this.submitted = true;
    }
  }

  resetForm() {
    this.submitted = false;
    this.formData = {
      fullName: '',
      email: '',
      academyName: '',
      userRole: 'Manager',
      subject: '',
      message: ''
    };
  }
}
