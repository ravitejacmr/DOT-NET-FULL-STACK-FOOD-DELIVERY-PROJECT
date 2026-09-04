import { Routes } from '@angular/router';
import { adminGuard, authGuard, customerGuard, deliveryGuard, ownerGuard } from './core/guards/auth.guard';
import { HomeComponent } from './public/home';
import { RestaurantsComponent } from './public/restaurants';
import { RestaurantDetailComponent } from './public/restaurant-detail';
import { InfoComponent, NotFoundComponent } from './public/info';
import { LoginComponent } from './auth/login';
import { RegisterComponent } from './auth/register';
import { ForgotPasswordComponent } from './auth/forgot-password';
import { CustomerDashboardComponent } from './customer/dashboard';
import { CartComponent } from './customer/cart';
import { CheckoutComponent } from './customer/checkout';
import { OrdersComponent } from './customer/orders';
import { OrderDetailComponent } from './customer/order-detail';
import { AddressesComponent } from './customer/addresses';
import { ProfileComponent } from './customer/profile';
import { NotificationsComponent } from './customer/notifications';
import { FavoritesComponent } from './customer/favorites';
import { ReviewsComponent } from './customer/reviews';
import { PaymentComponent } from './customer/payment';
import { OwnerDashboardComponent } from './restaurant/dashboard';
import { MenuManagementComponent } from './restaurant/menu-management';
import { OwnerOrdersComponent } from './restaurant/orders';
import { RestaurantProfileComponent } from './restaurant/restaurant-profile';
import { DeliveryDashboardComponent } from './delivery/dashboard';
import { DeliveriesComponent } from './delivery/deliveries';
import { DeliveryDetailComponent } from './delivery/delivery-detail';
import { AdminDashboardComponent } from './admin/dashboard';
import { AdminManagementComponent } from './admin/management';

export const routes: Routes = [
  {path:'',component:HomeComponent,title:'FoodFlow — Food delivered with care'},
  {path:'restaurants',component:RestaurantsComponent,title:'Restaurants'}, {path:'restaurants/:id',component:RestaurantDetailComponent,title:'Restaurant menu'},
  {path:'foods/:id',redirectTo:'restaurants'},
  {path:'about',component:InfoComponent,data:{title:'About FoodFlow',text:'Better local food delivery, built around clarity and care.'}},
  {path:'contact',component:InfoComponent,data:{title:'Contact us',text:'Questions, feedback or partnership ideas? We would love to hear from you.'}},
  {path:'offers',component:InfoComponent,data:{title:'Fresh offers',text:'Use WELCOME50, FEAST20 and other active coupons during checkout.'}},
  {path:'login',component:LoginComponent,title:'Sign in'}, {path:'register',component:RegisterComponent,title:'Create account'}, {path:'forgot-password',component:ForgotPasswordComponent},
  {path:'cart',component:CartComponent,canActivate:[authGuard,customerGuard]}, {path:'checkout',component:CheckoutComponent,canActivate:[authGuard,customerGuard]},
  {path:'order-confirmation/:id',component:OrderDetailComponent,canActivate:[authGuard,customerGuard]},
  {path:'payment/:id',component:PaymentComponent,canActivate:[authGuard,customerGuard]},
  {path:'customer/dashboard',component:CustomerDashboardComponent,canActivate:[authGuard,customerGuard]}, {path:'customer/profile',component:ProfileComponent,canActivate:[authGuard,customerGuard]},
  {path:'customer/addresses',component:AddressesComponent,canActivate:[authGuard,customerGuard]}, {path:'customer/orders',component:OrdersComponent,canActivate:[authGuard,customerGuard]},
  {path:'customer/orders/:id',component:OrderDetailComponent,canActivate:[authGuard,customerGuard]}, {path:'customer/tracking/:id',component:OrderDetailComponent,canActivate:[authGuard,customerGuard]},
  {path:'customer/favorites',component:FavoritesComponent,canActivate:[authGuard,customerGuard]}, {path:'customer/reviews',component:ReviewsComponent,canActivate:[authGuard,customerGuard]},
  {path:'customer/notifications',component:NotificationsComponent,canActivate:[authGuard,customerGuard]},
  {path:'owner/dashboard',component:OwnerDashboardComponent,canActivate:[authGuard,ownerGuard]}, {path:'owner/restaurant',component:RestaurantProfileComponent,canActivate:[authGuard,ownerGuard]},
  {path:'owner/categories',component:RestaurantProfileComponent,canActivate:[authGuard,ownerGuard]}, {path:'owner/menu',component:MenuManagementComponent,canActivate:[authGuard,ownerGuard]},
  {path:'owner/orders',component:OwnerOrdersComponent,canActivate:[authGuard,ownerGuard]}, {path:'owner/orders/:id',component:OwnerOrdersComponent,canActivate:[authGuard,ownerGuard]},
  {path:'owner/sales',component:OwnerDashboardComponent,canActivate:[authGuard,ownerGuard]}, {path:'owner/reviews',component:ReviewsComponent,canActivate:[authGuard,ownerGuard]},
  {path:'delivery/dashboard',component:DeliveryDashboardComponent,canActivate:[authGuard,deliveryGuard]}, {path:'delivery/profile',component:ProfileComponent,canActivate:[authGuard,deliveryGuard]},
  {path:'delivery/available',component:DeliveriesComponent,canActivate:[authGuard,deliveryGuard]}, {path:'delivery/active',component:DeliveryDashboardComponent,canActivate:[authGuard,deliveryGuard]},
  {path:'delivery/orders/:id',component:DeliveryDetailComponent,canActivate:[authGuard,deliveryGuard]}, {path:'delivery/history',component:DeliveriesComponent,data:{history:true},canActivate:[authGuard,deliveryGuard]},
  {path:'delivery/earnings',component:DeliveryDashboardComponent,canActivate:[authGuard,deliveryGuard]},
  {path:'admin/dashboard',component:AdminDashboardComponent,canActivate:[authGuard,adminGuard]},
  {path:'admin/users',component:AdminManagementComponent,data:{resource:'users'},canActivate:[authGuard,adminGuard]}, {path:'admin/restaurants',component:AdminManagementComponent,data:{resource:'restaurants'},canActivate:[authGuard,adminGuard]},
  {path:'admin/orders',component:AdminManagementComponent,data:{resource:'orders'},canActivate:[authGuard,adminGuard]}, {path:'admin/payments',component:AdminManagementComponent,data:{resource:'payments'},canActivate:[authGuard,adminGuard]},
  {path:'admin/coupons',component:AdminManagementComponent,data:{resource:'coupons'},canActivate:[authGuard,adminGuard]}, {path:'admin/reviews',component:AdminManagementComponent,data:{resource:'reviews'},canActivate:[authGuard,adminGuard]},
  {path:'**',component:NotFoundComponent,title:'Not found'}
];
