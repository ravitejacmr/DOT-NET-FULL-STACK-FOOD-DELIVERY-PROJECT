import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, User } from '../models/models';

@Injectable({providedIn: 'root'})
export class AuthService {
  private readonly base = `${environment.apiUrl}/auth`;
  readonly user = signal<User | null>(this.loadUser());
  readonly loggedIn = computed(() => !!this.user());
  constructor(private http: HttpClient, private router: Router) {}
  private loadUser(): User|null { try { return JSON.parse(localStorage.getItem('user') || 'null') as User|null; } catch { return null; } }
  login(credentials: {email:string; password:string}): Observable<AuthResponse> { return this.http.post<AuthResponse>(`${this.base}/login/`, credentials).pipe(tap(v => this.store(v))); }
  register(data: Record<string,string>): Observable<User> { return this.http.post<User>(`${this.base}/register/`, data); }
  profile(): Observable<User> { return this.http.get<User>(`${this.base}/profile/`).pipe(tap(user => { this.user.set(user); localStorage.setItem('user', JSON.stringify(user)); })); }
  updateProfile(data: Partial<User>): Observable<User> { return this.http.patch<User>(`${this.base}/profile/`, data).pipe(tap(user => { this.user.set(user); localStorage.setItem('user', JSON.stringify(user)); })); }
  get accessToken(): string|null { return localStorage.getItem('access_token'); }
  get refreshToken(): string|null { return localStorage.getItem('refresh_token'); }
  refresh(): Observable<{access:string; refresh?:string}> { return this.http.post<{access:string; refresh?:string}>(`${this.base}/token/refresh/`, {refresh: this.refreshToken}).pipe(tap(v => { localStorage.setItem('access_token', v.access); if (v.refresh) localStorage.setItem('refresh_token', v.refresh); })); }
  logout(): void { const refresh = this.refreshToken; if (refresh) this.http.post(`${this.base}/logout/`, {refresh}).subscribe({error:()=>{}}); this.clear(); }
  clear(): void { localStorage.removeItem('access_token'); localStorage.removeItem('refresh_token'); localStorage.removeItem('user'); this.user.set(null); void this.router.navigateByUrl('/login'); }
  dashboardPath(): string { const role=this.user()?.role; return role === 'restaurant_owner' ? '/owner/dashboard' : role === 'delivery_partner' ? '/delivery/dashboard' : role === 'admin' ? '/admin/dashboard' : '/customer/dashboard'; }
  private store(v: AuthResponse): void { localStorage.setItem('access_token', v.access); localStorage.setItem('refresh_token', v.refresh); localStorage.setItem('user', JSON.stringify(v.user)); this.user.set(v.user); }
}
