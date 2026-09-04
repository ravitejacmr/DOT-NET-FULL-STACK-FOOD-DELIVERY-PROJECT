import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth=inject(AuthService); const toast=inject(ToastService); const router=inject(Router);
  const token=auth.accessToken;
  const outgoing=token && !req.url.includes('/auth/token/refresh/') ? req.clone({setHeaders:{Authorization:`Bearer ${token}`}}) : req;
  return next(outgoing).pipe(catchError((err:HttpErrorResponse)=>{
    if(err.status===401 && auth.refreshToken && !req.url.includes('/auth/')) {
      return auth.refresh().pipe(switchMap(()=>next(req.clone({setHeaders:{Authorization:`Bearer ${auth.accessToken}`}}))),catchError(refreshErr=>{auth.clear(); return throwError(()=>refreshErr);}));
    }
    const details=err.error?.error?.details; const message=typeof details==='string'?details:details?.detail||Object.values(details||{})[0]||'Something went wrong. Please try again.';
    toast.show(String(message),'error');
    if(err.status===403) void router.navigateByUrl('/');
    return throwError(()=>err);
  }));
};
