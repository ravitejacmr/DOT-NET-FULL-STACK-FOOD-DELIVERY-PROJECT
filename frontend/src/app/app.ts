import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar';
import { ToastContainerComponent } from './shared/components/toast-container';
import { CustomerCareChatComponent } from './shared/components/customer-care-chat';
@Component({selector:'app-root',imports:[RouterOutlet,NavbarComponent,ToastContainerComponent,CustomerCareChatComponent],template:`
  <app-navbar/><main><router-outlet/></main><footer><div class="container footer-grid"><div><b>FoodFlow</b><p>Great food, delivered with care.</p></div><div><b>Explore</b><p>Restaurants · Offers · Help</p></div><div><b>Contact</b><p>hello@foodflow.local</p></div></div></footer><app-customer-care-chat/><app-toast-container/>
`})
export class App {}
