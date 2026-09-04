import { Component, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { environment } from '../../environments/environment';
@Component({imports:[ReactiveFormsModule,RouterLink],template:`<section class="auth-simple"><form class="form-card" [formGroup]="form" (ngSubmit)="submit()"><span class="eyebrow">ACCOUNT RECOVERY</span><h1>Reset your password</h1><p>Enter your account email. Development reset instructions are sent to the Django console.</p><label>Email<input type="email" formControlName="email"></label><button class="btn full" [disabled]="form.invalid">Send reset instructions</button>@if(sent()){<div class="success-box">If an account exists, instructions have been sent.</div>}<a routerLink="/login">← Back to sign in</a></form></section>`})
export class ForgotPasswordComponent{sent=signal(false);form=this.fb.nonNullable.group({email:['',[Validators.required,Validators.email]]});constructor(private fb:FormBuilder,private http:HttpClient){}submit(){this.http.post(`${environment.apiUrl}/auth/password/forgot/`,this.form.getRawValue()).subscribe(()=>this.sent.set(true));}}
