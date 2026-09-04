import { Component, Input } from '@angular/core';
@Component({selector:'app-loading',template:`<div class="loading"><span></span>{{label}}</div>`})
export class LoadingComponent{@Input() label='Loading…';}
