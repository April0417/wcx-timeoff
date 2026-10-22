import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideRouter([])]
    }).compileComponents();
  });

  it('renders a sign-in form with email and password fields', () => {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('form')).not.toBeNull();
    expect(element.querySelector('label[for="email"]')?.textContent).toContain('Email');
    expect(element.querySelector('label[for="password"]')?.textContent).toContain('Password');
    expect(element.textContent).toContain('Sign in');
  });
});
