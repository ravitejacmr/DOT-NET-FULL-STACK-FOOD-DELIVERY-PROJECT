import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from '../models/models';
import { AuthService } from '../services/auth.service';
export const authGuard:CanActivateFn=()=>{const auth=inject(AuthService);return auth.loggedIn()||inject(Router).createUrlTree(['/login']);};
export function roleGuard(...roles:UserRole[]):CanActivateFn { return ()=>{const auth=inject(AuthService); return (!!auth.user()&&roles.includes(auth.user()!.role))||inject(Router).createUrlTree(['/']);}; }
export const customerGuard=roleGuard('customer');
export const ownerGuard=roleGuard('restaurant_owner');
export const deliveryGuard=roleGuard('delivery_partner');
export const adminGuard=roleGuard('admin');
