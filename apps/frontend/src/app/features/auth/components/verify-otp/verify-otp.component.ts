import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonComponent } from "../../../../shared/components/button/button.component";
import { ROUTES } from '../../../../shared/config/constants';

@Component({
  selector: 'app-verify-otp',
  imports: [ReactiveFormsModule, FormsModule, ButtonComponent],
  templateUrl: './verify-otp.component.html',
  styleUrls: ['./verify-otp.component.css'],
})
export class VerifyOtpComponent {
  
}
