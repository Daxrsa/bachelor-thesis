import { Routes } from '@angular/router';
import { authGuard } from '../../auth.guard';
import { Access } from './access';
import { Login } from './login';
import { Error } from './error';
import { Register } from './register';
import { RegisterChoice } from './register-choice';

export default [
    { path: 'access', component: Access },
    { path: 'error', component: Error },
    { path: 'login', component: Login },
    { path: 'register', component: Register },
    { path: 'welcome', component: RegisterChoice, canActivate: [authGuard] }
] as Routes;
